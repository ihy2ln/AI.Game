using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// Turn state machine with two modes:
    ///  - Auto: both sides act automatically each turn (BD2/gacha-style "auto battle").
    ///  - Manual: enemy turns still resolve automatically, but a player-faction turn
    ///    pauses and waits for a tap/click on one of the highlighted valid targets.
    /// Only one skill exists per unit in this slice (standardSkill), so "manual" means
    /// choosing WHO to hit/heal, not picking a skill -- skill selection is a later pass.
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
        public IReadOnlyList<BattleUnit> PendingTargets => _pendingTargets;

        public bool CanUndo => _history.CanUndo;
        public bool CanRedo => _history.CanRedo;

        public event Action OnRestartRequested;

        BattleVisuals _visuals;
        Camera _cam;
        TurnOrder _turnOrder;
        readonly BattleHistory _history = new();
        Coroutine _runCoroutine;
        List<BattleUnit> _pendingTargets = new();
        BattleUnit _submittedTarget;
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

            _history.Capture(World.AllUnits, Log);
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

            if (PendingActor != null && !Paused && Input.GetMouseButtonDown(0)) HandleClick(Input.mousePosition);
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
            _history.Undo(World.AllUnits, Log);
            ResumeFromHistory();
        }

        public void Redo()
        {
            if (!_history.CanRedo) return;
            _history.Redo(World.AllUnits, Log);
            ResumeFromHistory();
        }

        void ResumeFromHistory()
        {
            if (_runCoroutine != null) StopCoroutine(_runCoroutine);
            PendingActor = null;
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

        IEnumerator RunBattle()
        {
            while (!World.IsOver)
            {
                var unit = _turnOrder.Next();
                if (unit == null) break;

                yield return new WaitForSeconds(PreActionDelaySeconds);

                var skill = unit.Definition.standardSkill;
                if (skill == null || skill.pattern == null)
                {
                    LogLine($"{unit.Definition.displayName} has no usable skill.");
                }
                else
                {
                    var targets = TargetResolver.GetValidTargets(unit, skill, World.AllUnits);
                    if (targets.Count == 0)
                    {
                        LogLine($"{unit.Definition.displayName} has no valid target.");
                    }
                    else
                    {
                        BattleUnit target;
                        if (ManualMode && unit.Faction == Faction.Player)
                        {
                            PendingActor = unit;
                            _pendingTargets = targets;
                            _submittedTarget = null;
                            LogLine($"{unit.Definition.displayName}'s turn -- tap a target.");
                            yield return new WaitUntil(() => _submittedTarget != null);
                            target = _submittedTarget;
                            PendingActor = null;
                            _pendingTargets = new List<BattleUnit>();
                        }
                        else
                        {
                            target = targets[UnityEngine.Random.Range(0, targets.Count)];
                        }

                        yield return _visuals.MoveToStage(unit, target);
                        ResolveAction(unit, skill, target);
                        yield return new WaitForSeconds(ImpactHoldSeconds);
                        yield return _visuals.ReturnToDock(unit, target);
                    }
                }

                // One capture per consumed TurnOrder.Next() -- see BattleHistory's
                // class doc for why this 1:1 correspondence matters for Undo/Redo.
                _history.Capture(World.AllUnits, Log);
            }

            Outcome = World.PlayerDefeated ? BattleOutcome.EnemyVictory : BattleOutcome.PlayerVictory;
            LogLine(Outcome == BattleOutcome.PlayerVictory ? "Victory!" : "Defeat...");
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
                if (!target.IsAlive) _visuals.SyncDefeated(target);
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
    }
}
