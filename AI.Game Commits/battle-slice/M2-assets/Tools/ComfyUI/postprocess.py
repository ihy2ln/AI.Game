"""
Post-processing for battle-slice ComfyUI output. Called by generate.py after each
asset finishes generating; can also be run standalone on already-generated files
(useful for re-tuning downsample/palette/chroma settings without re-running ComfyUI).

Pipeline per FOUNDATION.md 3.2-3.3:
  render at fixed template framing -> downsample nearest-neighbour -> quantize to
  a master palette -> (manual Aseprite cleanup happens outside this script).

No master palette file exists yet (FOUNDATION.md says "defined as the Wuthering Waves
palette" but no concrete hex list has been authored) -- quantize_palette() uses PIL's
adaptive median-cut quantization as a stand-in. Swap in a fixed palette .act/.pal file
here once one exists; this is a known placeholder, not a guess to build on.
"""

import argparse
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image
import numpy as np


def hex_to_rgb(hex_color: str) -> tuple[int, int, int]:
    h = hex_color.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4))


def key_to_alpha(img: Image.Image, key_hex: str, tolerance: int) -> Image.Image:
    """Color-distance chroma key -> RGBA with keyed pixels made transparent.
    Same technique as the video chroma-key shader, applied to a still image, so we
    don't need an ML background-removal dependency for solid-background AI output."""
    img = img.convert("RGB")
    # float32, not int16 -- squared per-channel diffs run up to 255**2=65025 each,
    # which overflows int16 (max 32767) and wraps, producing garbage distances (this
    # was silently happening: numpy warns "invalid value encountered in sqrt" from the
    # resulting negative values rather than raising, easy to miss).
    arr = np.asarray(img).astype(np.float32)
    key = np.array(hex_to_rgb(key_hex), dtype=np.float32)
    dist = np.sqrt(((arr - key) ** 2).sum(axis=-1))
    alpha = np.where(dist <= tolerance, 0, 255).astype(np.uint8)
    rgba = np.dstack([np.asarray(img), alpha])
    return Image.fromarray(rgba, mode="RGBA")


def sample_background_key(img: Image.Image, margin: int = 6) -> str:
    """Average the four corner blocks and return them as a hex color. A fixed
    '#00FF00' key was observed to under-key sprite backgrounds in practice --
    Krea2's "green screen" render isn't flat, it has a lighting gradient/vignette
    that puts most of the frame well outside any reasonable fixed-key tolerance.
    Sampling per-image is what actually gets clean transparency out of this model."""
    arr = np.asarray(img.convert("RGB"))
    h, w = arr.shape[:2]
    m = min(margin, h // 2, w // 2)
    corners = np.concatenate([
        arr[0:m, 0:m].reshape(-1, 3), arr[0:m, w - m:w].reshape(-1, 3),
        arr[h - m:h, 0:m].reshape(-1, 3), arr[h - m:h, w - m:w].reshape(-1, 3),
    ])
    r, g, b = corners.mean(axis=0)
    return "#%02X%02X%02X" % (int(r), int(g), int(b))


def downsample_nearest(img: Image.Image, w: int, h: int) -> Image.Image:
    return img.resize((w, h), Image.NEAREST)


def resize_smooth(img: Image.Image, w: int, h: int) -> Image.Image:
    return img.resize((w, h), Image.LANCZOS)


def quantize_palette(img: Image.Image, max_colors: int = 48) -> Image.Image:
    """Placeholder adaptive quantization -- see module docstring."""
    has_alpha = img.mode == "RGBA"
    if has_alpha:
        alpha = img.getchannel("A")
        rgb = img.convert("RGB")
    else:
        rgb = img.convert("RGB")
    quantized = rgb.quantize(colors=max_colors, method=Image.MEDIANCUT).convert("RGB")
    if has_alpha:
        quantized = quantized.convert("RGBA")
        quantized.putalpha(alpha)
    return quantized


def process_sprite(
    src_path: Path,
    dst_path: Path,
    final_w: int,
    final_h: int,
    chroma_key: str,
    tolerance: int,
    palette_colors: int = 48,
) -> None:
    """chroma_key is a fallback; the actual key used is sampled from this specific
    image's own corners (see sample_background_key) since a project-wide fixed key
    doesn't survive per-generation lighting variance."""
    img = Image.open(src_path)
    sampled_key = sample_background_key(img)
    img = key_to_alpha(img, sampled_key, tolerance)
    img = downsample_nearest(img, final_w, final_h)
    img = quantize_palette(img, palette_colors)
    dst_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst_path, "PNG")


def process_portrait_or_background(
    src_path: Path, dst_path: Path, final_w: int, final_h: int
) -> None:
    img = Image.open(src_path).convert("RGB")
    img = resize_smooth(img, final_w, final_h)
    dst_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst_path, "PNG")


def probe_frame_count(path: Path) -> int:
    """MiniMax H3's `length` input is a request, not the actual output frame count
    (confirmed empirically: 32 requested -> 39 produced, 8 requested -> 22 produced,
    the model quantizes internally). Read the truth back from the file instead of
    trusting what was asked for."""
    result = subprocess.run(
        [
            "ffprobe", "-v", "error", "-count_frames", "-select_streams", "v:0",
            "-show_entries", "stream=nb_read_frames", "-of", "csv=p=0", str(path),
        ],
        capture_output=True, text=True, check=True,
    )
    return int(result.stdout.strip())


def encode_video(src_path: Path, dst_path: Path, fps: int) -> None:
    """Re-encode whatever ComfyUI produced (mp4/webm/frame sequence) to H.264 mp4 at
    the exact fps recorded in the manifest, so impact_frames timing stays honest."""
    dst_path.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        "ffmpeg", "-y",
        "-i", str(src_path),
        "-r", str(fps),
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-an",
        str(dst_path),
    ]
    subprocess.run(cmd, check=True, capture_output=True)


def encode_video_from_frames(frame_dir: Path, pattern: str, dst_path: Path, fps: int) -> None:
    dst_path.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        "ffmpeg", "-y",
        "-framerate", str(fps),
        "-i", str(frame_dir / pattern),
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        str(dst_path),
    ]
    subprocess.run(cmd, check=True, capture_output=True)


def pack_fx_sheet(
    frame_paths: list[Path],
    dst_png: Path,
    dst_json: Path,
    frame_w: int,
    frame_h: int,
    chroma_key: str,
    tolerance: int,
    cols: int = 4,
    prekeyed: bool = False,
) -> None:
    """prekeyed=True means the source frames already have real alpha (e.g. produced by
    a ComfyUI chroma-key node) -- re-running key_to_alpha on those would flatten the
    existing alpha via the RGB convert() and corrupt already-transparent pixels."""
    if prekeyed:
        frames = [Image.open(p).convert("RGBA") for p in frame_paths]
    else:
        frames = [key_to_alpha(Image.open(p), chroma_key, tolerance) for p in frame_paths]
    frames = [downsample_nearest(f, frame_w, frame_h) for f in frames]
    rows = (len(frames) + cols - 1) // cols
    sheet = Image.new("RGBA", (frame_w * cols, frame_h * rows), (0, 0, 0, 0))
    rects = []
    for i, frame in enumerate(frames):
        x = (i % cols) * frame_w
        y = (i // cols) * frame_h
        sheet.paste(frame, (x, y))
        rects.append({"index": i, "x": x, "y": y, "w": frame_w, "h": frame_h})
    dst_png.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(dst_png, "PNG")
    dst_json.parent.mkdir(parents=True, exist_ok=True)
    dst_json.write_text(json.dumps({"frameWidth": frame_w, "frameHeight": frame_h, "frames": rects}, indent=2))


def main() -> None:
    parser = argparse.ArgumentParser(description="Standalone postprocess re-run on an existing file")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p_sprite = sub.add_parser("sprite")
    p_sprite.add_argument("src", type=Path)
    p_sprite.add_argument("dst", type=Path)
    p_sprite.add_argument("--w", type=int, required=True)
    p_sprite.add_argument("--h", type=int, required=True)
    p_sprite.add_argument("--key", default="#00FF00")
    p_sprite.add_argument("--tolerance", type=int, default=40)

    p_still = sub.add_parser("still")
    p_still.add_argument("src", type=Path)
    p_still.add_argument("dst", type=Path)
    p_still.add_argument("--w", type=int, required=True)
    p_still.add_argument("--h", type=int, required=True)

    args = parser.parse_args()
    if args.cmd == "sprite":
        process_sprite(args.src, args.dst, args.w, args.h, args.key, args.tolerance)
    elif args.cmd == "still":
        process_portrait_or_background(args.src, args.dst, args.w, args.h)
    print(f"wrote {args.dst}")


if __name__ == "__main__":
    sys.exit(main())
