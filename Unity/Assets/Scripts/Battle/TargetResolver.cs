using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// Pure C#, no MonoBehaviour/scene dependency -- applies a skill's SkillPattern
    /// range from a caster's column to find valid, occupied targets. Lane is always 0
    /// in this side-view slice (see BattleUnit), so only the column axis matters.
    /// </summary>
    public static class TargetResolver
    {
        /// <summary>Units the skill could legally be aimed at, respecting faction
        /// (targetsAllies flips which side is offered) and pattern range.</summary>
        public static List<BattleUnit> GetValidTargets(BattleUnit caster, SkillDefinition skill, IReadOnlyList<BattleUnit> allUnits)
        {
            var reachableColumns = new HashSet<int>(
                skill.pattern.GetRange(caster.FacingRight).Select(o => caster.Column + o.y));

            var targetFaction = skill.targetsAllies ? caster.Faction : Opposing(caster.Faction);

            return allUnits
                .Where(u => u.IsAlive && u.Faction == targetFaction && reachableColumns.Contains(u.Column))
                .ToList();
        }

        /// <summary>Tiles actually hit once a target tile is chosen (area, not range) --
        /// area offsets are relative to the target, same facing-mirror rule as range.</summary>
        public static List<BattleUnit> GetAreaTargets(BattleUnit caster, SkillDefinition skill, int targetColumn, IReadOnlyList<BattleUnit> allUnits)
        {
            var hitColumns = new HashSet<int>(
                skill.pattern.GetArea(caster.FacingRight).Select(o => targetColumn + o.y));
            var targetFaction = skill.targetsAllies ? caster.Faction : Opposing(caster.Faction);

            return allUnits
                .Where(u => u.IsAlive && u.Faction == targetFaction && hitColumns.Contains(u.Column))
                .ToList();
        }

        public static int ColumnDistance(BattleUnit a, BattleUnit b) => Mathf.Abs(a.Column - b.Column);

        static Faction Opposing(Faction f) => f == Faction.Player ? Faction.Enemy : Faction.Player;
    }
}
