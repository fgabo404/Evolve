# Layer 06 — Climate

**Status:** Documentation scaffold; implementation not started.  
**Planned branch:** `world-climate` (not created)  
**Prerequisite:** 05 — Base Hydrology

## Responsibility

Derive plausible climate distributions from geography, altitude, maritime influence and atmospheric factors.

## Conceptual inputs

Latitude, altitude, topography, coastlines and preliminary hydrology.

## Conceptual outputs

Temperature, humidity, precipitation and climate-related fields.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Geographic consistency; deterministic outputs; explicitly resolved hydrology/climate feedback.

These descriptions are preliminary: precise algorithms, data models, tests and success criteria must be approved during the stage-specific design.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
