# Evolve — WG-CORE Implementation Roadmap

**Repository:** `fgabo404/Evolve`  
**Integration branch:** `world`  
**Working branch:** `world-core`

**Status:** APPROVED BY PROJECT OWNER — 2026-11-18; CORE-01 accepted, CORE-02 authorized; CORE-03..05 not authorized  
**Approved design:** [WG-CORE architecture](core-architecture.md)  
**Proposed engineering contract:** [WG-CORE technical contracts](core-contracts.md)

## Objective

Build and verify the **generation control core only**: one orchestrator, a temporary generation context, a strict eight-stage contract, deterministic execution and rebuild/invalidation. It must remain usable without a graphical window.

No hex-grid algorithm, real-world tectonics, landmass generator, elevations, hydrology, climate, biome logic, Laboratory screen or playable world creation is in this roadmap.

## Small, independently gated implementation increments

| Increment | Scope | Evidence / exit gate | Status |
| --- | --- | --- | --- |
| **CORE-01 — Contract foundation** | Approve exact contracts, stage identities, generation request shape and minimal headless project/test organization; register eight test-only executors | Stage registry validates order/identity; builds without GPU; owner-approved C# API | Accepted by project owner — 2026-11-18 |
| **CORE-02 — GenerationContext** | Immutable request metadata, per-stage output ownership, read-only inspection and stage progress states | No mutability leak; no partial or invalidated outputs visible; test-only data accepted | Authorized — pending implementation |
| **CORE-03 — Orchestrator execution** | `CreateGeneration`, `RunNext`, `RunUntil`, `RunAll`; prerequisite validation, atomic publication, failure and retry | No skipped stages; no invalid commit; all entry modes share one execution path | Not started |
| **CORE-04 — Determinism and rebuild** | Stage-specific seeded streams, one active execution per context, `RebuildFrom` and downstream invalidation | Same seed/settings yield same output across modes; rebuilding N removes N..07 | Not started |
| **CORE-05 — Finalization and verification** | `FinalizeWorld` as complete-result boundary; unit/contract tests; verification and developer/auditor evidence | Completed eight stages only; headless tests and Release build; accepted review | Not started |

**Execution rule:** work on only the individually authorized increment. Do not advance from CORE-01 to CORE-02 merely because its code is complete; wait for explicit owner approval.

## Proposed delivery flow

```mermaid
flowchart TD
    APPROVAL["Owner approves detailed WG-CORE plan"] --> BRANCH["Create world-core from world"]
    BRANCH --> C1["CORE-01: Contracts"]
    C1 --> G1{"Owner accepts increment?"}
    G1 -->|Yes| C2["CORE-02: GenerationContext"]
    G1 -->|No| C1
    C2 --> C3["CORE-03: Execution"]
    C3 --> C4["CORE-04: Determinism + rebuild"]
    C4 --> C5["CORE-05: Finalization + tests"]
    C5 --> AUD["Independent audit and owner acceptance"]
    AUD --> INT["Separate authorization to integrate into world"]
```

Every increment receives its own test evidence and accepted Git commit, with root `VERSION` incremented according to `AGENTS.md`. No automatic merge, branch deletion or next-layer development is implied.

## Key architectural gates

- Only the orchestrator can publish validated output or mutate progress.
- Stage N cannot run before stage N-1 completes in the same context.
- The Laboratory can call only `RunNext()` for forward execution and inspect completed layers.
- `RunAll()`, `RunUntil()` and `RunNext()` share the same stage execution path.
- Failed output is not published; later stages remain locked; retry respects prerequisites.
- Rebuild invalidates the selected layer and all downstream outputs.
- Finalization requires eight valid completed layers and returns a result, not direct `WorldState` mutation.
- Test-only fake layers are allowed for WG-CORE; no real geographical algorithms are implemented ahead of their layer stage.
- No new native window, UI change, OpenTK dependency or save-game snapshot machinery.

## Verification requirements per increment

- Add/update GPU-independent tests for the increment's invariants.
- Verify actual test results, a Release build and `git diff --check` where applicable.
- Report exactly what ran, any skipped checks and outstanding decisions.
- Confirm only authorized source paths changed; preserve existing menu tests.
- Obtain independent audit and owner approval before final integration into `world`.

## Pending pre-implementation decisions

1. Approve the proposed operation semantics and concrete API/error types.
2. Approve initial .NET project boundaries and dependency direction.
3. Choose how completed data becomes read-only and how atomic publication is implemented.
4. Choose a stable, versioned PRNG and stage-seed derivation strategy.
5. Define cancellation/concurrency detail appropriate to the first implementation.
6. Finalize CORE-01 acceptance tests and minimal file list.

These decisions must be resolved **before or within a separately authorized CORE-01 design gate**, not silently coded as developer assumptions.

**Roadmap status:** approved. CORE-01 was accepted by the project owner on
2026-11-18 following developer test evidence and an architectural source review.
CORE-02 is authorized as the only next increment; CORE-03..05 remain pending
separate approval. No merge into `world` or geographic layer development is authorized.

## CORE-01 delivery record

The implemented C# signatures and project-boundary decisions are documented in
[core-contracts.md, section 10](core-contracts.md#10-core-01-implemented-c-contract).
The constructor validates a complete fixed registry; no layer is executed.
Minimal immutable request/settings types contain only the main seed, four world
types and three dimensionless map-size choices. Eight fake layer implementations
are confined to `tests/worldgen/`.

The CORE-01 design gate resolves stage identity/order, request representation,
registration validation exceptions and the checks → engine → definitions
dependency direction (engine references definitions; checks reference engine).
Execution parameters, read-only outputs, atomic publication, operation error
results, context lifecycle, PRNG, concurrency, rebuild and finalization remain
deferred to their separately authorized increments. No placeholder executable
contract is introduced to fill those gaps.

Development evidence: 15/15 CORE-01 checks passed, the existing 16/16 menu checks
passed, engine and game Release builds reported zero warnings/errors, and
whitespace verification passed. The compiled display version is `v0.0.15`.
The commands and coverage are recorded in `core-contracts.md`. This evidence supported the owner's subsequent acceptance; it is **development
validation and architectural source review**, not a new independent audit.
CORE-02 authorization does not authorize later increments.
