# AI.Game

Anime lane-tactics RPG with farming, town building, and gacha — personal Unity-target project with playable web prototypes per section.

**Repo:** https://github.com/ihy2ln/AI.Game

## Layout

| Path | Purpose |
|---|---|
| `sections/farm/` | Playable farm map (current focus) |
| `DataLayer/` | ScriptableObject-style data definitions from the design package |
| `S:\AI\Game\AI.Game Commits\<section>\` | Per-section snapshots for incremental commits |

## Farm (v0.1)

Stardew Valley × Rune Factory clearing loop on a **4×4** starter plot.

- Obstacles: weeds, bushes, stumps, trees, rocks, boulders
- Clearance gated by **farm level** (XP from clearing)
- Visuals: Brown Dust 2–inspired HD-2D isometric field (sunset haze, chibi farmer, level badges)

### Run locally

ES modules need a static server (file:// will block `fetch`):

```bash
cd AI.Game
npx --yes serve .
```

Open the printed URL → **Farm — Starter Plot**.

### Controls

- **WASD / arrows** — move
- **Space / E** — clear obstacle on current tile
- **Click** — step to adjacent tile or clear current

## Design reference

Built against `game-design-package-v0.5` (Farm data layer + Brown Dust 2 view notes in the sprite scale tool).

## Roadmap (farm)

1. ~~4×4 obstacle map + level gates~~
2. Larger overgrown farm (expand grid)
3. Tilling / planting / watering (CropDefinition)
4. Tools + stamina
5. Unity port of the same map data
