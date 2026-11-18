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

- [Approved WG-CORE architecture](core-architecture.md)
- [Approved technical design](core-contracts.md)
- [Approved WG-CORE implementation roadmap](core-roadmap.md)
- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

The project owner approved WG-CORE's technical design and staged roadmap, authorizing CORE-01 only on `world-core`. No source code is added by this documentation approval.
