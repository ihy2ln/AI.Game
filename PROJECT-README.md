# Battle Vertical Slice — status

Tracks the `feature/battle-slice` branch. Task brief:
`S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md`. Full design:
`S:\AI\Game\FOUNDATION.md`.

**Combat model:** lane/column tactics grid from FOUNDATION.md §1 (movement, height, jump
all as designed there) — **not** Darkest Dungeon rank-formation combat. "Darkest Dungeon"
in the brief refers only to camera framing: fixed, non-rotating, static angle, no free
3D rotation. Presentation for this slice is the Brown Dust 2–style unified battlefield
view (units on the grid, FMV plays in place) per FOUNDATION.md §2.2.

**Art pipeline:** pixel sprites + chroma-keyed FMV clips (FOUNDATION.md §3), generated via
local ComfyUI (Krea2 for stills, Minimax H3 for clips). Asset Forge (low-poly 3D) is
deferred — not used in this slice.

## Milestones

| # | Milestone | Tag | Status |
|---|---|---|---|
| M0 | Branch, `.gitignore`, folder scaffold, docs stub | `v0.4.0-m0-scaffold` | Done |
| M1 | ComfyUI pipeline (workflows, manifest, generate.py, postprocess.py) | `v0.4.0-m1-comfyui-pipeline` | Done |
| M2 | Generated assets + import automation + ScriptableObject build | `v0.4.0-m2-assets` | Not started |
| M3 | Battle logic (grid, turns, targeting, damage) + tests, placeholder art | `v0.4.0-m3-battle-logic` | Not started |
| M4 | Visuals: sprites, background, chroma-key FMV, HUD, damage numbers | `v0.4.0-m4-visuals` | Not started |
| M5 | Android build + on-device verification | `v0.4.0-m5-android-apk` | Not started |

## Known environment gaps

- **No local Unity Editor install found** at the path the brief expects
  (`S:\AI\Unity\UnityEditors\Editor\6000.5.7f1`). Code will be written against Unity
  6000.5.7f1 APIs but cannot be compiled/verified in-editor by the assistant until this is
  resolved. Affects M2 (import verification), M4 (Play Mode check), M5 (build).
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

## Next up

M2: generate the remaining 16 assets, then Unity import automation
(`GeneratedAssetImporter.cs`, `AI.Game -> Battle -> Build Assets From Manifest`).
