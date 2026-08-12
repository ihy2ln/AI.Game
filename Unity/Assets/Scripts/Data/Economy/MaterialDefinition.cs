using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A material or currency. Classified on four independent axes so drop tables,
    /// crafting recipes and shop stock can all filter the same catalogue:
    ///   tier     - power band, gates what it can be spent on
    ///   age      - era band, ties to the archipelago's genre-per-island structure
    ///   island   - where it originates
    ///   rarity   - drop scarcity
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Economy/Material", fileName = "Mat_")]
    public class MaterialDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string materialId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Classification")]
        [Tooltip("Power band. Higher tier materials gate higher tier crafting and building.")]
        [Range(1, 10)] public int tier = 1;

        public Age age;

        [Tooltip("Island of origin. Empty means it drops everywhere.")]
        public string islandId;

        public Rarity rarity = Rarity.Common;

        [Header("Category")]
        public MaterialCategory category;

        [Header("Economy")]
        [Min(0)] public int sellValue;
        [Min(1)] public int stackSize = 999;

        [Tooltip("Currencies bypass inventory stacking and display in the header bar.")]
        public bool isCurrency;
    }

    /// <summary>
    /// Category decides which mode may drop it. Per the mode-ownership rule, every mode
    /// must exclusively own at least one category or it becomes dead content.
    /// </summary>
    public enum MaterialCategory
    {
        BuildingMaterial,   // Battle mode
        GearUpgrade,        // Gear stages
        PremiumCurrency,    // Endless
        StandardCurrency,   // Idle
        FarmInput,          // seeds, fertiliser
        Produce,            // harvested crops
        CraftComponent,
        Consumable
    }
}
