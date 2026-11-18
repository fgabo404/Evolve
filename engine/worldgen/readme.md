# World Generation Engine

**Status:** Documentation scaffold; implementation not started.

## Responsibility

Coordinate the layered, deterministic generation pipeline independently of the UI.

Future conceptual components:

- `WorldGenerationOrchestrator` — the single generation entry point, sequencing, prerequisite enforcement, failure handling and downstream invalidation.
- `GenerationContext` — temporary world draft, intermediate layer data, seed/settings and completion state; not an active game or saved-game snapshot.
- `GenerationStage` — formal stage identification and execution contracts, pending design.
- `layers/` — the geographically focused generation algorithms.

The engine supports full generation and sequential Laboratory generation through the **same** execution pipeline. Only a complete, valid world can be transferred into `state/world/`.

No OpenGL, rendering, menu code, file-save handling or parallel generator implementation belongs here.

## Documents

- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

All implementation contracts remain subject to stage-specific approval.
