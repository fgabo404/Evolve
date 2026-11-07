# World generation validation report

- Run: `P03-A721_20261107_223547253Z_a9387211`
- Started UTC: 2026-11-07T22:35:47.2536337Z
- Finished UTC: 2026-11-07T22:45:37.1713914Z
- Branch: `worldgeneration_pangea`
- Validated commit: `f862a0c265cd9c341a761c5c0606c6f107dc1f2b` - feat(game): simplify Pangaea laboratory controls and elevation display
- .NET SDK: `10.0.402`
- Validation set: `LaboratoryPangaea`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 10.561 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-laboratory` | PASS | 0 | 18.716 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 67.738 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 491.763 | [all-suites.log](all-suites.log) |

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
<REPO>\GEvolve.Tests\PangaeaLaboratoryTests.cs(79,62): warning CS8602: Dereference of a possibly null reference. [<REPO>\GEvolve.Tests\GEvolve.Tests.csproj]
    1 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in pangaea-laboratory.log.
```text
LAB_CASE_PASS Small: automatic grid; five visible actions; grouped plates/H04; seven engine stages; numeric heights; white threshold >90; FINAL/BASE fallback; cache/invalidation; original automatic equivalence; reset confirmation
LAB_CASE_PASS Medium: automatic grid; five visible actions; grouped plates/H04; seven engine stages; numeric heights; white threshold >90; FINAL/BASE fallback; cache/invalidation; original automatic equivalence; reset confirmation
LAB_CASE_PASS Large: automatic grid; five visible actions; grouped plates/H04; seven engine stages; numeric heights; white threshold >90; FINAL/BASE fallback; cache/invalidation; original automatic equivalence; reset confirmation
LAB_CASE_PASS Enormous: automatic grid; five visible actions; grouped plates/H04; seven engine stages; numeric heights; white threshold >90; FINAL/BASE fallback; cache/invalidation; original automatic equivalence; reset confirmation
LAB_ASYNC_PASS: invalid request/recovery, reset during worker, obsolete work detached, cooperative cancellation/retry; no UI thread blocking
LAB_SEEDS_PASS: 100 signed int32 samples, boundary editing, no implicit generation, confirmation/cancel and presentation-only toggle
LAB_PASS: four sizes; auto grid; five visible actions; grouped plates/H04; signed random seeds; height labels and strict >90 surface white; non-linear dependencies; original automatic equivalence; cache/reset/cancellation. NO window, GL context, screenshots or visual approval.
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
- Sensitive-substring replacements across captured logs: 17.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A721_20261107_223547253Z_a9387211\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A721
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.