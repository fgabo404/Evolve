# World generation validation report

- Run: `P03-A72_20261107_220535936Z_c4ebdf57`
- Started UTC: 2026-11-07T22:05:35.9363654Z
- Finished UTC: 2026-11-07T22:17:17.3118689Z
- Branch: `worldgeneration_pangea`
- Validated commit: `e231bbf26f9ba81865ea9bd2158838771c639780` - feat(game): add interactive Pangaea generation laboratory
- .NET SDK: `10.0.402`
- Validation set: `LaboratoryPangaea`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 14.174 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-laboratory` | PASS | 0 | 19.281 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 114.728 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 551.947 | [all-suites.log](all-suites.log) |

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
LAB_CASE_PASS Small: seven stages, cached partial data, dependency buttons, real zero/pending/invalidated, BASE repeat, original automatic equivalence, layout contracts, reset confirmation
LAB_CASE_PASS Medium: seven stages, cached partial data, dependency buttons, real zero/pending/invalidated, BASE repeat, original automatic equivalence, layout contracts, reset confirmation
LAB_CASE_PASS Large: seven stages, cached partial data, dependency buttons, real zero/pending/invalidated, BASE repeat, original automatic equivalence, layout contracts, reset confirmation
LAB_CASE_PASS Enormous: seven stages, cached partial data, dependency buttons, real zero/pending/invalidated, BASE repeat, original automatic equivalence, layout contracts, reset confirmation
LAB_ASYNC_PASS: invalid request/recovery, reset during worker, obsolete work detached, cooperative cancellation/retry; no UI thread blocking
LAB_PASS: four sizes; seven real stages; non-linear dependencies; partial input contracts; revision cache; regeneration; reset; obsolete completion; cancellation; errors/recovery. NO window, GL context, screenshots or visual approval.
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
- Sensitive-substring replacements across captured logs: 13.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A72_20261107_220535936Z_c4ebdf57\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A72
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.