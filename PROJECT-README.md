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

M11 (this session) is the milestone where M10's Skill Move content **actually reached
the game**. It had been written entirely as C# inside `BattleAssetBuilder` and never
built into the ScriptableObjects, so every character shipped with an empty `skillMoves`
list -- which showed up in play as three apparently separate bugs (SM icon permanently
greyed out, skills 1-3 missing, mana bar never moving) that were all the same cause.
Fixed, then guarded three ways so it can't recur: an Editor-load auto-rebuild
(`BattleContentGuard`), 7 tests that read the *built* assets rather than in-memory
objects, and a boot-time error naming any character with no Skill Moves. SM also
changed from press-and-hold to a **tap**. See item 8 below.

M10 replaces the old two-skill model with a real **Skill Move (SM)
system**: every unit has a free "BA" (Basic Attack) plus 3 mana-cost Skill Moves,
accessed via the compact SM icon. The manual-mode action menu is
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
8. **Skill Moves actually shipped, content-staleness guard, SM tap -- M11.** Everything
   in item 7's Skill Move system was real, compiled, and unit-tested, and **none of it
   was in the game.** `BattleAssetBuilder` authors the 9 skills in C#, but
   `AI.Game > Battle > Build Assets From Manifest` was never run, so every
   `CharacterDefinition` on disk still had an empty `skillMoves` list. Three symptoms,
   one cause: `BattleHud.DrawActionMenu` enables SM on `SkillMoveOptions.Count > 0`
   (empty -> greyed out forever, with no explanation surfaced anywhere), and
   `BattleController.ChooseAutoSkill` looks in `skillMoves` for a `targetsAllies` entry
   to decide whether a healer heals (empty -> always fell through to the free 0-MP BA,
   so no unit ever spent MP and the bar looked broken).
   **Why nothing caught it:** every existing test builds its subjects in memory via
   `BattleTestHelpers.MakeUnit` (`ScriptableObject.CreateInstance`), so the whole
   22-test suite passed against correct logic while the shipped content was empty.
   Nothing read `Resources/Battle`. Fixed in three layers:
   - **`BattleContentGuard.cs`** (new, `Scripts/Editor/`) -- `[InitializeOnLoad]` hook
     that checks on every Editor load whether `Resources/Battle` matches what the
     builder currently authors and re-runs `BattleAssetBuilder.Build()` if not. Two
     independent staleness checks: a `ContentVersion` stamp in `EditorPrefs` (catches
     "builder C# is newer than disk") and a direct probe of the loaded
     `CharacterDefinition`s (catches "the assets are wrong right now" regardless of the
     stamp -- e.g. after a `git revert` of `Resources/Battle`). **Hard-bails on
     `Application.isBatchMode`**, so it can never fire in the headless path that
     corrupts `m_Script` references (see "Known gaps"). Bump
     `BattleAssetBuilder.ContentVersion` whenever the authored content changes shape.
   - **`BattleAssetContentTests.cs`** (new, 7 tests) -- asserts against the real assets
     via `Resources.Load`. Covers: free BA with a pattern on every character; exactly 3
     distinct Skill Moves each (distinct `skillId` *and* `displayName`, see below);
     every move affordable from a full MP pool with at least one costing MP; healers
     attack with BA and heal from `skillMoves`; melee BAs not flagged `isRanged` (which
     would silently lose the walk-up animation); the 3 AoE patterns really covering more
     than one column; both maps fielding 3 enemies with a background.
   - **`BattleWorld.WarnOnStaleContent`** -- `Debug.LogError` at boot naming any
     character with no Skill Moves. A standalone build has its assets already baked and
     the guard can't help there, so a loud log line is the only available signal.

   Two more real bugs found on the way, both caught by inspecting the rebuilt assets
   rather than by playing:
   - **Every ally-targeting skill displayed as "Heal."** `DrawSkillListPopup` labelled
     rows `skill.targetsAllies ? "Heal" : skill.displayName`, so Kestrel's SM list
     rendered as *Heal (30 MP) / Heal (25 MP) / Power Strike* -- Second Wind and Rally
     both masquerading as Heal -- and Linnet's as *Heal / Heal / Heal*. `targetsAllies`
     means "aims at my side," not "is the heal." Now always `displayName`, and the
     `Skill_SupportBasic` asset was renamed from "Support Basic Attack" to "Heal"
     (`skillId: skill_support_heal`) since it's no longer anyone's basic attack.
   - **Another latent `??`-on-a-`UnityEngine.Object`** -- `LoadBackgroundSprite(...) ??
     manifestBackground` in `BattleAssetBuilder`, the same trap as item 7's camera bug.
     Harmless today (both backgrounds exist) but it would have silently skipped the
     fallback the moment one didn't. Replaced with an `OrFallback<T>` helper using
     Unity's overloaded `== null`.

   **SM is a tap now.** Press-and-hold became `_showSkillList = !_showSkillList`, and
   the whole hold apparatus (`SmHoldSeconds`, `_smButtonRect`, `_smHoldStartTime`, and
   the `Update()` polling behind them) is gone. Holding read as an unresponsive button:
   tapping SM did nothing and nothing on screen hinted that holding was the gesture,
   especially with BA/R/S all being taps. Tapping again closes the list, which is also
   the only way to back out without committing to a skill.
9. **MP economy (a first pass) and FMV chroma-key components -- M12.** Direction from
   the project owner: verify Skill Moves by code-based stats rather than interactive
   play (item 1 below), then build a real MP economy using arbitrary numbers (not a
   tuned balance pass), and build the chroma-key/VideoPlayer plumbing FMV clips need
   even though final clip assets/pipeline aren't chosen yet.
   - **MP economy.** `BattleUnit` gained `SpendMp`/`RestoreMp` (both clamp to
     `[0, MaxMp]`), `RestoreMpFull`, and `RecoverMpAfterBattle`. Four sources, per the
     project owner's design: (1) a small passive trickle from the true BA specifically
     (`BasicAttackMpRegen = 4`, gated on `skill == unit.Definition.standardSkill` so a
     Skill Move that happens to cost 0 MP via the dev-tuning slider can't be farmed for
     it); (2) 25%-50% of the *missing* MP restored per unit when a carried-over roster
     loads map 2 (`BattleWorld`'s existing carry-over path -- deliberately distinct from
     HP, which still does not recover between maps, since the carried wound is the
     point); (3) a full restore hook (`RestoreMpFull`) for a future farm/town "sleep to
     recover" system -- not called by anything yet, since no persistence layer connects
     battle party state to the farm scene; (4) a new Support Skill Move, **Mana Spring**
     (15 MP, restores ~20 MP to an ally at Linnet's base magic), the first skill to use
     a new `SkillDefinition.restoresMana` flag that routes `BattleController
     .ResolveAction`'s ally-targeting branch to `DamageCalculator.ComputeManaRestore`
     instead of `ComputeHeal`. Found and fixed a real bug while adding it:
     `ChooseAutoSkill`'s auto-heal lookup (`skillMoves.FirstOrDefault(s =>
     s.targetsAllies)`) would just as easily have handed auto mode Mana Spring instead
     of the real heal once a second ally-targeting move existed on the same unit --
     fixed by excluding `restoresMana` from that filter.
   - **FMV chroma-key components.** `Assets/Shaders/ChromaKeyVideo.shader` (Unlit,
     discards pixels near `ClipEntry.chromaKey` within `chromaTolerance`, `Cull Off` so
     a facing-flipped quad still renders) and `BattleClipPlayer.cs` (one per unit view,
     built inactive in `BattleVisuals.BuildUnitView`; owns a `VideoPlayer` targeting a
     `RenderTexture` fed into a runtime quad using that shader). `BattleVisuals` exposes
     `HasActionClip`/`PlayActionClip`; `BattleController.PlayImpactBeat` (new, replacing
     a wait that was duplicated verbatim in both turn-flow methods) plays the clip when
     one exists, else falls back to the original flat pause. **Deliberately restricted
     to the true BA only** -- every Skill Move currently shares `clipKey: "basicAttack"`
     (`BattleAssetBuilder.BuildSkillMove`), so playing it for e.g. Power Strike would
     show the wrong (generic melee-swing) clip; Skill Moves keep the sprite+flash/
     impact-FX presentation until they get dedicated clips. This is reachable *today*
     with the 3 real clips M1/M2 already generated (`clip_melee_basic`/`clip_ranged_
     basic`/`clip_heal_basic`, imported and referenced by `Clips_MeleeBasic`/etc.) --
     not just scaffolding for hypothetical future assets.
     **Found, and later fixed, a real, separate bug while building this:** the
     `impactFrames` metadata baked into those Clip assets read as corrupted --
     `Clips_MeleeBasic`'s was `12000000` where the manifest's own source data says `18`;
     `Clips_SupportBasic`'s concatenated two values the same way. `BattleClipPlayer`
     failed safe against it either way (a threshold that's never reached just never
     fires `onImpact`, no crash), so it didn't block anything at the time. **Fixed
     later this session** -- see "Known gaps" for the investigation (root cause not
     fully confirmed; a `JsonUtility` array-parsing edge case is suspected) and the fix
     (hand-corrected to the manifest's real values, guarded by new `ClipMetadataTests`).
   - **Item 1: code-based skill verification, not interactive play.** Two new test
     files, `MpRegenTests.cs` (7 tests: spend/restore clamping, the 25%-50% recovery
     band via 50 rolls, `ComputeManaRestore` scaling) and additions to
     `BattleAssetContentTests.cs` (`SupportUnits_HaveAnAffordableManaRestoreSkill`,
     asserting a real `ComputeManaRestore` value against Linnet's actual base stats,
     plus the Support archetype's expected Skill Move count moving from 3 to 4). All
     pure C#, all safe headless. `BattleAssetBuilder.ContentVersion` bumped to 4.

## Roster

| Unit | Role | Faction | BA (free) | Skill Moves (mana, tap SM) |
|---|---|---|---|---|
| **Kestrel** | Melee | Player | Melee Basic Attack, 1 col | Second Wind (self-heal, 30MP) / Rally (heal ally, 25MP) / Power Strike (heavy hit, 35MP) |
| **Sable** | Ranged | Player | Ranged Basic Attack, any col | Volley (3-wide AoE, 25MP) / Snipe (heavy hit, 30MP) / Barrage (full-team AoE, 45MP) |
| **Linnet** | Support | Player | Support Strike (low power attack) | Heal (20MP) / Mass Heal (AoE heal, 35MP) / Focus Heal (big heal, 30MP) / Mana Spring (restore ally MP, 15MP) |
| **Husk** | Melee | Enemy | same as Kestrel's archetype | same as Kestrel's archetype |
| **Warden** | Ranged | Enemy | same as Sable's archetype | same as Sable's archetype |
| **Stinger** | Support | Enemy | same as Linnet's archetype | same as Linnet's archetype |

**Bench reserves (player)** — sub in for any active player unit via the manual-mode Sub
action, same archetype stats/BA/Skill Moves as their active counterpart, distinct art:
**Thorne** (Melee, reskin of Kestrel's archetype), **Reed** (Ranged, reskin of Sable's),
**Vesper** (Support, reskin of Linnet's).

Skill Move content is heal/damage primitives only (no status-effect system yet -- an
explicit scope decision, see item 7 above). All 10 non-BA skills, plus the relocated
Heal and M12's Mana Spring, live in `CharacterDefinition.skillMoves`; `standardSkill`
is always BA.

**MP has a first-pass economy now (M12), not a tuned one.** Sources: a small trickle
(+4) from a unit's own BA, 25%-50% of missing MP restored per unit between maps 1 and
2 (HP still doesn't recover there -- see `BattleWorld`'s carry-over doc), and Mana
Spring as an active, targeted top-up. There's still no per-turn passive regen and no
potion/item system (no inventory exists in this slice at all) -- see "Known gaps."

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
| M11 | Skill Moves built into the assets, content guard + asset-level tests, SM tap, duplicate-label fix | *(not yet tagged)* |
| M12 | MP economy (BA trickle, between-map recovery, Mana Spring), FMV chroma-key components, code-based Skill Move tests | *(not yet tagged)* |

Each of M0-M2's commits has a `NOTES.md` snapshot under
`AI.Game Commits/battle-slice/<milestone>/` and a zip under `releases/zips/`. That
per-milestone full-tree snapshot step was dropped from M3 onward (M4's status note
already flagged it as skipped for time) — it duplicated the whole `Unity/` tree per
milestone for state git history already gives you for free. M6/M7 follow the M3-M5
precedent: commit + docs update, no snapshot/zip.

## Controls

`T` toggle auto/manual mode · `L` open/close the turn log · `Esc` pause ·
`Ctrl+Z`/`Ctrl+Y` undo/redo last turn · `R` restart after the battle ends · `?` keybind
legend. Manual mode: a player unit's turn opens a small 4-icon menu under their feet,
all four tapped -- **BA** (free basic attack), **SM** (opens the mana-cost Skill Move
list; tap again to close it), **R** (Reposition), **S** (Sub) -- then click a
highlighted target on the field (BA/SM/Reposition) or pick from the popup (SM's list,
Sub's bench).

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
- **GitHub wiki is unblocked** (was blocked through M9 — GitHub only provisions a
  repo's wiki git backend after a page is saved once through the web UI, with no
  API/git-push way around it; the project owner has since created that first page). The
  4 pages (Home, Battle-System, Art-Pipeline, Roadmap) live at
  `S:\AI\Game\AI.Game.wiki\` — a **separate git clone**, not part of the main repo, so
  it needs its own commit + push alongside the main one:
  `cd "S:\AI\Game\AI.Game.wiki" && git add -A && git commit && git push`.
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
- **Resolved in M11: the 9-skill Skill Move content is now actually built into the
  assets** (verified on disk -- all 12 skill assets and 7 patterns present, all 9
  characters carrying `maxMp: 100` and 3 `skillMoves` references, **zero
  `m_Script: {fileID: 0}` corruption**), and `BattleContentGuard` re-runs the builder
  automatically whenever it goes stale, so "authored in C#, never written to disk" can't
  silently recur. That rebuild also ran through an interactive Editor session and came
  out clean, which is further evidence for the "interactive is safe, headless
  `-executeMethod` isn't" hypothesis below.
- **Resolved in M12: a first-pass MP economy exists** (BA trickle, between-map partial
  recovery, Mana Spring) -- see item 9 above. Still open: no *per-turn* passive regen
  (a unit that dumps its whole pool mid-battle is genuinely tapped out until the next
  map), and no potion/item system -- the project owner named "recover through ... a
  potion-like item" as a goal, but there's no inventory system in this slice at all to
  hang it on. `BattleUnit.RestoreMp(int)` is the primitive a future item system should
  call; nothing else about it exists yet. `RestoreMpFull()` is similarly just a hook --
  no farm/town "sleep" system calls it, since no persistence layer connects battle party
  state to the farm scene.
- **Resolved: FMV clip `impactFrames` metadata hand-fixed.** `Clips_MeleeBasic`/
  `RangedBasic`/`SupportBasic` had `impactFrames: 12000000` etc. -- a bare scalar,
  where every other `List<int>` in this project's assets serializes as a normal YAML
  block list, holding a value in the millions where the manifest's own ground-truth
  data (`Tools/ComfyUI/manifest.export.json`, `impact_frames: [18]` etc.) says a small
  one. **Root cause not fully confirmed** -- the JSON itself is clean and
  `BattleAssetBuilder.BuildClipSet`'s logic (`new List<int>(clipAsset.impact_frames)`)
  looks correct by inspection, and the corrupted values were byte-for-byte identical
  across two independent `Build()` runs months apart (confirmed via `git log` on
  `Clips_MeleeBasic.asset` — untouched since the M3 commit despite M11's rebuild
  running `BuildClipSet` unconditionally), which rules out a stale-manifest
  explanation. A `JsonUtility` array-parsing edge case is suspected but unproven — a
  live diagnostic (`JsonUtility.FromJson` dumped via a throwaway EditMode test) was the
  next step but couldn't run this session (the interactive Editor was open, and
  headless can't share the project lock). **Fixed directly**: hand-edited the 3 asset
  files to the manifest's real values (melee 18, ranged 22, heal 16+24) in correct
  block-list YAML. `ClipMetadataTests.cs` (new) asserts every impact frame is under a
  sane bound (10,000) and would fail loudly if this regresses -- including from a
  future `Build Assets From Manifest` re-run, if the underlying bug turns out to still
  be live. **Run that test once Unity is closed** to confirm the fix holds; it hasn't
  been run yet this session.

## Natural next steps, roughly in priority order

1. **Run the full test suite once Unity is closed** to confirm both the M12 rebuild
   (38/38 expected, including the new `ClipMetadataTests`) and the `impactFrames`
   hand-fix actually
   hold -- neither has been verified headlessly yet as of the fix landing.
2. **A potion/item system**, if the project owner wants the third MP-recovery source
   from M12's design to actually exist -- needs an inventory concept this slice doesn't
   have at all yet, so it's a bigger lift than the other two economy pieces were.
3. **A per-turn passive MP regen tick**, if between-map recovery alone proves too
   sparse in practice once the project owner plays a few battles.
4. Choose/build final FMV clip assets (Unity Asset Store base or new ComfyUI
   generations, per the project owner's plan) once the components above are solid --
   the plumbing is ready, only the content is placeholder. Now that `impactFrames` is
   real data, frame-accurate impact sync (flash/FX synced to the clip's own hit frame
   instead of firing at clip start) is worth building too.
5. On-device Android verification, whenever a session has `adb` access.
6. Consider a real status-effect system if future Skill Moves need to go beyond
   heal/damage (buffs, shields, taunt) — explicitly deferred in M10, see item 7
   under "What changed."
7. Beyond the vertical slice: the roster is currently 6 fixed archetypes plus 3 bench
   reserves, no save/persistence. FOUNDATION.md's broader systems (tier/fusion, gacha,
   farm/town economy) are designed but not connected to this battle system yet —
   that's the actual "rest of the game," this slice only proves the battle screen works.
