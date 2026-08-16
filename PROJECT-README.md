# Battle Vertical Slice — status

**Read this first if picking up a fresh session.** Tracks the `feature/battle-slice`
branch (not merged to `main` yet). Original task brief:
`S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md`. Original full
design: `S:\AI\Game\FOUNDATION.md`. **Both are stale on combat model/camera/art
pipeline** — see "What changed from the original design" below before trusting
anything they say about the battle system specifically. Wiki (if reachable — see
"Known gaps"): https://github.com/ihy2ln/AI.Game/wiki, pages: Home, Battle-System,
Art-Pipeline, Roadmap. Same content as this file, more browsable.

## TL;DR current state

A playable, real-art, side-view party battle exists and is verified working:
6 named characters (Kestrel/Sable/Linnet vs Husk/Warden/Stinger), auto-battle and a
manual/turn-based mode (press `T`), HP bars, hit VFX, win/lose. Runs in-editor
(`Assets/Scenes/Battle.unity`), as a Windows standalone dev build, and as an Android
APK (build succeeds; **not verified on a physical device** — no adb on this machine).
All 5 milestones below are done and pushed to GitHub.

## What changed from the original design

1. **Combat model/camera — pivoted at M3.** FOUNDATION.md specifies an isometric
   lane/column tactics grid. After seeing the running scene, the project owner asked
   for a genuine **Darkest Dungeon / Slay the Spire / Chaos Zero Nightmare style side
   view**: static 2D, player party left, enemies right, no isometric camera, no free
   repositioning. Implemented as a **single-lane** `MapDefinition`
   (`laneCount = 1`, `columnCount = 6`) — column *is* the horizontal rank, so the
   data layer barely changed. Player ranks = columns 0(back)–2(front); enemy ranks =
   3(front)–5(back); the two melee front-liners land adjacent (2 vs 3). No
   movement/repositioning in this slice (matches the original brief's "push/pull"
   exclusion).
2. **Roster art — pivoted at M4.** The original brief's own ComfyUI pixel-art
   pipeline (`Tools/ComfyUI/`) had real quality problems (weak chroma-key on one
   character, feet cropped by generation framing). The project owner pointed at a
   **separate pre-existing curated asset library**
   (`S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\charcter`) with six named
   characters that happened to map exactly onto the 6-archetype roster. **Do not
   regenerate assets** — the project owner was explicit about this. If more/different
   character art is ever needed, check that library first.
3. **Asset Forge** (`S:\AI\Game Engine\assetforge`, a separate custom-built local
   tool, FastAPI+React on port 8420) is **not** the Unity Asset Store voxel tool
   originally assumed. It's an asset library/editor app. Not used to build the game's
   logic (that's all hand-written C# in this repo) — used only to register the
   processed roster assets for the project owner to browse/re-edit later. If asked to
   "use AssetForge to build the game," the judgment call made this session was: keep
   the proven, tested C# pipeline (`BattleAssetBuilder.cs` etc.) as the actual game
   logic, and use AssetForge only where it's genuinely a better fit (asset
   library/editing) — revisit this if the project owner wants something more drastic.

## Roster

| Unit | Role | Faction | Skill pattern |
|---|---|---|---|
| **Kestrel** | Melee (dual katana) | Player | 1 column away only |
| **Sable** | Ranged (sniper) | Player | any column, damage scales up with distance |
| **Linnet** | Support (lantern-healer) | Player | heals own faction, ±1 column + self |
| **Husk** | Melee (armored knight) | Enemy | 1 column away only |
| **Warden** | Ranged (dark caster) | Enemy | any column |
| **Stinger** | Support (insect monster) | Enemy | heals own faction |

## Milestones — all done

| # | Milestone | Tag |
|---|---|---|
| M0 | Branch, `.gitignore`, folder scaffold, docs stub | `v0.4.0-m0-scaffold` |
| M1 | ComfyUI pipeline (workflows, manifest, generate.py, postprocess.py) | `v0.4.0-m1-comfyui-pipeline` |
| M2 | Generated assets + import automation + ScriptableObject build | `v0.4.0-m2-assets` |
| M3 | Side-view battle logic, scene, EditMode tests | `v0.4.0-m3-battle-logic` |
| M4 | Curated HD roster art, hit FX, manual/turn-based mode | `v0.4.0-m4-roster-and-manual-mode` |
| M5 | Android build (APK builds; on-device unverified) | *(not yet tagged — see below)* |

Each milestone's commit history has a `NOTES.md` snapshot under
`AI.Game Commits/battle-slice/<milestone>/` and a zip under `releases/zips/`, per this
repo's existing per-section convention. M4 skipped the snapshot/zip step (time
pressure) — worth backfilling if a session has spare time.

## How to run it

- **In Editor:** open `Unity/Assets/Scenes/Battle.unity`, press Play. If the scene or
  `Resources/Battle/*` assets don't exist yet, run
  `AI.Game → Battle → Create Battle Scene` first (also rebuilds the data assets).
- **Windows standalone:** `AI.Game → Battle → Build Windows Standalone (dev)` →
  `Unity/Builds/BattleStandalone/AI.Game-Battle.exe`.
- **Android APK:** `AI.Game → Battle → Build Android APK` →
  `releases/AI.Game-Battle-v0.4.0-debug.apk` (24.8MB, IL2CPP, ARM64, min API 26,
  package `com.aigame.aigame`). Built successfully this session but **never installed
  on a device** — no adb here. Needs `adb install -r` + a manual play-through on
  whatever machine/session has Android platform tools.
- **Headless verification** (what this session actually used, since driving the
  Editor GUI directly wasn't available): with Unity **closed** (batchmode can't run
  alongside an open Editor on the same project — same lockfile),
  ```
  "S:\AI\Game Engine\Unity\UnityEditors\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "S:\AI\Game\AI.Game\Unity" -executeMethod Game.EditorTools.BuildBattleStandalone.Build -logFile "S:\AI\Game\AI.Game\Unity\batchmode-build.log"
  ```
  and for tests: same but `-runTests -testPlatform EditMode -testResults <path>.xml`
  instead of `-executeMethod`. To actually *see* the standalone build running, launch
  the exe then screenshot its window via `PrintWindow` (Win32 API through
  PowerShell) rather than a plain screen capture — the exe isn't a "known installed
  app" so the usual computer-use tools can't target it by name.

## Known gaps

- **No `adb` on this machine** — M5's on-device install/verification is genuinely
  blocked here, not skipped out of laziness. Needs a different machine/session.
- **GitHub wiki push still blocked** as of last check. GitHub only provisions a
  repo's wiki git backend after a page is saved once through the web UI — there's no
  API/git-push way around it. The 4 wiki pages (Home, Battle-System, Art-Pipeline,
  Roadmap) are written and committed locally at `S:\AI\Game\AI.Game.wiki\` (a
  separate git clone, not part of the main repo), ready to push the moment
  https://github.com/ihy2ln/AI.Game/wiki has its first page created (one click, "Create
  the first page", any content). Then: `cd "S:\AI\Game\AI.Game.wiki" && git push -u
  origin master`.
- **Manual mode's click-to-target is code-complete and mirrors the already-tested
  auto path, but hasn't been screenshot-verified interactively** — the dev build
  window was closed mid-test by the project owner. Quick thing to check next session:
  launch the standalone build, press T, click a highlighted target, confirm it
  resolves.
- **FMV clip playback is not wired in.** `Tools/ComfyUI/` generated 3 real h264 clips
  in M1/M2 and `ClipEntry`/`ClipSet` exist on `CharacterDefinition`, but
  `BattleVisuals` only ever rendered static sprites — no `VideoPlayer`, no chroma-key
  shader. This is real, uncompleted scope from the original brief's three-layer
  renderer design, not something that was decided against.
- **No fixed master palette** for the (now largely superseded) `Tools/ComfyUI/`
  pixel-sprite pipeline — `postprocess.quantize_palette()` uses adaptive median-cut as
  a placeholder. Low priority now that the roster uses curated HD art instead.
- **M4 has no snapshot/zip** under `AI.Game Commits/battle-slice/` (see Milestones
  table) — the commit/tag/push happened, just not the extra per-section copy step.

## Natural next steps, roughly in priority order

1. Verify manual mode interactively (see "Known gaps").
2. Wire FMV clips into `BattleVisuals` (chroma-key shader + `VideoPlayer`, sync to
   `ClipEntry.impactFrames`) — closes out the original three-layer renderer design.
3. Get the wiki unblocked (needs the project owner's one click, or a working Claude
   in Chrome connection to do it directly).
4. On-device Android verification, whenever a session has `adb` access.
5. Beyond the vertical slice: the roster is currently 6 fixed archetypes with 1 skill
   each and no save/persistence. FOUNDATION.md's broader systems (tier/fusion, gacha,
   farm/town economy, multiple skills per unit) are designed but not connected to
   this battle system yet — that's the actual "rest of the game," this slice only
   proves the battle screen works.
