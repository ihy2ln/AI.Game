using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Gear template. No starter gear is granted — every piece is dropped, summoned or crafted.
    /// Substats roll on acquisition, so instances carry the rolled values.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Economy/Equipment", fileName = "Gear_")]
    public class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string equipmentId;
        public string displayName;
        public Sprite icon;
        public EquipSlot slot;
        public Rarity rarity = Rarity.Common;
        public Age age;

        [Header("Acquisition")]
        public GearSource source = GearSource.Drop;

        [Tooltip("Exclusive gear is summon- or craft-only and never appears in drop tables.")]
        public bool isExclusive;

        [Header("Stats")]
        public StatBlock mainStats;

        [Tooltip("Pool of substats that may roll. Count rolled is between min and max.")]
        public List<SubstatOption> substatPool = new();
        [Min(0)] public int minSubstats = 0;
        [Min(0)] public int maxSubstats = 4;

        [Header("Enhancement")]
        [Min(0)] public int maxEnhanceLevel = 15;
        [Tooltip("Stat gain per enhance level, as a fraction of mainStats.")]
        public float enhancePerLevel = 0.08f;

        [Tooltip("Materials consumed per enhance level. Sourced from gear stages.")]
        public List<DropEntry> enhanceCost = new();

        [Header("Set bonus")]
        public EquipmentSetDefinition set;

        [Header("Crafting")]
        [Tooltip("Empty when this item is not craftable.")]
        public List<DropEntry> craftCost = new();
        [Min(0)] public int craftTimeSeconds;
    }

    [System.Serializable]
    public struct SubstatOption
    {
        public StatType stat;
        public int minValue;
        public int maxValue;
        [Min(0f)] public float rollWeight;
    }

    public enum StatType { HP, Attack, Defense, Magic, Resistance, Speed }
}
