using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Battle;

namespace Game.Tests
{
    static class BattleTestHelpers
    {
        public static BattleUnit MakeUnit(StatBlock stats, Faction faction, int column, bool facingRight)
        {
            var def = ScriptableObject.CreateInstance<CharacterDefinition>();
            def.characterId = "test_" + faction;
            def.displayName = "Test " + faction;
            def.baseStats = stats;

            var instance = new CharacterInstance
            {
                instanceId = "test-instance",
                characterId = def.characterId,
                rolledStats = stats,
                level = 1,
            };

            return new BattleUnit(def, instance, faction, column, facingRight);
        }

        public static SkillDefinition MakeSkill(SkillPattern pattern, bool isRanged = false, bool usesMagic = false,
            float power = 1f, bool targetsAllies = false)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.pattern = pattern;
            skill.isRanged = isRanged;
            skill.usesMagic = usesMagic;
            skill.power = power;
            skill.targetsAllies = targetsAllies;
            return skill;
        }

        public static SkillPattern MakePattern(IEnumerable<Vector2Int> rangeOffsets, bool mirrorOnFacing = true)
        {
            var pattern = ScriptableObject.CreateInstance<SkillPattern>();
            pattern.rangeOffsets = new List<Vector2Int>(rangeOffsets);
            pattern.areaOffsets = new List<Vector2Int> { Vector2Int.zero };
            pattern.mirrorOnFacing = mirrorOnFacing;
            return pattern;
        }
    }
}
