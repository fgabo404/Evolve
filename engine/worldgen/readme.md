# World Generation Engine

**Status:** CORE-01 and CORE-02 accepted; CORE-03 implemented, pending review and owner acceptance.

## Responsibility

Coordinate the layered generation pipeline independently of the UI. The .NET 10
engine library references only the definitions library. No OpenTK, rendering,
active state, save storage or test dependency belongs here.

## Current components

- `GenerationStage`: eight canonical stage identities, Grid through Biomes.
- `GenerationRequest`: immutable main seed and validated definitions settings.
- `WorldGenerationLayerRegistry`: complete, unique, ordered registration with
  read-only membership; registration itself executes no layer.
- `IWorldGenerationLayer`: stable identity and synchronous execution returning
  a candidate or explicit failure, using only read-only inputs.
- `GenerationContext`: request metadata and the authoritative private table of
  stage status, published data and failure diagnostics. No public mutators.
- `GenerationStageStatus` / `GenerationProgress`: five lifecycle states and
  immutable observations derived from that table, including the completed prefix
  and next eligible stage.
- `LayerData` / `LayerData<T>`: stage-owned immutable buffers of unmanaged
  values, copied once from their sources and reused safely after publication.
- `GenerationLayerInputs`: immutable request and earlier completed buffers
  only; no context handle, own-stage access or future-stage access.
- `LayerExecutionResult`: either an immutable candidate or a failure diagnostic.
- `WorldGenerationOrchestrator`: `CreateGeneration`, `RunNext`, `RunUntil` and
  `RunAll`. All forward modes use one synchronous stage runner for prerequisite
  validation, invocation, ownership validation, atomic publication and retry.
- `AssemblyVisibility.cs`: restricted named friend access for the existing
  headless checks, without adding public storage mutation APIs.

## Execution and limitations

Create a context from a validated request and a complete registered pipeline.
The future Laboratory will call `RunNext` for forward execution. Automation can
use `RunUntil` or `RunAll`; every mode executes prerequisites in canonical order.
Only completed earlier results are visible to a layer. A valid candidate is
published together with `Completed`; explicit failures, exceptions, missing
results and wrong ownership record `Failed` without publishing the candidate.
Bulk modes stop immediately. Retry resumes at the failed stage, preserving
earlier data. Completed targets/worlds are no-ops.

Use `context.GetProgress()` for all progress and diagnostic inspection; there is
no second progress store. While a stage is `Running`, synchronous/reentrant
operations are rejected before mutation. Callers must serialize operations and
inspection; cross-thread operation exclusivity, cancellation and async execution
are not guaranteed by CORE-03. The full concurrency strategy belongs to CORE-04.

Inconsistent completion prefixes and invalidated storage are rejected without
mutation. Internal single-slot discard remains a storage primitive, not an
implicit rebuild. PRNG/seed derivation, operational rebuild/downstream
invalidation, finalization and transfer into `WorldState` remain unimplemented.
The production core contains no layer executors; the eight neutral fake
executors live exclusively in `tests/worldgen/`. Real algorithms belong to
separately authorized layer roadmaps under `layers/`.

Published buffers accept unmanaged values only. Future reference-rich outputs
require a separately approved immutable contract. No world is copied per stage,
and context/progress inspection is neither active game state nor persistence.
No UI integration or GUI validation is included in this increment.

## Build and checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\worldgen\Evolve.WorldGeneration.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

The checks run real production contracts, storage and orchestration with no GPU
or geographic data. See [test coverage](../../tests/worldgen/readme.md).

## Documents

- [Approved WG-CORE architecture](core-architecture.md)
- [Technical contracts and exact CORE-03 signatures](core-contracts.md#14-core-03-sequential-execution-contract)
- [Approved WG-CORE implementation roadmap](core-roadmap.md)
- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

CORE-03 implementation does not grant acceptance, authorize CORE-04/05 or
authorize merging `world-core` into `world`.
