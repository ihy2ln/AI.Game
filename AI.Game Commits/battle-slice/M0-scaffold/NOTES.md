# M0 — branch, gitignore, folder scaffold, docs stub

**Tag:** `v0.4.0-m0-scaffold`
**Branch:** `feature/battle-slice`

## What works

Nothing playable — this is pure setup. Repo now has:
- `feature/battle-slice` branch off `main`.
- Folder scaffold: `Unity/Assets/Scripts/Battle/`, `Unity/Assets/Tests/`,
  `Tools/ComfyUI/` (+ `workflows/`), `releases/zips/`, `AI.Game Commits/battle-slice/`.
- `PROJECT-README.md` tracking milestone status and known environment gaps.
- `.gitignore` fix: removed an unused generic `tools/` rule that was shadowing the new
  `Tools/ComfyUI/` folder on the case-insensitive Windows filesystem; scoped future
  battle APKs (`releases/AI.Game-Battle-*.apk`) to be ignored rather than committed.

## What's stubbed

Everything past folder structure — no code, no assets, no ComfyUI pipeline yet.

## What's next

M1: ComfyUI pipeline — workflow JSON, `manifest.yaml` for the 17 slice assets,
`generate.py` driver, `postprocess.py`.

## Known environment gaps (carried forward from PROJECT-README.md)

- No local Unity Editor install found at `S:\AI\Unity\UnityEditors\Editor\6000.5.7f1`.
- No `adb` on PATH.
- ComfyUI confirmed reachable at `127.0.0.1:8188` (v0.33.0).

## Revert

`git checkout main` (nothing on `main` is touched by this milestone), or on the branch:
`git revert 965b0c6` (single commit, safe to revert in isolation).
