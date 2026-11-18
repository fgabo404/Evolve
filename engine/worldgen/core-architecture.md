# Evolve — WG-CORE: Orchestrator and Generation Context

**Repository:** `fgabo404/Evolve`  
**Integration branch:** `world`  
**Planned implementation branch:** `world-core` (not created)  
**Design status:** APPROVED BY PROJECT OWNER — 2026-11-18  
**Implementation status:** CORE-01 authorized as next increment; no code implemented by approval  
**Parent documents:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md) · [General Roadmap](roadmap.md)
**Technical artifacts:** [Approved technical design](core-contracts.md) · [Approved WG-CORE roadmap](core-roadmap.md)

## 1. Purpose and architectural boundary

WG-CORE defines the common execution contract for Evolve's eight-stage world-generation pipeline. A **single `WorldGenerationOrchestrator`** coordinates execution, while **`GenerationContext`** holds the world being built and the validated intermediate outputs. Stage algorithms remain in `engine/worldgen/layers/`; presentation remains in `game/laboratory/`.

Normal generation and Laboratory generation **must use the same orchestrator and layer implementations**. Laboratory generation advances one stage at a time and can inspect every previously completed stage. The core remains independent of OpenTK, the UI, GPU rendering and save-file storage.

### Logical diagram

```mermaid
flowchart TD
    GAME["Game / Laboratory"] --> ORCH["WorldGenerationOrchestrator"]
    SETTINGS["Definitions: Settings + seed"] --> ORCH
    ORCH --> CTX["GenerationContext: World draft"]
    ORCH --> EXEC["Ordered layer executors"]
    EXEC <--> CTX
    EXEC --> L00["00 — Grid"]
    EXEC --> L01["01 — Tectonics"]
    EXEC --> L02["02 — Landmasses"]
    EXEC --> L03["03 — Elevation"]
    EXEC --> L04["04 — Landforms"]
    EXEC --> L05["05 — Hydrology"]
    EXEC --> L06["06 — Climate"]
    EXEC --> L07["07 — Biomes"]
    CTX --> INSPECT["Laboratory: inspect completed data"]
    ORCH -->|"All 8 stages valid"| RESULT["WorldGenerationResult"]
    RESULT --> STATE["State: active WorldState"]
```

This shows information flow and responsibilities, not required assembly references. The `layers/` directory is a conceptual ordered set of executors, not eight independent generation engines.

## 2. WorldGenerationOrchestrator — functional contract

The orchestrator owns the stage registry/order, eligibility checks, execution lifecycle, success/failure status, downstream invalidation and finalization eligibility. It **does not** generate grid cells, tectonic plates, terrain, rivers, climate or biomes itself.

| Proposed operation | Contract |
| --- | --- |
| `CreateGeneration()` | Create a generation session/context from validated settings and main seed. |
| `RunNext()` | Execute **exactly the next eligible** unfinished stage. Laboratory's forward-execution entry point. |
| `RunAll()` | Run remaining stages in order; stop on failure. Uses the same stage execution path as `RunNext()`. |
| `RunUntil(stage)` | Execute all outstanding prerequisites and stages sequentially up to a target; intended for automation/tests, not Laboratory skipping. |
| `RebuildFrom(stage)` | Invalidate the chosen stage **and every downstream stage**, preserving only earlier valid results; subsequent execution stays sequential. |
| `GetProgress()` | Return per-stage states, latest completed stage, next eligible stage, and relevant error information. |
| `GetLayerData(stage)` | Expose **read-only** results for a successfully completed, non-invalidated stage. |
| `FinalizeWorld()` | Produce a complete world result only after all eight stages have completed and passed validation. |

These operation names describe the **approved functional design**; exact C# signatures, return types, error models, cancellation and async semantics remain implementation-contract decisions.

### Execution modes and entry-point rules

- **Normal world generation:** `RunAll()`, using the identical ordered layer pipeline.
- **Laboratory:** only `RunNext()` is used for forward progression; the user may not select a later stage for execution.
- **Automated runs/tests:** `RunUntil(stage)` progresses through all prerequisites rather than bypassing them.
- **Iteration:** `RebuildFrom(stage)` invalidates that stage and its dependents; any rerun is again in order.
- **Inspection is separate from execution:** viewing Grid while Climate is already complete does not alter completion or generation state.

No operation may grant execution eligibility merely because a UI button was enabled. The orchestrator revalidates prerequisites independently.

## 3. Stage lifecycle and safety guarantees

```mermaid
flowchart TD
    P["Pending"] --> CHECK{"Prior stage valid?"}
    CHECK -->|No| BLOCK["Blocked; no execution"]
    CHECK -->|Yes| RUN["Running"]
    RUN --> VALID{"Generation + validation"}
    VALID -->|Success| DONE["Completed"]
    VALID -->|Failure| FAIL["Failed"]
    FAIL -->|"Retry when eligible"| RUN
    DONE -->|"Rebuild from this or earlier"| INV["Invalidated"]
    INV --> P
```

Stage states are `Pending`, `Running`, `Completed`, `Failed` and `Invalidated`; `Blocked` is a **rejected operation**, not a persisted stage completion state.

The following invariants are approved:

1. **Strict order.** Stage `N` requires the successful, valid completion of preceding stage `N - 1` in the **same** `GenerationContext`; the first stage is Grid (00).
2. **Atomic publication.** A failed stage must not publish partial or unvalidated data into the context or corrupt earlier completed data. Failed results cannot unlock a later stage.
3. **Downstream invalidation.** Rebuilding stage `N` invalidates `N` through stage 07. No old downstream result may be treated as current.
4. **Single active execution per context.** Simultaneous stage executions must be rejected or serialized so they cannot mutate the same world concurrently.
5. **Read-only completion.** Later layers consume earlier validated outputs without modifying the owners' data in place.
6. **Finalization gate.** Only a world with all eight stages completed and validated can produce a final world result for `state/world/`.

The chosen atomic-result storage strategy, cancellation semantics, retry behavior and data memory budgets will be defined before implementation.

## 4. GenerationContext — logical contract

The `GenerationContext` is **temporary generation state**, separate from the one active `WorldState` and from saved-game snapshots in `saves/`.

| Conceptual section | Contents | Ownership |
| --- | --- | --- |
| **GenerationMetadata** | Generation ID, seed, map size, world type, approved settings, relevant generator version metadata | Fixed for one generation session, except through an explicitly defined rebuild/reset workflow |
| **LayerProgress** | Stage status, last completed stage, next eligible stage, execution errors | Orchestrator-controlled; not UI-maintained |
| **LayerData** | GridData, TectonicData, LandmassData, ElevationData, LandformData, HydrologyData, ClimateData, BiomeData | Published by their corresponding stage only after validation |

These data names are **conceptual**, not a commitment to eight concrete class names.

### Ownership and memory principles

- Each stage publishes its own data **indexed to the shared hex-grid identity**, where applicable.
- Reuse grid/cell identifiers instead of copying the entire map after every stage.
- Completed data is exposed to later layers and Laboratory presentation as **read-only**.
- Intermediate results stay inspectable while valid. They are not automatically persisted as game saves.
- Full snapshots, checkpoint persistence, structural sharing, deep copies, memory limits and cleanup policies are not yet specified.
- Partial data is **not** silently promoted into `WorldState`.

### Conceptual data flow

```mermaid
sequenceDiagram
    participant Lab as Laboratory
    participant Orch as Orchestrator
    participant Ctx as GenerationContext
    participant Layer as Next layer executor
    Lab->>Orch: RunNext()
    Orch->>Ctx: Check next stage and prerequisites
    Orch->>Layer: Execute using validated inputs
    Layer-->>Orch: Candidate output or failure
    Orch->>Orch: Validate candidate output
    alt Success
        Orch->>Ctx: Publish output + Completed
        Orch-->>Lab: Updated progress
    else Failure
        Orch->>Ctx: Record failure; preserve valid outputs
        Orch-->>Lab: Failure; next stage locked
    end
    Lab->>Ctx: Inspect completed layer (read-only)
```

This is a **logical** sequence. It does not prescribe direct mutable access from the UI or the final internal C# API.

## 5. Determinism and rebuilding

**Guarantee:** same main seed, same settings, same algorithm versions and the same valid execution inputs must reproduce the same world, regardless of whether stages were run by `RunAll()` or sequential `RunNext()` calls.

The approved random-stream design derives **stage-specific deterministic streams** from the main seed and stage identity. This prevents consuming random values in one stage from accidentally shifting unrelated stages' random sequences.

For example:

```text
WorldSeed
├── GridSeed
├── TectonicsSeed
├── LandmassesSeed
├── ElevationSeed
├── LandformsSeed
├── HydrologySeed
├── ClimateSeed
└── BiomesSeed
```

Example: `RebuildFrom(Elevation)` preserves Grid, Tectonics and Landmasses; it invalidates Elevation, Landforms, Hydrology, Climate and Biomes. Changing the main seed, map size or a global setting that affects the grid starts a **new generation session from Grid**.

The exact seed-derivation function and algorithm-version strategy remain pending engineering design.

## 6. Laboratory integration

The Laboratory will run **inside the existing single `EvolveWindow`**. The UI requests `RunNext()` and renders the results; it does not implement world generation.

- Initially, only **Generate Grid (00)** is eligible.
- When Grid succeeds, only **Tectonics (01)** becomes executable.
- Successfully completed stages can be selected for inspection, even after later stages exist.
- The UI must reflect the orchestrator's progress and errors rather than maintaining a second independent set of stage states.
- The UI may request rebuilding an earlier stage through the orchestrator, which invalidates its dependent stages.
- A generated world is eligible for finalization only after Biomes (07) completes.

Laboratory overlays, navigation design, renderer classes and interactive controls are outside WG-CORE's implementation scope.

## 7. Planned source organization

These are **candidate source files only**. No C# file, interface or project is created by approving this design.

```text
engine/worldgen/
├── WorldGenerationOrchestrator.cs
├── GenerationContext.cs
├── GenerationStage.cs
├── GenerationProgress.cs
├── GenerationResult.cs
├── IWorldGenerationLayer.cs
├── core-architecture.md
├── readme.md
├── roadmap.md
└── layers/
    ├── grid/
    ├── tectonics/
    ├── landmasses/
    ├── elevation/
    ├── landforms/
    ├── hydrology/
    ├── climate/
    └── biomes/
```

Final class/interface decomposition and method signatures are subject to the WG-CORE implementation contract. New C# files must follow the English `//` file-purpose comment policy in `AGENTS.md`.

## 8. WG-CORE acceptance criteria for a future implementation

WG-CORE can be accepted only after tests and technical review demonstrate:

1. Tectonics cannot run before Grid, and no stage can skip a prerequisite.
2. A failed layer leaves prior completed output intact and never unlocks the next layer.
3. `RunAll()` and eight valid sequential `RunNext()` executions produce equivalent results for the same seed/settings and algorithm versions.
4. Rebuilding an earlier layer invalidates all dependent outputs.
5. Completed outputs remain available for read-only inspection.
6. The engine executes and can be validated **without OpenTK or GPU dependencies**.
7. `FinalizeWorld()` fails or remains unavailable until every stage is valid.

These are **future validation gates, not test results**. No engine, stage or UI implementation has been executed as part of this design approval.

## 9. Remaining engineering decisions

The approved design does **not yet determine**: exact C# API signatures; settings DTOs and dependency direction between assemblies; stage result representation; resource ownership/copy semantics; concurrency primitives; cancellation and asynchronous execution; failure recovery details; checkpoint storage; exact derivation of stage seeds; internal event/progress notification mechanism; or user-interface layout.

The owner has **approved** the [technical design](core-contracts.md) and [implementation roadmap](core-roadmap.md), and authorized **CORE-01 only**. Exact C# signatures and implementation details belong to CORE-01, with later increments gated separately. The `world-core` branch is created from this approved baseline.

**Design approved. Implementation remains pending.**
