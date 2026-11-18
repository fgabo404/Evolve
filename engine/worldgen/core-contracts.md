# Evolve — WG-CORE Technical Contracts

**Repository:** `fgabo404/Evolve`  
**Integration branch:** `world`  
**Planned delivery branch:** `world-core` (not created)  
**Status:** APPROVED TECHNICAL DESIGN — 2026-11-18; exact C# signatures resolved in CORE-01  
**Approved behavioral foundation:** [WG-CORE architecture](core-architecture.md)  
**Delivery plan:** [WG-CORE roadmap](core-roadmap.md)

## 1. Contract boundaries

The **approved** system behavior is sequential execution of eight stages by one `WorldGenerationOrchestrator`, over a temporary `GenerationContext`, with atomic publication of validated results, deterministic replay, strict prerequisites, downward invalidation and completion-only finalization.

The project owner approved this technical design on 2026-11-18. It translates approved behavior into engineering contracts; **exact C# signatures and implementation details are decisions for the separately authorized CORE-01 stage**, not existing code.

- `definitions/worldgen/` owns configuration/value definitions; no generation algorithms.
- `engine/worldgen/` owns the orchestrator, lifecycle and generation draft.
- `engine/worldgen/layers/` supplies focused layer executors, developed in later stages.
- `game/laboratory/` invokes the orchestrator and renders completed outputs; no generator logic.
- `state/world/` eventually accepts the finalized result as part of the one active game state.
- `saves/` remains outside WG-CORE.

The Laboratory runs **only `RunNext()`** for forward stage execution. `RunUntil()` is for automation/tests and may not skip any prerequisite. No additional OpenTK window is permitted.

## 2. Stage identities and dependencies

| Stage | Stable conceptual ID | Requires |
| --- | --- | --- |
| 00 | `Grid` | Valid generation request |
| 01 | `Tectonics` | `Grid` |
| 02 | `Landmasses` | `Tectonics` |
| 03 | `Elevation` | `Landmasses` |
| 04 | `Landforms` | `Elevation` |
| 05 | `Hydrology` | `Landforms` |
| 06 | `Climate` | `Hydrology` |
| 07 | `Biomes` | `Climate` |

The initial contract is a fixed, ordered pipeline of eight stages. A stage can read earlier validated outputs it needs, but completion of its immediate predecessor is the minimum execution gate. Stage registration must reject duplicate/missing IDs, unexpected orders and unsupported stages rather than silently generating partial worlds.

**WG-CORE scope:** use **test-only deterministic fake executors** to validate this contract. Actual Grid/Tectonics/... algorithms belong to their separate layer branches and roadmaps.

## 3. Candidate data and execution contracts

These are **logical signatures**, not literal finalized C# syntax:

```text
CreateGeneration(request: GenerationRequest)
  -> GenerationContext

RunNext(context: GenerationContext)
  -> StageExecutionResult

RunAll(context: GenerationContext)
  -> GenerationProgress

RunUntil(context: GenerationContext, target: GenerationStage)
  -> GenerationProgress

RebuildFrom(context: GenerationContext, stage: GenerationStage)
  -> GenerationProgress

GetProgress(context: GenerationContext)
  -> GenerationProgress

GetLayerData(context: GenerationContext, stage: GenerationStage)
  -> ReadOnlyLayerData | NotAvailable

FinalizeWorld(context: GenerationContext)
  -> WorldGenerationResult | NotComplete
```

Proposed semantic types:

| Contract | Responsibility |
| --- | --- |
| `GenerationRequest` | Main seed, world type, size, approved settings and relevant generator-version metadata |
| `GenerationStage` | Stable identity/order of the eight stages |
| `IWorldGenerationLayer` | Reports its stage ID and produces an isolated candidate output from approved read-only inputs and its deterministic random stream |
| `GenerationContext` | Temporary per-session request metadata, validated outputs and orchestrator-owned lifecycle |
| `GenerationProgress` | Read-only snapshot of statuses, last completed stage, next eligible stage and failure detail |
| `StageExecutionResult` | Outcome of one attempted stage, including success/failure and diagnostics |
| `WorldGenerationResult` | Completed, validated generation output for an eventual state integration boundary |

Avoid one concrete type per conceptual field unless needed by the first executable layer. Exact namespaces, API parameters, C# records/interfaces, error return strategy and project boundaries require a later implementation-contract decision.

### Proposed layer boundary

```mermaid
sequenceDiagram
    participant Caller as Laboratory / automated caller
    participant Orch as WorldGenerationOrchestrator
    participant Ctx as GenerationContext
    participant Layer as IWorldGenerationLayer
    Caller->>Orch: RunNext(context)
    Orch->>Ctx: Resolve next eligible + read-only inputs
    Orch->>Layer: Execute(inputs, stageRandom)
    Layer-->>Orch: Isolated candidate or failure
    Orch->>Orch: Validate candidate and stage ownership
    alt Valid
        Orch->>Ctx: Atomically publish output + Completed
    else Invalid / failed
        Orch->>Ctx: Record Failed; publish no candidate output
    end
    Orch-->>Caller: Execution status + progress
```

The layer receives no publicly mutable handle to earlier completed outputs. The orchestrator is the sole authority for publication and lifecycle transitions.

## 4. GenerationContext ownership

### GenerationMetadata — fixed for the session

- Generation/session ID for tracking; **not** automatically a source of random-world variation.
- Main seed, selected world type, map size, effective settings.
- Generator/algorithm version identifiers needed for reproducibility, once specified.

Settings are validated and **frozen** when the context is created. Changing the main seed, map size or any global grid-affecting setting creates a **new generation session** rather than mutating an already published grid.

### LayerProgress — orchestrator writes only

Per-stage states:

| State | Semantics |
| --- | --- |
| `Pending` | Not executed and not previously invalidated |
| `Running` | One valid stage execution is active |
| `Completed` | Validated stage output is published |
| `Failed` | Attempt failed; no candidate data published |
| `Invalidated` | Previous result discarded because this or an upstream stage must rebuild |

`Blocked` represents an **invalid request response**, not a persisted stage state.

The last completed stage is the end of a **contiguous valid prefix** from Grid (00). The next eligible stage is the earliest non-completed stage whose predecessors are valid; this **includes a failed stage for retry**, while the following stages remain locked. The UI never calculates this independently.

### LayerData — read-only after publication

- Every published output belongs to exactly one stage ID.
- Later stages may read prior results but cannot mutate them.
- Grid IDs provide the shared indexing space when geometry exists.
- The Laboratory receives read-only, stage-specific data for completed stages; failed or invalidated stages are unavailable.
- The context does **not** require eight copies of the complete world; store only data owned by each stage and shared references where safe.
- Publishing an output must not expose mutable aliases that let a layer change committed data later.

Context snapshots used for inspection are not saved-game snapshots. Persistence, checkpoint serialization, deep-copy technique and memory budgets are deliberately deferred.

## 5. Execution, failure and rebuild semantics

### 5.1 Common stage engine

`RunNext()`, `RunAll()` and `RunUntil()` must call the **same internal stage execution path**; no parallel implementation for Laboratory mode.

1. Validate the context/session and registry.
2. Resolve the earliest eligible stage and verify its predecessors.
3. Enforce **one running execution per context**.
4. Expose earlier validated results as read-only inputs.
5. Execute the chosen layer with its stage-derived deterministic random stream.
6. Validate a separately held candidate output.
7. Commit the output + `Completed` **atomically**, or publish no candidate and record `Failed`.
8. Return outcome and current progress.

`RunNext()` must never run stage 01 when stage 00 is incomplete; reject requests to skip stages even if the caller or UI is wrong. A retry of stage `Failed` runs only that stage after its prerequisites remain valid. Already-completed outputs cannot be silently recomputed without an explicit rebuild.

### 5.2 RebuildFrom

`RebuildFrom(N)`:

- Is valid only for an existing stage in the same context and only when no stage is running.
- Keeps `0..N-1` outputs and statuses valid.
- Removes/invalidate outputs and finalization eligibility for **N..07**.
- Leaves `N` as the next eligible stage; subsequent generation must still progress forward sequentially.
- Invalidates cached view/overlay data referencing the removed outputs.

Changing the main seed or geometry-defining settings starts a fresh context at Grid. Changing layer-specific settings may eventually use `RebuildFrom` at the earliest affected stage, once a settings dependency model has been approved.

### 5.3 Finalization boundary

`FinalizeWorld()` is a **pure completion gate**: all eight stages must be `Completed`, their data valid and no stage running/failed/invalidated. It returns a final result; **it does not mutate active `WorldState` directly**, open a window or save a game.

The later integration into `state/world/` needs a separate approved contract. A partial Laboratory world never becomes an active game by accident.

## 6. Deterministic random streams

Approved behavior: identical main seed, settings, algorithm versions and valid inputs yield the same world for a full run or eight sequential calls. Use **one deterministic random stream per stage**, derived from main seed plus stable stage identity.

Engineering requirements for a future implementation:

- Specify a stable, versioned seed derivation function independent of runtime process randomization.
- Do not use process-dependent hash functions or shared ambient RNG state as the seed contract.
- Reset a stage's stream deterministically when rebuilding it, while preserving the earlier stage results.
- Compare complete output values in headless tests, not only the count of successful stage executions.

The exact PRNG implementation and stream derivation remain to be selected; the behavior above is approved, while the concrete mechanism is proposed.

## 7. Preliminary error model and exclusivity

The orchestrator must distinguish at least:

- Invalid request or unsupported stage.
- Missing prerequisite / operation not eligible.
- Concurrent or conflicting operation on one context.
- Layer execution failure.
- Invalid candidate output / contract violation.
- Attempt to finalize an incomplete generation.

Operation rejection must not mutate valid outputs. Failure diagnostics should be available in `GetProgress()` without leaking UI-specific wording.

**Still to decide explicitly:** synchronous vs asynchronous execution, cancellation, exception-to-result conversion, CPU/memory limits and detailed concurrency primitive. Do not invent these mechanisms during initial scaffolding.

## 8. Headless contract test cases

The first WG-CORE test harness should use test-only fake layers and **must not** need OpenTK/GPU or real geographic algorithms.

1. New context: all stages pending, Grid next.
2. Grid omission: Tectonics execution request is rejected; no state change.
3. Sequential gating: only stage N becomes eligible after valid completion of N-1.
4. Duplicate/missing/out-of-order layer registrations are rejected.
5. Failure: no partial output published; later stages remain unavailable.
6. Retry: the failed eligible stage can run again, without repeating completed predecessors.
7. Atomicity and read-only data: attempted mutation does not change committed data.
8. `RunAll()` matches sequential `RunNext()` output for equivalent requests.
9. `RunUntil()` generates every prerequisite rather than skipping.
10. `RebuildFrom(Elevation)` preserves stages 00–02 and invalidates 03–07.
11. Rebuilding stage 00 invalidates the entire world draft.
12. A second simultaneous operation on the same context cannot modify data.
13. `GetLayerData()` rejects unavailable/invalidated data and exposes completed output safely.
14. `FinalizeWorld()` rejects incomplete/invalid worlds and succeeds after eight valid stages.
15. Same seed and settings repeat outputs; different stage streams are independent.
16. No reference to OpenTK, window APIs or GPU-dependent execution in the core tests.

These are **planned acceptance checks**, not executed tests or proof of performance.

## 9. Decisions awaiting detailed owner approval

- Exact C# API shapes (sync/async, records/interfaces and result/error types).
- Assembly/project boundary and dependencies among `definitions/`, `engine/`, `state/` and tests.
- Immutable output representation and how atomic publication avoids mutable aliases.
- Precise random algorithm, versioning and stage seed derivation.
- Rebuild/failure/cancellation policies beyond the invariants already approved.
- Metrics, diagnostics, checkpoints and memory/performance budgets.
- Integration with actual Grid later, without adding any geography algorithm to WG-CORE.

**Status:** technical design approved by project owner. CORE-01 is authorized as the next implementation increment on the dedicated `world-core` branch. No source code was implemented by this documentation commit.
