# Layer 00 — Hex Grid

**Status:** WG-00-A including FIX01 and WG-00-B formally accepted by the owner; WG-00-C implemented, pending independent audit and formal owner acceptance. WG-00-D not started.\
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
WG-00-A implements geometry/coordinates in `geometry/FlatHexGeometry.cs`
and `geometry/HexCoordinates.cs`. WG-00-B adds `HexGridData.cs` only.
WG-00-C adds `HexGridLayer.cs` and `HexGridDescriptor.cs` for integration with
the unchanged WG-CORE contracts. No later geographic executor is implemented.

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
The owner has now formally accepted WG-00-A including FIX01 at
`0631717ed123089da83e3b864f187ab0a2a60bbc` (`v0.0.29`) and authorized WG-00-B.
The Auditor's reported .NET 10 native-execution limitation remains recorded;
native developer checks here do not claim independent .NET 10 reproduction
or removal of that audit limitation.

FIX01 development validation at `v0.0.29`: the complete headless suite passed,
including all 1,160,000 existing BFS comparisons and both new regression groups.
The engine Release build reported zero warnings/errors; whitespace checking
passed. This evidence does not claim that independent re-audit has occurred.

## WG-00-B compact grid contract

`HexGridData` is sealed, in `Evolve.Engine.WorldGeneration.Layers.Grid`.
Its constructor `HexGridData(MapSize size)` uses the existing definitions enum:
Small = 200×100 (20,000 cells), Medium = 300×150 (45,000), Large = 400×200
(80,000). Only two readonly int dimensions are stored. No cell objects, arrays,
neighbor cache, seed, world type or geographic/gameplay attributes are retained.

| Public member | Contract |
| --- | --- |
| `Columns`, `Rows`, `CellCount` | Get-only dimensions and calculated cell count. |
| `GetCellId(HexOffset coordinates)` | Canonical row-major ID, row * Columns + column. Coordinates must be in bounds; this lookup does not wrap invalid input. |
| `GetOffset(int cellId)` | Exact inverse address; IDs range from zero through CellCount - 1. |
| `GetAxial(int cellId)` | Accepted derived axial representation. |
| `GetGeographicCenter(int cellId)` | Normalized cylindrical center using WG-00-A. |
| `TryGetNeighbor(int cellId, HexDirection direction, out int neighborId)` | Existing direction/seam rules; returns false and -1 at a missing polar neighbor. |
| `GetNeighbors(int cellId)` | Calculated valid IDs in accepted clockwise direction order; validates ID immediately, enumerates lazily, exposes no stored collection. |
| `GetDistance(int firstCellId, int secondCellId)` | Accepted minimum cylindrical distance. |

Invalid sizes, IDs, canonical coordinates and directions throw
ArgumentOutOfRangeException. All topology, axial, geographic and distance
mathematics delegate to unchanged WG-00-A methods. Other layers will associate
their own data by ID; HexGridData itself has no GenerationContext or LayerData dependency.
Query enumerators allocate transient objects; they are not persistent grid data.

## WG-00-B validation and measurements

The same headless project runs all retained WG-00-A checks and new
`HexGridDataChecks.cs` assertions. Developer Release execution passed:

- All 145,000 cells: exact dimensions, unique/consecutive IDs and round trips,
  equivalent-instance queries, normalized geographic coordinates, reciprocal
  edges and exact polar/interior neighbor counts.
- Undirected edges: Small 59,600; Medium 134,400; Large 239,200.
- All original 1,160,000 geometry-to-BFS comparisons retained, plus 1,160,000
  direct grid-API comparisons to the same BFS results.
- Invalid inputs, scalar immutable storage, caller-owned query-array mutation,
  seam/adjacent/self/symmetric distances and all FIX01 extreme-value regressions.
- Engine Release build: zero warnings/errors. Whitespace verification passed.

Measured environment: .NET 10.0.13, Windows 10.0.26200, X64, 16 logical CPUs.
Reported processor identifier: Intel64 Family 6 Model 154 Stepping 3, GenuineIntel.
After 10,000 warm-up constructions per size, a preallocated array retains
100,000 newly constructed grids per size. The array allocation is excluded.
Allocated bytes use GC.GetAllocatedBytesForCurrentThread; retained heap delta
uses GC.GetTotalMemory(true) before/after; Stopwatch timestamps time only the
construction loop. The retained batch is kept alive through measurement.

| Size | Allocated bytes / grid | Retained heap delta (batch) | Construction (100,000 grids) | Mean ns / grid |
| --- | ---: | ---: | ---: | ---: |
| Small | 24.00 | 2,392,968 B | 2.307 ms | 23.07 |
| Medium | 24.00 | 2,392,968 B | 2.223 ms | 22.23 |
| Large | 24.00 | 2,392,968 B | 2.132 ms | 21.32 |

Every batch allocated 2,400,000 B; observed retained delta was 23.93 B/grid.
Retained heap deltas include unrelated runtime/GC noise and are approximate,
not exact object-size measurements. Timings are single-run micro-measurements,
not controlled benchmarks or approved acceptance thresholds. Test-only exhaustive
arrays/BFS buffers are excluded from these construction measurements.
The owner has formally accepted WG-00-B at
`3115c6362d46d0340ca4ec72902ea0edd4d17a45` (`v0.0.30`) and authorized
WG-00-C. The measurements above preserve that B delivery's evidence.
The Auditor's .NET 10 native-execution limitation remains documented separately
from the developer's successful native .NET 10 execution.

## WG-00-C immutable publication contract

`HexGridLayer : IWorldGenerationLayer` reports `GenerationStage.Grid`.
`Execute(GenerationLayerInputs inputs)` reads
`inputs.Request.Settings.MapSize` and returns
`LayerExecutionResult.Success(new LayerData<HexGridDescriptor>(Grid, [descriptor]))`.
No random values are consumed. Null inputs throw ArgumentNullException;
inputs belonging to a different stage throw ArgumentException. All progress,
publication, operation exclusivity, failure and retry behavior stays in WG-CORE.

`public readonly record struct HexGridDescriptor` contains only the get-only
`MapSize Size` value. Its public `HexGridDescriptor(MapSize size)` constructor
builds an approved HexGridData to validate the selection, retaining only Size.
Unsupported values throw ArgumentOutOfRangeException. The default descriptor
represents Small, the existing MapSize zero value. Its sole conversion,
`HexGridData ToGridData()`, reconstructs a fresh immutable wrapper using the
same accepted preset mapping. No second dimensions table is introduced.
Dimensions are preserved by that mapping, rather than redundant fields.

The Grid result has exactly one descriptor, not one item per cell. It is
unmanaged and fits the unchanged LayerData constraint; no managed HexGridData
object is passed through LayerData. A consumer reads:

```csharp
var data = (LayerData<HexGridDescriptor>)context.GetLayerData(GenerationStage.Grid)!;
HexGridDescriptor descriptor = data[0];
HexGridData grid = descriptor.ToGridData();
```

Consumers first require available Completed Grid data and can verify Count == 1,
Stage == Grid and descriptor.Size == context.Request.Settings.MapSize.
WG-CORE retains its generic ownership validation; no grid-specific schema
validator or mutable-reference bypass is added. Descriptor values are immutable
copies, source buffers are defensively copied by LayerData, and reconstruction
creates no per-cell state or adjacency cache.

## WG-00-C developer validation and limitations

The complete headless grid project passed on .NET 10.0.13:

- All retained WG-00-A/FIX01 checks and exhaustive WG-00-B checks over 145,000
  cells, including 1,160,000 original geometry-to-BFS comparisons and 1,160,000
  direct grid-API comparisons to the same BFS oracle.
- Ten named integration checks against the actual production orchestrator:
  real Grid registration/publication for every size; unmanaged immutable
  descriptors; prerequisite order and no accidental reruns in all forward modes;
  full-suffix Grid rebuild/replay; seed/world-type independence; atomic faults
  and retry; stable finalized results; concurrency/reentrancy; argument/registry
  rejection; and actual graphics-free assembly dependencies.
- The independence check covers all three sizes, four world types and four
  signed seeds (48 combinations), comparing every cell's Offset and geographic
  coordinates. A test-only Grid probe confirms the real executor leaves its
  supplied PRNG stream unconsumed and returns no publicly visible partial data.
- Four fault kinds at each of eight stages (32 scenarios) reject explicit
  failure, exception, wrong-stage candidate or missing result. Earlier outputs
  remain unchanged and a later RunNext retries the failed eligible stage.
- The seven downstream stages are test-only executors producing neutral data.
  Grid probes delegate to the real Grid or inject test faults. There is no
  second runner, production placeholder geography or access to internal context
  mutation. Existing CORE test files retired before this baseline are not
  claimed present or executed; current integration checks exercise its public API.
- Engine Release build: zero warnings/errors. Whitespace verification passed.

The retained B construction check again observed 24 allocated B/grid; batch
times were 2.194/2.152/2.200 ms for Small/Medium/Large (100,000 instances each),
with 2,392,968 B retained heap delta per batch. The same single-run/GC-noise
limitations apply. Neighbor-query and distance-query throughput measurements
remain pending WG-00-D; no thresholds are invented or D work started here.
The Auditor's native .NET 10 limitation is not cleared by these developer
results. Independent WG-00-C audit and formal owner acceptance remain pending.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
