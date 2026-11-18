# World Generation Definitions

**Status:** CORE-01 minimal configuration implemented; pending owner acceptance.

## Responsibility

Hold static world-generation definitions and configuration models consumed by the engine, including map size (Small / Medium / Large), world type (Pangea, Continents, Archipelagos, Real Earth), tectonic settings and later approved parameters.

This area defines **what can be configured**, not the algorithms that generate a world, game progress or rendering state.

## CORE-01 configuration

- `WorldGenerationSettings.cs` — immutable, validated world type and map size only.
- `WorldType.cs` — supported world presets.
- `MapSize.cs` — supported size selections.

`Evolve.Definitions.WorldGeneration.csproj` is a .NET 10 library with no project
or package dependencies. The engine references this library; definitions do not
reference the engine. Exact signatures and project decisions are recorded in
[WG-CORE contracts](../../engine/worldgen/core-contracts.md#10-core-01-implemented-c-contract).
Quantitative settings, grid dimensions and additional parameters still require
separate approval; no algorithms or geometry are implemented here.

**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)  
**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)
