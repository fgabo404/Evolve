# Layer 05 — Base Hydrology

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-hydrology` (not created)  
**Prerequisite:** 04 — Terrestrial Landforms

## Responsibility

Model terrain-dependent drainage without presupposing final climate-based river discharge.

## Conceptual inputs

Terrain, slopes, landforms and land/ocean boundary.

## Conceptual outputs

Watersheds, drainage directions and preliminary river/lake candidates.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Coherent drainage topology and basins; no unjustified uphill flow; agreed outlet handling.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
