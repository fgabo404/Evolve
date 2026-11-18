# Layer 02 — Landmasses

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-landmasses` (not created)  
**Prerequisite:** 01 — Tectonic Plates

## Responsibility

Support Pangea, 2–4 continents, archipelagos and real-Earth geography with plate-informed land configuration.

## Conceptual inputs

Grid, plate results and world type.

## Conceptual outputs

Land/ocean regions, coastlines, connected continental groups and islands.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Seam-aware connected components; world-type constraints; meaningful tectonic relation.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
