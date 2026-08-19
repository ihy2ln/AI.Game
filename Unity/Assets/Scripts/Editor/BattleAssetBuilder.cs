#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Game.Data;

namespace Game.EditorTools
{
    /// <summary>
    /// Builds ClipSet/SkillPattern/SkillDefinition/CharacterDefinition/MapDefinition
    /// ScriptableObjects for the battle vertical slice from
    /// Tools/ComfyUI/manifest.export.json (a JSON mirror of manifest.yaml -- this
    /// project has no YAML parser or Newtonsoft.Json package, so generate.py writes
    /// the JSON copy on every run; see Tools/ComfyUI/generate.py export_manifest_json).
    ///
    /// Idempotent: re-running loads and updates existing assets in place rather than
    /// duplicating them, per the M2 brief. Missing generated art is not an error --
    /// fields are just left null and a warning is logged, so this can run before every
    /// asset exists and fill in the rest on a later run (never let missing art block
    /// the pipeline).
    /// </summary>
    public static class BattleAssetBuilder
    {
        const string OutDir = "Assets/Resources/Battle";

        [MenuItem("AI.Game/Battle/Build Assets From Manifest")]
        public static void Build()
        {
            var export = LoadManifestExport();
            if (export == null) return;

            string unityRelRoot = export.outputRoot.StartsWith("Unity/")
                ? export.outputRoot.Substring("Unity/".Length)
                : export.outputRoot;

            var byType = export.assets.GroupBy(a => a.type).ToDictionary(g => g.Key, g => g.ToList());
            var spritesByUnit = GetOrEmpty(byType, "sprite").ToDictionary(a => a.unit_id);
            var portraitsByUnit = GetOrEmpty(byType, "portrait").ToDictionary(a => a.unit_id);
            var clipsById = GetOrEmpty(byType, "clip").ToDictionary(a => a.id);
            var backgroundAsset = GetOrEmpty(byType, "background").FirstOrDefault();

            Directory.CreateDirectory(OutDir + "/Tiers");
            Directory.CreateDirectory(OutDir + "/Patterns");
            Directory.CreateDirectory(OutDir + "/Skills");
            Directory.CreateDirectory(OutDir + "/Clips");
            Directory.CreateDirectory(OutDir + "/Characters");
            Directory.CreateDirectory(OutDir + "/Maps");

            var tier = BuildTier();

            // Side-view (Darkest Dungeon / Slay the Spire style) formation, not the isometric
            // lane grid FOUNDATION.md originally specified -- see PROJECT-README.md pivot note.
            // Single lane; column IS the horizontal rank. Player ranks 0(back)-2(front),
            // enemy ranks 3(front)-5(back), so the two melee units land adjacent (2 vs 3).
            var archetypes = new[]
            {
                new ArchetypeSpec("Melee", "clip_melee_basic", ClassType.Warrior, isRanged: false, usesMagic: false,
                    power: 1.2f, rangeOffsets: new() { new Vector2Int(0, 1) }),
                new ArchetypeSpec("Ranged", "clip_ranged_basic", ClassType.Ranger, isRanged: true, usesMagic: false,
                    power: 1.0f, rangeOffsets: Enumerable.Range(1, 5).Select(c => new Vector2Int(0, c)).ToList()),
                new ArchetypeSpec("Support", "clip_heal_basic", ClassType.Healer, isRanged: false, usesMagic: true,
                    power: 1.0f, targetsAllies: true,
                    rangeOffsets: new() { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }),
            };

            var skillByArchetype = new Dictionary<string, SkillDefinition>();
            var clipSetByArchetype = new Dictionary<string, ClipSet>();

            foreach (var arch in archetypes)
            {
                var pattern = BuildPattern(arch);
                var clipAsset = TryGet(clipsById, arch.ClipAssetId);
                var clipSet = BuildClipSet(arch, clipAsset, unityRelRoot);
                var skill = BuildSkill(arch, pattern);

                skillByArchetype[arch.Name] = skill;
                clipSetByArchetype[arch.Name] = clipSet;
            }

            var unitIds = new[]
            {
                "player_melee", "player_ranged", "player_support",
                "enemy_melee", "enemy_ranged", "enemy_support",
                // Bench reserves (sub-in/sub-out) -- reskins of the matching active
                // archetype's stats/skill/pattern, distinct art + name only. Substring
                // matching against archetype names (below) already resolves these
                // correctly since e.g. "player_bench_melee".Contains("melee").
                "player_bench_melee", "player_bench_ranged", "player_bench_support",
            };

            // Healer archetypes also get a low-power attack (secondarySkill) so they're
            // not heal-only -- reuses the Support archetype's own ±1-column pattern,
            // just targeting the opposing faction instead of allies.
            var secondaryAttack = BuildSecondaryAttackSkill(skillByArchetype["Support"].pattern);

            var characterDefs = new Dictionary<string, CharacterDefinition>();
            foreach (var unitId in unitIds)
            {
                string archName = archetypes.First(a => unitId.Contains(a.Name.ToLowerInvariant())).Name;
                var arch = archetypes.First(a => a.Name == archName);
                var charDef = BuildCharacter(
                    unitId, arch, tier,
                    skillByArchetype[archName], clipSetByArchetype[archName],
                    TryGet(spritesByUnit, unitId), TryGet(portraitsByUnit, unitId),
                    unityRelRoot, secondarySkill: archName == "Support" ? secondaryAttack : null);
                characterDefs[unitId] = charDef;
            }

            var (fxSheet, fxRects) = LoadFxSheet();
            var manifestBackground = LoadSprite(backgroundAsset, unityRelRoot);
            BuildMap(1, characterDefs, tier,
                LoadBackgroundSprite("bg_battle1") ?? manifestBackground, fxSheet, fxRects);
            BuildMap(2, characterDefs, tier,
                LoadBackgroundSprite("bg_battle2") ?? manifestBackground, fxSheet, fxRects);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AI.Game] Battle assets built from manifest.");
        }

        // -- manifest loading -----------------------------------------------------

        static ManifestExport LoadManifestExport()
        {
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string manifestPath = Path.Combine(repoRoot, "Tools", "ComfyUI", "manifest.export.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[AI.Game] manifest.export.json not found at {manifestPath}. "
                    + "Run `python Tools/ComfyUI/generate.py --dry-run` at least once to generate it.");
                return null;
            }
            return JsonUtility.FromJson<ManifestExport>(File.ReadAllText(manifestPath));
        }

        static Sprite LoadSprite(ManifestAsset asset, string unityRelRoot)
        {
            if (asset == null) return null;
            string path = $"{unityRelRoot}/{asset.output}";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[AI.Game] Expected sprite not found (not generated yet?): {path}");
            return sprite;
        }

        // Curated HD roster art (Tools/AssetImport/import_roster.py), separate from the
        // ComfyUI manifest pipeline -- see that script's docstring for provenance.
        static readonly Dictionary<string, string> RosterNames = new()
        {
            ["player_melee"] = "Kestrel",
            ["player_ranged"] = "Sable",
            ["player_support"] = "Linnet",
            ["enemy_melee"] = "Husk",
            ["enemy_ranged"] = "Warden",
            ["enemy_support"] = "Stinger",
            // Bench reserves -- see Tools/AssetImport/import_bench_and_maps.py.
            ["player_bench_melee"] = "Thorne",
            ["player_bench_ranged"] = "Reed",
            ["player_bench_support"] = "Vesper",
        };

        static Sprite LoadBattleSprite(string unitId)
        {
            string path = $"Assets/Art/Generated/battle_sprites/char_{unitId}_battle.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[AI.Game] Expected battle sprite not found (run "
                + $"Tools/AssetImport/import_roster.py?): {path}");
            return sprite;
        }

        // Battle-map backgrounds curated by Tools/AssetImport/import_bench_and_maps.py,
        // same "not the ComfyUI manifest pipeline" provenance as LoadBattleSprite.
        static Sprite LoadBackgroundSprite(string key)
        {
            string path = $"Assets/Art/Generated/backgrounds/{key}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[AI.Game] Expected background not found (run "
                + $"Tools/AssetImport/import_bench_and_maps.py?): {path}");
            return sprite;
        }

        static VideoClip LoadClip(ManifestAsset asset, string unityRelRoot)
        {
            if (asset == null) return null;
            string path = $"{unityRelRoot}/{asset.output}";
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (clip == null) Debug.LogWarning($"[AI.Game] Expected clip not found (not generated yet?): {path}");
            return clip;
        }

        // -- asset builders --------------------------------------------------------

        static TierDefinition BuildTier()
        {
            var tier = LoadOrCreate<TierDefinition>($"{OutDir}/Tiers/Tier_Standard.asset");
            tier.tier = Tier.C;
            tier.statMultiplier = 1f;
            tier.variance = 0.15f;
            tier.summonWeight = 1f;
            EditorUtility.SetDirty(tier);
            return tier;
        }

        static SkillPattern BuildPattern(ArchetypeSpec arch)
        {
            var pattern = LoadOrCreate<SkillPattern>($"{OutDir}/Patterns/Pattern_{arch.Name}Basic.asset");
            pattern.rangeOffsets = new List<Vector2Int>(arch.RangeOffsets);
            pattern.areaOffsets = new List<Vector2Int> { Vector2Int.zero };
            pattern.mirrorOnFacing = true;
            pattern.requiresLineOfSight = false;
            EditorUtility.SetDirty(pattern);
            return pattern;
        }

        static SkillDefinition BuildSkill(ArchetypeSpec arch, SkillPattern pattern)
        {
            var skill = LoadOrCreate<SkillDefinition>($"{OutDir}/Skills/Skill_{arch.Name}Basic.asset");
            skill.skillId = $"skill_{arch.Name.ToLowerInvariant()}_basic";
            skill.displayName = $"{arch.Name} Basic Attack";
            skill.pool = SkillPool.Standard;
            skill.pattern = pattern;
            skill.mpCost = 0;
            skill.cooldown = 0;
            skill.power = arch.Power;
            skill.usesMagic = arch.UsesMagic;
            skill.isRanged = arch.IsRanged;
            skill.targetsAllies = arch.TargetsAllies;
            skill.clipKey = "basicAttack"; // matches the ClipEntry.key built in BuildClipSet
            EditorUtility.SetDirty(skill);
            return skill;
        }

        static ClipSet BuildClipSet(ArchetypeSpec arch, ManifestAsset clipAsset, string unityRelRoot)
        {
            var clipSet = LoadOrCreate<ClipSet>($"{OutDir}/Clips/Clips_{arch.Name}Basic.asset");
            var entry = clipSet.clips.Find(c => c.key == "basicAttack") ?? new ClipEntry { key = "basicAttack" };
            entry.clip = LoadClip(clipAsset, unityRelRoot);
            entry.impactFrames = clipAsset?.impact_frames != null
                ? new List<int>(clipAsset.impact_frames)
                : new List<int>();
            entry.frameRate = clipAsset != null && clipAsset.fps > 0 ? clipAsset.fps : 24;
            entry.chromaKey = Color.green; // FOUNDATION.md: FMV clips are always keyed on solid #00FF00
            entry.chromaTolerance = 0.25f;
            entry.loop = false;

            if (!clipSet.clips.Contains(entry)) clipSet.clips.Add(entry);
            EditorUtility.SetDirty(clipSet);
            return clipSet;
        }

        static CharacterDefinition BuildCharacter(
            string unitId, ArchetypeSpec arch, TierDefinition tier,
            SkillDefinition skill, ClipSet clipSet,
            ManifestAsset spriteAsset, ManifestAsset portraitAsset, string unityRelRoot,
            SkillDefinition secondarySkill = null)
        {
            var def = LoadOrCreate<CharacterDefinition>($"{OutDir}/Characters/Char_{unitId}.asset");
            def.characterId = unitId;
            def.displayName = RosterNames.TryGetValue(unitId, out var name) ? name : Titleize(unitId);
            def.classType = arch.ClassType;
            def.element = ElementType.Neutral;
            def.age = Age.Modern;
            def.baseStats = arch.BaseStats;
            def.movePoints = 4;
            def.jump = 1;
            def.costLateral = 2;
            def.costForward = 1;
            def.costPerHeightLevel = 1;
            def.standardSkill = skill;
            def.secondarySkill = secondarySkill;
            def.growthPerLevel = 0.06f;
            def.clips = clipSet;
            def.portrait = LoadSprite(portraitAsset, unityRelRoot);
            def.pixelSprite32 = LoadSprite(spriteAsset, unityRelRoot);
            def.battleSprite = LoadBattleSprite(unitId);
            EditorUtility.SetDirty(def);
            return def;
        }

        // Low-power attack alongside the Support archetype's heal -- "healers can attack
        // too" per the project owner's ask. Reuses the heal's own ±1-column pattern
        // (SkillPattern is just range/area geometry; targetsAllies is what makes a skill
        // heal vs. attack), so no new Pattern_ asset is needed. "Low attack" falls out of
        // the Support archetype's own low `attack` base stat (8, vs. 22 melee/18 ranged) --
        // no separate balance knob required.
        static SkillDefinition BuildSecondaryAttackSkill(SkillPattern supportPattern)
        {
            var skill = LoadOrCreate<SkillDefinition>($"{OutDir}/Skills/Skill_SupportAttackBasic.asset");
            skill.skillId = "skill_support_attack_basic";
            skill.displayName = "Support Strike";
            skill.pool = SkillPool.Standard;
            skill.pattern = supportPattern;
            skill.mpCost = 0;
            skill.cooldown = 0;
            skill.power = 0.5f;
            skill.usesMagic = false;
            skill.isRanged = false;
            skill.targetsAllies = false;
            skill.clipKey = "basicAttack";
            EditorUtility.SetDirty(skill);
            return skill;
        }

        static (Sprite, List<Vector4>) LoadFxSheet()
        {
            const string pngPath = "Assets/Art/Generated/fx/fx_hit_impact_sheet.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            var rects = new List<Vector4>();
            string jsonFullPath = Path.Combine(Application.dataPath, "Art/Generated/fx/fx_hit_impact_sheet.json");
            if (sprite == null || !File.Exists(jsonFullPath))
            {
                Debug.LogWarning("[AI.Game] fx_hit_impact_sheet not found -- hit VFX will be skipped.");
                return (sprite, rects);
            }
            var meta = JsonUtility.FromJson<FxSheetMeta>(File.ReadAllText(jsonFullPath));
            foreach (var f in meta.frames) rects.Add(new Vector4(f.x, f.y, f.w, f.h));
            return (sprite, rects);
        }

        /// <summary>Builds one battle map. Both maps in this 2-map sequence reuse the
        /// same 3 enemy archetypes/tier -- no second set of enemy art was provided, only
        /// a different background per map (see BattleWorld.MapCount /
        /// Tools/AssetImport/import_bench_and_maps.py).</summary>
        static void BuildMap(int mapNumber, Dictionary<string, CharacterDefinition> characterDefs, TierDefinition tier,
            Sprite backgroundSprite, Sprite fxSheet, List<Vector4> fxRects)
        {
            // Side-view formation: a single lane, column = horizontal rank (see archetypes
            // comment above). Player ranks 0(back)-2(front); enemy ranks 3(front)-5(back) --
            // the two front-liners land adjacent (2 vs 3) so melee's 1-column range meets.
            const int lanes = 1, cols = 6;
            var map = LoadOrCreate<MapDefinition>($"{OutDir}/Maps/Map_BattleSlice{mapNumber}.asset");
            map.laneCount = lanes;
            map.columnCount = cols;
            map.backgroundSprite = backgroundSprite;
            map.fxImpactSheet = fxSheet;
            map.fxImpactFrameRects = fxRects;

            map.tiles = new List<TileData>(lanes * cols);
            for (int i = 0; i < lanes * cols; i++)
                map.tiles.Add(new TileData { height = 0, terrain = TerrainType.Plain, blocked = false });

            map.playerDeployTiles = new List<Vector2Int>
            {
                new(0, 0), new(0, 1), new(0, 2),
            };

            map.enemies = new List<EnemyPlacement>
            {
                new() { character = characterDefs["enemy_melee"], tier = tier, position = new Vector2Int(0, 3), level = 1 },
                new() { character = characterDefs["enemy_support"], tier = tier, position = new Vector2Int(0, 4), level = 1 },
                new() { character = characterDefs["enemy_ranged"], tier = tier, position = new Vector2Int(0, 5), level = 1 },
            };

            EditorUtility.SetDirty(map);
        }

        // Dictionary.GetValueOrDefault isn't available under this project's .NET Standard
        // 2.0 API compatibility level (ProjectSettings apiCompatibilityLevel: 6) -- roll our own.
        static List<ManifestAsset> GetOrEmpty(Dictionary<string, List<ManifestAsset>> dict, string key) =>
            dict.TryGetValue(key, out var list) ? list : new List<ManifestAsset>();

        static TValue TryGet<TValue>(Dictionary<string, TValue> dict, string key) where TValue : class =>
            dict.TryGetValue(key, out var value) ? value : null;

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static string Titleize(string unitId) =>
            string.Join(" ", unitId.Split('_').Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));

        // -- archetype placeholder tuning -------------------------------------------

        class ArchetypeSpec
        {
            public readonly string Name;
            public readonly string ClipAssetId;
            public readonly ClassType ClassType;
            public readonly bool IsRanged;
            public readonly bool UsesMagic;
            public readonly bool TargetsAllies;
            public readonly float Power;
            public readonly List<Vector2Int> RangeOffsets;
            public readonly StatBlock BaseStats;

            public ArchetypeSpec(string name, string clipAssetId, ClassType classType, bool isRanged, bool usesMagic,
                float power, List<Vector2Int> rangeOffsets, bool targetsAllies = false)
            {
                Name = name;
                ClipAssetId = clipAssetId;
                ClassType = classType;
                IsRanged = isRanged;
                UsesMagic = usesMagic;
                TargetsAllies = targetsAllies;
                Power = power;
                RangeOffsets = rangeOffsets;
                BaseStats = name switch
                {
                    "Melee" => new StatBlock { hp = 120, attack = 22, defense = 14, magic = 6, resistance = 8, speed = 9 },
                    "Ranged" => new StatBlock { hp = 90, attack = 18, defense = 8, magic = 8, resistance = 8, speed = 11 },
                    "Support" => new StatBlock { hp = 95, attack = 8, defense = 9, magic = 20, resistance = 12, speed = 10 },
                    _ => new StatBlock { hp = 100, attack = 15, defense = 10, magic = 10, resistance = 10, speed = 10 },
                };
            }
        }

        // -- manifest.export.json mirror (field names match the JSON keys exactly for JsonUtility) --

        [Serializable]
        class ManifestAsset
        {
            public string id;
            public string type;
            public string unit_id;
            public int[] impact_frames;
            public int fps;
            public string output;
        }

        [Serializable]
        class ManifestExport
        {
            public List<ManifestAsset> assets;
            public string outputRoot;
        }

        // fx_hit_impact_sheet.json mirror (written by both generate.py's postprocess.py
        // and Tools/AssetImport/import_roster.py -- same shape either way)
        [Serializable]
        class FxFrameRect
        {
            public int index;
            public int x;
            public int y;
            public int w;
            public int h;
        }

        [Serializable]
        class FxSheetMeta
        {
            public int frameWidth;
            public int frameHeight;
            public List<FxFrameRect> frames;
        }
    }
}
#endif
