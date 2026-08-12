using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>An owned piece of gear with rolled substats. Save data.</summary>
    [Serializable]
    public class EquipmentInstance
    {
        public string instanceId;
        public string equipmentId;

        [Tooltip("Seed used to roll substats. Stored so the roll reproduces across devices.")]
        public int rollSeed;

        public List<RolledSubstat> substats = new();

        [Min(0)] public int enhanceLevel;

        [Tooltip("Instance id of the unit holding this, empty when unequipped.")]
        public string equippedBy = "";

        public bool locked;
    }

    [Serializable]
    public struct RolledSubstat
    {
        public StatType stat;
        public int value;
    }
}
