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

    /// <summary>
    /// Auto-battle state machine: NextTurn -> pick skill (only standardSkill exists
    /// in this slice) -> pick target -> resolve -> repeat until one side is wiped.
    /// Manual unit/skill/target selection is a later pass; auto-battle gets a full,
    /// watchable fight on screen fastest, and is genre-appropriate (BD2/gacha-style
    /// "auto battle" toggle) rather than a placeholder shortcut.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public BattleWorld World { get; private set; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public string LastAction { get; private set; } = "";
        public readonly List<DamageNumber> DamageNumbers = new();

        public event Action OnRestartRequested;

        BattleVisuals _visuals;
        TurnOrder _turnOrder;
        const float TurnDelaySeconds = 0.9f;

        public void Init(BattleWorld world, BattleVisuals visuals)
        {
            World = world;
            _visuals = visuals;
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
        }

        IEnumerator RunBattle()
        {
            while (!World.IsOver)
            {
                var unit = _turnOrder.Next();
                if (unit == null) break;

                yield return new WaitForSeconds(TurnDelaySeconds);
                TakeTurn(unit);
            }

            Outcome = World.PlayerDefeated ? BattleOutcome.EnemyVictory : BattleOutcome.PlayerVictory;
            LastAction = Outcome == BattleOutcome.PlayerVictory ? "Victory!" : "Defeat...";
        }

        void TakeTurn(BattleUnit unit)
        {
            var skill = unit.Definition.standardSkill;
            if (skill == null || skill.pattern == null)
            {
                LastAction = $"{unit.Definition.displayName} has no usable skill.";
                return;
            }

            var targets = TargetResolver.GetValidTargets(unit, skill, World.AllUnits);
            if (targets.Count == 0)
            {
                LastAction = $"{unit.Definition.displayName} has no valid target.";
                return;
            }

            var target = targets[UnityEngine.Random.Range(0, targets.Count)];

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
