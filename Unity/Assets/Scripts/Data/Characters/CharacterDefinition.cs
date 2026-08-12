using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>The authored template for a unit. Rolled values live on CharacterInstance.</summary>
    [CreateAssetMenu(menuName = "Game/Character/Character", fileName = "Char_")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;
        public string displayName;
        public ClassType classType;
        public ElementType element;
        public Age age;
        [TextArea] public string bio;

        [Header("Base stats (before tier multiplier and variance)")]
        public StatBlock baseStats;

        [Header("Movement - fixed, never rolled")]
        [Min(1)] public int movePoints = 4;

        [Tooltip("Max climbable height difference per step. Exceeding it makes the edge ILLEGAL, not expensive.")]
        [Min(0)] public int jump = 1;

        [Tooltip("MP cost to change lane. Default 2. Skirmisher archetypes lower this.")]
        [Min(1)] public int costLateral = 2;

        [Tooltip("MP cost to move along a lane.")]
        [Min(1)] public int costForward = 1;

        [Tooltip("Extra MP per level of elevation change.")]
        [Min(0)] public int costPerHeightLevel = 1;

        [Header("Skills - 1 fixed, 2 rolled")]
        public SkillDefinition standardSkill;

        [Tooltip("Leave empty to roll from the global pool matching classType.")]
        public List<SkillDefinition> classSkillPool = new();

        [Tooltip("Leave empty to roll from the global pool matching element.")]
        public List<SkillDefinition> elementSkillPool = new();

        [Header("Growth")]
        [Tooltip("Stat gain per level as a fraction of base.")]
        public float growthPerLevel = 0.06f;

        [Header("Presentation")]
        public ClipSet clips;
        public Sprite portrait;
        public Sprite pixelSprite32;

        [Tooltip("Leave empty to use a 3x nearest-neighbour upscale of pixelSprite32.")]
        public Sprite pixelSprite96;

        [Header("Dialogue")]
        [TextArea(4, 12)]
        [Tooltip("Static personality block injected into the LLM system prompt.")]
        public string personaPrompt;
    }
}
