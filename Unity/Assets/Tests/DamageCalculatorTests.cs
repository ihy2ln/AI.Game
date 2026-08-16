using NUnit.Framework;
using Game.Data;
using Game.Battle;
using static Game.Tests.BattleTestHelpers;

namespace Game.Tests
{
    public class DamageCalculatorTests
    {
        [Test]
        public void Damage_NeverGoesNegative()
        {
            var attacker = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 1, resistance = 1, speed = 1 }, Faction.Player, 0, true);
            var target = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 999, magic = 1, resistance = 999, speed = 1 }, Faction.Enemy, 3, false);
            var skill = MakeSkill(pattern: null, isRanged: false, usesMagic: false, power: 1f);

            int damage = DamageCalculator.ComputeDamage(attacker, target, skill, columnDistance: 1);

            Assert.GreaterOrEqual(damage, 0);
        }

        [Test]
        public void RangedDamage_IncreasesWithDistance()
        {
            var attacker = MakeUnit(new StatBlock { hp = 100, attack = 20, defense = 5, magic = 10, resistance = 5, speed = 10 }, Faction.Player, 0, true);
            var target = MakeUnit(new StatBlock { hp = 100, attack = 10, defense = 5, magic = 10, resistance = 5, speed = 10 }, Faction.Enemy, 5, false);
            var skill = MakeSkill(pattern: null, isRanged: true, usesMagic: false, power: 1f);

            int near = DamageCalculator.ComputeDamage(attacker, target, skill, columnDistance: 1);
            int far = DamageCalculator.ComputeDamage(attacker, target, skill, columnDistance: 5);

            Assert.Greater(far, near);
        }

        [Test]
        public void MeleeDamage_DoesNotScaleWithDistance()
        {
            var attacker = MakeUnit(new StatBlock { hp = 100, attack = 20, defense = 5, magic = 10, resistance = 5, speed = 10 }, Faction.Player, 0, true);
            var target = MakeUnit(new StatBlock { hp = 100, attack = 10, defense = 5, magic = 10, resistance = 5, speed = 10 }, Faction.Enemy, 5, false);
            var skill = MakeSkill(pattern: null, isRanged: false, usesMagic: false, power: 1f);

            int atDistance1 = DamageCalculator.ComputeDamage(attacker, target, skill, columnDistance: 1);
            int atDistance5 = DamageCalculator.ComputeDamage(attacker, target, skill, columnDistance: 5);

            Assert.AreEqual(atDistance1, atDistance5);
        }

        [Test]
        public void Heal_ScalesWithMagic()
        {
            var weakHealer = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 5, resistance = 1, speed = 1 }, Faction.Player, 1, true);
            var strongHealer = MakeUnit(new StatBlock { hp = 100, attack = 1, defense = 1, magic = 30, resistance = 1, speed = 1 }, Faction.Player, 1, true);
            var skill = MakeSkill(pattern: null, usesMagic: true, power: 1f, targetsAllies: true);

            Assert.Greater(DamageCalculator.ComputeHeal(strongHealer, skill), DamageCalculator.ComputeHeal(weakHealer, skill));
        }
    }
}
