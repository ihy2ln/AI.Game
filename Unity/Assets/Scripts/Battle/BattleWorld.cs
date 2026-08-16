using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// Loads the battle-slice ScriptableObjects (built by BattleAssetBuilder into
    /// Resources/Battle/) and rolls the 6 units for one battle. Plain C# like
    /// FarmWorld -- no scene dependency.
    /// </summary>
    public class BattleWorld
    {
        public MapDefinition Map { get; }
        public List<BattleUnit> AllUnits { get; } = new();

        public IEnumerable<BattleUnit> PlayerUnits => AllUnits.Where(u => u.Faction == Faction.Player);
        public IEnumerable<BattleUnit> EnemyUnits => AllUnits.Where(u => u.Faction == Faction.Enemy);

        public bool PlayerDefeated => PlayerUnits.Any() && PlayerUnits.All(u => !u.IsAlive);
        public bool EnemyDefeated => EnemyUnits.Any() && EnemyUnits.All(u => !u.IsAlive);
        public bool IsOver => PlayerDefeated || EnemyDefeated;

        public bool LoadedOk { get; }

        // Mirrors the enemy formation built by BattleAssetBuilder: front-line melee
        // adjacent to the enemy's front line (column 2 vs 3), support/ranged behind.
        static readonly (string unitId, int column)[] PlayerFormation =
        {
            ("player_ranged", 0),
            ("player_support", 1),
            ("player_melee", 2),
        };

        public BattleWorld()
        {
            Map = Resources.Load<MapDefinition>("Battle/Maps/Map_BattleSlice1v1Formation");
            var tier = Resources.Load<TierDefinition>("Battle/Tiers/Tier_Standard");

            if (Map == null || tier == null)
            {
                Debug.LogError("[AI.Game] Battle data assets missing -- run "
                    + "AI.Game > Battle > Build Assets From Manifest first.");
                LoadedOk = false;
                return;
            }

            int seed = 0;
            var noRollPool = new List<SkillDefinition>(); // only standardSkill is used in this slice

            foreach (var (unitId, column) in PlayerFormation)
            {
                var def = Resources.Load<CharacterDefinition>($"Battle/Characters/Char_{unitId}");
                if (def == null)
                {
                    Debug.LogWarning($"[AI.Game] Missing CharacterDefinition for {unitId}");
                    continue;
                }
                var instance = CharacterFactory.Create(def, tier, noRollPool, seed++);
                AllUnits.Add(new BattleUnit(def, instance, Faction.Player, column, facingRight: true));
            }

            foreach (var placement in Map.enemies)
            {
                if (placement.character == null) continue;
                var placementTier = placement.tier != null ? placement.tier : tier;
                var instance = CharacterFactory.Create(placement.character, placementTier, noRollPool, seed++);
                AllUnits.Add(new BattleUnit(placement.character, instance, Faction.Enemy, placement.position.y, facingRight: false));
            }

            LoadedOk = AllUnits.Count > 0;
        }
    }
}
