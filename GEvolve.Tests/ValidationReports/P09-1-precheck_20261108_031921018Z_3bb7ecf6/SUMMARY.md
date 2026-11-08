# World generation validation report

- Run: `P09-1-precheck_20261108_031921018Z_3bb7ecf6`
- Started UTC: 2026-11-08T03:19:21.0189486Z
- Finished UTC: 2026-11-08T03:20:38.3065749Z
- Branch: `worldgeneration_pangea`
- Validated commit: `a53a754a02363d0b26dc347f7e2c5f53db6475c2` - docs(game): publish readable elevation label validation
- .NET SDK: `10.0.402`
- Validation set: `EcologyControls`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P09-1-precheck_20261108_031921018Z_3bb7ecf6` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: True
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P09-1-precheck_20261108_031921018Z_3bb7ecf6` | PASS | 0 | 5.718 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P09-1-precheck_20261108_031921018Z_3bb7ecf6 -- --pangaea-ecology` | PASS | 0 | 58.069 | [pangaea-ecology.log](pangaea-ecology.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P09-1-precheck_20261108_031921018Z_3bb7ecf6 -- --pangaea-laboratory` | PASS | 0 | 13.049 | [pangaea-laboratory.log](pangaea-laboratory.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in pangaea-ecology.log.
```text
ECOLOGY_ENV {"CatalogHash":"0A78770E8281178EA848EE4838696FF4EC2DFF813BB63CF669E3F864B05E8254","Temperature":"temperature-2","Precipitation":"precipitation-2","Classifier":"biomes-4","CatalogRules":22,"Worlds":16,"MoistureUnit":"index 0..100, not relative atmospheric humidity"}
ECOLOGY_SESSION_PASS Small: stage dependencies, isolated snapshots, downstream invalidation, eight buttons, BIOMAS/inspector, exact automatic state, original readable labels
ECOLOGY_CASE_PASS Small/2026: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Small/-1387338765: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Small/-2024691550: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Small/-174327693: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_SESSION_PASS Medium: stage dependencies, isolated snapshots, downstream invalidation, eight buttons, BIOMAS/inspector, exact automatic state, original readable labels
ECOLOGY_CASE_PASS Medium/2026: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Medium/-1387338765: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Medium/-2024691550: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Medium/-174327693: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_SESSION_PASS Large: stage dependencies, isolated snapshots, downstream invalidation, eight buttons, BIOMAS/inspector, exact automatic state, original readable labels
ECOLOGY_CASE_PASS Large/2026: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Large/-1387338765: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Large/-2024691550: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Large/-174327693: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_SESSION_PASS Enormous: stage dependencies, isolated snapshots, downstream invalidation, eight buttons, BIOMAS/inspector, exact automatic state, original readable labels
ECOLOGY_CASE_PASS Enormous/2026: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Enormous/-1387338765: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Enormous/-2024691550: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_CASE_PASS Enormous/-174327693: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence
ECOLOGY_PASS: 16 worlds, four sizes, 16 FINAL/BASE climate-sensitive worlds; no quotas or calibration; geological equivalence and historical save contracts preserved
```

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 10.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P09-1-precheck_20261108_031921018Z_3bb7ecf6\commands.ps1 -ValidationSet EcologyControls -Label P09-1-precheck -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.