# Layer 00 — Hex Grid

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-grid` (not created)  
**Prerequisite:** World settings and seed only (first layer)

## Responsibility

Define a continuous east/west hex map with polar north/south limits, three configurable sizes and a proposed 2:1 equirectangular geographic representation.

## Conceptual inputs

Map size, projection and topology settings.

## Conceptual outputs

Hex cells, cell IDs, geographic coordinates, six-neighbor topology and horizontal wrap.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Unique cell identities; seam-aware neighbors and distances; no north/south wrap; correct size handling.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
