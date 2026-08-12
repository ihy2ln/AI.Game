# AI.Game

Anime lane-tactics RPG with farming, town building, and gacha — personal Unity-target project with playable web prototypes per section.

**Repo:** https://github.com/ihy2ln/AI.Game

## Layout

| Path | Purpose |
|---|---|
| `sections/farm/` | Playable farm map (current focus) |
| `android/` | Android WebView project → APK |
| `releases/` | Built APKs |
| `DataLayer/` | ScriptableObject-style data definitions from the design package |
| `S:\AI\Game\AI.Game Commits\<section>\` | Per-section snapshots for incremental commits |

## Farm (v0.2)

Stardew Valley × Rune Factory clearing loop on a **4×4** starter plot.

- Obstacles: weeds, bushes, stumps, trees, rocks, boulders
- Clearance gated by **farm level** (XP from clearing)
- Visuals: Brown Dust 2 HD-2D (sunset grade, tactical grid, god rays, tilt-shift, dense obstacle art) — refs in `sections/farm/art-refs/`
- **Android APK:** `releases/AI.Game-Farm-v0.2.0-debug.apk` (sideload)

### Controls

- **WASD / arrows** or on-screen pad — move
- **Space / E / CLEAR** — clear obstacle on current tile
- **Tap/click** — step to adjacent tile or clear current

## Design reference

Built against `game-design-package-v0.5` + Brown Dust 2 stills (`art-refs/`).

## Roadmap (farm)

1. ~~4×4 obstacle map + level gates~~
2. ~~BD2 art pass + Android APK~~
3. Larger overgrown farm (expand grid)
4. Tilling / planting / watering (CropDefinition)
5. Tools + stamina
6. Unity port of the same map data
