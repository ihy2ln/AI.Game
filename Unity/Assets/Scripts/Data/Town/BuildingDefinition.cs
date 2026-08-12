using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A town building. Costs materials plus a real-time build countdown, then runs a
    /// production queue with its own timing.
    ///
    /// Design rule: every building should unlock a VERB the player could not do before,
    /// not a flat stat bonus. Higher levels unlock better products.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Town/Building", fileName = "Bld_")]
    public class BuildingDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string buildingId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public DistrictType district;

        [Header("Unlocked capability")]
        [Tooltip("The verb this building grants. Referenced by UI gating.")]
        public BuildingFunction function;

        [Header("Levels")]
        [Tooltip("One entry per level. Index 0 is level 1.")]
        public List<BuildingLevel> levels = new();

        [Header("Requirements")]
        public List<string> requiredFlags = new();

        [Tooltip("Set when this building first completes. Gates other content.")]
        public string unlockFlagOnBuild;
    }

    /// <summary>The verb a building grants. Kept explicit so UI can gate on it.</summary>
    public enum BuildingFunction
    {
        None,
        Recruitment,        // Guild Hall
        GearEnhancement,    // Blacksmith
        Cooking,            // Kitchen
        SellProduce,        // Market
        IdleTeamSlots,      // Barracks / Dorm
        Crafting,
        SeedProduction,
        Storage
    }

    [System.Serializable]
    public class BuildingLevel
    {
        [Header("Cost to reach this level")]
        public List<DropEntry> buildCost = new();

        [Tooltip("Real-time countdown to complete construction or upgrade.")]
        [Min(0)] public int buildTimeSeconds = 300;

        [Header("Production")]
        [Tooltip("Concurrent production slots at this level.")]
        [Min(0)] public int queueSlots = 1;

        [Tooltip("Multiplier on production time. Lower is faster.")]
        [Min(0.1f)] public float queueTimeMultiplier = 1f;

        [Tooltip("Recipes unlocked at this level. Higher levels make better products.")]
        public List<ProductionRecipe> recipes = new();

        [Header("Function scaling")]
        [Tooltip("Generic magnitude for the building's function, e.g. idle team slots granted.")]
        public int functionValue;
    }

    [System.Serializable]
    public class ProductionRecipe
    {
        public string recipeId;
        public string displayName;
        public List<DropEntry> inputs = new();
        public List<DropEntry> outputs = new();
        [Min(0)] public int baseTimeSeconds = 60;
    }
}
