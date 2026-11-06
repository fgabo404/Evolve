# World generation validation report

- Run: `P03-A6_20261106_213733272Z_a40eedf0`
- Started UTC: 2026-11-06T21:37:33.2722895Z
- Finished UTC: 2026-11-06T21:43:36.3354562Z
- Branch: `worldgeneration_pangea`
- Validated commit: `ab7c78914e0153ba946f7d5be5b6c17dc6c66ae7` - feat(worldgen): calibrate tectonic mountain uplift in Pangaea
- .NET SDK: `10.0.402`
- Validation set: `TectonicCalibration`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 6.935 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-elevations` | PASS | 0 | 31.567 | [pangaea-elevations.log](pangaea-elevations.log) |
| `dotnet run --project GEvolve.Tests -- --mountain-ranges` | PASS | 0 | 26.971 | [mountain-ranges.log](mountain-ranges.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` | PASS | 0 | 8.451 | [tectonic-audit.log](tectonic-audit.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-uplift` | PASS | 0 | 17.275 | [pangaea-uplift.log](pangaea-uplift.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 270.867 | [all-suites.log](all-suites.log) |

## Reported suite totals
- all-suites: 14 suites passed; 2 suites failed.
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
```text
SUITE FAIL: TectonicMotion: System.InvalidOperationException: Pre-motion terrain baselines differ in 4 scenario(s): Small/Pangaea/2026, Medium/Pangaea/2026, Large/Pangaea/2026, Enormous/Pangaea/2026
SUITE FAIL: ContinentalGeology: System.InvalidOperationException: Approved non-continental world baselines differ in 12 scenario(s): Small/Pangaea/2026, Small/Pangaea/42, Small/Pangaea/-731, Medium/Pangaea/2026, Medium/Pangaea/42, Medium/Pangaea/-731, Large/Pangaea/2026, Large/Pangaea/42, Large/Pangaea/-731, Enormous/Pangaea/2026, Enormous/Pangaea/42, Enormous/Pangaea/-731
```

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; full decisions, distributions, prominence and H04 metrics are in pangaea-uplift.log.
```text
UPLIFT_COUNTS Small/2026: systems=13; axisMean=58.328->74.325; white=99->99; generationMs=294.565
UPLIFT_COUNTS Small/42: systems=7; axisMean=38.822->65.311; white=193->193; generationMs=240.148
UPLIFT_COUNTS Small/-731: systems=17; axisMean=49.495->67.430; white=170->170; generationMs=242.813
UPLIFT_COUNTS Medium/2026: systems=16; axisMean=67.677->74.415; white=343->343; generationMs=420.219
UPLIFT_COUNTS Medium/42: systems=3; axisMean=69.754->74.902; white=233->233; generationMs=378.683
UPLIFT_COUNTS Medium/-731: systems=6; axisMean=52.211->71.012; white=101->101; generationMs=313.940
UPLIFT_COUNTS Large/2026: systems=5; axisMean=60.488->68.726; white=183->183; generationMs=409.674
UPLIFT_COUNTS Large/42: systems=13; axisMean=66.986->76.465; white=475->475; generationMs=436.358
UPLIFT_COUNTS Large/-731: systems=14; axisMean=51.086->70.120; white=170->170; generationMs=417.151
UPLIFT_COUNTS Enormous/2026: systems=4; axisMean=86.268->88.426; white=635->635; generationMs=835.598
UPLIFT_COUNTS Enormous/42: systems=11; axisMean=78.358->81.817; white=584->584; generationMs=912.133
UPLIFT_COUNTS Enormous/-731: systems=6; axisMean=46.617->64.612; white=85->85; generationMs=858.001
UPLIFT_COUNTS Large/-714216669: systems=15; axisMean=60.593->70.563; white=135->135; generationMs=448.607
UPLIFT_COUNTS Large/-2024691550: systems=5; axisMean=83.291->85.727; white=629->645; generationMs=426.325
UPLIFT_COUNTS Large/-174327693: systems=7; axisMean=53.017->71.467; white=191->191; generationMs=437.896
UPLIFT_COUNTS Large/-1979952984: systems=7; axisMean=52.034->63.282; white=175->175; generationMs=415.952
UPLIFT_COUNTS Large/2076374193: systems=7; axisMean=54.534->75.943; white=526->526; generationMs=410.411
UPLIFT_COUNTS Large/-234020843: systems=12; axisMean=68.322->80.238; white=279->279; generationMs=409.944
UPLIFT_COUNTS Large/-690013300: systems=19; axisMean=69.000->78.903; white=602->602; generationMs=409.470
UPLIFT_COUNTS Enormous/1697481406: systems=18; axisMean=60.356->72.954; white=475->475; generationMs=791.055
UPLIFT_PASS: twenty cases; immutable P03-A.4 geography/BASE/geometry/influence and additive heights verified; deterministic composition and metadata round-trip
```
```text
P03-A Small/2026: 13 ranges, 268 axis cells, 2 ms P03-A, 302 ms total
P03-A Small/42: 7 ranges, 135 axis cells, 1 ms P03-A, 262 ms total
P03-A Small/-731: 17 ranges, 293 axis cells, 3 ms P03-A, 182 ms total
P03-A Medium/2026: 16 ranges, 347 axis cells, 2 ms P03-A, 338 ms total
P03-A Medium/42: 3 ranges, 61 axis cells, 1 ms P03-A, 461 ms total
P03-A Medium/-731: 6 ranges, 171 axis cells, 1 ms P03-A, 442 ms total
P03-A Large/2026: 5 ranges, 168 axis cells, 3 ms P03-A, 636 ms total
P03-A Large/42: 13 ranges, 346 axis cells, 4 ms P03-A, 821 ms total
P03-A Large/-731: 14 ranges, 383 axis cells, 6 ms P03-A, 651 ms total
P03-A Enormous/2026: 4 ranges, 209 axis cells, 4 ms P03-A, 1498 ms total
P03-A Enormous/42: 11 ranges, 344 axis cells, 4 ms P03-A, 962 ms total
P03-A Enormous/-731: 6 ranges, 214 axis cells, 3 ms P03-A, 898 ms total
```
```text
SUITE PASS: World
SUITE PASS: Grid
SUITE PASS: Landmass
SUITE PASS: Biomes
SUITE PASS: Slope
SUITE PASS: Temperature
SUITE PASS: Precipitation
SUITE PASS: PlateBoundary
SUITE PASS: TectonicPlate
SUITE PASS: StagedLandmass
SUITE PASS: Game
BASELINE MISMATCH Small/Pangaea/2026
BASELINE MISMATCH Medium/Pangaea/2026
BASELINE MISMATCH Large/Pangaea/2026
BASELINE MISMATCH Enormous/Pangaea/2026
SUITE FAIL: TectonicMotion: System.InvalidOperationException: Pre-motion terrain baselines differ in 4 scenario(s): Small/Pangaea/2026, Medium/Pangaea/2026, Large/Pangaea/2026, Enormous/Pangaea/2026
APPROVED BASELINE MISMATCH Small/Pangaea/2026
APPROVED BASELINE MISMATCH Small/Pangaea/42
APPROVED BASELINE MISMATCH Small/Pangaea/-731
APPROVED BASELINE MISMATCH Medium/Pangaea/2026
APPROVED BASELINE MISMATCH Medium/Pangaea/42
APPROVED BASELINE MISMATCH Medium/Pangaea/-731
APPROVED BASELINE MISMATCH Large/Pangaea/2026
APPROVED BASELINE MISMATCH Large/Pangaea/42
APPROVED BASELINE MISMATCH Large/Pangaea/-731
APPROVED BASELINE MISMATCH Enormous/Pangaea/2026
APPROVED BASELINE MISMATCH Enormous/Pangaea/42
APPROVED BASELINE MISMATCH Enormous/Pangaea/-731
SUITE FAIL: ContinentalGeology: System.InvalidOperationException: Approved non-continental world baselines differ in 12 scenario(s): Small/Pangaea/2026, Small/Pangaea/42, Small/Pangaea/-731, Medium/Pangaea/2026, Medium/Pangaea/42, Medium/Pangaea/-731, Large/Pangaea/2026, Large/Pangaea/42, Large/Pangaea/-731, Enormous/Pangaea/2026, Enormous/Pangaea/42, Enormous/Pangaea/-731
SUITE PASS: PangaeaMountainRanges
SUITE PASS: PangaeaElevations
SUITE PASS: PangaeaUplift
```

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 13.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A6_20261106_213733272Z_a40eedf0\commands.ps1 -ValidationSet TectonicCalibration -Label P03-A6
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.