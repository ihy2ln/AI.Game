using System.Collections.Generic;

namespace Game.Battle
{
    public readonly struct BattleLogEntry
    {
        public readonly int Round;
        public readonly string Text;

        public BattleLogEntry(int round, string text)
        {
            Round = round;
            Text = text;
        }
    }

    /// <summary>Plain C# turn log -- ordered record of every line BattleController would
    /// otherwise only expose as a single transient LastAction string. Append-only during
    /// normal play; BattleHistory replaces its contents wholesale on undo/redo (see
    /// RestoreFrom -- TruncateTo alone can't support redo since it deletes entries for
    /// good, and redo needs to bring them back).</summary>
    public class BattleLog
    {
        readonly List<BattleLogEntry> _entries = new();

        public IReadOnlyList<BattleLogEntry> Entries => _entries;

        public void Add(int round, string text) => _entries.Add(new BattleLogEntry(round, text));

        public void TruncateTo(int count) => _entries.RemoveRange(count, _entries.Count - count);

        /// <summary>Replaces the entire log with a prior snapshot -- unlike TruncateTo,
        /// this can move forward (redo) as well as backward (undo).</summary>
        public void RestoreFrom(IReadOnlyList<BattleLogEntry> snapshot)
        {
            _entries.Clear();
            _entries.AddRange(snapshot);
        }
    }
}
