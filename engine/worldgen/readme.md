# World Generation Engine

**Status:** CORE-01 contract foundation implemented; pending owner acceptance.

## Responsibility

Coordinate the layered, deterministic generation pipeline independently of the UI.

Current CORE-01 components:

- `GenerationStage` — the eight canonical stage IDs, Grid through Biomes.
- `IWorldGenerationLayer` — a stable stage identity only; execution is deferred.
- `WorldGenerationLayerRegistry` — complete, unique and correctly ordered
  registration, with read-only membership and no execution.
- `GenerationRequest` — immutable main seed and validated definitions settings.

`Evolve.Engine.WorldGeneration.csproj` targets .NET 10 and references only the
definitions library. It does not reference OpenTK, UI, active state or saves.
Exact signatures, validation and deferred contracts are in
[core-contracts.md](core-contracts.md#10-core-01-implemented-c-contract).

Future conceptual components, not implemented by CORE-01:

- `WorldGenerationOrchestrator` — the single generation entry point, sequencing, prerequisite enforcement, failure handling and downstream invalidation.
- `GenerationContext` — temporary world draft, intermediate layer data, seed/settings and completion state; not an active game or saved-game snapshot.
- `layers/` — the geographically focused generation algorithms.

The planned engine will support full generation and sequential Laboratory
generation through the **same** execution pipeline. Only a complete, valid world
may eventually be transferred into `state/world/`; CORE-01 implements neither
execution nor that transfer.

No OpenGL, rendering, menu code, file-save handling or parallel generator implementation belongs here.

## Documents

- [Approved WG-CORE architecture](core-architecture.md)
- [Approved technical design](core-contracts.md)
- [Approved WG-CORE implementation roadmap](core-roadmap.md)
- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

Only CORE-01 is implemented on `world-core`, subject to acceptance. CORE-02 and
later increments are not authorized by this delivery.
