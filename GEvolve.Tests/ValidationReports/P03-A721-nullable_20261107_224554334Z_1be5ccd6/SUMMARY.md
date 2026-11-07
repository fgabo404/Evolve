# World generation validation report

- Run: `P03-A721-nullable_20261107_224554334Z_1be5ccd6`
- Started UTC: 2026-11-07T22:45:54.3349551Z
- Finished UTC: 2026-11-07T22:46:44.0566075Z
- Branch: `worldgeneration_pangea`
- Validated commit: `5bfbfc96bcb896615679d78dabe091f60d308164` - fix(tests): guard retained laboratory base before inspection
- .NET SDK: `10.0.402`
- Validation set: `LaboratoryControls`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 20.093 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-laboratory` | PASS | 0 | 27.922 | [pangaea-laboratory.log](pangaea-laboratory.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
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

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 5.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A721-nullable_20261107_224554334Z_1be5ccd6\commands.ps1 -ValidationSet LaboratoryControls -Label P03-A721-nullable
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.