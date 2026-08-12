using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A town district. Buildings are placed within districts, and districts gate which
    /// building types may be built and how many.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Town/District", fileName = "District_")]
    public class DistrictDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string districtId;
        public string displayName;
        public DistrictType type;
        [TextArea] public string description;

        [Header("Unlock")]
        public List<DropEntry> unlockCost = new();
        [Tooltip("Progression flags required before this district can be unlocked.")]
        public List<string> requiredFlags = new();

        [Header("Capacity")]
        [Min(1)] public int buildingSlots = 4;

        [Tooltip("Building types permitted here. Empty allows any.")]
        public List<BuildingDefinition> allowedBuildings = new();

        [Header("District bonus")]
        [Tooltip("Applied to all buildings in this district. 0.1 = 10% faster queues.")]
        [Range(0f, 0.9f)] public float queueSpeedBonus;
    }
}
