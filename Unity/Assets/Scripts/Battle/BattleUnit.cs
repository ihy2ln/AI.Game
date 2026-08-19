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

        public void SpendMp(int amount) => CurrentMp = Mathf.Max(0, CurrentMp - amount);
        public void RestoreMp(int amount) => CurrentMp = Mathf.Min(MaxMp, CurrentMp + amount);

        /// <summary>Full restore -- not called by anything in the battle scene today.
        /// Provided as the hook a future farm/town "sleep to recover" system should call;
        /// there's no persistence layer connecting battle party state to the farm scene
        /// yet, so wiring an actual sleep mechanic is out of scope until that exists.</summary>
        public void RestoreMpFull() => CurrentMp = MaxMp;

        /// <summary>Between-battle partial MP recovery: 25%-50% of the *missing* amount,
        /// rolled per unit. Deliberately distinct from HP, which does not recover between
        /// this slice's two maps (the carried wound is the point, per BattleWorld's
        /// carry-over doc) -- an arbitrary number by design (project owner direction), not
        /// a tuned value. Called from BattleWorld when a carried-over roster loads map 2.</summary>
        public void RecoverMpAfterBattle()
        {
            int missing = MaxMp - CurrentMp;
            if (missing <= 0) return;
            float fraction = UnityEngine.Random.Range(0.25f, 0.5f);
            RestoreMp(Mathf.RoundToInt(missing * fraction));
        }
    }
}
