using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Fixed per-stage drop table. Deliberately not randomised per-session and not
    /// rotating — players plan farming routes against known tables.
    ///
    /// Boss clears unlock new materials that are added retroactively to earlier tables
    /// via unlockGatedEntries, which keeps old stages relevant instead of abandoned.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Economy/Drop Table", fileName = "Drops_")]
    public class DropTable : ScriptableObject
    {
        public List<DropEntry> guaranteed = new();
        public List<DropEntry> chance = new();

        [Tooltip("Entries that only appear once their unlockFlag has been earned (usually a boss clear).")]
        public List<GatedDropEntry> unlockGatedEntries = new();

        [Header("Dungeon tiering")]
        [Tooltip("Dungeon level. Higher levels shift the whole table upward in tier.")]
        [Min(1)] public int dungeonLevel = 1;
    }

    [Serializable]
    public struct DropEntry
    {
        public MaterialDefinition material;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;

        [Tooltip("0-1. Ignored for guaranteed entries.")]
        [Range(0f, 1f)] public float chance;
    }

    [Serializable]
    public struct GatedDropEntry
    {
        public DropEntry entry;
        [Tooltip("Progression flag that must be set before this entry becomes active.")]
        public string unlockFlag;
    }
}
