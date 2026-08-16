using System.Collections.Generic;
using System.Linq;

namespace Game.Battle
{
    /// <summary>Speed-sorted turn queue. Re-sorts alive units at the start of every
    /// round (classic JRPG "highest speed acts first each round", not a continuous
    /// ATB rail -- simplest thing that gives a readable, deterministic order).</summary>
    public class TurnOrder
    {
        readonly List<BattleUnit> _allUnits;
        Queue<BattleUnit> _round = new();

        public int RoundNumber { get; private set; }

        public TurnOrder(IEnumerable<BattleUnit> allUnits)
        {
            _allUnits = allUnits.ToList();
        }

        /// <summary>Next acting unit, starting a new round (re-sorted by current speed)
        /// whenever the previous round runs out. Returns null if no unit is alive.</summary>
        public BattleUnit Next()
        {
            while (true)
            {
                if (_round.Count == 0)
                {
                    var alive = _allUnits.Where(u => u.IsAlive)
                        .OrderByDescending(u => u.Stats.speed)
                        .ToList();
                    if (alive.Count == 0) return null;
                    RoundNumber++;
                    _round = new Queue<BattleUnit>(alive);
                }

                var unit = _round.Dequeue();
                if (unit.IsAlive) return unit;
                // Dequeued a unit that died mid-round -- try the next one instead of
                // handing back a corpse.
            }
        }
    }
}
