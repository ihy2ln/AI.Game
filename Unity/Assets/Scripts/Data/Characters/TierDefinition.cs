using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Per-tier roll behaviour, one asset per tier.
    ///
    /// Spec: F-A fluctuate +/-15%. S-SSS receive a stat bonus.
    /// Both fields exist on every tier so the split is tunable rather than hardcoded —
    /// you can decide later whether S+ also rolls variance or gets a flat bonus instead.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Character/Tier Definition", fileName = "Tier_")]
    public class TierDefinition : ScriptableObject
    {
        public Tier tier;

        [Tooltip("Flat multiplier applied to base stats before variance. 1.0 = no bonus.")]
        [Min(0f)] public float statMultiplier = 1f;

        [Tooltip("Random spread per stat. 0.15 = +/-15%. Set 0 for no variance.")]
        [Range(0f, 1f)] public float variance = 0.15f;

        [Tooltip("Relative weight when rolling a tier at summon time.")]
        [Min(0f)] public float summonWeight = 1f;

        public Color displayColor = Color.white;
    }
}
