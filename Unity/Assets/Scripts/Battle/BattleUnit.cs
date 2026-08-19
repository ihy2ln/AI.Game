using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// A unit's runtime battle state: rolled definition + instance, current HP/MP,
    /// and its column in the side-view formation (lane is always 0 for this slice --
    /// see BattleAssetBuilder's "side-view formation" comment for why column alone
    /// is enough to express a Darkest Dungeon-style rank).
    /// </summary>
    public class BattleUnit
    {
        public readonly CharacterDefinition Definition;
        public readonly CharacterInstance Instance;
        public readonly Faction Faction;
        public readonly bool FacingRight;

        public int Column;
        public int CurrentHp;
        public int CurrentMp;

        public bool IsAlive => CurrentHp > 0;
        public StatBlock Stats => Instance.EffectiveStats(Definition);
        public int MaxMp => Definition.maxMp;

        public BattleUnit(CharacterDefinition def, CharacterInstance instance, Faction faction, int column, bool facingRight)
        {
            Definition = def;
            Instance = instance;
            Faction = faction;
            Column = column;
            FacingRight = facingRight;
            CurrentHp = Stats.hp;
            CurrentMp = Definition.maxMp;
        }

        public void ApplyDamage(int amount) => CurrentHp = Mathf.Max(0, CurrentHp - amount);
        public void ApplyHeal(int amount) => CurrentHp = Mathf.Min(Stats.hp, CurrentHp + amount);
    }
}
