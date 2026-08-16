"""
One-off import of the pre-existing curated "aigame" character roster into the battle
slice, replacing the low-res auto-generated placeholder sprites with real HD art.

Source: S:\\AI\\ComfyUI_windows_portable\\ComfyUI\\output\\aigame\\charcter\\
  (a separate, previously-generated asset library, not produced by this repo's
  Tools/ComfyUI/ pipeline -- curated by hand here instead of regenerated, per the
  project owner's "no more asset generation" instruction).

Six named characters happened to exist there already (husk, kestrel x3 variants,
linnet, sable, stinger, warden), matching this project's 6-archetype roster exactly:
melee/ranged/support x2 factions. Mapped by visual read (see ROSTER below), not by
any embedded metadata -- there isn't any.

Run once: `python Tools/AssetImport/import_roster.py`. Idempotent (safe to re-run).
"""

from pathlib import Path
from PIL import Image, ImageDraw

SOURCE = Path(r"S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\charcter")
FX_SOURCE = Path(r"S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\effect")
REPO_ROOT = Path(__file__).resolve().parents[2]
OUT_ROOT = REPO_ROOT / "Unity" / "Assets" / "Art" / "Generated"

# unit_id -> (display name, source sprite filename, source portrait filename)
ROSTER = {
    "player_melee":  ("Kestrel", "sprite_kestrel_00002_.png", "portrait_kestrel_00001_.png"),
    "player_ranged": ("Sable",   "sprite_sable_00001_.png",   "portrait_sable_00001_.png"),
    "player_support": ("Linnet", "sprite_linnet_00001_.png",  "portrait_linnet_00001_.png"),
    "enemy_melee":   ("Husk",    "sprite_husk_00001_.png",    "portrait_husk_00001_.png"),
    "enemy_ranged":  ("Warden",  "sprite_warden_00001_.png",  "portrait_warden_00001_.png"),
    "enemy_support": ("Stinger", "sprite_stinger_00001_.png", "portrait_stinger_00001_.png"),
}


def flood_key_background(img: Image.Image, tolerance: int = 24) -> Image.Image:
    """Flood-fill from the border inward, not a per-pixel threshold -- so enclosed
    background-colored pockets inside the silhouette (rare) stay opaque, and
    background-colored clothing connected to the silhouette (e.g. a white shirt)
    stays intact since it's not connected to the border."""
    img = img.convert("RGBA")
    w, h = img.size
    seeds = [
        (0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
        (w // 2, 0), (0, h // 2), (w - 1, h // 2), (w // 2, h - 1),
    ]
    for seed in seeds:
        if img.getpixel(seed)[3] != 0:
            ImageDraw.floodfill(img, seed, (0, 0, 0, 0), thresh=tolerance)
    return img


def crop_to_content(img: Image.Image, padding: int = 12) -> Image.Image:
    bbox = img.getbbox()
    if bbox is None:
        return img
    left, top, right, bottom = bbox
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(img.width, right + padding)
    bottom = min(img.height, bottom + padding)
    return img.crop((left, top, right, bottom))


def import_sprites():
    dst_dir = OUT_ROOT / "battle_sprites"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for unit_id, (name, sprite_file, _) in ROSTER.items():
        src = SOURCE / "In game models" / sprite_file
        img = Image.open(src)
        img = flood_key_background(img, tolerance=24)
        img = crop_to_content(img)
        dst = dst_dir / f"char_{unit_id}_battle.png"
        img.save(dst, "PNG")
        print(f"sprite  {unit_id:16s} ({name:8s}) -> {dst.relative_to(REPO_ROOT)}  {img.size}")


def import_portraits():
    dst_dir = OUT_ROOT / "portraits"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for unit_id, (name, _, portrait_file) in ROSTER.items():
        src = SOURCE / "portrait" / portrait_file
        img = Image.open(src).convert("RGB")
        img = img.resize((256, 256), Image.LANCZOS)
        dst = dst_dir / f"char_{unit_id}_portrait.png"
        img.save(dst, "PNG")
        print(f"portrait {unit_id:16s} ({name:8s}) -> {dst.relative_to(REPO_ROOT)}")


def import_fx(frame_count: int = 10, key_tolerance: int = 70, frame_size: int = 96):
    frames = sorted(FX_SOURCE.glob("fx_impact_*.png"))
    if not frames:
        print("no fx_impact frames found, skipping")
        return
    step = len(frames) / frame_count
    sampled = [frames[int(i * step)] for i in range(frame_count)]

    keyed = []
    for f in sampled:
        img = Image.open(f).convert("RGBA")
        # 4-corner-only seeding left the corners un-keyed on several frames -- the
        # star-burst's diagonal rays reach the corners and block flood-fill from
        # spreading along the diagonal, isolating background pockets a corner seed
        # can't reach. The same 8-seed pattern (+ edge midpoints) used for character
        # sprites has enough entry points to get past that.
        img = flood_key_background(img, tolerance=key_tolerance)
        img = img.resize((frame_size, frame_size), Image.LANCZOS)
        keyed.append(img)

    cols = 5
    rows = (frame_count + cols - 1) // cols
    sheet = Image.new("RGBA", (frame_size * cols, frame_size * rows), (0, 0, 0, 0))
    rects = []
    for i, frame in enumerate(keyed):
        x = (i % cols) * frame_size
        y = (i // cols) * frame_size
        sheet.paste(frame, (x, y))
        rects.append({"index": i, "x": x, "y": y, "w": frame_size, "h": frame_size})

    dst_dir = OUT_ROOT / "fx"
    dst_dir.mkdir(parents=True, exist_ok=True)
    sheet.save(dst_dir / "fx_hit_impact_sheet.png", "PNG")
    import json
    (dst_dir / "fx_hit_impact_sheet.json").write_text(
        json.dumps({"frameWidth": frame_size, "frameHeight": frame_size, "frames": rects}, indent=2)
    )
    print(f"fx -> {dst_dir.relative_to(REPO_ROOT)}/fx_hit_impact_sheet.png ({frame_count} frames from {len(frames)})")


ASSETFORGE_URL = "http://127.0.0.1:8420"


def register_with_assetforge():
    """Registers the processed output folders with the AssetForge asset manager
    (local app, separate from this pipeline -- S:\\AI\\Game Engine\\assetforge) so the
    project owner can browse/re-edit them there without re-running this script.
    Best-effort: silently skipped if AssetForge isn't running."""
    import urllib.request
    import urllib.error

    def api_post(path: str, payload: dict):
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            f"{ASSETFORGE_URL}{path}", data=data,
            headers={"Content-Type": "application/json"}, method="POST",
        )
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode("utf-8"))

    def api_get(path: str):
        with urllib.request.urlopen(f"{ASSETFORGE_URL}{path}", timeout=10) as resp:
            return json.loads(resp.read().decode("utf-8"))

    try:
        api_get("/api/health")
    except (urllib.error.URLError, OSError):
        print("AssetForge not reachable at 127.0.0.1:8420 -- skipping library registration")
        return

    folders = ["battle_sprites", "portraits", "fx"]
    for folder in folders:
        path = str(OUT_ROOT / folder)
        result = api_post("/api/assets/import", {"path": path, "mode": "copy", "recursive": True})
        print(f"assetforge import {folder} -> {result}")

    tagged = 0
    for folder in folders:
        for asset in api_get("/api/assets?q=")["items"]:
            if folder in asset.get("dir_path", "") and "AI.Game" in asset.get("dir_path", ""):
                api_post(f"/api/assets/{asset['id']}/tags", {"tags": ["battle-roster", folder]})
                tagged += 1
    print(f"assetforge tagged {tagged} assets with battle-roster")


if __name__ == "__main__":
    import_sprites()
    import_portraits()
    import_fx()
    register_with_assetforge()
    print("done")
