# Layer 07 — Biomes

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-biomes` (not created)  
**Prerequisite:** 06 — Climate

## Responsibility

Classify biomes as consequences of environmental conditions and approved biome rules.

## Conceptual inputs

Climate, elevation, hydrology, terrain and approved biome definitions.

## Conceptual outputs

Biome assignments and biome distribution by hex cell.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Valid biome coverage; compliance with environmental constraints and distribution rules.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
