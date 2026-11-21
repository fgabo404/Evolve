# CAL01 local-only comparative experiment

No commit, push, VERSION increment, profile adoption or compatibility-vector
replacement is authorized. P0 remains the production Candidate and the original
candidate-calibration.txt is preserved byte-for-byte.

## Reproduce from the repository root

```powershell
dotnet build .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --compare-profiles .\tests\worldgen\tectonics\experiments\cal01
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --export-control-maps .\tests\worldgen\tectonics\experiments\cal01
.\tests\worldgen\tectonics\experiments\RenderComparisons.ps1
.\tests\worldgen\tectonics\experiments\SummarizeComparisons.ps1
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
git diff --check
```

Re-running the comparison replaces CAL01 outputs, not the original P0 archive.
Preserve reviewed result directories separately if collecting additional runs.
The measurements were collected before post-experiment regressions/rendering.

The local Windows PowerShell policy prevented invoking .ps1 files directly.
It was not changed. Rendering succeeded using the same inline commands:

```powershell
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path (Get-Location) 'tests/worldgen/tectonics/experiments/TectonicComparisonRasterizer.cs') -ReferencedAssemblies System.Drawing
$resultsPath = (Resolve-Path tests/worldgen/tectonics/experiments/cal01).Path
foreach ($scene in Get-ChildItem -LiteralPath (Join-Path $resultsPath 'scenes') -Filter '*.scene') {
    [TectonicComparisonRasterizer]::Render($scene.FullName,
        (Join-Path (Join-Path $resultsPath 'images') ($scene.BaseName + '.png')))
}
```

SummarizeComparisons.ps1 contains the exact ordinary PowerShell statements used
inline to generate summaries and the image index; no execution-policy setting
was changed. The renderer needs Windows GDI+/System.Drawing on the PowerShell
runtime, not System.Drawing.Common in Engine or the net10 test executable.
Its source is explicitly excluded from the .NET 10 test compilation.

## Files and trust boundaries

- TectonicProfileComparison.cs: actual production partition invocation through
  the existing orchestrator and a test-only adapter, independent BFS and full
  replay, diagnostics, raw data and offline scenes.
- TectonicComparisonRasterizer.cs: renders the supplied real centers/corners/
  owner bytes/boundary masks; performs no generation or topology.
- RenderComparisons.ps1 / SummarizeComparisons.ps1: local image and data tooling.
- cal01/environment.json, profiles.json, seeds.json: exact environment,
  experimental constants and predeclared samples.
- cal01/cases.json and cases.csv: all 375 world/profile statistics and fingerprints.
- cal01/areas.csv: all 5,250 plate-level areas, percentages, origins and costs.
- cal01/checks.txt: 375 passed cases and full-replay/coverage/connectivity totals.
- cal01/summary.csv / summary.md: original (5), additional (20) and all (25)
  sample summaries for every size/profile; exploratory thresholds only.
- cal01/scenes/*.scene: actual exported geometry and ownership for image replay.
- cal01/images/*.png / image-index.md: 23 inspected/decoded local comparisons.
- cal01/local-files.csv: complete local file inventory, distinguishing previous
  uncommitted B work, temporary edits and experiment-only additions.

P1/P2/R/W have no supported production revision: their explicit experiment tags
are rejected by the unchanged TectonicData reader. Only this local adapter packs
them as copied LayerData<uint> for experiments. Default P0 is still decoded by
the real production codec, with unchanged golden tests. No second generator,
accepted CORE/Grid modification, game UI or other geographic stage is included.

The two additional controls R and W isolate clamp and warp relative to P2.
They are not secretly adopted extra settings. The owner must select a profile
before final B delivery. [Report and recommendation](../../../../engine/worldgen/layers/tectonics/calibration.md).
