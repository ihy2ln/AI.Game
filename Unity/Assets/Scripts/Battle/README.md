# Battle scripts

Headless-first battle system. Mirrors the `Farm/` folder's pattern: one `*Bootstrap.cs`
builds the scene at runtime from script, no Inspector wiring.

Planned files (see `S:\AI\Game\Foundation\CLAUDE-CODE-PROMPT-Battle-Vertical-Slice.md` §5):

| File | Responsibility |
|---|---|
| `BattleBootstrap.cs` | Entry point, builds the scene at runtime |
| `BattleGrid.cs` | Lane/column occupancy, world↔grid conversion |
| `BattleUnit.cs` | Runtime unit: `CharacterInstance` + current HP/MP + grid position |
| `TurnOrder.cs` | Speed-sorted turn queue |
| `TargetResolver.cs` | `SkillPattern` range/area + melee/ranged row rules |
| `DamageCalculator.cs` | Pure C#, no `MonoBehaviour`, unit-testable |
| `BattleController.cs` | State machine: SelectUnit → SelectSkill → SelectTarget → Resolve → NextTurn |
| `BattleVisuals.cs` | Sprites, background, grid highlights |
| `ClipPlayer.cs` | `VideoPlayer` + RenderTexture + chroma-key material |
| `BattleHud.cs` | HP bars, turn order strip, skill buttons, damage numbers |
| `PlaceholderArt.cs` | Runtime flat-colour fallback for any missing asset |

`DamageCalculator` and `TargetResolver` stay pure C# (no scene dependency) so they can be
covered by EditMode tests under `Unity/Assets/Tests/`.
