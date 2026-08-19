using System.Collections.Generic;
using NUnit.Framework;
using Game.Data;
using Game.Battle;
using static Game.Tests.BattleTestHelpers;

namespace Game.Tests
{
    public class FormationTests
    {
        static readonly StatBlock DummyStats = new() { hp = 100, attack = 10, defense = 5, magic = 10, resistance = 5, speed = 10 };

        [Test]
        public void Compact_PlayerFrontDies_NextInLineBecomesFront()
        {
            var back = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var mid = MakeUnit(DummyStats, Faction.Player, column: 1, facingRight: true);
            var front = MakeUnit(DummyStats, Faction.Player, column: 2, facingRight: true);
            front.ApplyDamage(9999);
            var all = new List<BattleUnit> { back, mid, front };

            Formation.Compact(all, Faction.Player);

            Assert.IsFalse(front.IsAlive);
            Assert.AreEqual(Formation.PlayerFrontColumn, mid.Column); // mid is now the frontline
            Assert.AreEqual(Formation.PlayerFrontColumn - 1, back.Column);
        }

        [Test]
        public void Compact_EnemyMiddleDies_BackRankFillsTheGap()
        {
            var front = MakeUnit(DummyStats, Faction.Enemy, column: 3, facingRight: false);
            var mid = MakeUnit(DummyStats, Faction.Enemy, column: 4, facingRight: false);
            var back = MakeUnit(DummyStats, Faction.Enemy, column: 5, facingRight: false);
            mid.ApplyDamage(9999);
            var all = new List<BattleUnit> { front, mid, back };

            Formation.Compact(all, Faction.Enemy);

            Assert.IsFalse(mid.IsAlive);
            Assert.AreEqual(Formation.EnemyFrontColumn, front.Column); // unchanged, still front
            Assert.AreEqual(Formation.EnemyFrontColumn + 1, back.Column); // closed the gap left by mid
        }

        [Test]
        public void Compact_NoDeaths_ColumnsUnchanged()
        {
            var back = MakeUnit(DummyStats, Faction.Player, column: 0, facingRight: true);
            var mid = MakeUnit(DummyStats, Faction.Player, column: 1, facingRight: true);
            var front = MakeUnit(DummyStats, Faction.Player, column: 2, facingRight: true);
            var all = new List<BattleUnit> { back, mid, front };

            Formation.Compact(all, Faction.Player);

            Assert.AreEqual(0, back.Column);
            Assert.AreEqual(1, mid.Column);
            Assert.AreEqual(2, front.Column);
        }

        [Test]
        public void Compact_OnlyTouchesTheAffectedFaction()
        {
            var playerFront = MakeUnit(DummyStats, Faction.Player, column: 2, facingRight: true);
            var enemyFront = MakeUnit(DummyStats, Faction.Enemy, column: 3, facingRight: false);
            var enemyBack = MakeUnit(DummyStats, Faction.Enemy, column: 5, facingRight: false);
            enemyFront.ApplyDamage(9999);
            var all = new List<BattleUnit> { playerFront, enemyFront, enemyBack };

            Formation.Compact(all, Faction.Enemy);

            Assert.AreEqual(2, playerFront.Column); // untouched -- different faction
            Assert.AreEqual(Formation.EnemyFrontColumn, enemyBack.Column); // promoted to front
        }
    }
}
