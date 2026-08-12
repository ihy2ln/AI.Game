using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>Set bonuses that activate at piece thresholds.</summary>
    [CreateAssetMenu(menuName = "Game/Economy/Equipment Set", fileName = "Set_")]
    public class EquipmentSetDefinition : ScriptableObject
    {
        public string setId;
        public string displayName;
        public List<SetTier> tiers = new();
    }

    [System.Serializable]
    public struct SetTier
    {
        [Min(2)] public int piecesRequired;
        public StatBlock bonus;
        [TextArea] public string description;
    }
}
