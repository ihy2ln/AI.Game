using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A battle-usable consumable (M13). Rank reuses the character Tier scale (F worst --
    /// SSS best, see Enums.cs) purely so both systems read the same way to a player --
    /// a potion's rank has no other connection to a character's rolled Tier. Potency per
    /// rank lives in Game.Battle.PotionCalculator, not here, so the formula stays in one
    /// place across every potion asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Economy/Potion", fileName = "Potion_")]
    public class PotionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string potionId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Effect")]
        public PotionKind kind;
        public Tier rank;

        [Tooltip("BattleInventorySlot.Count is clamped to this per the project owner's "
            + "'carry as much as 99' direction.")]
        [Min(1)] public int maxStack = 99;
    }
}
