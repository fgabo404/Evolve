# World Generation Tests

**Status:** CORE-01 and CORE-02 headless checks implemented.

## Run the current checks

```powershell
dotnet run --project .\tests\worldgen\Evolve.WorldGeneration.Checks.csproj --configuration Release
```

Run from the repository root with the .NET 10 SDK. This dependency-free console
suite returns `0` only when every named check passes, otherwise `1`. Its actual
production references are engine → definitions; it introduces no copied core,
OpenTK dependency or test framework. Use `dotnet run`, not `dotnet test`.

`WorldGenerationCheckEntryPoint.cs` runs the unchanged `CoreContractChecks.cs`
suite and `GenerationContextChecks.cs`; both must pass for exit code `0`.
`FakeWorldGenerationLayers.cs` contains eight test-only metadata implementations.
Current coverage includes stage identities, complete registry construction,
duplicate/missing/unknown/out-of-order stages, nulls, immutable membership,
minimal configuration, seed preservation and actual compiled dependencies.

The context checks use production storage methods directly through the
`Evolve.WorldGeneration.Checks` friend grant. `FakeLayerValue.cs` supplies only
neutral unmanaged sample values, not geographic data or an execution simulator.
Coverage includes eight initial Pending slots, immutable metadata, exclusive
stage ownership, unavailable data, single-slot discard, source and consumer
alias protection, immutable progress snapshots, failure diagnostics, rejection
safety, independent contexts and preservation of buffer identity.

The public result view exposes no mutable collection/backing array. Attempts to
modify original arrays/lists or retrieved value copies do not change published
data. Context and progress have no public mutators. The named friend access is
tested explicitly; no fake orchestrator or production dependency on tests exists.

No geographic stage, orchestrator or random generator is executed. Context
storage calls are preparatory operations only, not `RunNext` or layer execution.
The remaining coverage below belongs to later authorized increments and layers.

## Responsibility

Verify the generator and each layer **without requiring the graphical interface** where possible.

Planned coverage:

- Orchestrator state transitions, prerequisite enforcement, failure behavior and rebuild invalidation.
- Reproducibility from seed/settings and consistent `GenerationContext` data ownership.
- Hex-grid topology and seamless horizontal wrapping with polar boundaries.
- Plate assignment and boundary validity.
- World-type landmass connectivity, including seam-aware checks.
- Elevation and landform consistency; physically meaningful hydrology.
- Climate response and environmental biome constraints.
- Laboratory eligibility rules, without duplicating generator logic.

Exact tests and performance targets belong to each separately approved internal layer roadmap. Preserve and continue running existing `tests/menu/` checks when authorized changes may affect UI integration.

**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)  
**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)
