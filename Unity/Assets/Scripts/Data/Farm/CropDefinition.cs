using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A growable crop. Stardew/Rune Factory model: seed goes in, water and fertiliser
    /// modify growth, produce comes out.
    ///
    /// Growth is driven by real time and/or battle count. Endless-mode fights each count
    /// as one battle. Clock manipulation is accepted by design — no server validation.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Farm/Crop", fileName = "Crop_")]
    public class CropDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string cropId;
        public string displayName;
        public Sprite icon;

        [Header("Planting")]
        [Tooltip("Seed material consumed on planting.")]
        public MaterialDefinition seed;

        [Tooltip("Where this crop can be planted. Dungeon plots use the same growth rules.")]
        public FarmPlotType plotType = FarmPlotType.Town;

        [Tooltip("Island this crop is native to. Empty means it grows anywhere.")]
        public string islandId;

        [Header("Growth")]
        [Min(1)] public int growthSeconds = 600;

        [Tooltip("Alternative growth trigger. 0 disables battle-count growth for this crop.")]
        [Min(0)] public int growthBattles = 0;

        [Tooltip("Growth stages for sprite display. Purely visual.")]
        [Min(1)] public int visualStages = 4;

        [Header("Inputs")]
        [Tooltip("Fractional growth time reduction when watered. 0.25 = 25% faster.")]
        [Range(0f, 0.9f)] public float waterSpeedBonus = 0.25f;

        [Tooltip("Crops needing water will stall if left dry.")]
        public bool requiresWater = true;

        [Tooltip("Fertilisers accepted by this crop and what they do.")]
        public List<FertilizerEffect> fertilizers = new();

        [Header("Harvest")]
        public MaterialDefinition produce;
        [Min(1)] public int minYield = 1;
        [Min(1)] public int maxYield = 3;

        [Tooltip("Regrows after harvest instead of clearing the plot. 0 = single harvest.")]
        [Min(0)] public int regrowSeconds = 0;
    }

    public enum FarmPlotType { Town, Dungeon, Both }

    [System.Serializable]
    public struct FertilizerEffect
    {
        public MaterialDefinition fertilizer;
        [Range(0f, 0.9f)] public float speedBonus;
        [Min(0)] public int bonusYield;
        [Range(0f, 1f)] public float qualityUpChance;
    }
}
