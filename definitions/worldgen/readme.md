# World Generation Definitions

**Status:** Documentation scaffold; no C# types or settings schemas implemented.

## Responsibility

Hold static world-generation definitions and configuration models consumed by the engine, including map size (Small / Medium / Large), world type (Pangea, Continents, Archipelagos, Real Earth), tectonic settings and later approved parameters.

This area defines **what can be configured**, not the algorithms that generate a world, game progress or rendering state.

## Proposed future files (not created)

- `WorldGenerationSettings.cs` — generation request configuration.
- `WorldType.cs` — supported world presets.
- `MapSize.cs` — supported size selections.

Exact data contracts and quantitative settings require separate approval.

**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)  
**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)
