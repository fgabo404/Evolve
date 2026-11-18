# World Generation Engine

**Status:** CORE-01 accepted; CORE-02 context/storage implemented pending acceptance.

## Responsibility

Coordinate the layered, deterministic generation pipeline independently of the UI.

Current CORE-01 components:

- `GenerationStage` — the eight canonical stage IDs, Grid through Biomes.
- `IWorldGenerationLayer` — a stable stage identity only; execution is deferred.
- `WorldGenerationLayerRegistry` — complete, unique and correctly ordered
  registration, with read-only membership and no execution.
- `GenerationRequest` — immutable main seed and validated definitions settings.

CORE-02 components:

- `GenerationContext` — immutable request access and private stage-owned storage;
  public callers can only inspect progress and completed results.
- `GenerationStageStatus` — the five approved storage states.
- `GenerationProgress` — immutable status/failure observations and derived
  contiguous progress, with no externally writable state.
- `LayerData` / `LayerData<T>` — a restricted immutable output contract with
  owned, copied unmanaged value buffers for individual stages.
- `AssemblyVisibility.cs` — named friend access for the existing headless checks
  to validate internal preparation methods without public mutation APIs.

`Evolve.Engine.WorldGeneration.csproj` targets .NET 10 and references only the
definitions library. It does not reference OpenTK, UI, active state or saves.
Exact signatures, validation and deferred contracts are in
[core-contracts.md](core-contracts.md#12-core-02-context-and-read-only-storage-contract).

Future conceptual components, not implemented by CORE-01/CORE-02:

- `WorldGenerationOrchestrator` — the single generation entry point, sequencing, prerequisite enforcement, failure handling and downstream invalidation.
- `layers/` — the geographically focused generation algorithms.

The planned engine will support full generation and sequential Laboratory
generation through the **same** execution pipeline. Only a complete, valid world
may eventually be transferred into `state/world/`; CORE-01/CORE-02 implement neither
execution nor that transfer.

Context transitions are internal storage primitives, not an execution pipeline.
No prerequisites, concurrency policy, automatic invalidation or reconstruction
are implemented. Published buffers accept unmanaged value types only; future
reference-rich geographical data require a separately approved immutable shape.
Context/progress inspection is not active game state or saved-game persistence.

No OpenGL, rendering, menu code, file-save handling or parallel generator implementation belongs here.

## Documents

- [Approved WG-CORE architecture](core-architecture.md)
- [Approved technical design](core-contracts.md)
- [Approved WG-CORE implementation roadmap](core-roadmap.md)
- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

CORE-02 is implemented on `world-core`, subject to acceptance. CORE-03 and later
increments are not authorized by this delivery.
