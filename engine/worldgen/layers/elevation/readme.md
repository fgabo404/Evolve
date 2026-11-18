# Layer 03 — Elevation

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-elevation` (not created)  
**Prerequisite:** 02 — Landmasses

## Responsibility

Build plausible terrain and ocean-floor heights influenced by plate boundaries and landmasses.

## Conceptual inputs

Tectonics, land/ocean regions and approved elevation settings.

## Conceptual outputs

Physical elevation and bathymetry in meters plus normalized 0–100 elevation data.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Consistent land/sea heights; valid numerical ranges; agreed physical-to-normalized conversion.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
