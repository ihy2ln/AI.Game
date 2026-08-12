using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>Runtime state of a placed building. Save data.</summary>
    [Serializable]
    public class BuildingState
    {
        public string stateId;
        public string buildingId;
        public string districtId;

        [Min(1)] public int level = 1;

        [Tooltip("Unix seconds when construction or upgrade completes. 0 when idle.")]
        public long buildCompletesAtUnix;

        public bool IsUnderConstruction => buildCompletesAtUnix > 0;

        public List<QueuedProduction> queue = new();
    }

    [Serializable]
    public class QueuedProduction
    {
        public string recipeId;
        public long completesAtUnix;
        [Min(1)] public int quantity = 1;
    }
}
