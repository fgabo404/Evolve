# Evolve — WG-CORE Technical Contracts

**Repository:** `fgabo404/Evolve`  
**Integration branch:** `world`  
**Delivery branch:** `world-core`

**Status:** APPROVED TECHNICAL DESIGN — 2026-11-18; exact C# signatures resolved in CORE-01  
**Approved behavioral foundation:** [WG-CORE architecture](core-architecture.md)  
**Delivery plan:** [WG-CORE roadmap](core-roadmap.md)

## 1. Contract boundaries

The **approved** system behavior is sequential execution of eight stages by one `WorldGenerationOrchestrator`, over a temporary `GenerationContext`, with atomic publication of validated results, deterministic replay, strict prerequisites, downward invalidation and completion-only finalization.

The project owner approved this technical design on 2026-11-18 and accepted
the CORE-01 surface in section 10 and CORE-02 storage surface in section 12.
CORE-03 implements sequential execution as documented in section 14, pending
review and owner acceptance. PRNG, rebuild and finalization remain deferred.

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

The following signatures record the approved conceptual design. CORE-02 provides
inspection (section 12), and CORE-03 provides synchronous forward execution
(section 14). Rebuild, PRNG and finalization remain deferred.

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

These are planned acceptance checks for the complete WG-CORE. CORE-01 verifies
registration/configuration (section 10); CORE-02 verifies temporary storage,
progress observations and alias protection (section 12). Neither increment
demonstrates layer execution, determinism, finalization or performance behavior.

## 9. Decisions awaiting detailed owner approval

- Exact C# API shapes (sync/async, records/interfaces and result/error types).
- Assembly/project boundary and dependencies among `definitions/`, `engine/`, `state/` and tests.
- Immutable output representation and how atomic publication avoids mutable aliases.
- Precise random algorithm, versioning and stage seed derivation.
- Rebuild/failure/cancellation policies beyond the invariants already approved.
- Metrics, diagnostics, checkpoints and memory/performance budgets.
- Integration with actual Grid later, without adding any geography algorithm to WG-CORE.

**Status:** CORE-01 and CORE-02 accepted by the project owner. CORE-03 is
implemented on `world-core`, pending review and acceptance. CORE-04 and CORE-05
remain unimplemented and require separate authorization. Sections 10 through 13
preserve the earlier delivery and authorization records; section 14 defines the
current execution surface.

## 10. CORE-01 implemented C# contract

### Project boundaries

| Project | Namespace / responsibility | Project references |
| --- | --- | --- |
| `definitions/worldgen/Evolve.Definitions.WorldGeneration.csproj` | `Evolve.Definitions.WorldGeneration`: approved configuration values only. | None. |
| `engine/worldgen/Evolve.Engine.WorldGeneration.csproj` | `Evolve.Engine.WorldGeneration`: request, stage identity, layer identity and validated registry. | Definitions project only. |
| `tests/worldgen/Evolve.WorldGeneration.Checks.csproj` | `Evolve.WorldGeneration.Checks`: executable headless checks against the actual production assemblies. | Engine project, with definitions available transitively. |

All three projects target `net10.0`, enable nullable reference checking and
implicit framework usings, and introduce no NuGet packages. The two production
projects are libraries; the test project is an executable, following the existing
menu-check convention. CORE-01 required no solution, shared UI project, friend
assembly or linked production-source copy. CORE-02 adds only the restricted
friend grant documented below; the project dependency direction is unchanged.
Existing game/menu-test projects and state/save modules remain unchanged.

### Stable identities

`Evolve.Engine.WorldGeneration.GenerationStage` is a public enum with exactly:

```csharp
Grid = 0,
Tectonics = 1,
Landmasses = 2,
Elevation = 3,
Landforms = 4,
Hydrology = 5,
Climate = 6,
Biomes = 7
```

The enum value is the unique layer identifier; a second string identifier or
independent order property would duplicate that source of truth. No unknown,
extension, sentinel or completion stage is included.

The definitions assembly exposes public enums `WorldType` (`Pangea = 0`,
`Continents = 1`, `Archipelagos = 2`, `RealEarth = 3`) and `MapSize` (`Small = 0`,
`Medium = 1`, `Large = 2`). They select approved configurations only. There are
no rows, columns, cell counts, physical dimensions or quantitative settings.

### CORE-01 delivery public members (execution extended in section 14)

| Type | C# signatures |
| --- | --- |
| `public sealed record WorldGenerationSettings` in definitions | `WorldGenerationSettings(WorldType worldType, MapSize mapSize)`; `WorldType WorldType { get; }`; `MapSize MapSize { get; }`. |
| `public sealed record GenerationRequest` in engine | `GenerationRequest(long seed, WorldGenerationSettings settings)`; `long Seed { get; }`; `WorldGenerationSettings Settings { get; }`. |
| `public interface IWorldGenerationLayer` in engine | `GenerationStage Stage { get; }` only. |
| `public sealed class WorldGenerationLayerRegistry` in engine | `WorldGenerationLayerRegistry(IEnumerable<IWorldGenerationLayer> layers)`; `IReadOnlyList<IWorldGenerationLayer> Layers { get; }`; `IWorldGenerationLayer GetLayer(GenerationStage stage)`. |

The constructors validate data without creating a session. Settings reject
undefined enum casts; requests reject null settings. The signed 64-bit seed is
preserved verbatim, including zero and negative values. This is a representation
choice, not a PRNG, normalization or seed-derivation algorithm. Effective layer
settings, generator/algorithm version metadata and context freezing are deferred
until their owning increments specify the contracts. Existing game display
metadata continues to derive from root `VERSION`; assembly versions are not
silently adopted as an algorithm-reproducibility contract.

### Registry guarantees and errors

The registry copies the input sequence, validates exactly eight non-null layers,
and checks known, unique stage identities in canonical Grid-to-Biomes order.
It never sorts, fills gaps, substitutes layers or registers production fakes.
With exactly eight known, unique ordered identities, no stage can be missing.
Only a fully validated read-only membership collection is exposed. Changes to a
caller's original array/list cannot change the registry.

Layer implementations must keep `Stage` stable for their entire lifetime. The
registry retains those instances; it does not clone executors or add a context
ownership system. `GetLayer` inspects registration only and grants no execution
eligibility. A future orchestrator still owns prerequisite checks and execution.

- Null input sequence or null request settings: `ArgumentNullException`.
- Wrong registration count, null entry, unknown/duplicate stage or wrong order:
  `ArgumentException` for `layers`.
- Undefined settings enum value or lookup stage: `ArgumentOutOfRangeException`.

These are constructor/lookup validation exceptions, not a decision about the
later orchestrator's operation-result or execution-failure model.

### Execution scope at CORE-01 delivery

`IWorldGenerationLayer` deliberately has no `Execute` method in CORE-01.
Read-only input views, isolated candidate output, stream ownership and result
validation require the actual CORE-02/CORE-03 context and execution design.
CORE-01 introduced no empty context, no-op execution, `object` payload, throwing
execution stub, random implementation or final-world result. CORE-02 adds the
actual storage lifecycle below without changing the layer interface. Later
execution signatures must extend it through their authorized increment.

Exactly eight metadata-only fake implementations live in
`tests/worldgen/FakeWorldGenerationLayers.cs`. They cover all eight identities;
the Grid fake can supply malformed metadata for rejection checks. They perform
no generation, and production libraries contain no layer implementations.

### Validation commands

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\worldgen\Evolve.WorldGeneration.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

The new executable checks canonical values/order, valid registration with eight
distinct fakes, every duplicate pair, each missing stage, bad counts and order,
unknown identities, null entries, read-only membership, invalid lookups,
configuration/request validation and compiled dependency independence from
OpenTK/UI. It needs no GPU or geographic algorithms. It uses `dotnet run`, not
`dotnet test`; any failed check returns a nonzero process exit code.

CORE-01 development validation: 15/15 new checks and all 16/16 existing menu
checks passed. Both explicit Release builds completed with zero warnings and
errors, and `git diff --check` passed. The built game informational version was
verified as `v0.0.15`. The world-generation check dependency manifest contained
only its own project and the engine/definitions libraries, with no OpenTK.
No GUI, context or geographic execution was performed; these checks are not
independent audit or owner acceptance.

## 11. CORE-01 acceptance and CORE-02 authorization

The project owner accepted CORE-01 on 2026-11-18 after reviewing the reported
15/15 core checks, 16/16 existing menu checks, clean Release builds, and a
separate architectural source review. Those checks were performed by the
developer and reproduced in the user's console output; no new independent
auditor execution is claimed.

CORE-02, `GenerationContext`, is authorized as the next increment on
`world-core`. Implementation must preserve the CORE-01 public contracts,
keep temporary state separate from `WorldState` and save files, provide
read-only completed-layer inspection and protect against mutable aliasing.
CORE-03 execution, CORE-04 determinism/rebuild, and CORE-05 finalization
remain explicitly unauthorized.

## 12. CORE-02 context and read-only storage contract

### Public C# surface

All new production types remain in `Evolve.Engine.WorldGeneration` and introduce
no packages or project references. CORE-01 types and signatures are unchanged.

| Type | Implemented explicit members |
| --- | --- |
| `GenerationStageStatus` | `Pending = 0`, `Running = 1`, `Completed = 2`, `Failed = 3`, `Invalidated = 4`. No `Blocked` storage state. |
| `public sealed class GenerationContext` | `GenerationContext(GenerationRequest request)`; `GenerationRequest Request { get; }`; `GenerationProgress GetProgress()`; `LayerData? GetLayerData(GenerationStage stage)`. |
| `public sealed class GenerationProgress` | `GenerationStageStatus GetStatus(GenerationStage stage)`; `string? GetFailure(GenerationStage stage)`; `GenerationStage? LastCompletedStage { get; }`; `GenerationStage? NextStage { get; }`. No public constructor or mutators. |
| `public abstract class LayerData` | `GenerationStage Stage { get; }`; a `private protected` constructor restricts derived implementations to engine/trusted friend code. |
| `public sealed class LayerData<T> : LayerData, IReadOnlyList<T> where T : unmanaged` | `LayerData(GenerationStage stage, IEnumerable<T> values)`; `IReadOnlyList<T> Values { get; }`; `int Count { get; }`; get-only `T this[int index]`; normal generic/non-generic enumeration. |

The context retains the already immutable sealed `GenerationRequest` and sealed
settings. Seed, world type and map size are read through `Request`; there is no
second metadata store, session randomness, geometry parameter or new default.

### Single authoritative state and immutable observations

The context owns one private table with eight immutable slot records. Each
record holds its status, published data and optional failure diagnostic together.
All eight slots begin `Pending`, with no data or diagnostics. Public callers
can only inspect the context; no arbitrary state setter is available.

`GetProgress` copies only the eight small status/diagnostic records into private
snapshot arrays, never the world or layer buffers. Snapshots expose lookups, not
mutable arrays, collections or live views. Old observations do not change after
later storage transitions. They are observations, not a second writable source
of progress truth.

`LastCompletedStage` ends the contiguous `Completed` prefix from Grid. `NextStage`
is the first non-completed slot after that prefix, including `Failed` or
`Invalidated`; it is null if every stage is completed or any slot is `Running`.
This is derived progress information, not an execution command or a substitute
for CORE-03's prerequisite checks.

### Output ownership and mutable-alias protection

`LayerData<T>` copies its source sequence once into a privately owned array.
The `unmanaged` constraint permits scalar/value-only records and prevents normal
C# callers from hiding mutable managed arrays, lists or reference objects inside
an element. The class is sealed. The public abstract base has a restricted
constructor, so ordinary external consumers cannot supply arbitrary mutable subclasses.
No `object` payload, generic shallow-copy callback or serialization format is used.

The public value view implements only read-only list/enumeration interfaces;
it is not an array or mutable collection wrapper, and supplies no backing-array
accessor. Indexing and iteration return values by copy. Changes to original
arrays/lists, including structured test values, cannot change captured data.
Consumers cannot cast the value view to `IList<T>` or a non-generic collection
to recover a writable container. Reflection, unsafe memory manipulation and
trusted engine/friend-assembly internals are not public mutation contracts.

Publication retains the immutable buffer rather than copying it again. A slot
accepts only data with the same immutable stage identity. Another stage cannot
overwrite or claim that slot. Inspection returns that same immutable result;
it never duplicates a complete world for each stage. More complex future
geographical outputs requiring managed references need their own explicitly
approved immutable contract; they must not bypass this constraint via `object`.

`GetLayerData` returns null for `Pending`, `Running`, `Failed` or `Invalidated`
slots. A candidate constructed outside the context is not published automatically.
Discarded results cannot be retrieved through new context queries. Previously
held immutable observations/results remain historical references; this storage
boundary does not revoke references or implement Laboratory cache invalidation.

### Restricted internal preparation methods

Only engine-internal code and the specifically named test friend can call:

| Method | Storage transition / invariant |
| --- | --- |
| `MarkRunning(GenerationStage stage)` | Only `Pending`, `Failed` or `Invalidated` → `Running`; clears that slot's data and diagnostic. |
| `Publish(GenerationStage stage, LayerData data)` | Only `Running` → `Completed`; requires matching ownership and replaces state/data in one immutable slot record. |
| `MarkFailed(GenerationStage stage, string diagnostic)` | Only `Running` → `Failed`; requires a nonblank diagnostic and publishes no candidate. |
| `InvalidateStage(GenerationStage stage)` | Only `Completed` → `Invalidated`; drops only that slot's published reference. No downstream cascade or rebuild. |

Arguments and transitions are checked before replacing a slot, so rejection
preserves its previous valid state and all other slots. Undefined stage IDs
throw `ArgumentOutOfRangeException`; null requests, candidates and value sources
throw `ArgumentNullException`; ownership mismatch/blank diagnostics throw
`ArgumentException`; invalid transitions throw `InvalidOperationException`.

These are storage preparation primitives, not layer execution, retry workflows,
prerequisite enforcement or an alternate orchestrator. They do not choose the
next layer, run an algorithm or automatically invalidate downstream stages.
Calls must be serialized by engine ownership; concurrent access, operation
exclusivity, cancellation and full execution transactions are not guaranteed by
CORE-02. CORE-03/CORE-04 must supply their approved operational policies.

`AssemblyVisibility.cs` grants `InternalsVisibleTo` only to the existing
`Evolve.WorldGeneration.Checks` assembly. This allows direct tests of real
storage methods without adding public mutators or a fake orchestration path.
No production project references tests, OpenTK, game, active state or saves.

### Validation and remaining scope

`tests/worldgen/GenerationContextChecks.cs` uses these internal methods directly
as storage tests. `FakeLayerValue.cs` contains a neutral unmanaged sample value
only; there is no geographic algorithm, test-only context or simulated runner.
The accepted CORE-01 check suite and metadata-only fake layers remain unchanged.

Run all regression commands from section 10. Real layer execution, `RunNext`,
`RunAll`, `RunUntil`, orchestrator APIs, PRNG, automatic downstream invalidation,
operational rebuild, `FinalizeWorld`, state integration and GUI changes remain
unimplemented. No acceptance of CORE-02 or authorization of CORE-03 is implied.

Executed development regression: 15/15 unchanged CORE-01 checks, 19/19 new
CORE-02 storage checks and 16/16 menu checks passed. The explicit engine and
game Release builds each reported zero warnings and errors. Whitespace checking
passed, and the compiled game informational version matched `v0.0.17`.
These are headless logic/build results, not GUI validation, geographic
generation, orchestrator execution or independent audit acceptance.

## 13. CORE-02 acceptance and CORE-03 authorization

CORE-02 was accepted by the project owner on 2026-11-18 after reviewing the
reported 15/15 CORE-01, 19/19 CORE-02 and 16/16 menu checks, Release builds,
and the source-level architectural review. These are developer-executed checks
and an architectural code review, not an independently executed audit.

CORE-03 is authorized on `world-core` to implement the sequential engine
orchestrator. The execution contract must have one shared stage runner for
`RunNext`, `RunAll` and `RunUntil`; precondition checking against the same
context; read-only earlier results; atomic publish on valid candidate results;
failed/invalid candidate results recorded as `Failed` with no candidate exposed;
and retry of the eligible failed stage. Test-only deterministic layer executors
may be added or adapted under `tests/worldgen/`.

`IWorldGenerationLayer` currently exposes identity only. CORE-03 may extend it
with the minimum needed invocation contract without introducing a PRNG, a
second generator, UI operations or a mutable reference escape hatch. The
existing registration guarantees must remain intact.

CORE-04 (seed streams, rebuild/invalidation and full operation exclusivity)
and CORE-05 (final result and state integration) are **not authorized** by this
approval. Exact invocation signatures and error-result representation must be
documented alongside the CORE-03 implementation.

## 14. CORE-03 sequential execution contract

**Status:** accepted by project owner on 2026-11-18. CORE-01/02 also remain
accepted. This section supersedes their execution deferrals only; registration,
configuration, immutable storage and public context inspection remain intact.

### Final C# surface

All types remain in `Evolve.Engine.WorldGeneration`, using existing .NET 10
projects with no added package, project reference or production layer executor.

| Type | Explicit public members |
| --- | --- |
| `IWorldGenerationLayer` | Existing `GenerationStage Stage { get; }`; new `LayerExecutionResult Execute(GenerationLayerInputs inputs)`. |
| `public sealed class GenerationLayerInputs` | `GenerationRequest Request { get; }`; `GenerationStage Stage { get; }`; `LayerData GetLayerData(GenerationStage stage)`. Internal construction only. |
| `public sealed class LayerExecutionResult` | `LayerData? Candidate { get; }`; `string? Failure { get; }`; `static LayerExecutionResult Success(LayerData candidate)`; `static LayerExecutionResult Failed(string diagnostic)`. Private construction. |
| `public sealed class WorldGenerationOrchestrator` | `WorldGenerationOrchestrator(WorldGenerationLayerRegistry registry)`; `GenerationContext CreateGeneration(GenerationRequest request)`; `GenerationProgress RunNext(GenerationContext context)`; `GenerationProgress RunAll(GenerationContext context)`; `GenerationProgress RunUntil(GenerationContext context, GenerationStage stage)`. |

The implementation is synchronous. All forward operations return the existing
immutable `GenerationProgress` snapshot, including status and diagnostic of any
failed attempt. A second `StageExecutionResult` wrapper for `RunNext` would
duplicate that information, so the conceptual return type in section 3 resolves
to `GenerationProgress`. `LayerExecutionResult` is solely a layer-to-orchestrator
candidate/failure envelope, not another progress store or published result.

### Read-only inputs and isolated candidates

The orchestrator constructs a restricted input view after prerequisite checks.
It contains immutable request metadata, the active identity and private
references to earlier completed immutable buffers only. It does not retain or
expose `GenerationContext`. Looking up the active or a later stage throws
`InvalidOperationException`; unknown IDs throw `ArgumentOutOfRangeException`.
Retaining an input view cannot later reveal the layer's own or future output.
Only the small array of buffer references is copied, not earlier layer data.

A layer returns `Success(new LayerData<T>(itsStage, values))` or
`Failed(diagnostic)`. Existing unmanaged-only buffer ownership and defensive
source copying remain unchanged. The outcome factory rejects null success data
and blank failure diagnostics. A failure outcome cannot carry candidate data.
No `object` payload, mutable context handle, PRNG argument, geographical shape
or fabricated execution context is introduced.

### One forward path and prerequisite gate

`RunNext`, `RunAll` and `RunUntil` delegate to one private `Advance` loop. Before
each attempt it checks the context's own progress and published ownership:

1. Any Running stage rejects the operation before mutation.
2. Completed results must form a contiguous valid prefix from Grid. A completed
   stage after a gap is rejected rather than silently used as a prerequisite.
3. Invalidated storage is rejected; operational rebuild is not part of CORE-03.
4. The first unfinished stage is eligible only with every predecessor completed.
   There is no public arbitrary-stage runner. A target limits forward progress;
   it cannot select Tectonics before Grid or skip any prerequisite.
5. A completed target or completely generated draft is a no-op. Completed layers
   never run again. Creating a context executes no layer.

The runner captures inputs, marks only the eligible stage Running, checks the
registered executor's still-matching identity, and invokes it once. A successful
candidate must be non-null and belong to that exact stage. `Publish` installs
data and Completed together using CORE-02's immutable slot replacement. There
is no separate validation algorithm for geographic content, since no real
layer data or algorithms exist yet; a layer reports its own validation failure
through the explicit failure outcome.

### Failure, retry and operation errors

Explicit layer failure, ordinary execution exception, missing execution result,
changed executor identity or mismatched candidate ownership records Failed in
the active slot with an English diagnostic. No candidate is published; prior
buffer references and all downstream Pending slots remain unchanged. Exceptions
are converted to diagnostic strings containing exception type and message.
`RunAll` and `RunUntil` return immediately on that failure. They never retry in
the same call. A later call retries the same earliest failed stage, without
rerunning completed predecessors; success replaces its failure diagnostic.

Null registry/request/context arguments throw `ArgumentNullException`;
undefined targets throw `ArgumentOutOfRangeException`. Running, inconsistent
completion prefixes and invalidated storage throw `InvalidOperationException`
before execution or publication. These are rejected operations, not layer
failures, and do not replace valid state with Failed.

Running rejection includes synchronous reentrancy from a layer's callback.
Callers must serialize operations and inspection. Cross-thread operation
exclusivity, cancellation and async scheduling are not guaranteed here; the
full concurrency strategy remains CORE-04. No locks, execution leases, PRNG,
seed streams, rebuild/cascade, finalization or `WorldState` integration are
implemented ahead of their authorized increments.

### Verification scope

`WorldGenerationOrchestratorChecks.cs` invokes the actual production runner and
the eight test-only executors. Its 17 named checks include 96 injected failure
scenarios (four failure kinds at eight stages in three modes) and 24 retry
scenarios. It verifies ordered execution, all targets/prerequisites, mode
equivalence using prior inputs, completed no-ops, atomic failure, immutable
inputs, source isolation, independent contexts, Running/reentrant rejection,
invalid arguments and absence of CORE-04/05 APIs. Exact coverage and regression
commands are in [tests/worldgen/readme.md](../../tests/worldgen/readme.md).

The existing 15 CORE-01 and 19 CORE-02 checks remain active. Only their former
execution-deferral assertions and test-layer implementations adapt to the
authorized CORE-03 interface. These checks are development evidence, not an
independent audit or owner acceptance. No geographic or GUI execution occurs.

Executed development evidence for `v0.0.19`: 15/15 CORE-01, 19/19 CORE-02,
17/17 CORE-03 and 16/16 menu checks passed. The explicit engine and game Release
builds each reported zero warnings and errors; `git diff --check` passed.
The built game's ProductVersion and generated AssemblyInformationalVersion
both matched root `VERSION`. The world-generation dependency manifest contained
only the checks, engine and definitions projects, with no OpenTK.

The initial sandboxed test invocation could not read the user's NuGet.Config;
the authorized invocation with that access succeeded. Direct reflection from
the installed PowerShell runtime could not resolve System.Runtime 10.0.0.0;
version verification instead used the compiled file's ProductVersion and the
generated assembly attribute. No GUI validation was performed or claimed.

## 15. CORE-03 acceptance and CORE-04 authorization

CORE-03 was accepted by the project owner on 2026-11-18 after the reported
15/15 CORE-01, 19/19 CORE-02, 17/17 CORE-03 and 16/16 menu check results,
Release builds and architectural source review. This acceptance does not imply
that a second independent test run was performed.

CORE-04 is authorized on `world-core` to implement the existing approved
invariants of deterministic stage-level PRNG, reproducible execution across
modes and rebuilds, downstream result invalidation and exclusive same-context
operation access across threads and orchestrator instances.

Implementation must define and document a stable, versioned PRNG and stage seed
derivation not based on process-randomized hashes, process ambient RNG, clock or
thread scheduling. The layer input contract may be minimally extended to
provide an isolated deterministic stream; the stream for stage N must not be
consumed by other stages. Identical seed, settings, version and validated inputs
must produce equal output values through `RunAll`, sequential `RunNext`,
`RunUntil` and rebuilding the same stage. Versioning must be recorded without
confusing root game VERSION with the algorithm compatibility contract.

`RebuildFrom(stage)` must atomically invalidate/discard all results from that
stage through Biomes, preserve only valid predecessors, restore forward
eligibility starting at that stage, and avoid exposing or using stale data.
Define precise statuses for previously completed, failed and pending downstream
slots. Reject rebuild while a context is being executed; ensure a rebuilt draft
can resume via the existing shared forward runner. A new main seed or
world-size change creates a new context rather than altering metadata in place.

Context-level cross-thread exclusivity must be real, not just the current
synchronous Running snapshot check. Competing operations from the same or
another orchestrator should be rejected without corrupting an active stage.
Inspecting progress and completed data must yield coherent observations under
the chosen ownership model. No mutable inputs/results escape externally.

CORE-05 (`FinalizeWorld` and final result/state boundary), real geographic
algorithms, UI modifications and player saves remain unauthorized. Document
exact new C# signatures, determinism test vectors and error semantics in the
CORE-04 delivery. All earlier core and menu regressions must remain valid.

## 16. CORE-04 deterministic streams and exclusive rebuild

**Status:** accepted by project owner on 2026-11-18. This section
supersedes the earlier execution/rebuild/concurrency deferrals. CORE-05 is authorized separately.

### Versioned arithmetic and public additions

`DeterministicStageRandom` is sealed. Its public contract is
`DeterministicStageRandom(ulong seed)`, `ulong NextUInt64()`,
`static ulong DeriveSeed(long seed, GenerationStage stage)`, and constant
`AlgorithmVersion = "splitmix64-stage-v1"`. `GenerationLayerInputs` adds the
get-only `DeterministicStageRandom Random` property. Each attempt creates a new
stream from request seed and canonical identity; retry/rebuild never continues
an old stream. No execution-interface signature changes.

Arithmetic is unsigned modulo 2^64, explicitly unchecked. Define gamma =
0x9E3779B97F4A7C15. Mixing x uses, in order:
`x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9`,
`x = (x ^ (x >> 27)) * 0x94D049BB133111EB`,
then `x ^ (x >> 31)`. Stage initial state is
`Mix(unchecked((ulong)mainSeed + gamma * ((ulong)stage + 1)))`.
Each NextUInt64 first adds gamma to stream state, then returns Mix(state).
Negative main seeds preserve their two's-complement bits.

Reproducible raw-stream vector for seed 0, first three NextUInt64 calls:
`E220A8397B1DCDAF`, `6E789E6AA1B965F4`, `06C45D188009454F`.
DeriveSeed(0, Grid) = `E220A8397B1DCDAF`. Raw-stream construction and stage
derivation are distinct operations; these constants are checked independently
of mode/replay tests. Different stage IDs have distinct states for a fixed seed.
Consumption in one stream cannot advance another.

This compatibility identifier versions both PRNG arithmetic and derivation,
independently of root game VERSION. Future arithmetic or derivation changes
require a new identifier and vectors. Layer algorithm versions and deterministic
layer behavior remain the responsibility of future layer implementations.
Matching seed, configuration, algorithm versions and validated inputs reproduce
outputs when executors use deterministic logic and their supplied stream.
No geographic algorithm, persistence format, clock or global random state exists.

### Exclusive operations and coherent inspection

Each context owns an internal Interlocked operation flag. All forward modes
acquire it before entering the single existing runner and release it in finally,
including on invalid input/state and failure. Rebuild acquires the same flag.
Competing or reentrant operations throw InvalidOperationException immediately,
including through different orchestrator instances. The flag spans entire bulk
calls, including gaps between stages. Different contexts have independent flags.

A separate private context lock protects slot transitions, progress snapshots,
result lookups and whole suffix invalidation. It is held only for short storage
operations, never during layer execution. A progress snapshot therefore sees
either side of a complete rebuild, not a partially invalidated suffix. Separate
queries may naturally observe different instants; previously retrieved immutable
results and progress remain historical observations. No mutable public context
access or global lock is introduced. The per-attempt random stream itself is
single-consumer, not an independently thread-safe shared service.

### Rebuild API and lifecycle

`GenerationProgress RebuildFrom(GenerationContext context, GenerationStage stage)`
invalidates without executing a layer. Null contexts throw ArgumentNullException;
undefined IDs throw ArgumentOutOfRangeException. Rebuild requires completed
predecessors in the same context; otherwise it throws InvalidOperationException
without mutation. A Running stage or active operation also rejects rebuilding.

The selected suffix through Biomes is replaced atomically:

- Completed and already Invalidated slots become/remain Invalidated.
- Failed and Pending slots become Pending.
- Every affected slot loses its published data and diagnostic.
- Earlier slots and their immutable buffer identities remain unchanged.

The next eligible stage is the selected stage, and the same forward runner
accepts Invalidated slots. Rebuilding Grid drops all eight outputs. New queries
return no stale suffix data. Old immutable references are historical values,
not current context results. A new seed, world type or size requires a new
immutable request/context; metadata is never changed in place.

### Checks and scope

Eight new named checks cover fixed vectors, signed seeds, stage independence,
all four world types and three sizes across all modes, every rebuilt suffix,
replay and ordering, mixed completed/failed/pending states, retry randomness,
invalid requests, real barrier-coordinated cross-thread contention across two
orchestrators, independent-context execution and coherent Running observations.
CORE-01/02/03 checks remain active; only former CORE-04 absence assertions are
updated to continue prohibiting CORE-05. There is no second execution engine,
PRNG package, GPU dependency, finalization, real geography or UI change.

Development validation for v0.0.21: 15/15 CORE-01, 19/19 CORE-02, 17/17 CORE-03,
8/8 CORE-04 and 16/16 menu checks passed. Engine and game Release builds reported
zero warnings/errors. Whitespace verification passed after removing trailing
blank lines in documentation. Compiled ProductVersion matched root VERSION.
No GUI or geographic validation or independent audit acceptance is claimed.

## 17. CORE-04 acceptance and CORE-05 authorization

CORE-04 was accepted by the project owner on 2026-11-18 after reviewing the
59/59 core checks, 16/16 menu checks, successful Release builds and source
architecture. This is developer-executed test evidence plus a source review,
not an independently executed audit.

CORE-05 is authorized on `world-core` to add the completion gate
`WorldGenerationOrchestrator.FinalizeWorld(context)` and an immutable
`WorldGenerationResult`, with the minimal contract needed for validation and
safe downstream state integration **at a later, separately authorized stage**.

Finalization must reject unfinished, Failed, Running, Invalidated, missing or
inconsistent layer data, including after an incomplete rebuild, with no context
mutation. Acquire the same context-wide operation lease as forward generation
and rebuilding; ensure a coherent snapshot against concurrent operations.
On success expose immutable request metadata, version-relevant metadata and
all eight completed stage results as read-only data, with no mutable aliases,
no re-execution, no full-world duplication and no automatic `WorldState` update.
A finalized result is a stable historical snapshot even if its source context
is later rebuilt. Decide and document the exact C# API/error policy during
CORE-05; do not invent geographic result classes or persistence requirements.

Add headless checks for all finalize gates, read-only historical result behavior,
repeat finalization, concurrent/reentrant rejection and preservation of
CORE-01..04 regressions. No new real geographic executors, UI, save system or
merge is authorized. After development the roadmap remains pending independent
audit and owner acceptance before any integration into `world`.
