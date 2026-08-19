using System.Collections.Generic;
using System.Linq;

namespace Game.Battle
{
    /// <summary>
    /// Multi-step undo/redo over the battle's HP/MP/Column + roster (active vs. bench)
    /// state + log. One Point is captured after every turn the TurnOrder queue consumes
    /// (resolved action *or* a skipped turn -- see BattleController.RunBattle), so Cursor
    /// always equals exactly the number of TurnOrder.Next() calls needed to reach "about
    /// to act" for whichever point is current. That lets BattleController rebuild a fresh
    /// TurnOrder and fast-forward it by Cursor discarded Next() calls after an Undo/Redo,
    /// instead of having to snapshot TurnOrder's own internal queue (speeds don't change
    /// mid-battle in this slice, so that replay is deterministic).
    ///
    /// Column and active/bench membership are snapshotted alongside HP/MP because
    /// Formation.Compact (frontline succession), Reposition, and Sub in/out all mutate
    /// them mid-battle -- a plain HP/MP-only history would leave stale columns/roster
    /// membership behind after an undo.
    /// </summary>
    public class BattleHistory
    {
        class Point
        {
            public Dictionary<BattleUnit, (int hp, int mp, int column)> State;
            public List<BattleUnit> ActiveOrder;
            public List<BattleUnit> BenchOrder;
            public List<BattleLogEntry> LogSnapshot;
        }

        readonly List<Point> _points = new();
        int _cursor = -1;

        public bool CanUndo => _cursor > 0;
        public bool CanRedo => _cursor >= 0 && _cursor < _points.Count - 1;

        /// <summary>Number of turns resolved to reach the current point.</summary>
        public int Cursor => _cursor < 0 ? 0 : _cursor;

        /// <summary>Snapshots current unit/roster/log state as the point after the most
        /// recent turn. `active`/`bench` are BattleWorld.AllUnits/Bench (kept as plain
        /// lists here, not a BattleWorld reference, so this class stays testable without
        /// constructing a full BattleWorld). If the cursor isn't at the end (the player
        /// undid, then this is a *new* action rather than a Redo), the stale forward
        /// branch is discarded first -- standard undo/redo-stack semantics.</summary>
        public void Capture(List<BattleUnit> active, List<BattleUnit> bench, BattleLog log)
        {
            if (_cursor < _points.Count - 1)
                _points.RemoveRange(_cursor + 1, _points.Count - _cursor - 1);

            var state = new Dictionary<BattleUnit, (int, int, int)>();
            foreach (var unit in active.Concat(bench))
                state[unit] = (unit.CurrentHp, unit.CurrentMp, unit.Column);

            _points.Add(new Point
            {
                State = state,
                ActiveOrder = new List<BattleUnit>(active),
                BenchOrder = new List<BattleUnit>(bench),
                LogSnapshot = new List<BattleLogEntry>(log.Entries),
            });
            _cursor = _points.Count - 1;
        }

        public void Undo(List<BattleUnit> active, List<BattleUnit> bench, BattleLog log)
        {
            if (!CanUndo) return;
            _cursor--;
            Restore(active, bench, log);
        }

        public void Redo(List<BattleUnit> active, List<BattleUnit> bench, BattleLog log)
        {
            if (!CanRedo) return;
            _cursor++;
            Restore(active, bench, log);
        }

        void Restore(List<BattleUnit> active, List<BattleUnit> bench, BattleLog log)
        {
            var point = _points[_cursor];
            foreach (var kv in point.State)
            {
                kv.Key.CurrentHp = kv.Value.hp;
                kv.Key.CurrentMp = kv.Value.mp;
                kv.Key.Column = kv.Value.column;
            }
            active.Clear();
            active.AddRange(point.ActiveOrder);
            bench.Clear();
            bench.AddRange(point.BenchOrder);
            log.RestoreFrom(point.LogSnapshot);
        }
    }
}
