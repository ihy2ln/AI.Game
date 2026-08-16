# Changelog — Battle Vertical Slice

Reverse-chronological history of the `feature/battle-slice` branch. For *current*
state (what's done, what's known-broken, what's next) see `PROJECT-README.md` instead
— this file is a record of what shipped when, not a living status doc.

## v0.4.0-battle-slice — M0-M8 (2026-08-16)

The full vertical slice: a playable, real-art, side-view party battle with auto and
manual/turn-based modes, a three-panel presentation, a reviewable turn log, and
modern-RPG UX (pause, settings, multi-step undo/redo).

- **M8 — Pause, settings, undo/redo, dock-spacing bugfix.** `BattleHistory` is a
  multi-step undo/redo stack (`Ctrl+Z`/`Ctrl+Y`) snapshotting unit HP/MP + the full
  turn log once per turn. Pause (`Esc`) and battle speed (0.5-4x) both ride on
  `Time.timeScale`. `BattleSettings` persists via `PlayerPrefs`. Also fixes a real
  dock-overlap bug found during visual verification (see M7).
- **M7 — Three-panel cinematic layout.** Allies dock left, enemies dock right, and
  the acting unit + its target tween into an empty centre "stage" for each turn's
  action before returning to their dock. Combat logic still only reasons about
  `Column` — the staging is presentation-only.
- **M6 — Turn log.** `BattleLog` records every turn as a round-tagged entry; a
  scrollable review panel (`L`) shows the full history.
- **M5 — Android build.** APK builds clean (IL2CPP, ARM64, min API 26); on-device
  install/verification blocked (no `adb` on the build machine at the time).
- **M4 — Curated HD roster art + manual mode.** Six named characters (Kestrel,
  Sable, Linnet vs. Husk, Warden, Stinger) replace generic placeholders, sourced from
  a pre-existing curated art library. Manual/turn-based mode (`T` to toggle) lets the
  player choose targets by clicking a highlighted unit.
- **M3 — Side-view battle logic pivot.** Pivoted from `FOUNDATION.md`'s original
  isometric lane/column tactics grid to a Darkest Dungeon / Slay the Spire-style
  static side view, after seeing the isometric version running. Turn queue, targeting,
  and damage resolution are pure C#, covered by EditMode tests.
- **M2 — Generated assets + import automation.** ComfyUI-generated sprites/portraits/
  clips imported and wired into `ScriptableObject` data via `BattleAssetBuilder`.
- **M1 — ComfyUI asset generation pipeline.** Workflows, manifest, generate/postprocess
  scripts for the (now largely superseded by curated art) pixel-art pipeline.
- **M0 — Scaffold.** Branch, `.gitignore`, folder structure, docs stub.

### Known gaps as of this release

- Manual mode's click-to-target and the M8 UI (pause/settings/undo/redo/log buttons)
  are code-complete and unit-tested, but haven't been interactively click-tested —
  see `PROJECT-README.md`'s "Known gaps" for why.
- Android APK builds clean but is unverified on a physical device (no `adb` here).
- FMV clip playback (chroma-keyed video, generated in M1/M2) isn't wired into
  `BattleVisuals` yet — static sprites only.
- Not merged to `main` — this is still a feature-branch vertical slice, not the
  shipped game.

## v0.3.x — Farm

Pre-battle-slice Unity farm scene (Stardew × Rune Factory 4×4 clearing,
Brown Dust 2-inspired isometric lighting). See tags `v0.1.0-farm-map` through
`v0.3.1-farm-aesthetics-2x2`.
