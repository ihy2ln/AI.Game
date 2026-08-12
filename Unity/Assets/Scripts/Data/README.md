# Data Layer — Build Step 1

ScriptableObject definitions for the lane-tactics RPG. Drop into `Assets/Scripts/Data/`.

Everything here is **pure data with no scene dependency** — deliberate, because Unity's
inspector wiring is the part an AI assistant can't do for you. Keeping the project
code-first means as much of it as possible lives in text files that can be edited directly.

---

## Layout

```
Core/         Enums, StatBlock
Characters/   CharacterDefinition, CharacterInstance, CharacterFactory,
              TierDefinition, SkillDefinition
Combat/       MapDefinition, SkillPattern, ClipSet
Economy/      MaterialDefinition, DropTable, StageDefinition,
              EquipmentDefinition/Instance/Set
Farm/         CropDefinition, FarmPlotState, DishDefinition
Town/         DistrictDefinition, BuildingDefinition, BuildingState
```

**Definition vs Instance:** `*Definition` = authored asset, never mutates.
`*Instance` / `*State` = rolled or runtime, and is save data.

---

## Key decisions encoded

**Stat rolls freeze at acquisition and store their seed.** Saves sync between Android and
PC, so a roll must resolve identically on both. Everything random flows through one seeded
`System.Random`.

**Movement stats never roll.** `movePoints` and `jump` sit on the definition as fixed ints.
Random movement range would make tactical planning unreadable.

**Jump is a legality check, not a cost.** Height difference above `jump` makes an edge
*illegal*. This must be validated inside pathfinding traversal, never post-filtered — an
unreachable-but-cheap tile must not enter the frontier.

**Tier behaviour is data, not code.** `TierDefinition` carries both `statMultiplier` and
`variance`, so the F–A / S–SSS split is tunable. Author F–A with variance 0.15 and
multiplier 1.0; author S–SSS with a multiplier above 1.0 and whatever variance you decide.

**Fusion is additive by default.** 10 fusions = +100%. One line in `CharacterInstance`
switches it to multiplicative (~2.59× at 10 stacks) — additive is safer, since
multiplicative compounds hard on top of tier variance.

**Height capped at 3.** `MapDefinition.MaxHeight`. This is an occlusion constraint, not a
balance preference — a fixed camera can't rotate around tall terrain like FFT's could.

**Facing is two-state.** `SkillPattern` mirrors on the column axis only. Lane movement
means units never face into or out of the screen — this is what keeps sprite work at 2
facings instead of 8.

**Clips carry impact frames.** Pre-rendered video can't tell the game when a hit lands, so
`ClipEntry.impactFrames` drives damage-number timing. Cheap now, painful to retrofit.

**Materials classify on four independent axes** — tier, age, island, rarity — so drop
tables, recipes and shops all filter one catalogue. `MaterialCategory` is what enforces
mode ownership: each mode should exclusively own at least one category.

**Drop tables support retroactive unlocks.** `unlockGatedEntries` lets a boss clear add
materials to *earlier* stages, keeping old content relevant.

**No starter gear.** All three equip slots on `CharacterInstance` initialise empty.

---

## Ambiguities parameterised rather than guessed

| Spec | How it's handled |
|---|---|
| S–SSS "get a bonus" — instead of variance, or as well? | Both fields on every `TierDefinition`. Author it either way. |
| Fusion +10% ×10 — additive or multiplicative? | Additive; one-line switch documented in `CharacterInstance`. |
| Elevation effect magnitudes | Flagged as balance-pass; `isRanged` on skills is the hook. |
| Dungeon farming vs town farming | `FarmPlotType { Town, Dungeon, Both }` on `CropDefinition`. |
| Growth: time or battle count? | Both fields; whichever completes first harvests. |

---

## Next step

**Headless combat resolution** — grid state, movement cost traversal with jump validation,
turn queue, skill pattern application, elevation modifiers, drop resolution. No rendering.

Four consumers will share that engine: Advance Wars view, Brown Dust 2 view,
instant-resolve, and the strategic overlay. Entangle it with presentation and you write it
four times.
