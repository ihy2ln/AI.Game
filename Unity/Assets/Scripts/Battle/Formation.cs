using System.Collections.Generic;
using System.Linq;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// Keeps a faction's formation gap-free after a death: "the next unit in line
    /// becomes frontline." Pure column bookkeeping -- BattleVisuals.ReflowFormation is
    /// what actually tweens the sprites to their new dock positions afterward.
    /// </summary>
    public static class Formation
    {
        public const int PlayerFrontColumn = 2;
        public const int EnemyFrontColumn = 3;

        /// <summary>Reassigns contiguous columns to every living unit of the given
        /// faction, anchored at that faction's front rank, preserving relative
        /// front-to-back order. No-op if there's no gap to close.</summary>
        public static void Compact(IEnumerable<BattleUnit> allUnits, Faction faction)
        {
            if (faction != Faction.Player && faction != Faction.Enemy) return;

            bool isPlayer = faction == Faction.Player;
            var alive = allUnits.Where(u => u.Faction == faction && u.IsAlive).ToList();
            alive.Sort((a, b) => isPlayer ? b.Column.CompareTo(a.Column) : a.Column.CompareTo(b.Column));

            int anchor = isPlayer ? PlayerFrontColumn : EnemyFrontColumn;
            for (int i = 0; i < alive.Count; i++)
                alive[i].Column = isPlayer ? anchor - i : anchor + i;
        }
    }
}
