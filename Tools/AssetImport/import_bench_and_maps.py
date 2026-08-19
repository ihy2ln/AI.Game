"""
Import three new player "bench" (reserve/sub-in) characters and two new battle-map
backgrounds from the same pre-existing "aigame" ComfyUI output library import_roster.py
already draws from -- not produced by this repo's Tools/ComfyUI/ pipeline, curated by hand
here instead of regenerated.

Source folders:
  S:\\AI\\ComfyUI_windows_portable\\ComfyUI\\output\\aigame\\charcter\\In game models\\
  S:\\AI\\ComfyUI_windows_portable\\ComfyUI\\output\\aigame\\background\\

None of these are exact matches for what the project owner originally referenced in chat
(no bow-archer/twin-dagger-rogue/gold-robed-cleric renders, no lightning-storm castle
balcony or greenhouse-ruins renders exist in this library) -- picked as the best
role/mood-fit substitutes from what's actually on disk. Trivial to re-point at different
source files later; this script is idempotent (safe to re-run) same as import_roster.py.

Run once: `python Tools/AssetImport/import_bench_and_maps.py`.
"""

from pathlib import Path
from PIL import Image
from import_roster import flood_key_background, crop_to_content

CHAR_SOURCE = Path(r"S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\charcter\In game models")
BG_SOURCE = Path(r"S:\AI\ComfyUI_windows_portable\ComfyUI\output\aigame\background")
REPO_ROOT = Path(__file__).resolve().parents[2]
OUT_ROOT = REPO_ROOT / "Unity" / "Assets" / "Art" / "Generated"

# unit_id -> (display name, source render filename)
BENCH_ROSTER = {
    "player_bench_melee":   ("Thorne", "2026-08-14-120816_krea2TurboFP8_krea2TURBO_204635769987062.png"),
    "player_bench_support": ("Vesper", "2026-08-14-123402_krea2TurboFP8_krea2TURBO_519585312357392.png"),
    "player_bench_ranged":  ("Reed",   "2026-08-13-233648_krea2TurboFP8_krea2TURBO_435324614119522.png"),
}

# map key -> source background filename
MAPS = {
    "bg_battle1": "background_shorebreak_00001_.png",
    "bg_battle2": "PhotoFlow_Krea2_00028_.png",
}


def import_bench_sprites():
    dst_dir = OUT_ROOT / "battle_sprites"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for unit_id, (name, filename) in BENCH_ROSTER.items():
        src = CHAR_SOURCE / filename
        img = Image.open(src)
        img = flood_key_background(img, tolerance=24)
        img = crop_to_content(img)
        dst = dst_dir / f"char_{unit_id}_battle.png"
        img.save(dst, "PNG")
        print(f"sprite  {unit_id:20s} ({name:7s}) -> {dst.relative_to(REPO_ROOT)}  {img.size}")


def import_bench_portraits():
    dst_dir = OUT_ROOT / "portraits"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for unit_id, (name, filename) in BENCH_ROSTER.items():
        src = CHAR_SOURCE / filename
        img = Image.open(src).convert("RGB")
        img = img.resize((256, 256), Image.LANCZOS)
        dst = dst_dir / f"char_{unit_id}_portrait.png"
        img.save(dst, "PNG")
        print(f"portrait {unit_id:20s} ({name:7s}) -> {dst.relative_to(REPO_ROOT)}")


def import_backgrounds():
    dst_dir = OUT_ROOT / "backgrounds"
    dst_dir.mkdir(parents=True, exist_ok=True)
    for key, filename in MAPS.items():
        src = BG_SOURCE / filename
        img = Image.open(src).convert("RGB")
        dst = dst_dir / f"{key}.png"
        img.save(dst, "PNG")
        print(f"background {key:12s} -> {dst.relative_to(REPO_ROOT)}  {img.size}")


if __name__ == "__main__":
    import_bench_sprites()
    import_bench_portraits()
    import_backgrounds()
    print("done")
