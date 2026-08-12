using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A cooked dish granting a limited-time combat buff. Dishes should be strong enough
    /// that cooking is the correct play before hard content, with selling produce as the
    /// fallback for surplus.
    ///
    /// Requires the Kitchen building.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Farm/Dish", fileName = "Dish_")]
    public class DishDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string dishId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Recipe")]
        public List<DropEntry> ingredients = new();
        [Min(0)] public int cookTimeSeconds = 60;

        [Tooltip("Kitchen level required to cook this.")]
        [Min(1)] public int requiredKitchenLevel = 1;

        [Header("Buff")]
        [Tooltip("Flat stat bonus applied to the whole party.")]
        public StatBlock flatBonus;

        [Tooltip("Percentage bonus applied to the whole party. 0.1 = +10%.")]
        public float percentBonus;

        [Tooltip("Real-time duration in seconds. Use battleDuration instead for per-run buffs.")]
        [Min(0)] public int durationSeconds = 1800;

        [Tooltip("Alternative: expires after N battles. 0 disables.")]
        [Min(0)] public int durationBattles = 0;

        [Tooltip("Only one dish buff may be active at a time when true.")]
        public bool exclusive = true;
    }
}
