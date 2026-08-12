using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Brown Dust 2-style fixed square targeting. Range is where the skill may be
    /// aimed; area is what it hits once aimed, relative to the target tile.
    /// Offsets are (laneDelta, columnDelta). Facing is a two-state axis because
    /// lane movement means units never face into or out of the screen.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Combat/Skill Pattern", fileName = "Pattern_")]
    public class SkillPattern : ScriptableObject
    {
        [Tooltip("Tiles the skill may target, relative to the caster.")]
        public List<Vector2Int> rangeOffsets = new();

        [Tooltip("Tiles hit, relative to the chosen target tile. Include (0,0) to hit the target.")]
        public List<Vector2Int> areaOffsets = new() { Vector2Int.zero };

        [Tooltip("If true, offsets flip on the column axis when the caster faces left.")]
        public bool mirrorOnFacing = true;

        public bool requiresLineOfSight;

        public IEnumerable<Vector2Int> GetRange(bool facingRight)
        {
            foreach (var o in rangeOffsets)
                yield return (mirrorOnFacing && !facingRight) ? new Vector2Int(o.x, -o.y) : o;
        }

        public IEnumerable<Vector2Int> GetArea(bool facingRight)
        {
            foreach (var o in areaOffsets)
                yield return (mirrorOnFacing && !facingRight) ? new Vector2Int(o.x, -o.y) : o;
        }
    }
}
