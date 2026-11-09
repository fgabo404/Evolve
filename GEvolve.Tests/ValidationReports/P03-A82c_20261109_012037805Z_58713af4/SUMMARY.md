# World generation validation report

- Run: `P03-A82c_20261109_012037805Z_58713af4`
- Started UTC: 2026-11-09T01:20:37.8058719Z
- Finished UTC: 2026-11-09T01:33:19.4358298Z
- Branch: `worldgeneration_pangea`
- Validated commit: `18856b46ed5a9c14bceca750d9b40bd0b2d8a848` - feat(game): add experimental Pangaea base variant comparison
- .NET SDK: `10.0.402`
- Validation set: `BaseVisualComparator`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4` | PASS | 0 | 3.642 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4 -- --pangaea-base-comparator` | PASS | 0 | 77.352 | [base-comparator.log](base-comparator.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4 -- --pangaea-staged` | PASS | 0 | 90.395 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4 -- --pangaea-laboratory` | PASS | 0 | 15.410 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c_20261109_012037805Z_58713af4 -- --all-suites` | FAIL | 1 | 573.843 | [all-suites.log](all-suites.log) |

## Reported suite totals
- all-suites: 17 suites passed; 2 suites failed.
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
```text
SUITE FAIL: TectonicMotion: System.InvalidOperationException: Pre-motion terrain baselines differ in 4 scenario(s): Small/Pangaea/2026, Medium/Pangaea/2026, Large/Pangaea/2026, Enormous/Pangaea/2026
SUITE FAIL: ContinentalGeology: System.InvalidOperationException: Approved non-continental world baselines differ in 12 scenario(s): Small/Pangaea/2026, Small/Pangaea/42, Small/Pangaea/-731, Medium/Pangaea/2026, Medium/Pangaea/42, Medium/Pangaea/-731, Large/Pangaea/2026, Large/Pangaea/42, Large/Pangaea/-731, Enormous/Pangaea/2026, Enormous/Pangaea/42, Enormous/Pangaea/-731
```

## Warnings emitted
```text
<REPO>\GEvolve.Tests\PangaeaBaseComparatorTests.cs(77,68): warning CS8602: Dereference of a possibly null reference. [<REPO>\GEvolve.Tests\GEvolve.Tests.csproj]
<REPO>\GEvolve.Tests\PangaeaBaseComparatorTests.cs(113,39): warning CS8602: Dereference of a possibly null reference. [<REPO>\GEvolve.Tests\GEvolve.Tests.csproj]
    2 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in base-comparator.log.
```text
COMPARATOR_CASE_PASS Small/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_PASS: 64 exact historical fields; 16 worlds; four sizes; detached asynchronous cache; official DAG/BASE/FINAL/ecology/persistence intact; labels/inspector/other views; reset and stale-result protection. NO window/GL/visual approval.
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
SUITE PASS: PangaeaStaged
SUITE PASS: PangaeaLaboratory
SUITE PASS: PangaeaEcology
```

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 26.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A82c_20261109_012037805Z_58713af4\commands.ps1 -ValidationSet BaseVisualComparator -Label P03-A82c -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.