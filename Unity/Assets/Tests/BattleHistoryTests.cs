using System.Collections.Generic;
using NUnit.Framework;
using Game.Data;
using Game.Battle;

namespace Game.Tests
{
    public class BattleHistoryTests
    {
        static BattleUnit MakeUnit(int hp) =>
            BattleTestHelpers.MakeUnit(new StatBlock { hp = hp, attack = 5, defense = 1, magic = 1, resistance = 1, speed = 10 }, Faction.Player, 2, true);

        static List<BattleUnit> EmptyBench => new();

        [Test]
        public void Undo_RestoresPreviousHp()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new List<BattleUnit> { unit };
            var history = new BattleHistory();

            history.Capture(units, EmptyBench, log); // point 0: hp 100

            unit.ApplyDamage(30);
            log.Add(1, "hit for 30");
            history.Capture(units, EmptyBench, log); // point 1: hp 70

            Assert.AreEqual(70, unit.CurrentHp);
            history.Undo(units, EmptyBench, log);
            Assert.AreEqual(100, unit.CurrentHp);
            Assert.AreEqual(0, log.Entries.Count);
        }

        [Test]
        public void Redo_ReappliesUndoneState()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new List<BattleUnit> { unit };
            var history = new BattleHistory();

            history.Capture(units, EmptyBench, log);
            unit.ApplyDamage(30);
            log.Add(1, "hit for 30");
            history.Capture(units, EmptyBench, log);

            history.Undo(units, EmptyBench, log);
            Assert.AreEqual(100, unit.CurrentHp);

            history.Redo(units, EmptyBench, log);
            Assert.AreEqual(70, unit.CurrentHp);
            Assert.AreEqual(1, log.Entries.Count);
        }

        [Test]
        public void Capture_AfterUndo_DiscardsStaleRedoBranch()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new List<BattleUnit> { unit };
            var history = new BattleHistory();

            history.Capture(units, EmptyBench, log); // 100
            unit.ApplyDamage(30);
            history.Capture(units, EmptyBench, log); // 70

            history.Undo(units, EmptyBench, log); // back to 100

            unit.ApplyDamage(10); // new branch: 90
            history.Capture(units, EmptyBench, log);

            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual(90, unit.CurrentHp);
        }

        [Test]
        public void CanUndoCanRedo_ReflectCursorPosition()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new List<BattleUnit> { unit };
            var history = new BattleHistory();

            history.Capture(units, EmptyBench, log);
            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            unit.ApplyDamage(10);
            history.Capture(units, EmptyBench, log);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            history.Undo(units, EmptyBench, log);
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);
        }

        [Test]
        public void Undo_RestoresColumnAndRosterMembership()
        {
            var log = new BattleLog();
            var active = MakeUnit(100);
            var benched = MakeUnit(80);
            benched.Column = BattleWorld.BenchColumn;
            var activeList = new List<BattleUnit> { active };
            var benchList = new List<BattleUnit> { benched };
            var history = new BattleHistory();

            history.Capture(activeList, benchList, log); // point 0: active=[active], bench=[benched]

            // Simulate a sub: benched takes active's column and swaps lists, mirroring
            // BattleController.SubUnit.
            benched.Column = active.Column;
            active.Column = BattleWorld.BenchColumn;
            activeList.Remove(active);
            activeList.Add(benched);
            benchList.Remove(benched);
            benchList.Add(active);
            log.Add(1, "subbed");
            history.Capture(activeList, benchList, log); // point 1: active=[benched], bench=[active]

            history.Undo(activeList, benchList, log);

            CollectionAssert.AreEqual(new[] { active }, activeList);
            CollectionAssert.AreEqual(new[] { benched }, benchList);
            Assert.AreEqual(2, active.Column);
            Assert.AreEqual(BattleWorld.BenchColumn, benched.Column);
        }

        /// <summary>M13: without passing `inventory` through, Undo after using a potion
        /// would leave the roster's HP/MP restored but the potion count still spent --
        /// exploitable as a free duplicate via Undo/Redo/act-again. Capture/Undo/Redo's
        /// `inventory` parameter is optional (defaults to null) specifically so this is
        /// the only test that needs to pass it; every other test above is unaffected.</summary>
        [Test]
        public void Undo_RestoresInventoryCounts()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new List<BattleUnit> { unit };
            var history = new BattleHistory();
            var inventory = new BattleInventory();
            inventory.Hp.Count = 5;

            history.Capture(units, EmptyBench, log, inventory); // point 0: 5 potions

            inventory.Hp.Count = 4; // "used" one
            log.Add(1, "used a potion");
            history.Capture(units, EmptyBench, log, inventory); // point 1: 4 potions

            history.Undo(units, EmptyBench, log, inventory);
            Assert.AreEqual(5, inventory.Hp.Count);

            history.Redo(units, EmptyBench, log, inventory);
            Assert.AreEqual(4, inventory.Hp.Count);
        }
    }
}
