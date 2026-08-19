using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// A skill. Units receive three at recruitment:
    ///   1 standard - fixed, defined on the character
    ///   1 class    - rolled from the class pool
    ///   1 element  - rolled from the element pool
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Character/Skill", fileName = "Skill_")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string skillId;
        public string displayName;
        [TextArea] public string description;

        [Tooltip("Which roll pool this belongs to. Standard skills are assigned directly, not rolled.")]
        public SkillPool pool = SkillPool.Class;

        [Tooltip("Used when pool == Class.")]
        public ClassType classType;

        [Tooltip("Used when pool == Element.")]
        public ElementType element;

        [Header("Targeting")]
        public SkillPattern pattern;

        [Header("Cost and effect")]
        [Min(0)] public int mpCost;
        [Min(0)] public int cooldown;
        [Tooltip("Multiplier against the relevant offensive stat.")]
        public float power = 1f;
        public bool usesMagic;

        [Header("Elevation interaction")]
        [Tooltip("Ranged skills gain the downward bonus. Melee skills take the upward penalty.")]
        public bool isRanged;

        [Tooltip("True for heals/buffs: TargetResolver offers the caster's own faction instead of the opposing one.")]
        public bool targetsAllies;

        [Tooltip("Only meaningful when targetsAllies is true: restores the target's MP "
            + "(via power * caster magic, same formula as a heal) instead of HP. "
            + "BattleController.ResolveAction branches on this within the targetsAllies path.")]
        public bool restoresMana;

        [Header("Presentation")]
        [Tooltip("Key into the character's ClipSet.")]
        public string clipKey;

        [Min(0f)] public float rollWeight = 1f;
    }
}
