# Battle Vertical Slice — status

Tracks the `feature/battle-slice` branch. Task brief:
`S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md`. Full design:
`S:\AI\Game\FOUNDATION.md`.

**Combat model — PIVOTED as of M3.** The original plan (below, kept for history) assumed
an isometric lane/column tactics grid. After seeing the actual scene, the project owner
asked for a genuine **Darkest Dungeon / Slay the Spire / Chaos Zero Nightmare style side
view**: a static 2D screen, player party lined up on the left, enemies on the right, no
isometric camera. This is implemented as a **single-lane** `MapDefinition`
(`laneCount = 1`, `columnCount = 6`) — column *is* the horizontal rank, so almost none of
the underlying data layer changed. Player ranks are columns 0(back)–2(front), enemy ranks
are 3(front)–6(back), so the two melee front-liners land adjacent (2 vs 3) and a
1-column melee range just works. Movement/repositioning is out of scope for this slice
(units hold their rank for the whole battle), matching the original brief's explicit
"push/pull repositioning" exclusion.
~~lane/column tactics grid from FOUNDATION.md §1 (movement, height, jump all as designed
there) — not Darkest Dungeon rank-formation combat. Presentation is the Brown Dust
2–style unified battlefield view (units on the grid, FMV plays in place) per
FOUNDATION.md §2.2.~~

**Art pipeline:** pixel sprites + chroma-keyed FMV clips (FOUNDATION.md §3), generated via
local ComfyUI (Krea2 for stills, Minimax H3 for clips). Asset Forge (a separate,
custom-built local tool at `S:\AI\Game Engine\assetforge` — not the Unity Asset Store
voxel tool originally assumed) is available and was surveyed but not used for this
slice's assets; all 17 assets came from the `Tools/ComfyUI/` pipeline built in M1/M2.

## Milestones

| # | Milestone | Tag | Status |
|---|---|---|---|
| M0 | Branch, `.gitignore`, folder scaffold, docs stub | `v0.4.0-m0-scaffold` | Done |
| M1 | ComfyUI pipeline (workflows, manifest, generate.py, postprocess.py) | `v0.4.0-m1-comfyui-pipeline` | Done |
| M2 | Generated assets + import automation + ScriptableObject build | `v0.4.0-m2-assets` | Done |
| M3 | Battle logic (grid, turns, targeting, damage) + tests, side-view scene | `v0.4.0-m3-battle-logic` | Done |
| M4 | Visuals: sprites, background, chroma-key FMV, HUD, damage numbers | `v0.4.0-m4-visuals` | Not started |
| M5 | Android build + on-device verification | `v0.4.0-m5-android-apk` | Not started |

## Known environment gaps

- **Unity Editor found** at `S:\AI\Game Engine\Unity\UnityEditors\Editor\6000.5.7f1\Editor\Unity.exe`
  (not `S:\AI\Unity\...` as `Unity/OpenUnity.bat` assumed -- fixed). The project owner
  has it open interactively; M3's code is therefore verified by them pressing Play in
  their own session rather than by the assistant running batchmode Unity (which would
  conflict with their open Editor holding the project lock).
- **No `adb` on PATH** — on-device install/verification (M5) will need to be done manually
  or from a machine that has the Android platform tools.
- **ComfyUI confirmed reachable** at `127.0.0.1:8188` (v0.33.0).

## What works

- ComfyUI asset generation pipeline: `Tools/ComfyUI/manifest.yaml` (17 assets),
  `generate.py` (submit/poll/fetch, `--dry-run`, `--force`, `--only`), `postprocess.py`
  (chroma-key transparency, palette quantization, video re-encode, FX sheet packing).
- Four validated ComfyUI API-format workflows (`workflows/*.json` +
  `*.patchmap.json`): Krea2 stills (sprites/portraits/backgrounds), MiniMax H3
  reference-to-video clips, MiniMax H3 + chroma-key FX flipbook frames. See
  `Tools/ComfyUI/README.md` "Workflow node graphs" for the real gotchas found (image
  upload requirement, non-literal frame counts, per-effect chroma key color, etc.).
- Smoke-tested end to end: `python generate.py --only bg_battle01` produced a real
  1920x1080 battle background at `Unity/Assets/Art/Generated/backgrounds/bg_battle01.png`.

- All 17 battle-slice assets generated and verified: background (1920x1080),
  6 pixel sprites (32x32, real transparency via per-image sampled chroma key --
  fixed a fixed-key bug where Krea2's background gradient didn't survive a single
  project-wide key color), 6 portraits (256x256), 3 attack clips (h264 mp4,
  512x768@24fps, ~39 frames), 1 FX hit flipbook (8 frames, transparent, packed
  sheet + rect JSON).
- `GeneratedAssetImporter.cs` (AssetPostprocessor: point-filtered/uncompressed for
  sprites+FX, bilinear/compressed for portraits/backgrounds) and
  `BattleAssetBuilder.cs` (`AI.Game -> Battle -> Build Assets From Manifest` menu
  item; builds ClipSet/SkillPattern/SkillDefinition/CharacterDefinition/MapDefinition
  ScriptableObjects into `Resources/Battle/` from `manifest.export.json`) are written
  but **unverified** -- no local Unity Editor install exists to compile/run them
  against (see "Known environment gaps" above). Written carefully against documented
  Unity APIs and cross-checked against this project's own conventions (e.g. avoided
  `Dictionary.GetValueOrDefault`, unavailable under this project's .NET Standard 2.0
  API compatibility level) -- but treat as unverified until an Editor actually opens
  this project and runs the menu item.

- **M3 (side-view battle, auto-battle vertical slice):** `Battle/` scripts --
  `BattleWorld` (rolls the 6 units from the M2 ScriptableObjects), `TurnOrder`
  (speed-sorted, re-sorted per round), `TargetResolver` + `DamageCalculator` (pure C#,
  covered by EditMode tests), `BattleController` (auto-battle loop: both sides act
  automatically each turn, matching the BD2/gacha-style "auto battle" convention rather
  than building manual touch-targeting in this pass), `BattleVisuals` (real generated
  sprites + background, side-view layout), `BattleHud` (IMGUI HP bars, damage numbers,
  win/lose banner, restart). `AI.Game -> Battle -> Create Battle Scene` builds
  `Assets/Scenes/Battle.unity` (also runs the M2 asset builder first for convenience).
  EditMode tests (`Unity/Assets/Tests/`) cover melee range, ranged distance scaling,
  damage floor, ally-targeting for heals, and facing-mirrored range -- added
  `com.unity.test-framework` to `Packages/manifest.json` and a `Game.Tests.asmdef`.
- **Fully verified, including visually.** Batchmode caught and fixed a compile bug
  (`Game.Tests.asmdef` -> proper `Game.Data`/`Game.Battle` asmdefs), 9/9 EditMode tests
  pass. Beyond that: built a Windows standalone dev player
  (`BuildBattleStandalone.cs`, `AI.Game > Battle > Build Windows Standalone (dev)`),
  launched it, and captured real screenshots of it running. Found and fixed a real
  layout bug this way that nothing else would have caught: `BattleLayout.UnitScale`
  (3) exceeded `ColumnSpacing` (2.4), so units visually overlapped into one cluster
  instead of forming the left/right formation -- fixed to `ColumnSpacing = 2.8`,
  `UnitScale = 2`. Confirmed via screenshots: clean formation, varied targeting,
  correct HP color thresholds, death handling, and a full VICTORY banner + Restart
  button at the end of a real auto-battle.
- **Known art-quality gaps** (not code bugs, not fixing without regenerating assets
  per project owner's "no more asset generation" instruction): `enemy_support`'s
  sprite still shows a visible chroma-key fringe (flagged in M2 notes); all six
  sprites are cropped at the feet -- confirmed this is baked into the generated
  32x32 art itself (checked the raw sprite pixels), not a camera/layout issue.

## Next up

M4/polish: manual unit-and-target selection (currently auto-battle only), wire the
FMV clips into `BattleVisuals` (currently pixel sprites only, clips exist but aren't
played back yet), tune the placeholder damage/heal numbers once the auto-battle is
confirmed working.
