# ComfyUI asset pipeline

Local ComfyUI instance driven over its HTTP API (`127.0.0.1:8188` by default).

| Path | Purpose |
|---|---|
| `workflows/` | One exported workflow JSON per asset type (`character_sprite.json`, `character_clip.json`, `background.json`, `fx.json`) |
| `manifest.yaml` | Every asset to generate: id, type, prompt, seed, output path, frame count / impact frames for clips |
| `generate.py` | Driver: reads the manifest, submits to `/prompt`, polls `/history`, writes outputs. Supports `--dry-run` and `--force` |
| `postprocess.py` | Background removal, frame-sequence → `.mp4` (ffmpeg), FX sprite-sheet packing |

Models: **Krea2** for stills (sprites, portraits, backgrounds), **Minimax H3** for FMV
clips. Outputs land in `Unity/Assets/Art/Generated/<type>/`.

Seeds are pinned in `manifest.yaml` — same manifest + same models = same assets,
regeneration is deterministic and reviewable as a diff.

Added M1 of the battle vertical slice (see
`S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md` §3).

## Workflow node graphs — real, validated against the live API

Each `workflows/<name>.json` is ComfyUI API format (flat `{node_id: {class_type,
inputs}}`), built directly from `/object_info` schemas and confirmed working via a
real `/prompt` submission, not hand-guessed from the UI graph format. Which node/input
each manifest field patches lives in the matching `workflows/<name>.patchmap.json`, so
`generate.py` never hardcodes node ids.

Non-obvious things worth knowing before touching these:

- **No `CheckpointLoaderSimple` anywhere** — this ComfyUI install has none registered.
  Both Krea2 and MiniMax H3 load through split `UNETLoader` / `CLIPLoader` /
  `VAELoader` nodes instead. The model filenames are baked into the saved workflow
  JSON as defaults (not patched per-generation, since they're fixed per pipeline).
- **`character_clip.json`'s reference image must be uploaded to ComfyUI first.**
  `LoadImage` takes a filename already present in ComfyUI's own `input/` directory,
  not a filesystem path — `generate.py` calls `POST /upload/image` before submitting.
- **`MiniMaxH3ReferenceToVideo.ref_images` is a dynamic (`COMFY_AUTOGROW_V3`) input.**
  In API format it's a list of link pairs, e.g. `"ref_images": [["5", 0]]` — the
  UI-style key `ref_image_0` fails at runtime (confirmed via a real rejected
  `/prompt` submission). This wiring lives in the saved workflow file already;
  `generate.py` only needs to patch what `LoadImage` (node 5) points to.
- **`length` on the MiniMax H3 nodes is a request, not the actual output frame
  count.** 32 requested produced 39 frames; 8 requested (fx) produced 22. The model
  quantizes internally. Don't assume the manifest's `frame_count` matches the real
  clip — `generate.py` reads the true count back from the output file.
- **No negative-prompt node in `character_clip.json` / `fx.json`.** Every real
  MiniMax H3 workflow inspected pairs `MiniMaxH3ReferenceToVideo` with `BasicGuider`
  (guidance-free), never `CFGGuider` — a decorative unused negative-prompt node was
  deliberately left out rather than wired to nothing.
- **`VHS_VideoCombine`'s output key in `/history` is `"gifs"`**, regardless of the
  actual format (it's writing h264 mp4 here). `generate.py`'s output-file scan checks
  `images`/`gifs`/`videos` for exactly this reason.
- **`VAEDecodeTiled` is used only for clip/fx video decode**, not for stills — the
  OOM risk that requires it is specific to long/high-res video decode; a 512×512
  still doesn't need it and plain `VAEDecode` is faster.
- **`fx.json`'s chroma key color is per-effect, not fixed green.** The `CRTChromaKeyOverlay`
  node keys `fx_hit_impact` on blue (`#0000FF`) instead of the project's usual green,
  because the effect's own orange/fire palette overlaps green enough that green keying
  left visible fringing. Pick a key color far from the effect's own palette; expect to
  tune it per effect in `manifest.yaml`'s `chroma_key_override` field.
- **`fx.json` frames arrive already keyed to transparency** (the `CRTChromaKeyOverlay`
  node runs inside the ComfyUI graph) — `postprocess.pack_fx_sheet` must not re-key
  them, only downsample and pack. Sprites (`character_sprite.json`) have no such node,
  so they still rely on `postprocess.py`'s own color-distance keying against a
  "plain solid color background" prompt — weaker than a dedicated keyer, acceptable
  for placeholder-quality slice art, worth revisiting if sprite edges look rough.
