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
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public BattleWorld World { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public string LastAction { get; private set; } = "";
        public readonly List<DamageNumber> DamageNumbers = new();

        public bool ManualMode { get; private set; }
        public BattleUnit PendingActor { get; private set; }
        public IReadOnlyList<BattleUnit> PendingTargets => _pendingTargets;

        public event Action OnRestartRequested;

        BattleVisuals _visuals;
        Camera _cam;
        TurnOrder _turnOrder;
        List<BattleUnit> _pendingTargets = new();
        BattleUnit _submittedTarget;
        const float TurnDelaySeconds = 0.9f;

        public void Init(BattleWorld world, BattleVisuals visuals, Camera cam)
        {
            World = world;
            _visuals = visuals;
            _cam = cam;
            Outcome = BattleOutcome.InProgress;
            _turnOrder = new TurnOrder(world.AllUnits);
            if (world.LoadedOk) StartCoroutine(RunBattle());
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

            if (PendingActor != null && Input.GetMouseButtonDown(0)) HandleClick(Input.mousePosition);
        }

        public void ToggleMode() => ManualMode = !ManualMode;

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

                yield return new WaitForSeconds(TurnDelaySeconds);

                var skill = unit.Definition.standardSkill;
                if (skill == null || skill.pattern == null)
                {
                    LastAction = $"{unit.Definition.displayName} has no usable skill.";
                    continue;
                }

                var targets = TargetResolver.GetValidTargets(unit, skill, World.AllUnits);
                if (targets.Count == 0)
                {
                    LastAction = $"{unit.Definition.displayName} has no valid target.";
                    continue;
                }

                BattleUnit target;
                if (ManualMode && unit.Faction == Faction.Player)
                {
                    PendingActor = unit;
                    _pendingTargets = targets;
                    _submittedTarget = null;
                    LastAction = $"{unit.Definition.displayName}'s turn -- tap a target.";
                    yield return new WaitUntil(() => _submittedTarget != null);
                    target = _submittedTarget;
                    PendingActor = null;
                    _pendingTargets = new List<BattleUnit>();
                }
                else
                {
                    target = targets[UnityEngine.Random.Range(0, targets.Count)];
                }

                ResolveAction(unit, skill, target);
            }

            Outcome = World.PlayerDefeated ? BattleOutcome.EnemyVictory : BattleOutcome.PlayerVictory;
            LastAction = Outcome == BattleOutcome.PlayerVictory ? "Victory!" : "Defeat...";
        }

        void ResolveAction(BattleUnit unit, SkillDefinition skill, BattleUnit target)
        {
            if (skill.targetsAllies)
            {
                int heal = DamageCalculator.ComputeHeal(unit, skill);
                target.ApplyHeal(heal);
                LastAction = $"{unit.Definition.displayName} heals {target.Definition.displayName} for {heal}.";
                SpawnDamageNumber(target, $"+{heal}", new Color(0.55f, 0.9f, 0.55f));
            }
            else
            {
                int distance = TargetResolver.ColumnDistance(unit, target);
                int damage = DamageCalculator.ComputeDamage(unit, target, skill, distance);
                target.ApplyDamage(damage);
                LastAction = $"{unit.Definition.displayName} hits {target.Definition.displayName} for {damage}.";
                SpawnDamageNumber(target, damage.ToString(), Color.white);
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
