# Active World State

**Status:** Documentation scaffold; no world-state source classes implemented.

## Responsibility

Represent the **completed generated world** within the current active game session. A partial `GenerationContext` in the Laboratory is not yet an active playable world.

The `engine/worldgen/` pipeline produces the world; this module represents active game data, not generation algorithms or UI rendering. The separate `saves/` module will eventually handle persistence and game snapshots according to `architecture.MD`.

## Future candidate file

- `WorldState.cs` — to be designed only after the result/finalization contract is approved.

**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)  
**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)
