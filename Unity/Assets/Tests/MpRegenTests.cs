using NUnit.Framework;
using UnityEngine;
using Game.Data;
using Game.Battle;
using static Game.Tests.BattleTestHelpers;

namespace Game.Tests
{
    /// <summary>
    /// The MP-economy pieces from M12: BattleUnit's spend/restore primitives and the
    /// between-battle partial recovery, all pure C# and independent of BattleController
    /// (which needs a live scene to test at all -- see PROJECT-README's testing
    /// philosophy). "Code-based stats" verification per the project owner's direction:
    /// confirm the numbers actually move correctly, not by playing the game.
    /// </summary>
    public class MpRegenTests
    {
        static readonly StatBlock DummyStats = new() { hp = 100, attack = 10, defense = 5, magic = 20, resistance = 5, speed = 10 };

        [Test]
        public void SpendMp_ClampsAtZero()
        {
            var unit = MakeUnit(DummyStats, Faction.Player, 0, true);
            unit.SpendMp(unit.MaxMp + 50);
            Assert.AreEqual(0, unit.CurrentMp);
        }

        [Test]
        public void RestoreMp_ClampsAtMax()
        {
            var unit = MakeUnit(DummyStats, Faction.Player, 0, true);
            unit.SpendMp(10);
            unit.RestoreMp(9999);
            Assert.AreEqual(unit.MaxMp, unit.CurrentMp);
        }

        [Test]
        public void RestoreMpFull_SetsToMax()
        {
            var unit = MakeUnit(DummyStats, Faction.Player, 0, true);
            unit.SpendMp(unit.MaxMp); // drain to 0
            Assert.AreEqual(0, unit.CurrentMp);

            unit.RestoreMpFull();

            Assert.AreEqual(unit.MaxMp, unit.CurrentMp);
        }

        [Test]
        public void RecoverMpAfterBattle_RestoresBetween25And50PercentOfMissing()
        {
            var unit = MakeUnit(DummyStats, Faction.Player, 0, true);
            unit.SpendMp(unit.MaxMp); // fully drained -- missing == MaxMp
            int missing = unit.MaxMp - unit.CurrentMp;

            for (int i = 0; i < 50; i++)
            {
                unit.CurrentMp = 0;
                unit.RecoverMpAfterBattle();
                Assert.GreaterOrEqual(unit.CurrentMp, Mathf.RoundToInt(missing * 0.25f) - 1,
                    "recovered less than the 25% floor (allowing 1 for rounding)");
                Assert.LessOrEqual(unit.CurrentMp, Mathf.RoundToInt(missing * 0.5f) + 1,
                    "recovered more than the 50% ceiling (allowing 1 for rounding)");
            }
        }

        [Test]
        public void RecoverMpAfterBattle_NoOpAtFullMp()
        {
            var unit = MakeUnit(DummyStats, Faction.Player, 0, true);
            Assert.AreEqual(unit.MaxMp, unit.CurrentMp);

            unit.RecoverMpAfterBattle();

            Assert.AreEqual(unit.MaxMp, unit.CurrentMp, "a unit already at full MP should never overflow past MaxMp.");
        }

        [Test]
        public void ComputeManaRestore_ScalesWithMagic()
        {
            var weakCaster = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 5, resistance = 1, speed = 1 }, Faction.Player, 1, true);
            var strongCaster = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 30, resistance = 1, speed = 1 }, Faction.Player, 1, true);
            var skill = MakeSkill(pattern: null, usesMagic: true, power: 1f, targetsAllies: true);
            skill.restoresMana = true;

            Assert.Greater(DamageCalculator.ComputeManaRestore(strongCaster, skill), DamageCalculator.ComputeManaRestore(weakCaster, skill));
        }

        [Test]
        public void ComputeManaRestore_NeverNegative()
        {
            var caster = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 0, resistance = 1, speed = 1 }, Faction.Player, 1, true);
            var skill = MakeSkill(pattern: null, usesMagic: true, power: 1f, targetsAllies: true);
            skill.restoresMana = true;

            Assert.GreaterOrEqual(DamageCalculator.ComputeManaRestore(caster, skill), 0);
        }
    }
}
