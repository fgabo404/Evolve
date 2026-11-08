# World generation validation report

- Run: `P03-A721-sidebar_20261108_013348836Z_784fadcd`
- Started UTC: 2026-11-08T01:33:48.8369088Z
- Finished UTC: 2026-11-08T01:41:47.6160705Z
- Branch: `worldgeneration_pangea`
- Validated commit: `51ca6458b7445334df4961ce7903640959ca3490` - fix(tests): isolate laboratory outputs and respect periodic camera anchors
- .NET SDK: `10.0.402`
- Validation set: `LaboratoryPangaea`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A721-sidebar_20261108_013348836Z_784fadcd` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A721-sidebar_20261108_013348836Z_784fadcd` | PASS | 0 | 8.051 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A721-sidebar_20261108_013348836Z_784fadcd -- --pangaea-laboratory` | PASS | 0 | 16.094 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A721-sidebar_20261108_013348836Z_784fadcd -- --pangaea-staged` | PASS | 0 | 69.085 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A721-sidebar_20261108_013348836Z_784fadcd -- --all-suites` | FAIL | 1 | 384.815 | [all-suites.log](all-suites.log) |

## Reported suite totals
- all-suites: 16 suites passed; 2 suites failed.
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
Selected actual records; complete emitted output is in pangaea-laboratory.log.
```text
LAB_CASE_PASS Small: sequential UI/unchanged engine DAG; integrated heights and >90 surface; sidebar/viewport/hit testing; cache; random/manual/size reset; exact automatic equivalence
LAB_CASE_PASS Medium: sequential UI/unchanged engine DAG; integrated heights and >90 surface; sidebar/viewport/hit testing; cache; random/manual/size reset; exact automatic equivalence
LAB_CASE_PASS Large: sequential UI/unchanged engine DAG; integrated heights and >90 surface; sidebar/viewport/hit testing; cache; random/manual/size reset; exact automatic equivalence
LAB_CASE_PASS Enormous: sequential UI/unchanged engine DAG; integrated heights and >90 surface; sidebar/viewport/hit testing; cache; random/manual/size reset; exact automatic equivalence
LAB_ASYNC_PASS: reset during group/size worker, discarded obsolete results, cancelled H04 chain blocked, error/recovery, immediate selection clear
LAB_SEEDS_PASS: signed random seeds, immediate complete reset/selection clear, initial grid only, boundary editing, no confirmation or full generation
LAB_PASS: four sizes; fixed sidebar/map viewport; sequential five actions over unchanged seven-stage engine; immediate seed/size/random reset; always-integrated numeric heights; surface >90; original automatic equivalence. NO window/GL/visual approval.
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
```

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 18.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A721-sidebar_20261108_013348836Z_784fadcd\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A721-sidebar -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.