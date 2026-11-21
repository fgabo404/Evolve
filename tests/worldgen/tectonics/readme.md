# WG-01-B — Frozen R headless verification

The .NET 10 executable references actual production Engine code; no OpenTK,
native window or duplicate generator. Use dotnet run, not dotnet test.
Nonzero exit means a failed assertion.

## Independent audit commands

From the repository root in PowerShell with .NET 10:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Measure R separately after tests/builds finish:

```powershell
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --calibration
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

That historical CLI name now measures only frozen production R. Recorded final
stdout is [r-performance.txt](r-performance.txt), with phase/memory summary
[r-performance-summary.csv](r-performance-summary.csv). Original P0
[candidate-calibration.txt](candidate-calibration.txt) is preserved unchanged.

## Executed coverage

10/10 named groups passed. T01–T03 cover all 145,000 cells, 14 owners,
independent Offset BFS and seam/pole adjacency. T04 compares complete words for
five signed/extreme seeds, three sizes and all four WorldTypes (60 fresh
comparisons). T05 pins the CORE/noise vectors, warp periodicity, seed fallback,
three stage draws, and definitive R buffers.

[TectonicFrozenVectors.cs](TectonicFrozenVectors.cs) checks all words of 15
production-generated [golden buffers](vectors/manifest.json), not only hashes.
It also verifies exact cost bits/constants against archived CAL01 R and checks
all 75 R sample cases: metadata plus the archived full-payload fingerprint after
normalizing only the revision word. Each of those 75 cases has another complete
mode/type replay and independent connectivity. This is equivalence to R, not
P0 interpreted as R. Old revision 1 and CAL01's experiment tag are rejected.

T06 rejects 204 field corruptions plus malformed types/stages/lengths/Grid,
indices, nonfinite costs, disconnected owner swaps and mutable-source changes.
T10 exercises actual prerequisite order, atomic failures, retries, suffix and
Grid rebuilds, historical finalization, contention and reentrancy. T11/T12 B
scope covers dependencies/absence of C features and existing LAB/menu
regressions, not a new tectonic UI. T07–T09 and C/D full-layer/visual acceptance
remain deferred.

The specifically named friend assembly accesses production tectonic vectors/
metrics only. No public test hooks or CORE mutation are used. Test-only
downstream executors provide neutral buffers for lifecycle checks, not real
geography. No motion, dynamic boundary classification or influences are built.

## Frozen fixture policy and provenance

Profile: owner-selected R, tectonic-partition-r-v2, partition revision 2.
Schema stays 1. The 15 .words files are complete little-endian uint32 payloads
exported explicitly from real registered production layers through WG-CORE.
manifest.json records all origins, areas, cost bits and canonical-text digests.
They total 733,640 payload bytes; nothing is reconstructed from area summaries.

Normal checks never overwrite goldens. Updating them requires explicit
algorithm/revision authorization and review. The initial authorized export was:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release -- --export-vectors .\tests\worldgen\tectonics\vectors
```

It rejects an existing manifest. This is provenance, not a routine audit command.
CORE/noise vectors remain shared because their arithmetic did not change;
P0-specific partition fixtures are historical evidence only.

## Evidence and limitations

[validation/](validation/) retains test/build/version outputs.
The local .gitattributes preserves raw evidence/vector bytes across Git checkouts;
line-ending conversion must not invalidate historical file fingerprints.
The immutable historical CAL01 summary also retains its original empty final
line via one file-specific attribute; source whitespace checks are unchanged.
[calibration report](../../../engine/worldgen/layers/tectonics/calibration.md)
contains the exact constants, observations, areas, final timing methodology and
limits. [Historical experiments](experiments/readme.md) contain text snapshots,
raw/summarized CAL01 evidence and PNGs; no experiment executable is compiled or
selectable. Discarded .scene geometry is regenerable; the PNGs/data remain.

No numerical performance threshold, geological realism, cross-platform bit
identity, native input/DPI/GPU cleanup or formal B acceptance is inferred.
WG-01-B is submitted for independent audit only.
