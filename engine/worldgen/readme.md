# World Generation Engine

**Status:** CORE-01 through CORE-05 formally accepted; WG-CORE closed after independent audit approval (2026-11-18).

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
- `WorldGenerationResult`: immutable request/version metadata and eight completed
  historical buffers captured by `FinalizeWorld`, stable across later rebuilding.

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
no second progress store. CORE-04 adds a per-context exclusive operation lease
across threads and orchestrator instances. Competing/reentrant operations reject
immediately. Storage lookups and progress snapshots use a short per-context
lock; rebuild invalidates its entire suffix atomically without blocking other
contexts. Separate inspection calls may observe different instants.

`GenerationLayerInputs.Random` supplies a fresh, versioned SplitMix64 stream per
stage attempt. `RebuildFrom` preserves completed predecessors, discards the
selected suffix and restores sequential eligibility. Completed/Invalidated
suffix slots become Invalidated; Failed/Pending slots become Pending; all lose
data and diagnostics. Rebuild requires completed predecessors and no active
operation. Previously retrieved immutable results remain historical observations.

The production core contains no geographic layer executors. Eight neutral fake
executors were previously used for CORE acceptance testing; that completed test
harness was retired and is retained in Git history. Real algorithms belong to
separately authorized layer roadmaps. Finalization requires eight Completed results with
matching ownership, holds the same operation lease, and changes no context state.
It copies only eight buffer references; repeated calls never execute layers.
WorldState integration remains unimplemented. See [CORE-04 signatures and vectors](core-contracts.md#16-core-04-deterministic-streams-and-exclusive-rebuild).

Published buffers accept unmanaged values only. Future reference-rich outputs
require a separately approved immutable contract. No world is copied per stage,
and context/progress inspection is neither active game state nor persistence.
No UI integration or GUI validation is included in this increment.

## Build and checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

The original 69 WG-CORE headless checks passed before acceptance. Their source
and executable project were later retired at the owner's request, so these
regression checks are **not currently runnable** in the integration branches.
They remain recoverable from the archived `world-core` branch. New geographic
layers must define their own independently approved test coverage.

## Documents

- [Approved WG-CORE architecture](core-architecture.md)
- [Technical contracts and exact CORE-03 signatures](core-contracts.md#14-core-03-sequential-execution-contract)
- Completed WG-CORE roadmap (retired; available in Git history)
- [Overall roadmap](roadmap.md)
- [Layer catalog](layers/readme.md)
- [Approved preliminary architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

CORE-05 and WG-CORE were formally accepted by the owner following the auditor's
APPROVED verdict. The accepted core was integrated into `world` and
`develop` by the owner. WG-00, state/UI integration and future geographic work
still require separate owner approval.

## Finalization boundary

Call `orchestrator.FinalizeWorld(context)` after successful completion. The result
exposes `Request`, `RandomAlgorithmVersion` and `GetLayerData(stage)`, with no
public mutation. Pending, Failed, Running, Invalidated or inconsistent outputs
reject with InvalidOperationException. An active operation rejects finalization,
including through another orchestrator or reentrant call. Null context and
unknown result stage IDs use the existing argument-exception conventions.

The result retains immutable historical data, independently of subsequent
RebuildFrom calls; it is not automatically installed into active state or saved.
RandomAlgorithmVersion captures PRNG compatibility, not the game's display
version or yet-unimplemented geographic algorithm versions. Exact signatures and
limits are in [CORE-05 contracts](core-contracts.md#18-core-05-finalization-contract).
