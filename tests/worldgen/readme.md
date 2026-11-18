# World Generation Tests

**Status:** CORE-01 headless contract checks implemented.

## Run the current checks

```powershell
dotnet run --project .\tests\worldgen\Evolve.WorldGeneration.Checks.csproj --configuration Release
```

Run from the repository root with the .NET 10 SDK. This dependency-free console
suite returns `0` only when every named check passes, otherwise `1`. Its actual
production references are engine → definitions; it introduces no copied core,
OpenTK dependency or test framework. Use `dotnet run`, not `dotnet test`.

`WorldGenerationCheckEntryPoint.cs` starts `CoreContractChecks.cs`.
`FakeWorldGenerationLayers.cs` contains eight test-only metadata implementations.
Current coverage includes stage identities, complete registry construction,
duplicate/missing/unknown/out-of-order stages, nulls, immutable membership,
minimal configuration, seed preservation and actual compiled dependencies.

No geographic stage, context, orchestrator or random generator is executed.
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
