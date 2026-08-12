using System;
using UnityEngine;

namespace Game.Farm
{
    [Serializable]
    public class FarmObstacleType
    {
        public string id;
        public string label;
        public int requiredLevel = 1;
        public int xp = 1;
        public string tool = "hand";
        public bool blocksMovement = true;
        public bool blocksPlanting = true;
    }

    public enum FarmSoilKind
    {
        Untilled,
        Tilled
    }

    /// <summary>Tiny 2×2 aesthetic sandbox — 2.5D anime × HD pixel art.</summary>
    public static class FarmStarterMap
    {
        public const string Id = "farm_aesthetics_2x2";
        public const string DisplayName = "Aesthetic Sandbox";
        public const int Width = 2;
        public const int Height = 2;

        public static readonly Vector2Int PlayerStart = new(1, 1);

        public static FarmObstacleType[] ObstacleTypes => new[]
        {
            new FarmObstacleType { id = "weed", label = "Weed", requiredLevel = 1, xp = 6, tool = "scythe", blocksMovement = false },
            new FarmObstacleType { id = "rock", label = "Rock", requiredLevel = 2, xp = 12, tool = "pickaxe" },
            new FarmObstacleType { id = "tree", label = "Oak Tree", requiredLevel = 3, xp = 20, tool = "axe" },
        };

        /// <summary>Row-major from north (y=0). Empty string = clear tile.</summary>
        public static readonly string[,] Tiles =
        {
            { "tree", "weed" },
            { "rock", "" },
        };

        public static readonly FarmSoilKind[,] Soil =
        {
            { FarmSoilKind.Untilled, FarmSoilKind.Untilled },
            { FarmSoilKind.Untilled, FarmSoilKind.Tilled },
        };
    }
}
