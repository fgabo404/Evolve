# Layer 01 — Tectonic Plates

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-tectonics` (not created)  
**Prerequisite:** 00 — Hex Grid

## Responsibility

Generate geologically useful tectonic plates and convergent/divergent/transform boundary relationships.

## Conceptual inputs

Completed grid, seed and approved tectonic settings.

## Conceptual outputs

10–14 varying-size plate regions, motions, boundaries and crust-related information.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Complete cell assignment; region continuity; valid boundary relationships; reproducible results.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
