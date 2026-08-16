# AI.Game

Anime lane-tactics RPG with farming, town building, and gacha.

**Repo:** https://github.com/ihy2ln/AI.Game  
**Unity Editor:** `S:\AI\Unity\UnityEditors\Editor\6000.5.7f1` (Unity 6)

## Layout

| Path | Purpose |
|---|---|
| `Unity/` | **Primary game project** (Unity 6000.5.7f1) |
| `Unity/Assets/Scripts/Farm/` | Starter 4×4 farm (BD2 isometric, level-gated clearing) |
| `Unity/Assets/Scripts/Data/` | Design-package data layer |
| `Unity/Assets/Art/` | Aesthetics/camera/map design doc + reference art |
| `sections/farm/` | Web prototype (reference / quick preview) |
| `android/` + `releases/` | Earlier WebView APK (sideload) |
| `AI.Game Commits/<section>/` | Per-section snapshots |

## Open Unity

```bat
S:\AI\Game\AI.Game\Unity\OpenUnity.bat
```

Play `Assets/Scenes/Farm.unity`.

## Farm (Unity v0.3)

Stardew × Rune Factory clearing on a **4×4** plot, Brown Dust 2–inspired lighting/grid/obstacles. Code-first bootstrap — no Inspector wiring required.

## Roadmap

1. ~~Web 4×4 farm + APK~~
2. ~~Unity project + farm scene~~
3. Larger overgrown farm map — see `Unity/Assets/Art/README.md` for the Home vs Overworld camera plan
4. Crops (CropDefinition) / tools / stamina
5. Unity Android build pipeline
6. Combat / town sections — in progress on `feature/battle-slice`, see [`PROJECT-README.md`](PROJECT-README.md)
