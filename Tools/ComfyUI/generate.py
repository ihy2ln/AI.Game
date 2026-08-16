"""
Battle-slice asset generator. Reads manifest.yaml, submits each asset to the local
ComfyUI HTTP API, waits for completion, and hands the result to postprocess.py.

Usage:
    python generate.py --dry-run              # list what would be generated
    python generate.py                          # generate everything missing
    python generate.py --force                  # regenerate everything
    python generate.py --only bg_battle01        # generate one asset by id

Each workflow's exact node graph lives in workflows/<name>.json (ComfyUI API format).
Which node id / input key corresponds to "prompt", "seed", "width", etc. for a given
workflow lives in workflows/<name>.patchmap.json -- keeps this script stable if a
workflow gets re-exported from ComfyUI's UI, only the patchmap needs updating.
"""

import argparse
import copy
import json
import sys
import time
import uuid
from pathlib import Path

import requests
import yaml

import postprocess

ROOT = Path(__file__).resolve().parent
REPO_ROOT = ROOT.parent.parent
WORKFLOWS_DIR = ROOT / "workflows"
SCRATCH_DIR = ROOT / ".generated_raw"  # raw ComfyUI output before postprocess, gitignored


def load_manifest() -> dict:
    return yaml.safe_load((ROOT / "manifest.yaml").read_text(encoding="utf-8"))


def export_manifest_json(manifest: dict) -> None:
    """Unity has no YAML parser in this project (no Newtonsoft.Json package either) --
    write a flat JSON mirror of the asset list so the Editor-side asset builder
    (GeneratedAssetImporter.cs / BattleAssetBuilder.cs) can read it with JsonUtility.
    Regenerated on every generate.py run so it never drifts from manifest.yaml."""
    export = {"assets": manifest["assets"], "outputRoot": manifest["output_root"]}
    (ROOT / "manifest.export.json").write_text(json.dumps(export, indent=2), encoding="utf-8")


def load_workflow(name: str) -> dict:
    return json.loads((WORKFLOWS_DIR / name).read_text(encoding="utf-8"))


def load_patchmap(name: str) -> dict:
    patchmap_path = WORKFLOWS_DIR / name.replace(".json", ".patchmap.json")
    if not patchmap_path.exists():
        raise FileNotFoundError(
            f"No patchmap for {name} -- see workflows/README.md. "
            f"Expected {patchmap_path}"
        )
    return json.loads(patchmap_path.read_text(encoding="utf-8"))


def set_node_input(workflow: dict, node_id: str, input_key: str, value) -> None:
    if node_id not in workflow:
        raise KeyError(f"workflow has no node id {node_id!r} (check patchmap)")
    workflow[node_id]["inputs"][input_key] = value


def apply_patchmap(workflow: dict, patchmap: dict, values: dict) -> dict:
    """patchmap maps logical field name -> {node, input}. values maps the same
    logical field names -> the value to set for this asset. Unset fields are left
    at whatever the saved workflow already has."""
    workflow = copy.deepcopy(workflow)
    for field, value in values.items():
        if field not in patchmap:
            continue  # workflow doesn't expose this field (e.g. fx.json has no seed override)
        target = patchmap[field]
        set_node_input(workflow, target["node"], target["input"], value)
    return workflow


class ComfyClient:
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")
        self.client_id = str(uuid.uuid4())

    def upload_image(self, local_path: Path, server_name: str) -> str:
        """LoadImage takes a filename already present in ComfyUI's own input/ dir, not
        a filesystem path. overwrite=true keeps re-runs idempotent under the same name."""
        with open(local_path, "rb") as fh:
            resp = requests.post(
                f"{self.base_url}/upload/image",
                files={"image": (server_name, fh, "image/png")},
                data={"overwrite": "true"},
                timeout=60,
            )
        resp.raise_for_status()
        return resp.json()["name"]

    def submit(self, workflow: dict) -> str:
        resp = requests.post(
            f"{self.base_url}/prompt",
            json={"prompt": workflow, "client_id": self.client_id},
            timeout=30,
        )
        if resp.status_code != 200:
            raise RuntimeError(f"ComfyUI rejected prompt: {resp.status_code} {resp.text}")
        data = resp.json()
        if "error" in data:
            raise RuntimeError(f"ComfyUI workflow validation error: {json.dumps(data, indent=2)}")
        return data["prompt_id"]

    def wait_for_completion(self, prompt_id: str, timeout_s: int = 600, poll_s: float = 2.0) -> dict:
        deadline = time.time() + timeout_s
        while time.time() < deadline:
            resp = requests.get(f"{self.base_url}/history/{prompt_id}", timeout=30)
            history = resp.json()
            if prompt_id in history:
                entry = history[prompt_id]
                status = entry.get("status", {})
                if status.get("completed"):
                    return entry
                if status.get("status_str") == "error":
                    raise RuntimeError(f"ComfyUI job {prompt_id} failed: {json.dumps(status, indent=2)}")
            time.sleep(poll_s)
        raise TimeoutError(f"ComfyUI job {prompt_id} did not complete within {timeout_s}s")

    def fetch_output(self, filename: str, subfolder: str, file_type: str, dst: Path) -> None:
        resp = requests.get(
            f"{self.base_url}/view",
            params={"filename": filename, "subfolder": subfolder, "type": file_type},
            timeout=60,
        )
        resp.raise_for_status()
        dst.parent.mkdir(parents=True, exist_ok=True)
        dst.write_bytes(resp.content)


def find_output_files(history_entry: dict) -> list[dict]:
    """Walk a /history entry's outputs and return every image/gif/video file the run
    produced, across every node (SaveImage, VHS_VideoCombine, etc.)."""
    files = []
    for node_output in history_entry.get("outputs", {}).values():
        for key in ("images", "gifs", "videos"):
            for f in node_output.get(key, []):
                files.append(f)
    return files


def output_path_for(manifest: dict, asset: dict) -> Path:
    return REPO_ROOT / manifest["output_root"] / asset["output"]


def generate_still(client: ComfyClient, manifest: dict, asset: dict, prompt_override: str | None = None) -> Path:
    workflow_name = asset["workflow"]
    workflow = load_workflow(workflow_name)
    patchmap = load_patchmap(workflow_name)
    models = manifest["models"]
    templates = manifest["prompt_templates"]
    if asset["type"] == "background":
        suffix = templates["environment_suffix"]
    elif asset["type"] == "sprite":
        # "plain solid color background" alone isn't enough -- Krea2 was observed
        # rendering a neutral studio-brown backdrop instead of anything keyable.
        # Sprites specifically need an explicit, named key color.
        suffix = f"{templates['character_suffix']}, {templates['sprite_chroma_suffix']}"
    else:
        suffix = templates["character_suffix"]
    full_prompt = f"{prompt_override or asset['prompt']}, {suffix}"

    values = {
        "prompt": full_prompt,
        "negative_prompt": templates["negative_default"],
        "seed": asset["seed"],
        "width": asset["gen_width"],
        "height": asset["gen_height"],
        "filename_prefix": f"battleslice_{asset['id']}",
    }
    patched = apply_patchmap(workflow, patchmap, values)

    prompt_id = client.submit(patched)
    entry = client.wait_for_completion(prompt_id)
    files = find_output_files(entry)
    if not files:
        raise RuntimeError(f"{asset['id']}: ComfyUI job completed but produced no output files")
    f = files[0]
    raw_path = SCRATCH_DIR / f"{asset['id']}{Path(f['filename']).suffix}"
    client.fetch_output(f["filename"], f.get("subfolder", ""), f.get("type", "output"), raw_path)
    return raw_path


def even_sample(items: list, k: int) -> list:
    """Evenly sample k items across the full sequence rather than truncating to the
    first k -- MiniMax H3's `length` request doesn't match its actual output frame
    count, so we usually have more frames than we asked for and want to represent the
    whole motion arc, not just its opening."""
    if k >= len(items):
        return items
    step = len(items) / k
    return [items[int(i * step)] for i in range(k)]


def generate_clip(client: ComfyClient, manifest: dict, asset: dict) -> Path:
    # Step 1: throwaway i2v reference frame, same seed, from the still workflow.
    ref_asset = {
        "id": f"{asset['id']}_ref",
        "type": "character",
        "workflow": "character_sprite.json",
        "prompt": asset["reference_prompt"],
        "seed": asset["seed"],
        "gen_width": asset["gen_width"],
        "gen_height": asset["gen_height"],
    }
    ref_raw = generate_still(client, manifest, ref_asset)
    uploaded_name = client.upload_image(ref_raw, f"battleslice_{asset['id']}_ref.png")

    # Step 2: MiniMax H3 reference-to-video using that uploaded frame.
    workflow_name = asset["workflow"]
    workflow = load_workflow(workflow_name)
    patchmap = load_patchmap(workflow_name)

    values = {
        "reference_image_filename": uploaded_name,
        "prompt": asset["prompt"],
        "seed": asset["seed"],
        "width": asset["gen_width"],
        "height": asset["gen_height"],
        "frame_count": asset["frame_count"],
        "filename_prefix": f"battleslice_{asset['id']}",
    }
    patched = apply_patchmap(workflow, patchmap, values)

    prompt_id = client.submit(patched)
    entry = client.wait_for_completion(prompt_id, timeout_s=1200)
    files = find_output_files(entry)
    if not files:
        raise RuntimeError(f"{asset['id']}: ComfyUI job completed but produced no output files")
    f = files[0]
    raw_path = SCRATCH_DIR / f"{asset['id']}{Path(f['filename']).suffix}"
    client.fetch_output(f["filename"], f.get("subfolder", ""), f.get("type", "output"), raw_path)

    actual_frames = postprocess.probe_frame_count(raw_path)
    if actual_frames != asset["frame_count"]:
        print(
            f"  note: {asset['id']} requested {asset['frame_count']} frames, "
            f"MiniMax H3 produced {actual_frames} (known non-literal 'length' behaviour)"
        )
    return raw_path


def generate_fx(client: ComfyClient, manifest: dict, asset: dict) -> list[Path]:
    workflow_name = asset["workflow"]
    workflow = load_workflow(workflow_name)
    patchmap = load_patchmap(workflow_name)
    templates = manifest["prompt_templates"]
    key_color = asset.get("chroma_key_override", manifest["chroma_key"])

    values = {
        "prompt": f"{asset['prompt']}, {templates['character_suffix']}",
        "seed": asset["seed"],
        "width": asset["gen_width"],
        "height": asset["gen_height"],
        "frame_count": asset["frame_count"],
        "chroma_key_color": key_color,
        "filename_prefix": f"battleslice_{asset['id']}",
    }
    patched = apply_patchmap(workflow, patchmap, values)

    prompt_id = client.submit(patched)
    entry = client.wait_for_completion(prompt_id)
    files = find_output_files(entry)
    if not files:
        raise RuntimeError(f"{asset['id']}: ComfyUI job completed but produced no output files")
    if len(files) != asset["frame_count"]:
        print(
            f"  note: {asset['id']} requested {asset['frame_count']} frames, "
            f"MiniMax H3 produced {len(files)} -- sampling evenly down to {asset['frame_count']}"
        )
    files = even_sample(files, asset["frame_count"])

    raw_paths = []
    for i, f in enumerate(files):
        raw_path = SCRATCH_DIR / f"{asset['id']}_frame{i}{Path(f['filename']).suffix}"
        client.fetch_output(f["filename"], f.get("subfolder", ""), f.get("type", "output"), raw_path)
        raw_paths.append(raw_path)
    return raw_paths


def process_asset(client: ComfyClient, manifest: dict, asset: dict) -> None:
    dst = output_path_for(manifest, asset)
    asset_type = asset["type"]

    if asset_type == "background":
        raw = generate_still(client, manifest, asset)
        postprocess.process_portrait_or_background(raw, dst, asset["final_width"], asset["final_height"])
    elif asset_type == "sprite":
        raw = generate_still(client, manifest, asset)
        postprocess.process_sprite(
            raw, dst, asset["final_width"], asset["final_height"],
            manifest["chroma_key"], manifest["sprite_chroma_tolerance"],
        )
    elif asset_type == "portrait":
        raw = generate_still(client, manifest, asset)
        postprocess.process_portrait_or_background(raw, dst, asset["final_width"], asset["final_height"])
    elif asset_type == "clip":
        raw = generate_clip(client, manifest, asset)
        postprocess.encode_video(raw, dst, asset["fps"])
    elif asset_type == "fx":
        raw_frames = generate_fx(client, manifest, asset)
        dst_json = dst.with_suffix(".json")
        key = asset.get("chroma_key_override", manifest["chroma_key"])
        tolerance = asset.get("chroma_tolerance_override", manifest["chroma_tolerance"])
        postprocess.pack_fx_sheet(
            raw_frames, dst, dst_json, asset["final_width"], asset["final_height"],
            key, tolerance, prekeyed=asset.get("prekeyed", False),
        )
    else:
        raise ValueError(f"unknown asset type {asset_type!r} for {asset['id']}")

    print(f"  -> {dst.relative_to(REPO_ROOT)}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true", help="list assets without generating")
    parser.add_argument("--force", action="store_true", help="regenerate even if output already exists")
    parser.add_argument("--only", help="generate a single asset id")
    args = parser.parse_args()

    manifest = load_manifest()
    export_manifest_json(manifest)
    assets = manifest["assets"]
    if args.only:
        assets = [a for a in assets if a["id"] == args.only]
        if not assets:
            print(f"no asset with id {args.only!r} in manifest.yaml", file=sys.stderr)
            sys.exit(1)

    if args.dry_run:
        print(f"{len(assets)} asset(s) in manifest:")
        for a in assets:
            dst = output_path_for(manifest, a)
            exists = dst.exists()
            skip = " (exists, would skip)" if exists and not args.force else ""
            print(f"  [{a['type']:9s}] {a['id']:32s} -> {a['output']}{skip}")
        return

    SCRATCH_DIR.mkdir(parents=True, exist_ok=True)
    client = ComfyClient(manifest["comfyui"]["base_url"])

    for asset in assets:
        dst = output_path_for(manifest, asset)
        if dst.exists() and not args.force:
            print(f"skip {asset['id']} (already exists, use --force to regenerate)")
            continue
        print(f"generating {asset['id']} ({asset['type']})...")
        process_asset(client, manifest, asset)


if __name__ == "__main__":
    main()
