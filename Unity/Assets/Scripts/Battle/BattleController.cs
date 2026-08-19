using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    public class DamageNumber
    {
        public Vector3 WorldPos;
        public string Text;
        public Color Color;
        public float Age;
    }

    public enum BattleOutcome { InProgress, PlayerVictory, EnemyVictory }

    /// <summary>Which top-level action a manual-mode player turn resolved to.</summary>
    public enum ChosenAction { None, Skill, Reposition, Sub }

    /// <summary>Drives what BattleHud shows during a manual-mode player turn.</summary>
    public enum ActionPhase { Idle, ChooseAction, ChooseBench, ChooseTarget }

    /// <summary>
    /// Turn state machine with two modes:
    ///  - Auto: both sides act automatically each turn (BD2/gacha-style "auto battle").
    ///  - Manual: enemy turns still resolve automatically, but a player-faction turn
    ///    pauses and waits for the player to choose an action (Attack/Heal, Reposition,
    ///    or Sub), then a target/bench pick if that action needs one.
    ///
    /// Also owns pause (Time.timeScale-driven -- every wait in this class and in
    /// BattleVisuals' stage tweens is a WaitForSeconds/Time.deltaTime, so scaling or
    /// zeroing Time.timeScale pauses and speed-controls the whole battle for free) and
    /// multi-step undo/redo via BattleHistory.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public BattleWorld World { get; private set; }
        public BattleSettings Settings { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public readonly BattleLog Log = new();
        public string LastAction => Log.Entries.Count > 0 ? Log.Entries[^1].Text : "";
        public readonly List<DamageNumber> DamageNumbers = new();

        public bool ManualMode { get; private set; }
        public bool Paused { get; private set; }
        public BattleUnit PendingActor { get; private set; }
        public ActionPhase Phase { get; private set; } = ActionPhase.Idle;
        public IReadOnlyList<BattleUnit> PendingTargets => _pendingTargets;
        public IReadOnlyList<BattleUnit> BenchOptions => World.Bench;
        public bool CanReposition => _repositionOptions.Count > 0;
        public bool CanSub => World.Bench.Count > 0;

        public bool CanUndo => _history.CanUndo;
        public bool CanRedo => _history.CanRedo;

        public event Action OnRestartRequested;
        public event Action OnAdvanceRequested;

        BattleVisuals _visuals;
        Camera _cam;
        TurnOrder _turnOrder;
        readonly BattleHistory _history = new();
        Coroutine _runCoroutine;
        List<BattleUnit> _pendingTargets = new();
        List<BattleUnit> _repositionOptions = new();
        BattleUnit _submittedTarget;
        ChosenAction _chosenAction;
        SkillDefinition _chosenSkill;
        BattleUnit _chosenSubIncoming;
        const float PreActionDelaySeconds = 0.35f;
        const float ImpactHoldSeconds = 0.5f;

        public void Init(BattleWorld world, BattleVisuals visuals, Camera cam, BattleSettings settings)
        {
            World = world;
            _visuals = visuals;
            _cam = cam;
            Settings = settings;
            Outcome = BattleOutcome.InProgress;
            Paused = false;
            ManualMode = !settings.AutoModeDefault;
            Time.timeScale = settings.SpeedMultiplier;

            _turnOrder = new TurnOrder(world.AllUnits);
            if (!world.LoadedOk) return;

            _history.Capture(World.AllUnits, World.Bench, Log);
            _runCoroutine = StartCoroutine(RunBattle());
        }

        void Update()
        {
            for (int i = DamageNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumbers[i].Age += Time.deltaTime;
                if (DamageNumbers[i].Age > 1.2f) DamageNumbers.RemoveAt(i);
            }

            if (Outcome != BattleOutcome.InProgress && Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.T)) ToggleMode();
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.Z)) Undo();
            if (ctrl && Input.GetKeyDown(KeyCode.Y)) Redo();

            if (Phase == ActionPhase.ChooseTarget && !Paused && Input.GetMouseButtonDown(0)) HandleClick(Input.mousePosition);
        }

        void OnDestroy()
        {
            // Battle scene owns global Time.timeScale while it's active -- don't leak a
            // paused/slowed state into whatever loads next.
            Time.timeScale = 1f;
        }

        public void ToggleMode() => ManualMode = !ManualMode;

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = Paused ? 0f : Settings.SpeedMultiplier;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            Settings.SpeedMultiplier = multiplier;
            Settings.Save();
            if (!Paused) Time.timeScale = multiplier;
        }

        public void Undo()
        {
            if (!_history.CanUndo) return;
            _history.Undo(World.AllUnits, World.Bench, Log);
            ResumeFromHistory();
        }

        public void Redo()
        {
            if (!_history.CanRedo) return;
            _history.Redo(World.AllUnits, World.Bench, Log);
            ResumeFromHistory();
        }

        void ResumeFromHistory()
        {
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            PendingActor = null;
            Phase = ActionPhase.Idle;
            _pendingTargets = new List<BattleUnit>();
            _submittedTarget = null;
            Outcome = BattleOutcome.InProgress;

            _turnOrder = new TurnOrder(World.AllUnits);
            for (int i = 0; i < _history.Cursor; i++) _turnOrder.Next();

            _visuals.SyncAll(World);
            _visuals.SnapAllToDock(World);

            _runCoroutine = StartCoroutine(RunBattle());
        }

        void LogLine(string text) => Log.Add(_turnOrder.RoundNumber, text);

        void HandleClick(Vector3 screenPos)
        {
            if (_cam == null || !_visuals.TryGetUnitAtScreenPoint(screenPos, _cam, out var clicked)) return;
            if (!_pendingTargets.Contains(clicked)) return;
            _submittedTarget = clicked;
        }

        // -- manual-mode action menu (called by BattleHud) --------------------------

        /// <summary>Player picked "Attack"/"Heal" (standardSkill) or the secondary
        /// attack, if the acting unit has one. Wakes RunManualPlayerTurn's action-choice
        /// wait; target selection happens next via HandleClick.</summary>
        public void ChooseSkill(SkillDefinition skill)
        {
            if (skill == null) return;
            _chosenSkill = skill;
            _chosenAction = ChosenAction.Skill;
        }

        /// <summary>Player picked Reposition -- swap column with an adjacent ally.</summary>
        public void ChooseReposition()
        {
            if (!CanReposition) return;
            _chosenAction = ChosenAction.Reposition;
        }

        /// <summary>HUD-only navigation into the bench picker -- doesn't wake the
        /// coroutine yet (that happens once a bench unit is actually chosen).</summary>
        public void OpenBenchMenu()
        {
            if (!CanSub) return;
            Phase = ActionPhase.ChooseBench;
        }

        public void CancelBenchMenu() => Phase = ActionPhase.ChooseAction;

        /// <summary>Player picked which bench unit subs in for the acting unit.</summary>
        public void ChooseSub(BattleUnit benchUnit)
        {
            if (benchUnit == null || !World.Bench.Contains(benchUnit)) return;
            _chosenSubIncoming = benchUnit;
            _chosenAction = ChosenAction.Sub;
        }

        IEnumerator RunBattle()
        {
            while (!World.IsOver)
            {
                var unit = _turnOrder.Next();
                if (unit == null) break;

                yield return new WaitForSeconds(PreActionDelaySeconds);

                if (ManualMode && unit.Faction == Faction.Player)
                    yield return RunManualPlayerTurn(unit);
                else
                    yield return RunAutoTurn(unit);

                // One capture per consumed TurnOrder.Next() -- see BattleHistory's
                // class doc for why this 1:1 correspondence matters for Undo/Redo.
                _history.Capture(World.AllUnits, World.Bench, Log);
            }

            Outcome = World.PlayerDefeated ? BattleOutcome.EnemyVictory : BattleOutcome.PlayerVictory;
            if (Outcome == BattleOutcome.PlayerVictory)
                LogLine(World.HasNextMap ? "Victory! Proceed to the next battle." : "Victory!");
            else
                LogLine("Defeat...");
        }

        IEnumerator RunAutoTurn(BattleUnit unit)
        {
            var skill = ChooseAutoSkill(unit, out var targets);
            if (skill == null)
            {
                LogLine($"{unit.Definition.displayName} has no usable skill.");
                yield break;
            }
            if (targets.Count == 0)
            {
                LogLine($"{unit.Definition.displayName} has no valid target.");
                yield break;
            }

            var target = targets[UnityEngine.Random.Range(0, targets.Count)];
            yield return _visuals.MoveToStage(unit, target);
            ResolveAction(unit, skill, target);
            yield return new WaitForSeconds(ImpactHoldSeconds);
            yield return _visuals.ReturnToDock(unit, target);
            if (!target.IsAlive) yield return _visuals.ReflowFormation(World, target.Faction);
        }

        /// <summary>Auto-mode/enemy skill choice. Healer-archetype units (secondarySkill
        /// set) heal when an ally is missing HP, otherwise fall back to their low-power
        /// attack if it has a target -- keeps a healer from wasting turns topping off a
        /// full-HP ally once nobody nearby needs it.</summary>
        SkillDefinition ChooseAutoSkill(BattleUnit unit, out List<BattleUnit> targets)
        {
            var primary = unit.Definition.standardSkill;
            var secondary = unit.Definition.secondarySkill;

            if (primary == null || primary.pattern == null)
            {
                targets = new List<BattleUnit>();
                return null;
            }

            if (secondary != null && primary.targetsAllies)
            {
                bool allyNeedsHeal = World.AllUnits.Any(u =>
                    u.Faction == unit.Faction && u.IsAlive && u.CurrentHp < u.Stats.hp);
                if (!allyNeedsHeal)
                {
                    var atkTargets = TargetResolver.GetValidTargets(unit, secondary, World.AllUnits);
                    if (atkTargets.Count > 0)
                    {
                        targets = atkTargets;
                        return secondary;
                    }
                }
            }

            targets = TargetResolver.GetValidTargets(unit, primary, World.AllUnits);
            return primary;
        }

        IEnumerator RunManualPlayerTurn(BattleUnit unit)
        {
            PendingActor = unit;
            _repositionOptions = World.AllUnits
                .Where(u => u.Faction == unit.Faction && u.IsAlive && Mathf.Abs(u.Column - unit.Column) == 1)
                .ToList();
            _chosenAction = ChosenAction.None;
            _chosenSkill = null;
            _chosenSubIncoming = null;
            _submittedTarget = null;
            Phase = ActionPhase.ChooseAction;
            LogLine($"{unit.Definition.displayName}'s turn -- choose an action.");

            yield return new WaitUntil(() => _chosenAction != ChosenAction.None);

            switch (_chosenAction)
            {
                case ChosenAction.Skill:
                {
                    Phase = ActionPhase.ChooseTarget;
                    _pendingTargets = TargetResolver.GetValidTargets(unit, _chosenSkill, World.AllUnits);
                    if (_pendingTargets.Count == 0)
                    {
                        LogLine($"{unit.Definition.displayName} has no valid target.");
                        break;
                    }
                    yield return new WaitUntil(() => _submittedTarget != null);
                    var target = _submittedTarget;
                    yield return _visuals.MoveToStage(unit, target);
                    ResolveAction(unit, _chosenSkill, target);
                    yield return new WaitForSeconds(ImpactHoldSeconds);
                    yield return _visuals.ReturnToDock(unit, target);
                    if (!target.IsAlive) yield return _visuals.ReflowFormation(World, target.Faction);
                    break;
                }
                case ChosenAction.Reposition:
                {
                    Phase = ActionPhase.ChooseTarget;
                    _pendingTargets = _repositionOptions;
                    yield return new WaitUntil(() => _submittedTarget != null);
                    var neighbor = _submittedTarget;
                    LogLine($"{unit.Definition.displayName} repositions with {neighbor.Definition.displayName}.");
                    (unit.Column, neighbor.Column) = (neighbor.Column, unit.Column);
                    yield return _visuals.SwapPositions(unit, neighbor);
                    break;
                }
                case ChosenAction.Sub:
                {
                    var incoming = _chosenSubIncoming;
                    SubUnit(unit, incoming);
                    yield return _visuals.SwapUnitView(unit, incoming);
                    break;
                }
            }

            PendingActor = null;
            Phase = ActionPhase.Idle;
            _pendingTargets = new List<BattleUnit>();
        }

        void SubUnit(BattleUnit outgoing, BattleUnit incoming)
        {
            incoming.Column = outgoing.Column;
            outgoing.Column = BattleWorld.BenchColumn;
            World.AllUnits.Remove(outgoing);
            World.AllUnits.Add(incoming);
            World.Bench.Remove(incoming);
            World.Bench.Add(outgoing);
            LogLine($"{outgoing.Definition.displayName} subs out for {incoming.Definition.displayName}.");
        }

        void ResolveAction(BattleUnit unit, SkillDefinition skill, BattleUnit target)
        {
            if (skill.targetsAllies)
            {
                int heal = DamageCalculator.ComputeHeal(unit, skill);
                target.ApplyHeal(heal);
                LogLine($"{unit.Definition.displayName} heals {target.Definition.displayName} for {heal}.");
                if (Settings.ShowDamageNumbers) SpawnDamageNumber(target, $"+{heal}", new Color(0.55f, 0.9f, 0.55f));
            }
            else
            {
                int distance = TargetResolver.ColumnDistance(unit, target);
                int damage = DamageCalculator.ComputeDamage(unit, target, skill, distance);
                target.ApplyDamage(damage);
                LogLine($"{unit.Definition.displayName} hits {target.Definition.displayName} for {damage}.");
                if (Settings.ShowDamageNumbers) SpawnDamageNumber(target, damage.ToString(), Color.white);
                _visuals.FlashHit(target);
                _visuals.PlayImpactFx(target);
                if (!target.IsAlive)
                {
                    _visuals.SyncDefeated(target);
                    Formation.Compact(World.AllUnits, target.Faction);
                }
            }
        }

        void SpawnDamageNumber(BattleUnit target, string text, Color color)
        {
            DamageNumbers.Add(new DamageNumber
            {
                WorldPos = _visuals.GetUnitWorldPosition(target),
                Text = text,
                Color = color,
                Age = 0f,
            });
        }

        public void Restart() => OnRestartRequested?.Invoke();

        /// <summary>Only meaningful when Outcome == PlayerVictory && World.HasNextMap.</summary>
        public void AdvanceToNextMap() => OnAdvanceRequested?.Invoke();
    }
}
