using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// Pure C#, no MonoBehaviour/scene dependency -- testable headlessly.
    /// power * attack (or magic if usesMagic) vs defense/resistance, per the original
    /// vertical-slice brief. Ranged distance scaling and exact numbers are an open
    /// balance-pass item (FOUNDATION.md 1.4/11) -- RangedDistanceBonusPerColumn is a
    /// placeholder constant, not a tuned value.
    /// </summary>
    public static class DamageCalculator
    {
        public const float RangedDistanceBonusPerColumn = 0.15f;

        public static int ComputeDamage(BattleUnit attacker, BattleUnit target, SkillDefinition skill, int columnDistance)
        {
            var atkStats = attacker.Stats;
            var defStats = target.Stats;

            float offense = skill.usesMagic ? atkStats.magic : atkStats.attack;
            float defense = skill.usesMagic ? defStats.resistance : defStats.defense;

            float raw = skill.power * offense - defense;

            if (skill.isRanged && columnDistance > 1)
                raw *= 1f + RangedDistanceBonusPerColumn * (columnDistance - 1);

            return Mathf.Max(0, Mathf.RoundToInt(raw));
        }

        public static int ComputeHeal(BattleUnit caster, SkillDefinition skill)
        {
            float magic = caster.Stats.magic;
            return Mathf.Max(0, Mathf.RoundToInt(skill.power * magic));
        }

        /// <summary>Same formula as ComputeHeal -- kept as a separate method (not an
        /// alias) so a future balance pass can diverge MP-restore scaling from HP-heal
        /// scaling without an implicit coupling.</summary>
        public static int ComputeManaRestore(BattleUnit caster, SkillDefinition skill)
        {
            float magic = caster.Stats.magic;
            return Mathf.Max(0, Mathf.RoundToInt(skill.power * magic));
        }
    }
}
