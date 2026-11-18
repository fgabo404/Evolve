# Layer 04 — Terrestrial Landforms

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-landforms` (not created)  
**Prerequisite:** 03 — Elevation

## Responsibility

Identify coherent geographical features from existing relief instead of arbitrarily scattering labels.

## Conceptual inputs

Elevation and tectonic deformation.

## Conceptual outputs

Classified mountain ranges, sierras, faults, valleys and other terrestrial features.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Terrain-consistent connected features and tectonically plausible classifications.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
