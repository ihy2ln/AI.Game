# M2 — generated assets + Unity import automation

**Tag:** `v0.4.0-m2-assets`
**Branch:** `feature/battle-slice`

## What works

All 17 battle-slice assets generated and verified (included in this snapshot):
- `backgrounds/bg_battle01.png` — 1920x1080.
- 6x `sprites/char_*_sprite32.png` — 32x32, real alpha transparency confirmed
  (13.8%-56.9% transparent pixels depending on character, verified via numpy, not
  just visual inspection).
- 6x `portraits/char_*_portrait.png` — 256x256.
- 3x `clips/clip_*_basic.mp4` — h264, 512x768, 24fps, ~39 frames (~1.6s) each.
- `fx/fx_hit_impact_sheet.png` + `.json` — 8-frame packed sheet, transparent,
  frame-rect sidecar for runtime slicing.

`GeneratedAssetImporter.cs` and `BattleAssetBuilder.cs` are written but **not run** --
no local Unity Editor install exists on this machine. Written carefully against
documented Unity APIs and cross-checked against this project's own conventions, but
genuinely unverified until an Editor opens the project and runs `AI.Game -> Battle ->
Build Assets From Manifest`.

## Bugs found and fixed during generation (worth knowing before touching this pipeline)

1. **int16 overflow in `postprocess.key_to_alpha`'s color-distance calc.** Squared
   per-channel diffs run up to 65025 each, overflowing int16 (max 32767) and wrapping
   to garbage values. Silently produced a numpy `RuntimeWarning: invalid value
   encountered in sqrt` rather than crashing -- easy to miss. Fixed: float32 instead
   of int16.
2. **A fixed `#00FF00` chroma key under-keys sprite backgrounds.** Krea2's "green
   screen" render has a real lighting gradient/vignette, not a flat color -- most of
   the frame sits far outside any reasonable fixed-key tolerance. Fixed two ways:
   sprites now sample their own background color per-image
   (`postprocess.sample_background_key`, averages the four corner blocks) instead of
   trusting one project-wide key, AND the sprite prompt template was strengthened
   (`sprite_chroma_suffix` in manifest.yaml) since "plain solid color background"
   alone was observed rendering as an unkeyable neutral studio-brown backdrop, not
   green at all.
3. **`fx.json`'s `CRTChromaKeyOverlay` node did not reliably key in this run** --
   despite the M1 validation agent confirming it worked in their test, this run's
   output came back fully opaque with a dark brown background instead of the
   requested blue. Rather than debug a fragile custom node further, fx frames now go
   through the same proven Python-side keying as sprites (`prekeyed: false` in
   manifest.yaml, keyed against the actually-observed `#3F200D`).

Net effect: none of the three original "keying" assumptions from M1 survived contact
with a full real generation run unchanged. All fixes are in `postprocess.py` /
`generate.py` / `manifest.yaml`, not one-off manual edits to the output files, so a
future `--force` regeneration stays reliable.

## Known remaining rough edges (acceptable for a placeholder slice)

- `char_enemy_support_sprite32.png` keys weakest of the six (13.8% transparent) --
  visible green fringe remains at the bottom. FOUNDATION.md already expects manual
  Aseprite cleanup per character regardless ("automated downsampling alone reads as
  wrong") -- this is exactly that expected gap, not a pipeline bug.
- No fixed master palette exists yet -- `quantize_palette()` still uses adaptive
  median-cut as a placeholder (see M1 notes).

## What's next

M3: headless battle logic (grid, turn order, targeting, damage) with EditMode tests,
placeholder-art-only battle scene, mirroring `FarmBootstrap`'s code-first pattern.

## Revert

`git revert 8d8a308` (single commit on top of M1, safe to revert in isolation).
