# Battle Vertical Slice — status

**Read this first if picking up a fresh session.** This repo lives at
`S:\AI\Game\test\AI.Game` (moved here from `S:\AI\Game\AI.Game` for a folder cleanup —
`S:\AI\Game\` is now `test\` for from-source projects/testing and `play\` for
ready-to-launch builds; see "How to run it" below). Tracks the `feature/battle-slice`
branch (not merged to `main` yet). Original task brief:
`S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md`. Original full
design: `S:\AI\Game\FOUNDATION.md`. **Both are stale on combat model/camera/art
pipeline** — see "What changed from the original design" below before trusting
anything they say about the battle system specifically. Wiki (if reachable — see
"Known gaps"): https://github.com/ihy2ln/AI.Game/wiki, pages: Home, Battle-System,
Art-Pipeline, Roadmap. Same content as this file, more browsable.

## TL;DR current state

A playable, real-art, side-view party battle exists and is unit-tested working:
6 named characters (Kestrel/Sable/Linnet vs Husk/Warden/Stinger) plus 3 bench reserves
(Thorne/Reed/Vesper), auto-battle and a manual/turn-based mode (press `T`), HP bars, hit
VFX, win/lose. Runs in-editor (`Assets/Scenes/Battle.unity`) and as a Windows standalone
dev build. Android APK path exists but **not verified on a physical device** — no adb on
this machine.

M6-M8 layer a reviewable **turn log** (`L`), a genuine **three-panel presentation**
(allies dock left, enemies dock right, the acting unit + its target tween into the empty
centre "stage" for each turn's action), and modern-RPG UX: pause (`Esc`), a settings
panel (battle speed, damage-number/log/auto-mode toggles, volume), and a full multi-step
**undo/redo** stack (`Ctrl+Z`/`Ctrl+Y`) alongside the existing full-battle restart.

M9 (this session) adds **frontline succession** (a faction's formation auto-compacts
when its frontmost unit dies — the next unit in line becomes frontline, no gaps),
**sub-in/sub-out** (swap an active unit for one of 3 bench reserves, costs the turn),
**reposition** (swap column with an adjacent ally, costs the turn), **healers that can
also attack** (low power, alongside their heal), and a **2-map battle sequence** (win map
1, the wounded party carries its HP into map 2). Manual mode now presents a per-turn
action menu (Attack/Heal, Reposition, Sub) instead of jumping straight to target-picking.
See "What changed" below and [[Battle-System]].

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
4. **Presentation split into three panels — M7.** The single-lane rank formation from
   M3 still governs targeting/range, but `BattleLayout` now places player docks and
   enemy docks in two clusters near the screen edges instead of one continuous line,
   leaving a wide empty centre gap. `BattleVisuals.MoveToStage`/`ReturnToDock` tween
   the acting unit and its target into that gap for each turn's cinematic beat, then
   back to their dock — so combat logic still only ever reasons about "who acts on
   whom" via `Column`, and the visual staging is a presentation-only concern layered
   on top.
5. **Pause/settings/undo-redo — M8.** `BattleController` now owns `BattleHistory`
   (snapshots unit HP/MP + the full turn log once per consumed turn, whether it
   resolved or was skipped) and drives pause/speed entirely through `Time.timeScale`
   (every wait in the turn loop and in `BattleVisuals`' stage tweens already runs
   through `WaitForSeconds`/`Time.deltaTime`, so this is a two-line change, not a new
   timing system). `BattleSettings` persists via `PlayerPrefs` — first use of it in
   the project.
6. **Frontline succession, bench/reposition actions, healer attacks, 2-map sequence —
   M9.** `Column` on `BattleUnit` is no longer fixed at construction: `Formation.Compact`
   renumbers a faction's alive units to contiguous columns (anchored at that faction's
   front rank) whenever one dies, so "the next unit in line becomes frontline" instead of
   melee's fixed ±1-column pattern silently missing a gapped formation. `BattleWorld` now
   also tracks a 3-unit player `Bench` (Thorne/Reed/Vesper, reskins of the
   melee/ranged/support archetypes with distinct curated art) that a manual-mode turn can
   swap in for an active unit (`BattleController.SubUnit`, costs the turn — the incoming
   unit joins next round since `TurnOrder` now holds a live reference to
   `BattleWorld.AllUnits` instead of a snapshot copy) or swap columns with an adjacent ally
   (`Reposition`, also costs the turn). Manual mode's turn flow changed from
   "pick a target" straight to "pick an action first" (`ActionPhase.ChooseAction` →
   optionally `ChooseBench` → `ChooseTarget`) to make room for these. Healer-archetype
   units (Linnet, Stinger, and bench healer Vesper) gained `CharacterDefinition
   .secondarySkill`, a low-power attack alongside their heal — auto mode heals when an
   ally needs it, attacks otherwise. `BattleWorld` takes a `mapIndex` + optional
   carry-over unit lists so a second map (`Map_BattleSlice2`, distinct background) loads
   after a map-1 victory with the surviving party's current HP intact
   (`BattleController.OnAdvanceRequested` → `BattleBootstrap.BootMap`).
   `BattleHistory` was extended to snapshot/restore `Column` and active/bench roster
   membership alongside HP/MP, since undo/redo now has to unwind those too. See
   "Known gaps" for a real batchmode-only asset-corruption issue hit while verifying this.

## Roster

| Unit | Role | Faction | Skill pattern |
|---|---|---|---|
| **Kestrel** | Melee (dual katana) | Player | 1 column away only |
| **Sable** | Ranged (sniper) | Player | any column, damage scales up with distance |
| **Linnet** | Support (lantern-healer) | Player | heals own faction, ±1 column + self |
| **Husk** | Melee (armored knight) | Enemy | 1 column away only |
| **Warden** | Ranged (dark caster) | Enemy | any column |
| **Stinger** | Support (insect monster) | Enemy | heals own faction, low-power attack (M9) |

**Bench reserves (player, M9)** — sub in for any active player unit via the manual-mode
Sub action, same archetype stats/skill/pattern as their active counterpart, distinct art:

| Unit | Role | Reskin of |
|---|---|---|
| **Thorne** | Melee | Kestrel's archetype |
| **Reed** | Ranged | Sable's archetype |
| **Vesper** | Support | Linnet's archetype, plus a low-power attack |

## Milestones — all done

| # | Milestone | Tag |
|---|---|---|
| M0 | Branch, `.gitignore`, folder scaffold, docs stub | `v0.4.0-m0-scaffold` |
| M1 | ComfyUI pipeline (workflows, manifest, generate.py, postprocess.py) | `v0.4.0-m1-comfyui-pipeline` |
| M2 | Generated assets + import automation + ScriptableObject build | `v0.4.0-m2-assets` |
| M3 | Side-view battle logic, scene, EditMode tests | `v0.4.0-m3-battle-logic` |
| M4 | Curated HD roster art, hit FX, manual/turn-based mode | `v0.4.0-m4-roster-and-manual-mode` |
| M5 | Android build (APK builds; on-device unverified) | *(not yet tagged — see below)* |
| M6 | Turn log (`BattleLog`, scrollable review panel, `L` to open) | *(not yet tagged)* |
| M7 | Three-panel layout: docked ally/enemy rosters + centre-stage cinematic action | *(not yet tagged)* |
| M8 | Pause, settings, multi-step undo/redo, dock-spacing bugfix | *(not yet tagged)* |
| M9 | Frontline succession, bench sub-in/out, reposition, healer attacks, 2-map sequence | *(not yet tagged)* |

Each of M0-M2's commits has a `NOTES.md` snapshot under
`AI.Game Commits/battle-slice/<milestone>/` and a zip under `releases/zips/`. That
per-milestone full-tree snapshot step was dropped from M3 onward (M4's status note
already flagged it as skipped for time) — it duplicated the whole `Unity/` tree per
milestone for state git history already gives you for free. M6/M7 follow the M3-M5
precedent: commit + docs update, no snapshot/zip.

## Controls

`T` toggle auto/manual mode · `L` open/close the turn log · `Esc` pause ·
`Ctrl+Z`/`Ctrl+Y` undo/redo last turn · `R` restart after the battle ends · `?` keybind
legend. Manual mode (M9): a player unit's turn opens an action menu (Attack/Heal,
[Attack again for Healer-archetype units], Reposition, Sub) — pick one, then click a
highlighted target on the field (skill/reposition) or a bench unit from the list (sub).

## How to run it

- **In Editor:** open `Unity/Assets/Scenes/Battle.unity`, press Play. If the scene or
  `Resources/Battle/*` assets don't exist yet, run
  `AI.Game → Battle → Create Battle Scene` first (also rebuilds the data assets).
- **Windows standalone:** `AI.Game → Battle → Build Windows Standalone (dev)` →
  outputs straight to `S:\AI\Game\play\windows\AI.Game-Battle.exe` (**not** into this
  repo — `play\` is the ready-to-launch sibling of `test\AI.Game\`, deliberately kept
  separate so a from-source build and something you'd hand someone to just play never
  live in the same tree; see `BuildBattleStandalone.cs`'s doc comment).
- **Android APK:** `AI.Game → Battle → Build Android APK` → outputs to
  `S:\AI\Game\play\android\AI.Game-Battle-v0.4.0-debug.apk` (24.8MB, IL2CPP, ARM64,
  min API 26, package `com.aigame.aigame`). Built successfully this session but
  **never installed on a device** — no adb here. Needs `adb install -r` + a manual
  play-through on whatever machine/session has Android platform tools.
- **Headless verification** (what this session actually used, since driving the
  Editor GUI directly wasn't available): with Unity **closed** (batchmode can't run
  alongside an open Editor on the same project — same lockfile),
  ```
  "S:\AI\Game Engine\Unity\UnityEditors\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "S:\AI\Game\test\AI.Game\Unity" -executeMethod Game.EditorTools.BuildBattleStandalone.Build -logFile "S:\AI\Game\test\AI.Game\Unity\batchmode-build.log"
  ```
  and for tests: same but `-runTests -testPlatform EditMode -testResults <path>.xml`
  instead of `-executeMethod` -- **drop `-quit` for the test invocation**, confirmed
  this session: with `-quit` present Unity finishes the asset refresh and exits
  before the test runner ever starts (no `test-results.xml` is written, exit code 0,
  looks like success but nothing ran); the test runner quits the process itself once
  done. To actually *see* the standalone build running, launch the exe then
  screenshot its window via `PrintWindow` (Win32 API through PowerShell) rather than
  a plain screen capture — the exe isn't a "known installed app" so the usual
  computer-use tools can't target it by name.

## Known gaps

- **New: headless `-batchmode -executeMethod` calls that touch `AssetDatabase
  .SaveAssets()`/`CreateAsset` (i.e. `BattleAssetBuilder.Build()`, and therefore
  `BattleSceneBuilder.CreateBattleScene()` and `BuildBattleStandalone.Build()` which call
  it) can corrupt every touched ScriptableObject's `m_Script` reference into
  `{fileID: 0}` — found and fought at length verifying M9.** Confirmed via a controlled
  test: the pristine pre-M9 code+assets built 0-warning clean through this exact
  toolchain; only after adding M9's new C# (new `Formation.cs` class, a new field on
  `CharacterDefinition`) and rebuilding ScriptableObject assets in the same headless
  session did `m_Script` corruption start, and it was **not** limited to newly-created
  assets — a second pass corrupted long-untouched files like `Char_player_melee.asset`
  and `Tier_Standard.asset` too, and giving brand-new assets fresh GUIDs didn't reliably
  fix them either (a later pass still flagged "Script attached to ... is missing" for
  literally every Resources/Battle asset, all 21 of them, in one run). Retrying more
  batchmode passes did not self-heal it. **Workaround used this session:** temporarily
  comment out the `BattleAssetBuilder.Build();` line inside `CreateBattleScene()` before
  running `BuildBattleStandalone.Build()` for verification (skips re-touching the
  ScriptableObjects), and hand-patch any asset that already got corrupted by restoring
  `m_Script: {fileID: <11500000 or 21300000>, guid: <the .cs file's own .meta guid>,
  type: 3}` (compare against a known-good sibling asset of the same C# type for the
  exact `m_EditorClassIdentifier` format too — `git diff` on a corrupted file shows
  exactly what broke). **This is very likely specific to a from-scratch headless
  compile-then-save in the same invocation** — every M2-M8 asset that predates this
  session was built via normal interactive Editor use and was never seen corrupting
  itself in a batchmode run that *didn't* also just recompile changed scripts. **Next
  session picking this up: open the project in the real Unity Editor GUI once (double-
  click into it, let it finish importing, maybe just press Play once) before trusting
  any further headless `-executeMethod` asset-builder runs** — this should "bless" the
  new scripts' MonoScript GUID registration the way batchmode apparently can't, after
  which headless verification should go back to being reliable like it was for M3-M8.
  Net effect on this session: M9's C# is unit-tested (22/22 EditMode tests, including
  new `FormationTests.cs` covering the frontline-compaction logic) and compiles clean
  end-to-end (`BuildResult: Succeeded, 0 errors` on multiple runs), but **the actual
  running standalone build was not successfully screenshotted this session** — treat
  M9 as code-complete + logic-tested, not yet visually verified, until someone opens
  the Editor normally once.
- **This repo moved from `S:\AI\Game\AI.Game` to `S:\AI\Game\test\AI.Game`** during a
  folder cleanup (same session as M8). `S:\AI\Main Game\AI.Game` — a *different*
  project, Asset Forge's own Unity import/validation sandbox, confusingly also named
  `AI.Game` — moved to `S:\AI\Game\test\AssetForge-Sandbox` at the same time and
  Asset Forge's `config.toml`/`config.py` were updated to match. `BuildBattleStandalone.cs`
  and `BuildAndroid.cs` now output straight to `S:\AI\Game\play\windows\` /
  `S:\AI\Game\play\android\` instead of into this repo (see "How to run it"). If any
  tooling/scripts/notes still reference the old `S:\AI\Game\AI.Game` path, they're
  stale — this file and the wiki are current as of the move.
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
- **Manual mode's click-to-target, and all of M8's new buttons (pause, settings,
  undo/redo, log toggle), are code-complete but still haven't been interactively
  click-tested** — same root cause both times: the standalone exe isn't a "known
  installed app," so the usual computer-use tools can't target it, and this session
  additionally found that synthetic input (`SendKeys`, and hardware-level `SendInput`
  with an `AttachThreadInput` focus-steal) doesn't reliably reach the game window
  either -- `GetForegroundWindow` confirmed the click's target window never actually
  changed away from the calling process, so clicks landed on the desktop, not the
  game. Auto mode itself *was* verified thoroughly this way (screenshotted across many
  real turns: hits, heals, a skip-turn on "no valid target", a unit death, HP bars and
  the turn-log toast all updating correctly, and the M7 dock-overlap bug below was
  *found* this way) — only mouse/keyboard-driven interaction is unverified. Needs
  either a physical click, or a working `adb`/on-device input path, or a proper
  fullscreen/exclusive relaunch that a background process is actually allowed to
  foreground.
- **M7's first-pass dock spacing overlapped units** (`DockColumnSpacing` cut to 1.9 to
  make room for the new centre stage, well under the 3.6 the original single-line
  layout's own comment says is required) — Husk and Stinger visibly collided on the
  enemy dock. Fixed in M8 by restoring 3.6 and widening the camera instead of
  shrinking spacing; worth remembering if the layout constants get touched again.
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

1. **Open the project in the real Unity Editor GUI once** (see the new batchmode
   asset-corruption gap above), then use `AI.Game → Battle → Build Assets From
   Manifest` and Play to actually see M9 (frontline succession, sub/reposition,
   healer attacks, the map-1→map-2 transition) running for the first time —
   code-complete and test-covered, but not yet visually confirmed.
2. Interactively click-test manual mode and the M8/M9 buttons (pause/settings/undo/redo/
   log/action-menu/bench-picker) from a real input device or a session with a working
   on-window input path (see "Known gaps") -- logic is unit-tested and pre-M9 auto mode
   was visually verified, but nothing mouse/keyboard-driven has been, and M9's own auto
   mode hasn't been screenshotted at all yet (see gap above).
3. Wire FMV clips into `BattleVisuals` (chroma-key shader + `VideoPlayer`, sync to
   `ClipEntry.impactFrames`) — closes out the original three-layer renderer design.
4. Get the wiki unblocked (needs the project owner's one click, or a working Claude
   in Chrome connection to do it directly).
5. On-device Android verification, whenever a session has `adb` access.
6. Beyond the vertical slice: the roster is currently 6 fixed archetypes with 1 skill
   each and no save/persistence. FOUNDATION.md's broader systems (tier/fusion, gacha,
   farm/town economy, multiple skills per unit) are designed but not connected to
   this battle system yet — that's the actual "rest of the game," this slice only
   proves the battle screen works.
