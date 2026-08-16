using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Data;
using Game.Battle;
using static Game.Tests.BattleTestHelpers;

namespace Game.Tests
{
    public class TargetResolverTests
    {
        static readonly StatBlock DummyStats = new() { hp = 100, attack = 10, defense = 5, magic = 10, resistance = 5, speed = 10 };

        [Test]
        public void Melee_CannotReachColumnPlusTwo()
        {
            var meleePattern = MakePattern(new[] { new Vector2Int(0, 1) });
            var caster = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var farTarget = MakeUnit(DummyStats, Faction.Enemy, column: 2, facingRight: false);
            var skill = MakeSkill(meleePattern);

            var targets = TargetResolver.GetValidTargets(caster, skill, new List<BattleUnit> { caster, farTarget });

            Assert.IsEmpty(targets);
        }

        [Test]
        public void Melee_CanReachColumnPlusOne()
        {
            var meleePattern = MakePattern(new[] { new Vector2Int(0, 1) });
            var caster = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var nearTarget = MakeUnit(DummyStats, Faction.Enemy, column: 1, facingRight: false);
            var skill = MakeSkill(meleePattern);

            var targets = TargetResolver.GetValidTargets(caster, skill, new List<BattleUnit> { caster, nearTarget });

            Assert.AreEqual(1, targets.Count);
            Assert.AreSame(nearTarget, targets[0]);
        }

        [Test]
        public void Ranged_CanReachAnyColumnOnOpposingSide()
        {
            var rangedPattern = MakePattern(new[] { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4), new Vector2Int(0, 5) });
            var caster = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var farTarget = MakeUnit(DummyStats, Faction.Enemy, column: 5, facingRight: false);
            var skill = MakeSkill(rangedPattern, isRanged: true);

            var targets = TargetResolver.GetValidTargets(caster, skill, new List<BattleUnit> { caster, farTarget });

            Assert.AreEqual(1, targets.Count);
        }

        [Test]
        public void TargetsAllies_OffersOwnFactionNotOpposing()
        {
            var healPattern = MakePattern(new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) });
            var caster = MakeUnit(DummyStats, Faction.Player, column: 1, facingRight: true);
            var ally = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var enemy = MakeUnit(DummyStats, Faction.Enemy, column: 0, facingRight: false); // same column index, opposing faction
            var skill = MakeSkill(healPattern, usesMagic: true, targetsAllies: true);

            var targets = TargetResolver.GetValidTargets(caster, skill, new List<BattleUnit> { caster, ally, enemy });

            Assert.AreEqual(1, targets.Count);
            Assert.AreSame(ally, targets[0]);
        }

        [Test]
        public void Melee_MirrorsRangeWhenFacingLeft()
        {
            var meleePattern = MakePattern(new[] { new Vector2Int(0, 1) }); // "forward" from the caster's own facing
            var caster = MakeUnit(DummyStats, Faction.Enemy, column: 3, facingRight: false); // enemy faces left
            var target = MakeUnit(DummyStats, Faction.Player, column: 2, facingRight: true); // one column toward the player side
            var skill = MakeSkill(meleePattern);

            var targets = TargetResolver.GetValidTargets(caster, skill, new List<BattleUnit> { caster, target });

            Assert.AreEqual(1, targets.Count);
        }
    }
}
