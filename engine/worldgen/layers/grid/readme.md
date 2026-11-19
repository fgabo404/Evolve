# Layer 00 — Hex Grid

**Status:** WG-00-A implemented; independent audit APPROVED WITH OBSERVATIONS. FIX01 numeric corrections implemented, pending independent re-audit and formal owner acceptance.\
**Working branch:** `world-grid`\
**Prerequisite:** World settings and seed only (first layer)

## Responsibility

Define a continuous east/west hex map with polar north/south limits, three configurable sizes and a proposed 2:1 equirectangular geographic representation.

## Conceptual inputs

Map size, projection and topology settings.

## Conceptual outputs

Hex cells, cell IDs, geographic coordinates, six-neighbor topology and horizontal wrap.

## Boundary

The layer must operate on the shared `GenerationContext`, not through its own separate generator or OpenGL rendering pipeline. It cannot execute ahead of its prerequisite. Rebuilding this layer invalidates all dependent downstream layer results.

## Proposed validation focus

Unique cell identities; seam-aware neighbors and distances; no north/south wrap; correct size handling.

The conceptual layer description above remains the design for later increments.
WG-00-A implements only geometry/coordinates in `geometry/FlatHexGeometry.cs`
and `geometry/HexCoordinates.cs`. Compact grid data (WG-00-B) and WG-CORE
integration (WG-00-C) are not implemented or authorized by FIX01.

## WG-00-A numeric correction and verification

The audited baseline is `7db3c2c70fa11173702640a232f4e909423d07a1` at
`v0.0.28`. The owner reports approval with observations after 1,160,000
independent BFS distance comparisons. FIX01 addresses intermediate distance
overflow, non-finite period width and non-finite map height only.

`WrappedDistance` retains the same three periodic images and cube metric,
evaluating intermediate differences in long arithmetic. Only the final minimum
is checked for int representability; an unrepresentable minimum throws
OverflowException. `PeriodWidth` and `MapHeight` throw OverflowException when
finite valid inputs produce non-finite extents. Invalid input radii continue to
throw ArgumentOutOfRangeException. No geometry, topology or preset changes.

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
git diff --check
```

The existing suite checks all cells in the three approved sizes and compares
eight BFS sources against every target: 160,000 + 360,000 + 640,000 =
1,160,000 distance comparisons. Added regressions cover maximum legal dimensions
without grid allocation, seam/self/symmetric distances, the exact int boundary,
unrepresentable minima, overflowing extents and large finite extents.
Developer regression results do not replace independent re-audit. Formal
WG-00-A closure and owner authorization are required before WG-00-B.

FIX01 development validation at `v0.0.29`: the complete headless suite passed,
including all 1,160,000 existing BFS comparisons and both new regression groups.
The engine Release build reported zero warnings/errors; whitespace checking
passed. This evidence does not claim that independent re-audit has occurred.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
