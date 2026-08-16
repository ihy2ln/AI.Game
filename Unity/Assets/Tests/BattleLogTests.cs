using NUnit.Framework;
using Game.Battle;

namespace Game.Tests
{
    public class BattleLogTests
    {
        [Test]
        public void Add_AppendsInOrder()
        {
            var log = new BattleLog();
            log.Add(1, "first");
            log.Add(1, "second");
            log.Add(2, "third");

            Assert.AreEqual(3, log.Entries.Count);
            Assert.AreEqual("first", log.Entries[0].Text);
            Assert.AreEqual("second", log.Entries[1].Text);
            Assert.AreEqual("third", log.Entries[2].Text);
        }

        [Test]
        public void Add_PreservesRoundNumberPerEntry()
        {
            var log = new BattleLog();
            log.Add(1, "round one action");
            log.Add(2, "round two action");

            Assert.AreEqual(1, log.Entries[0].Round);
            Assert.AreEqual(2, log.Entries[1].Round);
        }

        [Test]
        public void TruncateTo_RemovesEntriesAfterCount()
        {
            var log = new BattleLog();
            log.Add(1, "a");
            log.Add(1, "b");
            log.Add(2, "c");

            log.TruncateTo(2);

            Assert.AreEqual(2, log.Entries.Count);
            Assert.AreEqual("a", log.Entries[0].Text);
            Assert.AreEqual("b", log.Entries[1].Text);
        }

        [Test]
        public void TruncateTo_Zero_ClearsLog()
        {
            var log = new BattleLog();
            log.Add(1, "a");

            log.TruncateTo(0);

            Assert.AreEqual(0, log.Entries.Count);
        }
    }
}
