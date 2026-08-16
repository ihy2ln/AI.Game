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
    /// normal play; BattleHistory truncates it on undo.</summary>
    public class BattleLog
    {
        readonly List<BattleLogEntry> _entries = new();

        public IReadOnlyList<BattleLogEntry> Entries => _entries;

        public void Add(int round, string text) => _entries.Add(new BattleLogEntry(round, text));

        public void TruncateTo(int count) => _entries.RemoveRange(count, _entries.Count - count);
    }
}
