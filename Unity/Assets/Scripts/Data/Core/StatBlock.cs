using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Combat stats that roll with variance at acquisition.
    /// Movement stats (movePoints, jump) deliberately live on CharacterDefinition
    /// and do NOT roll — random movement range would make tactical planning unreadable.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public int hp;
        public int attack;
        public int defense;
        public int magic;
        public int resistance;
        public int speed;

        public static StatBlock operator *(StatBlock s, float m) => new StatBlock
        {
            hp         = Mathf.RoundToInt(s.hp * m),
            attack     = Mathf.RoundToInt(s.attack * m),
            defense    = Mathf.RoundToInt(s.defense * m),
            magic      = Mathf.RoundToInt(s.magic * m),
            resistance = Mathf.RoundToInt(s.resistance * m),
            speed      = Mathf.RoundToInt(s.speed * m)
        };

        public static StatBlock operator +(StatBlock a, StatBlock b) => new StatBlock
        {
            hp         = a.hp + b.hp,
            attack     = a.attack + b.attack,
            defense    = a.defense + b.defense,
            magic      = a.magic + b.magic,
            resistance = a.resistance + b.resistance,
            speed      = a.speed + b.speed
        };

        /// <summary>Independent random roll per stat. Caller supplies a seeded RNG.</summary>
        public StatBlock RollVariance(System.Random rng, float variancePct)
        {
            if (variancePct <= 0f) return this;
            float Roll() => 1f + ((float)rng.NextDouble() * 2f - 1f) * variancePct;
            return new StatBlock
            {
                hp         = Mathf.RoundToInt(hp * Roll()),
                attack     = Mathf.RoundToInt(attack * Roll()),
                defense    = Mathf.RoundToInt(defense * Roll()),
                magic      = Mathf.RoundToInt(magic * Roll()),
                resistance = Mathf.RoundToInt(resistance * Roll()),
                speed      = Mathf.RoundToInt(speed * Roll())
            };
        }
    }
}
