using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Runtime state of one farm plot. Save data.
    ///
    /// Progress is stored as both a timestamp and a battle counter so a crop can use
    /// either trigger. Whichever completes first harvests.
    /// </summary>
    [Serializable]
    public class FarmPlotState
    {
        public string plotId;
        public FarmPlotType plotType;

        [Tooltip("Empty when the plot is fallow.")]
        public string cropId = "";

        [Tooltip("Unix seconds when planting occurred. Device clock; manipulation accepted.")]
        public long plantedAtUnix;

        [Tooltip("Battle count at planting time. Compared against the running total.")]
        public int plantedAtBattleCount;

        public bool watered;
        public string fertilizerId = "";

        [Tooltip("Set once harvested if the crop regrows.")]
        public int harvestsTaken;
    }
}
