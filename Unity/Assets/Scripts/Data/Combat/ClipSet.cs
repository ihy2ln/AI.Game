using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Data
{
    /// <summary>
    /// A character's FMV library. Clips are chroma-keyed so one set serves both the
    /// Advance Wars panel layout and the Brown Dust 2 in-place layout — never bake
    /// backgrounds in, or the library multiplies by terrain count and loses cross-view reuse.
    ///
    /// impactFrames drive damage-number timing: pre-rendered video cannot signal the game
    /// when a hit lands, so the markers must be authored alongside the clip.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Combat/Clip Set", fileName = "Clips_")]
    public class ClipSet : ScriptableObject
    {
        public const string KeyIdle  = "idle";
        public const string KeyHit   = "hit";
        public const string KeyDeath = "death";

        public List<ClipEntry> clips = new();

        public ClipEntry Get(string key) => clips.Find(c => c.key == key);
    }

    [Serializable]
    public class ClipEntry
    {
        [Tooltip("Matches SkillDefinition.clipKey. Reserved keys: idle, hit, death.")]
        public string key;

        public VideoClip clip;

        [Tooltip("Frames at which damage numbers fire. Multi-hit skills list several.")]
        public List<int> impactFrames = new();

        [Min(1)] public int frameRate = 24;

        [Tooltip("Chroma colour to key out. Must match the generation background exactly.")]
        public Color chromaKey = Color.green;

        [Range(0f, 1f)] public float chromaTolerance = 0.25f;

        public bool loop;

        public float ImpactTime(int index) =>
            (index >= 0 && index < impactFrames.Count) ? impactFrames[index] / (float)frameRate : 0f;
    }
}
