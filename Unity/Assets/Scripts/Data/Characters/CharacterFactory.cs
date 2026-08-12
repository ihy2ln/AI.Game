using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Builds a CharacterInstance from a definition. All randomness flows through one
    /// seeded System.Random, so the same seed always produces the same unit on any platform.
    /// </summary>
    public static class CharacterFactory
    {
        public static CharacterInstance Create(
            CharacterDefinition def,
            TierDefinition tierDef,
            IReadOnlyList<SkillDefinition> globalSkills,
            int seed)
        {
            var rng = new System.Random(seed);

            var stats = (def.baseStats * tierDef.statMultiplier)
                        .RollVariance(rng, tierDef.variance);

            var classPool = def.classSkillPool.Count > 0
                ? (IReadOnlyList<SkillDefinition>)def.classSkillPool
                : globalSkills.Where(s => s.pool == SkillPool.Class && s.classType == def.classType).ToList();

            var elemPool = def.elementSkillPool.Count > 0
                ? (IReadOnlyList<SkillDefinition>)def.elementSkillPool
                : globalSkills.Where(s => s.pool == SkillPool.Element && s.element == def.element).ToList();

            return new CharacterInstance
            {
                instanceId     = Guid.NewGuid().ToString("N"),
                characterId    = def.characterId,
                tier           = tierDef.tier,
                rollSeed       = seed,
                rolledStats    = stats,
                classSkillId   = PickWeighted(classPool, rng)?.skillId ?? "",
                elementSkillId = PickWeighted(elemPool, rng)?.skillId ?? "",
                level          = 1,
                fusionCount    = 0
            };
        }

        /// <summary>Rolls a tier from weighted definitions. Used by gacha and recruitment.</summary>
        public static TierDefinition RollTier(IReadOnlyList<TierDefinition> tiers, System.Random rng)
        {
            float total = tiers.Sum(t => Mathf.Max(0f, t.summonWeight));
            if (total <= 0f) return tiers[rng.Next(tiers.Count)];

            double roll = rng.NextDouble() * total;
            foreach (var t in tiers)
            {
                roll -= Mathf.Max(0f, t.summonWeight);
                if (roll <= 0d) return t;
            }
            return tiers[tiers.Count - 1];
        }

        /// <summary>Fuse a duplicate into an owned unit. False when already capped.</summary>
        public static bool Fuse(CharacterInstance target)
        {
            if (target.fusionCount >= CharacterInstance.MaxFusions) return false;
            target.fusionCount++;
            return true;
        }

        static SkillDefinition PickWeighted(IReadOnlyList<SkillDefinition> pool, System.Random rng)
        {
            if (pool == null || pool.Count == 0) return null;
            float total = pool.Sum(s => Mathf.Max(0f, s.rollWeight));
            if (total <= 0f) return pool[rng.Next(pool.Count)];

            double roll = rng.NextDouble() * total;
            foreach (var s in pool)
            {
                roll -= Mathf.Max(0f, s.rollWeight);
                if (roll <= 0d) return s;
            }
            return pool[pool.Count - 1];
        }
    }
}
