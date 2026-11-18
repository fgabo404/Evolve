# World Generation Tests

**Status:** CORE-01 through CORE-04 headless checks implemented.

## Run the current checks

```powershell
dotnet run --project .\tests\worldgen\Evolve.WorldGeneration.Checks.csproj --configuration Release
```

Run from the repository root with the .NET 10 SDK. This dependency-free console
suite returns `0` only when every named check passes, otherwise `1`. It references
the actual engine library, which references definitions, with no linked core
copy, OpenTK dependency or test framework. Use `dotnet run`, not `dotnet test`.

`WorldGenerationCheckEntryPoint.cs` runs all four suites regardless of earlier
failures and combines their process results:

| File | Responsibility | Named checks |
| --- | --- | --- |
| `CoreContractChecks.cs` | Canonical identities; complete, ordered, unique registry; invalid registrations; immutable membership; configuration and seed preservation; actual headless dependencies. | 15 |
| `GenerationContextChecks.cs` | Metadata, initial Pending states, exclusive ownership, unavailable data, alias protection, immutable progress/failure observations, safe internal transitions and restricted test access. | 19 |
| `WorldGenerationOrchestratorChecks.cs` | Actual production execution, prerequisites, targets, mode equivalence, failures, retry, read-only inputs, Running rejection and deferred scope. | 17 |
| `DeterministicGenerationChecks.cs` | Versioned vectors, stage streams, replay, suffix rebuild and cross-thread exclusivity. | 8 |

The CORE-01 layer-boundary assertion now checks the authorized CORE-03 extension.
The CORE-02 deferred-scope assertion permits orchestration outside the context
while still rejecting CORE-05 operations. Other prior checks remain in place.

## Test-only data and executors

`FakeWorldGenerationLayers.cs` contains exactly eight concrete implementations,
one per canonical stage. They return neutral immutable sample values or run
scenario callbacks to inject failures. They implement no geographic algorithm.
`FakeLayerValue.cs` supplies an unmanaged structured sample for storage checks.

The storage suite directly calls engine-internal preparation primitives through
the named `Evolve.WorldGeneration.Checks` friend grant. These calls deliberately
test isolated slots and can construct inconsistent drafts for rejection tests;
they are not a simulated or alternate orchestrator. All execution checks invoke
the actual `WorldGenerationOrchestrator`.

## CORE-03 coverage

- Creation runs no layer; Grid is first eligible. Every `RunNext` executes one
  stage and observes Running before candidate publication.
- `RunUntil` covers all eight targets and all their prerequisites. All forward
  modes derive equivalent sample values from actual preceding outputs.
- Completed targets/worlds do not rerun. Invalid prerequisite prefixes are
  rejected before invoking any layer.
- Explicit failures, exceptions after candidate allocation, wrong result
  ownership and null execution results are injected at each of eight stages in
  each of three modes (96 failure scenarios). No candidate or downstream result
  is published; previous buffer references remain unchanged.
- Every stage is failed and retried through each mode (24 retry scenarios),
  without repeating predecessors; failure diagnostics clear only after success,
  and old progress snapshots retain historical observations.
- Inputs expose only immutable metadata and prior buffers. Retained inputs
  cannot acquire own-stage/future-stage access. Mutating source arrays after
  candidate creation cannot change published values.
- Running states reject all forward operations, including reentrant calls from
  actual executors. These are synchronous guards, not a cross-thread stress test
  or proof of CORE-04 operation exclusivity.
- Invalid arguments, outcome construction, independent contexts and rejection of
  invalidated storage preserve existing data. No implicit rebuild is performed.
- `FinalizeWorld` remains absent; PRNG and rebuild coverage is documented below.
  Actual compiled dependencies remain headless.

## Regression and remaining coverage

```powershell
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

CORE-04 covers PRNG reproducibility, operation exclusivity and rebuild/invalidation
as detailed below. Future increments must verify complete-world finalization. Geographic
layers separately own topology, tectonics, landmass connectivity, elevation,
hydrology, climate, biome and performance checks. No real world, Laboratory
screen or GPU validation is performed here.

CORE-04 awaits review and owner acceptance; these checks do not constitute an
independent audit or authorize later increments.

**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)  
**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)

## CORE-04 coverage update

`DeterministicGenerationChecks.cs` adds eight named checks to the 51 earlier
checks. Fixed raw SplitMix64 and stage-derivation vectors protect compatibility.
Four signed seeds, four world types and three sizes exercise mode equivalence.
Every suffix is rebuilt and replayed, checking preserved prefix identities,
unavailable stale data and canonical order. Partial rebuilds clear failure
diagnostics and distinguish Invalidated from Pending. Retry restarts randomness.

Event barriers hold a real worker thread inside execution while another thread
attempts all forward modes and rebuilding through two orchestrator instances.
All competing calls reject; observations expose no partial buffer. A second
context completes independently before releasing the first. Timeouts bound
test failures rather than relying on sleeps. CORE-03's reentrant guards remain
covered. Finalization remains absent.

Earlier CORE-03 concurrency/rebuild deferral notes above are historical coverage
limits, superseded by this suite. Future coverage still includes CORE-05,
geographic layer correctness and performance; no GPU or geographic data is used.
