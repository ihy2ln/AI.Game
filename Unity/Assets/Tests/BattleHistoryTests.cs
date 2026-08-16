using NUnit.Framework;
using Game.Data;
using Game.Battle;

namespace Game.Tests
{
    public class BattleHistoryTests
    {
        static BattleUnit MakeUnit(int hp) =>
            BattleTestHelpers.MakeUnit(new StatBlock { hp = hp, attack = 5, defense = 1, magic = 1, resistance = 1, speed = 10 }, Faction.Player, 2, true);

        [Test]
        public void Undo_RestoresPreviousHp()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new[] { unit };
            var history = new BattleHistory();

            history.Capture(units, log); // point 0: hp 100

            unit.ApplyDamage(30);
            log.Add(1, "hit for 30");
            history.Capture(units, log); // point 1: hp 70

            Assert.AreEqual(70, unit.CurrentHp);
            history.Undo(units, log);
            Assert.AreEqual(100, unit.CurrentHp);
            Assert.AreEqual(0, log.Entries.Count);
        }

        [Test]
        public void Redo_ReappliesUndoneState()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new[] { unit };
            var history = new BattleHistory();

            history.Capture(units, log);
            unit.ApplyDamage(30);
            log.Add(1, "hit for 30");
            history.Capture(units, log);

            history.Undo(units, log);
            Assert.AreEqual(100, unit.CurrentHp);

            history.Redo(units, log);
            Assert.AreEqual(70, unit.CurrentHp);
            Assert.AreEqual(1, log.Entries.Count);
        }

        [Test]
        public void Capture_AfterUndo_DiscardsStaleRedoBranch()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new[] { unit };
            var history = new BattleHistory();

            history.Capture(units, log); // 100
            unit.ApplyDamage(30);
            history.Capture(units, log); // 70

            history.Undo(units, log); // back to 100

            unit.ApplyDamage(10); // new branch: 90
            history.Capture(units, log);

            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual(90, unit.CurrentHp);
        }

        [Test]
        public void CanUndoCanRedo_ReflectCursorPosition()
        {
            var log = new BattleLog();
            var unit = MakeUnit(100);
            var units = new[] { unit };
            var history = new BattleHistory();

            history.Capture(units, log);
            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            unit.ApplyDamage(10);
            history.Capture(units, log);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);

            history.Undo(units, log);
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);
        }
    }
}
