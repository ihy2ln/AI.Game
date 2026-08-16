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
| M4 | HD roster art, hit FX, manual/turn-based mode | `v0.4.0-m4-roster-and-manual-mode` | Done |
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
- **M2's known art-quality gaps are superseded, not fixed.** The old auto-generated
  32px sprites (fringe on `enemy_support`, cropped feet) are still on disk as
  `pixelSprite32` but the battle no longer renders them -- `BattleVisuals` prefers
  `CharacterDefinition.battleSprite` now, which points at the M4 curated roster art
  instead. `pixelSprite32` stays reserved for a future pixel/strategic view.

## M4 — curated HD roster + manual mode (`v0.4.0-m4-roster-and-manual-mode`)

- Real character identity: **Kestrel** (player melee, dual katana), **Sable** (player
  ranged, sniper), **Linnet** (player support, lantern-healer), **Husk** (enemy melee,
  armored knight), **Warden** (enemy ranged, dark caster), **Stinger** (enemy support,
  insect monster) -- sourced from a pre-existing curated library at
  `S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\charcter`, not regenerated.
  `Tools/AssetImport/import_roster.py` does border-seeded flood-fill background
  removal + crop-to-content; re-run it if that source library changes.
- Hit-impact flipbook re-sourced from the same library's `effect/` folder (31 real
  frames vs. the old auto-generated 8) and wired into `BattleController` -- it now
  actually plays on every hit, not just a red flash.
- `BattleVisuals` normalizes unit size by world-space height instead of a flat scale
  multiplier, so swapping in art with a different native resolution can't repeat the
  M3 overlap bug.
- **Manual/turn-based mode added** alongside auto-battle -- press `T` in-game to
  toggle. Player turns pause and highlight valid targets (yellow ring); enemy turns
  still resolve automatically. Click-to-target via screen-space sprite bounds, no
  colliders needed.
- Verified: clean batchmode compile, 9/9 EditMode tests, and a real screenshot of the
  standalone build confirmed the new art/layout. Manual mode's code path mirrors the
  already-tested auto path (same `ResolveAction`) but its interactive click-to-target
  hasn't been screenshot-verified yet -- the dev build window was closed mid-test.
  Worth a quick check next session.

## Next up

M5: Android build. FMV clip playback into `BattleVisuals` (clips exist, chroma-key
shader doesn't yet) is worth doing before considering the vertical slice's visual
layer "complete" per the original brief's three-layer renderer design.
