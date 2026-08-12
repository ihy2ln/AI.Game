using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A rolled, owned unit. This is save data.
    ///
    /// Stats and skills roll ONCE at acquisition then freeze. The seed is stored so the
    /// result is reproducible on any device — saves sync between Android and PC and must
    /// resolve identically on both.
    ///
    /// No starter gear: all three equip slots begin empty.
    /// </summary>
    [Serializable]
    public class CharacterInstance
    {
        public const int MaxFusions = 10;
        public const float FusionStepPct = 0.10f;

        public string instanceId;
        public string characterId;

        [Header("Roll results - frozen at acquisition")]
        public Tier tier;
        public int rollSeed;
        public StatBlock rolledStats;
        public string classSkillId;
        public string elementSkillId;

        [Header("Progression")]
        public int level = 1;

        [Tooltip("Duplicate fusions. Each adds 10%. Capped at 10.")]
        [Range(0, MaxFusions)] public int fusionCount;

        [Header("Equipment - instance ids, empty when unequipped")]
        public string weaponId = "";
        public string armorId = "";
        public string accessoryId = "";

        /// <summary>
        /// Additive stacking: 10 fusions = +100%.
        /// For multiplicative, use Mathf.Pow(1f + FusionStepPct, fusionCount) (~2.59x at 10).
        /// Additive is the safer default — multiplicative compounds hard on top of tier variance.
        /// </summary>
        public float FusionMultiplier => 1f + FusionStepPct * Mathf.Min(fusionCount, MaxFusions);

        public StatBlock EffectiveStats(CharacterDefinition def)
        {
            float levelMul = 1f + def.growthPerLevel * (level - 1);
            return rolledStats * (levelMul * FusionMultiplier);
        }
    }
}
