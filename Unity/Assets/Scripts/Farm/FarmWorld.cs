using System.Collections.Generic;
using UnityEngine;

namespace Game.Farm
{
    public class FarmPlayerState
    {
        public int X;
        public int Y;
        public int Level = 1;
        public int Xp;

        static readonly int[] LevelCurve = { 0, 15, 40, 80, 140, 220 };

        public int? XpToNext()
        {
            if (Level >= LevelCurve.Length) return null;
            return LevelCurve[Level];
        }

        public bool ApplyXp(int amount, out int levelsGained)
        {
            levelsGained = 0;
            Xp += amount;
            while (true)
            {
                var need = XpToNext();
                if (need == null || Xp < need.Value) break;
                Xp -= need.Value;
                Level++;
                levelsGained++;
            }
            return levelsGained > 0;
        }
    }

    public class FarmWorld
    {
        public int Width { get; }
        public int Height { get; }
        public string DisplayName { get; }
        public FarmPlayerState Player { get; } = new();
        public int ClearedCount { get; private set; }

        readonly string[,] _tiles;
        readonly FarmSoilKind[,] _soil;
        readonly Dictionary<string, FarmObstacleType> _types;

        public FarmWorld()
        {
            Width = FarmStarterMap.Width;
            Height = FarmStarterMap.Height;
            DisplayName = FarmStarterMap.DisplayName;
            _tiles = (string[,])FarmStarterMap.Tiles.Clone();
            _soil = (FarmSoilKind[,])FarmStarterMap.Soil.Clone();
            _types = new Dictionary<string, FarmObstacleType>();
            foreach (var t in FarmStarterMap.ObstacleTypes)
                _types[t.id] = t;

            Player.X = FarmStarterMap.PlayerStart.x;
            Player.Y = FarmStarterMap.PlayerStart.y;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public string GetObstacleId(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            var id = _tiles[y, x];
            return string.IsNullOrEmpty(id) ? null : id;
        }

        public FarmObstacleType GetObstacle(int x, int y)
        {
            var id = GetObstacleId(x, y);
            return id != null && _types.TryGetValue(id, out var t) ? t : null;
        }

        public FarmSoilKind GetSoil(int x, int y) => _soil[y, x];

        public bool BlocksMovement(int x, int y)
        {
            var obs = GetObstacle(x, y);
            return obs != null && obs.blocksMovement;
        }

        public bool TryMove(int dx, int dy)
        {
            var nx = Player.X + dx;
            var ny = Player.Y + dy;
            if (!InBounds(nx, ny) || BlocksMovement(nx, ny)) return false;
            Player.X = nx;
            Player.Y = ny;
            return true;
        }

        public bool CanClear(int x, int y, out string reason)
        {
            var obs = GetObstacle(x, y);
            if (obs == null)
            {
                reason = "Nothing to clear.";
                return false;
            }
            if (Player.Level < obs.requiredLevel)
            {
                reason = $"Need Farm Lv.{obs.requiredLevel} (you are Lv.{Player.Level}).";
                return false;
            }
            reason = $"Clear with {obs.tool}.";
            return true;
        }

        public bool TryClear(int x, int y, out FarmObstacleType cleared, out int xpGained, out int levelsGained, out string message)
        {
            cleared = null;
            xpGained = 0;
            levelsGained = 0;
            if (!CanClear(x, y, out message)) return false;

            var id = GetObstacleId(x, y);
            cleared = _types[id];
            _tiles[y, x] = "";
            ClearedCount++;
            xpGained = cleared.xp;
            Player.ApplyXp(xpGained, out levelsGained);
            message = $"Cleared {cleared.label} (+{xpGained} XP).";
            return true;
        }

        public int RemainingObstacles()
        {
            var n = 0;
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (!string.IsNullOrEmpty(_tiles[y, x])) n++;
            return n;
        }
    }
}
