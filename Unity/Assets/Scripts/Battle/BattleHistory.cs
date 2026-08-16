using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// Multi-step undo/redo over the battle's HP/MP + log state. One Point is captured
    /// after every turn the TurnOrder queue consumes (resolved action *or* a skipped
    /// turn -- see BattleController.RunBattle), so Cursor always equals exactly the
    /// number of TurnOrder.Next() calls needed to reach "about to act" for whichever
    /// point is current. That lets BattleController rebuild a fresh TurnOrder and
    /// fast-forward it by Cursor discarded Next() calls after an Undo/Redo, instead of
    /// having to snapshot TurnOrder's own internal queue (speeds don't change mid-battle
    /// in this slice, so that replay is deterministic).
    /// </summary>
    public class BattleHistory
    {
        class Point
        {
            public Dictionary<BattleUnit, (int hp, int mp)> State;
            public List<BattleLogEntry> LogSnapshot;
        }

        readonly List<Point> _points = new();
        int _cursor = -1;

        public bool CanUndo => _cursor > 0;
        public bool CanRedo => _cursor >= 0 && _cursor < _points.Count - 1;

        /// <summary>Number of turns resolved to reach the current point.</summary>
        public int Cursor => _cursor < 0 ? 0 : _cursor;

        /// <summary>Snapshots current unit/log state as the point after the most recent
        /// turn. If the cursor isn't at the end (the player undid, then this is a *new*
        /// action rather than a Redo), the stale forward branch is discarded first --
        /// standard undo/redo-stack semantics.</summary>
        public void Capture(IEnumerable<BattleUnit> units, BattleLog log)
        {
            if (_cursor < _points.Count - 1)
                _points.RemoveRange(_cursor + 1, _points.Count - _cursor - 1);

            var state = new Dictionary<BattleUnit, (int, int)>();
            foreach (var unit in units) state[unit] = (unit.CurrentHp, unit.CurrentMp);

            _points.Add(new Point { State = state, LogSnapshot = new List<BattleLogEntry>(log.Entries) });
            _cursor = _points.Count - 1;
        }

        public void Undo(IEnumerable<BattleUnit> units, BattleLog log)
        {
            if (!CanUndo) return;
            _cursor--;
            Restore(units, log);
        }

        public void Redo(IEnumerable<BattleUnit> units, BattleLog log)
        {
            if (!CanRedo) return;
            _cursor++;
            Restore(units, log);
        }

        void Restore(IEnumerable<BattleUnit> units, BattleLog log)
        {
            var point = _points[_cursor];
            foreach (var unit in units)
            {
                if (!point.State.TryGetValue(unit, out var hpMp)) continue;
                unit.CurrentHp = hpMp.hp;
                unit.CurrentMp = hpMp.mp;
            }
            log.RestoreFrom(point.LogSnapshot);
        }
    }
}
