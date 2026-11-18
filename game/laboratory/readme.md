# World Generation Laboratory

**Status:** Documentation scaffold; no screen or rendering classes implemented.

## Responsibility

Provide **step-by-step world generation** within the existing single `EvolveWindow`. The Laboratory is not a separate world generator or a standalone OpenTK window.

The Laboratory will:

- Configure seed, map size and world type using approved settings.
- Request the **next eligible stage** from `WorldGenerationOrchestrator`; it cannot bypass unmet prerequisites.
- Display stage progress, errors and the accumulated partial world.
- Allow inspection of any earlier successfully generated layer, with layer-specific visualization and eventual overlays.
- Request an explicitly authorized rebuild from an earlier stage, invalidating downstream results.

All generation rules belong in `engine/worldgen/`. This directory will contain presentation, stage controls and rendering integration only.

## Future candidate files

- `WorldLaboratoryScreen.cs` — Laboratory presentation/navigation inside `EvolveWindow`.
- `LayerVisualization.cs` — visualization of selected completed-layer data.

**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)  
**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)
