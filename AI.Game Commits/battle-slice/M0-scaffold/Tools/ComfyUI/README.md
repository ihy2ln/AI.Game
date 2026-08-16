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
