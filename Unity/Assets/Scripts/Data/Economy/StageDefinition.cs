using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// One playable stage. Boss stages sit every 10th index and unlock a new farmable
    /// material on first clear.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Economy/Stage", fileName = "Stage_")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string stageId;
        public string displayName;
        public string islandId;
        [Min(1)] public int stageNumber = 1;

        public GameMode mode = GameMode.Story;

        [Tooltip("Every 10th stage. Grants unlockFlagOnClear.")]
        public bool isBoss;

        [Header("Content")]
        public MapDefinition map;
        public DropTable drops;

        [Header("Progression")]
        [Tooltip("Flag set on first clear. Referenced by gated drop entries and unlocks.")]
        public string unlockFlagOnClear;

        [Tooltip("Flags that must all be set before this stage is playable.")]
        public List<string> requiredFlags = new();

        [Header("First-clear rewards")]
        public List<DropEntry> firstClearRewards = new();

        [Header("Recommendations")]
        [Min(1)] public int recommendedLevel = 1;
    }

    public enum GameMode { Story, Battle, GearStage, Endless, Idle }
}
