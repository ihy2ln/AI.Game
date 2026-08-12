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

    /// <summary>Authoring data for the starter 4×4 farm. Code-first (no SO wiring required).</summary>
    public static class FarmStarterMap
    {
        public const string Id = "farm_starter_4x4";
        public const string DisplayName = "Starter Plot";
        public const int Width = 4;
        public const int Height = 4;

        public static readonly Vector2Int PlayerStart = new(0, 3);

        public static FarmObstacleType[] ObstacleTypes => new[]
        {
            new FarmObstacleType { id = "weed", label = "Weed", requiredLevel = 1, xp = 4, tool = "scythe", blocksMovement = false },
            new FarmObstacleType { id = "bush", label = "Thorn Bush", requiredLevel = 2, xp = 8, tool = "scythe" },
            new FarmObstacleType { id = "stump", label = "Tree Stump", requiredLevel = 2, xp = 12, tool = "axe" },
            new FarmObstacleType { id = "tree", label = "Oak Tree", requiredLevel = 3, xp = 20, tool = "axe" },
            new FarmObstacleType { id = "rock", label = "Rock", requiredLevel = 2, xp = 10, tool = "pickaxe" },
            new FarmObstacleType { id = "boulder", label = "Large Boulder", requiredLevel = 4, xp = 35, tool = "pickaxe" },
        };

        /// <summary>Row-major from north (y=0). Empty string = clear tile.</summary>
        public static readonly string[,] Tiles =
        {
            { "tree", "weed", "rock", "bush" },
            { "weed", "boulder", "stump", "tree" },
            { "rock", "weed", "", "weed" },
            { "stump", "bush", "weed", "" },
        };

        public static readonly FarmSoilKind[,] Soil =
        {
            { FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Untilled },
            { FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Untilled },
            { FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Tilled, FarmSoilKind.Untilled },
            { FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Untilled, FarmSoilKind.Tilled },
        };
    }
}
