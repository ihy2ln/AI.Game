using System.Collections.Generic;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// Flat restore amount per potion rank (M13) -- arbitrary numbers, per the project
    /// owner's explicit direction, not a tuned balance pass. Reuses the same F..SSS scale
    /// as character Tier so the two systems read consistently to a player, with no other
    /// coupling between them. A Multi potion restores this same amount to *both* HP and
    /// MP rather than a split/reduced number -- the simplest rule, worth revisiting once
    /// potions have a real economy (drop rates, shop prices) to balance against.
    /// </summary>
    public static class PotionCalculator
    {
        static readonly Dictionary<Tier, int> PotencyByRank = new()
        {
            [Tier.F] = 20, [Tier.E] = 30, [Tier.D] = 45, [Tier.C] = 65,
            [Tier.B] = 90, [Tier.A] = 125, [Tier.S] = 170, [Tier.SS] = 230, [Tier.SSS] = 300,
        };

        public static int Potency(Tier rank) => PotencyByRank.TryGetValue(rank, out var v) ? v : 0;
    }
}
