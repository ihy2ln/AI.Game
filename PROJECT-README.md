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

M9 adds **frontline succession** (a faction's formation auto-compacts when its frontmost
unit dies — the next unit in line becomes frontline, no gaps), **sub-in/sub-out** (swap
an active unit for one of 3 bench reserves, costs the turn), **reposition** (swap column
with an adjacent ally, costs the turn), **healers that can also attack**, and a **2-map
battle sequence** (win map 1, the wounded party carries its HP into map 2).

M10 (this session) replaces the old two-skill model with a real **Skill Move (SM)
system**: every unit has a free "BA" (Basic Attack) plus 3 mana-cost Skill Moves,
accessed via **press-and-hold on the compact SM icon**. The manual-mode action menu is
now 4 small icons (**BA / SM / R / S**) anchored right under the acting character
instead of a full-width panel, and melee-flavoured attacks now have the attacker walk up
to the target instead of both units jumping to generic stage marks. See "What changed"
below and [[Battle-System]]. **This session also found and fixed a real, previously
unknown bug** (`BattleBootstrap`/`FarmBootstrap` never actually attaching a `Camera`
component, crashing the very first `Play`) — see item 7 below; this was root-caused
via the project owner's own interactive Editor session, the first time this project's
battle scene had actually been played back interactively rather than only screenshotted
via automation.

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
7. **Camera bug found + fixed, Skill Move system, compact action UI, melee movement —
   M10.** The project owner opened the Editor interactively (not automation) for the
   first time this project has been played back that way, and hit an immediate crash:
   `camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>()` in both
   `BattleBootstrap.cs` and `FarmBootstrap.cs` never actually attached a `Camera` --
   Unity 6's component binding can return a non-CLR-null wrapper for "component not
   found," so `??` (plain reference-null check) skipped `AddComponent` entirely.
   `MissingComponentException` on `cam.orthographic = true` in `BattleLayout
   .ApplyBattleCamera`. Fixed by using an explicit `== null` check (which uses Unity's
   overloaded equality, correctly detecting the fake-null) instead of `??` in both files
   -- this bug predates M9 and would have hit any prior session that tried Play, not
   something introduced this session. Once fixed, the rest of this session was a real
   back-and-forth UI/content pass driven by the project owner actually playing the game:
   - **`CharacterDefinition.secondarySkill` (M9) replaced by `skillMoves` (List, up to
     3)** -- `standardSkill` is now always the free "BA" for every archetype (Healers'
     BA became their attack; Heal itself moved into `skillMoves`). `BattleController
     .SkillMoveOptions`/`.BasicAttackSkill` read these; `EffectiveMpCost` scales
     `skill.mpCost` by the new `Settings.MpCostMultiplier` dev slider.
   - **9 new skills**, 3 per archetype, heal/damage primitives only (explicit scope
     call -- no status-effect system yet): Melee gets Second Wind (self-heal)/Rally
     (heal an ally)/Power Strike (heavy hit); Healer gets Heal/Mass Heal (AoE
     heal)/Focus Heal (big single heal); Ranged gets Volley (3-wide AoE)/Snipe (heavy
     single hit)/Barrage (guaranteed full-team AoE, ±5-column area). `BattleController
     .ResolveAction`'s heal branch was generalized to support AoE the same way the
     damage branch already did (`TargetResolver.GetAreaTargets` when
     `pattern.areaOffsets.Count > 1`), needed for Mass Heal.
   - **Compact action menu**: `BattleHud.DrawActionMenu` now draws 4 small icon buttons
     (BA/SM/R/S) anchored at the acting unit's true visual feet -- `DockPosition` is the
     sprite's *pivot*, which is Center (Unity's default import pivot) not Bottom, so the
     anchor steps down `BattleLayout.TargetUnitHeight / 2` first. Horizontally clamped
     (`ClampedLeftX`) so it can't run off-screen for an edge-column unit. SM's own click
     is ignored; a separate `Update()`-driven hold-timer (`SmHoldSeconds = 0.35f`, real
     `Time.unscaledTime` so pause/speed settings don't affect it) opens a skill-list
     popup once the button's been held long enough, listing each Skill Move with its MP
     cost and greying out ones the unit can't currently afford.
   - **Melee approach movement**: `BattleVisuals.MoveToMelee` -- for any attack that
     isn't ranged or a heal/self-buff (`BattleController.IsMeleeAction`), the attacker
     walks to just beside the target (target stays put) instead of both units jumping to
     the generic centre-stage marks `MoveToStage` uses for everything else.
     `ReturnToDock` (unchanged) handles the trip back either way.
   - **HP/MP bars**: shrunk and given a companion MP bar per unit
     (`Definition.maxMp`, default 100, everyone starts full). Found and fixed a real
     rendering bug along the way: `DrawBarFill`'s padding (4px) exceeded the MP bar's
     height (4px), leaving zero pixels for the fill regardless of the underlying value --
     looked exactly like "MP never shows/decreases" even though the data was always
     correct. Padding is 1px/side now, bar height bumped to 6px.
   - **Dev-tuning sliders** (Settings panel): damage dealt (0.25x-5x) / damage received
     (0x-2x) multipliers applied in `ResolveAction`, and an MP-cost multiplier (0x-2x,
     0x = free Skill Moves) for testing skills repeatedly without waiting on regen
     (there isn't any yet -- MP only ever decreases once spent).

## Roster

| Unit | Role | Faction | BA (free) | Skill Moves (mana, hold SM) |
|---|---|---|---|---|
| **Kestrel** | Melee | Player | Melee Basic Attack, 1 col | Second Wind (self-heal, 30MP) / Rally (heal ally, 25MP) / Power Strike (heavy hit, 35MP) |
| **Sable** | Ranged | Player | Ranged Basic Attack, any col | Volley (3-wide AoE, 25MP) / Snipe (heavy hit, 30MP) / Barrage (full-team AoE, 45MP) |
| **Linnet** | Support | Player | Support Strike (low power attack) | Heal (20MP) / Mass Heal (AoE heal, 35MP) / Focus Heal (big heal, 30MP) |
| **Husk** | Melee | Enemy | same as Kestrel's archetype | same as Kestrel's archetype |
| **Warden** | Ranged | Enemy | same as Sable's archetype | same as Sable's archetype |
| **Stinger** | Support | Enemy | same as Linnet's archetype | same as Linnet's archetype |

**Bench reserves (player)** — sub in for any active player unit via the manual-mode Sub
action, same archetype stats/BA/Skill Moves as their active counterpart, distinct art:
**Thorne** (Melee, reskin of Kestrel's archetype), **Reed** (Ranged, reskin of Sable's),
**Vesper** (Support, reskin of Linnet's).

Skill Move content is heal/damage primitives only (no status-effect system yet -- an
explicit scope decision this session, see item 7 above). All 9 non-BA skills, plus the
relocated Heal, live in `CharacterDefinition.skillMoves`; `standardSkill` is always BA.

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
| M10 | Camera bug fix, Skill Move system (9 skills), compact action UI, melee movement | *(not yet tagged)* |

Each of M0-M2's commits has a `NOTES.md` snapshot under
`AI.Game Commits/battle-slice/<milestone>/` and a zip under `releases/zips/`. That
per-milestone full-tree snapshot step was dropped from M3 onward (M4's status note
already flagged it as skipped for time) — it duplicated the whole `Unity/` tree per
milestone for state git history already gives you for free. M6/M7 follow the M3-M5
precedent: commit + docs update, no snapshot/zip.

## Controls

`T` toggle auto/manual mode · `L` open/close the turn log · `Esc` pause ·
`Ctrl+Z`/`Ctrl+Y` undo/redo last turn · `R` restart after the battle ends · `?` keybind
legend. Manual mode: a player unit's turn opens a small 4-icon menu under their feet --
**BA** (tap, free basic attack), **SM** (press and hold ~0.35s to open the Skill Move
list, mana-cost), **R** (tap, Reposition), **S** (tap, Sub) -- then click a highlighted
target on the field (BA/SM/Reposition) or pick from the popup (SM's list, Sub's bench).

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

- **Resolved this session, confirmed by the project owner's own interactive Editor
  use: manual mode's click-to-target and the action-menu buttons work fine with a real
  mouse.** The long-standing "never interactively click-tested" gap below was always
  specifically about *this project's automation tooling* not being able to synthesize
  clicks against the standalone exe -- it was never evidence that clicking wouldn't work
  for an actual person at the keyboard. The project owner played manual mode directly
  (screenshots this session show `Kestrel's turn -- choose an action` / `Sable's turn --
  choose an action` mid-interaction, catching and reporting three real bugs along the
  way -- the camera crash, the action-menu position, and the MP-bar rendering bug, all
  itemized above). **Net: prefer testing via the project owner's own interactive Editor
  session over automation for anything UI-shaped going forward** -- it's strictly more
  capable than anything this environment's automation can reach, and already caught
  bugs automation never would have (the camera bug especially -- batchmode never got far
  enough to hit it, since it fails earlier on asset loading; see the item below).
- **Headless `-batchmode -executeMethod` calls that touch `AssetDatabase
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
  **Update: the project owner did exactly that later this session** -- opened the
  Editor interactively, and M9+M10 both ran and were played there without ever hitting
  this corruption (only the pre-existing camera bug above, unrelated). That's real
  evidence for the "interactive-first-use blesses it" hypothesis, though still not
  fully proven (no controlled A/B was re-run afterward). M9's C# is also unit-tested
  (22/22 EditMode tests, including `FormationTests.cs`) and compiles clean end-to-end.
  **Bottom line: don't use headless `-executeMethod` asset-builder runs after a script
  change; open the Editor normally instead** -- confirmed to work, and it's what the
  project owner will be doing anyway to playtest.
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
- **Automation still can't drive the standalone exe's window directly** (see the two
  items above for why, and why it no longer matters much: the project owner's own
  interactive Editor session covers this better than automation ever could). Synthetic
  input (`SendKeys`, hardware-level `SendInput` with an `AttachThreadInput` focus-steal)
  doesn't reliably reach the game window -- `GetForegroundWindow` confirmed the click's
  target window never actually changed, so clicks landed on the desktop, not the game.
  Auto mode was thoroughly verified this way in earlier sessions regardless (screenshots
  across many real turns), and the M7 dock-overlap bug was *found* this way.
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
- **M10's 9-skill Skill Move content is code-complete but not yet confirmed running.**
  `BattleAssetBuilder.Build()` needs to run (`AI.Game → Battle → Build Assets From
  Manifest`, from inside the already-open interactive Editor -- see above) to actually
  populate `CharacterDefinition.skillMoves` on every character; as of the last check
  this session the on-disk character assets still had none of it (verified by grepping
  `Char_player_melee.asset` for `skillMoves`/`maxMp` and finding neither key). The
  camera bug and the action-menu/MP-bar bugs above were all found and fixed via real
  interactive play *before* this rebuild happened, so those are confirmed; the skill
  content itself, and the melee-approach movement, are not yet confirmed by the project
  owner actually seeing them run.

## Natural next steps, roughly in priority order

1. **Run `AI.Game → Battle → Build Assets From Manifest`** from the already-open
   interactive Editor (not headless), then Play and actually test the 9-skill Skill
   Move system (hold SM, spend mana, watch Mass Heal/Barrage hit multiple targets) and
   the melee-approach movement -- the one piece of M10 not yet confirmed running (see
   the gap above). Frontline succession, sub/reposition, healer attacks, and the
   map-1→map-2 transition (M9) plus the camera fix/action-menu/MP-bar fixes (M10) are
   all confirmed working via the project owner's own interactive play this session.
2. Wire FMV clips into `BattleVisuals` (chroma-key shader + `VideoPlayer`, sync to
   `ClipEntry.impactFrames`) — closes out the original three-layer renderer design.
3. Get the wiki unblocked (needs the project owner's one click, or a working Claude
   in Chrome connection to do it directly).
4. On-device Android verification, whenever a session has `adb` access.
5. Consider a real status-effect system if future Skill Moves need to go beyond
   heal/damage (buffs, shields, taunt) — explicitly deferred this session, see item 7
   under "What changed."
6. Beyond the vertical slice: the roster is currently 6 fixed archetypes plus 3 bench
   reserves, no save/persistence. FOUNDATION.md's broader systems (tier/fusion, gacha,
   farm/town economy) are designed but not connected to this battle system yet —
   that's the actual "rest of the game," this slice only proves the battle screen works.
