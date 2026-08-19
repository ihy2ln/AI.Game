using NUnit.Framework;
using Game.Data;
using Game.Battle;

namespace Game.Tests
{
    /// <summary>The M13 battle-inventory potion system: 3 fixed slots (Hp/Mp/Multi),
    /// F..SSS rank potency. Pure C#, independent of BattleController's Item action
    /// (which needs a live scene to test at all).</summary>
    public class PotionTests
    {
        [Test]
        public void PotionCalculator_PotencyIncreasesWithRank()
        {
            int f = PotionCalculator.Potency(Tier.F);
            int c = PotionCalculator.Potency(Tier.C);
            int sss = PotionCalculator.Potency(Tier.SSS);

            Assert.Greater(c, f);
            Assert.Greater(sss, c);
        }

        [Test]
        public void BattleInventorySlot_IsUsable_FalseWhenEmpty()
        {
            var slot = new BattleInventorySlot();
            Assert.IsFalse(slot.IsUsable, "no Potion assigned yet -- shouldn't be usable.");

            slot.Potion = ScriptableObject_CreatePotion();
            Assert.IsFalse(slot.IsUsable, "Count is still 0.");

            slot.Count = 5;
            Assert.IsTrue(slot.IsUsable);

            slot.Count = 0;
            Assert.IsFalse(slot.IsUsable, "using the last one should make it unusable again.");
        }

        [Test]
        public void BattleInventory_Slot_ReturnsTheMatchingKind()
        {
            var inventory = new BattleInventory();
            inventory.Hp.Count = 3;
            inventory.Mp.Count = 7;
            inventory.Multi.Count = 1;

            Assert.AreEqual(3, inventory.Slot(PotionKind.Hp).Count);
            Assert.AreEqual(7, inventory.Slot(PotionKind.Mp).Count);
            Assert.AreEqual(1, inventory.Slot(PotionKind.Multi).Count);
        }

        [Test]
        public void BattleInventory_HasAnyUsable_FalseWhenAllSlotsEmpty()
        {
            var inventory = new BattleInventory();
            Assert.IsFalse(inventory.HasAnyUsable);

            inventory.Mp.Potion = ScriptableObject_CreatePotion();
            inventory.Mp.Count = 1;
            Assert.IsTrue(inventory.HasAnyUsable);
        }

        static PotionDefinition ScriptableObject_CreatePotion() =>
            UnityEngine.ScriptableObject.CreateInstance<PotionDefinition>();
    }
}
