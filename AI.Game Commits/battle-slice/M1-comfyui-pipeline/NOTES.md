# M1 — ComfyUI asset generation pipeline

**Tag:** `v0.4.0-m1-comfyui-pipeline`
**Branch:** `feature/battle-slice`

## What works

- `manifest.yaml` describes all 17 battle-slice assets (1 background, 6 sprites,
  6 portraits, 3 attack clips, 1 FX flipbook) with pinned seeds and confirmed models
  (`Krea2\moodyKrea2Mix_v50.safetensors` for stills, MiniMax H3 ref2va + turbo LoRA
  for clips — both confirmed with the project owner, avoiding the NSFW-branded Krea2
  variants also present on this ComfyUI install).
- `generate.py --dry-run` lists all 17 assets correctly (M1 acceptance criteria #1).
- `generate.py --only bg_battle01` produced a real, correct 1920x1080 PNG at
  `Unity/Assets/Art/Generated/backgrounds/bg_battle01.png` (M1 acceptance criteria
  #2) — a genuine anime-style battle arena on an archipelago coastline, matching the
  desaturated/muted target palette. Included in this snapshot as proof.
- Four ComfyUI API-format workflows, each traced from real `/object_info` schemas and
  validated against the live API (not the UI graph format, not guessed): background
  generation was validated directly (background image proof above); character_sprite
  and character_clip were validated by a background research agent with real
  `/prompt` submissions before this milestone's code was finalized; fx.json was
  validated by that same agent (chroma-keyed transparent PNG frames confirmed).
- `postprocess.py`: color-distance chroma-key to alpha (used for sprites and, in
  "prekeyed" mode, to just pack already-transparent FX frames), nearest-neighbour
  downsample, adaptive palette quantization (placeholder — no fixed master palette
  file exists yet, see caveat below), Lanczos resize for portraits/backgrounds,
  ffmpeg video re-encode, FX sprite-sheet packing with a frame-rect JSON sidecar.

## What's stubbed / known gaps

- Only 1 of 17 assets has actually been generated (the background, as pipeline
  proof). The remaining 16 are M2's job — `generate.py` (no `--only`) will produce
  them all.
- **No fixed master palette file.** `postprocess.quantize_palette()` uses PIL's
  adaptive median-cut quantization as a placeholder. FOUNDATION.md says the palette
  is "defined as the Wuthering Waves palette" but no concrete hex list has been
  authored anywhere in the project yet — this needs a real decision, not a guess,
  before final art quality matters (placeholder is fine for a functional slice).
- **Sprite chroma-keying is weaker than FX's.** `fx.json` has a dedicated
  `CRTChromaKeyOverlay` ComfyUI node baked into the graph; `character_sprite.json`
  does not (only core nodes, no custom-node dependency for stills) — sprites rely on
  postprocess.py's own color-distance keying against a "plain solid color background"
  prompt instruction, which is less reliable. Acceptable for placeholder-quality
  slice art per the brief's "ugly-but-visible beats pretty-but-blocked" directive;
  worth revisiting (e.g. adding the same chroma-key node to the sprite workflow) if
  edges look rough once all 6 sprites exist.
- **MiniMax H3's `length` input is a request, not the actual frame count** (confirmed:
  32 requested -> 39 produced; 8 requested -> 22 produced). `generate.py` reads the
  real count back via `ffprobe` for clips and evenly samples down to the requested
  count for the FX flipbook, rather than trusting the input. Worth knowing if anyone
  tunes `frame_count` in the manifest later.
- Full gotcha list (image upload requirement, `ref_images` link-array format, the
  `"gifs"` output key on `VHS_VideoCombine`, no negative-prompt node on the MiniMax H3
  path, per-effect chroma key color): documented in `Tools/ComfyUI/README.md`.

## What's next

M2: run `generate.py` for the remaining 16 assets, then build Unity import automation
(`GeneratedAssetImporter.cs` AssetPostprocessor, `AI.Game -> Battle -> Build Assets
From Manifest` menu item that writes `ClipSet`/`SkillPattern`/`SkillDefinition`/
`CharacterDefinition`/`MapDefinition` ScriptableObjects into `Resources/Battle/`).

## Revert

`git revert ef2c99f` (single commit on top of M0, safe to revert in isolation) or
`git checkout v0.4.0-m0-scaffold` to go back further.
