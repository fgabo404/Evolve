# World generation validation report

- Run: `P03-A6-reference_20261106_213235533Z_2bb49306`
- Started UTC: 2026-11-06T21:32:35.5332299Z
- Finished UTC: 2026-11-06T21:32:37.2722323Z
- Branch: `worldgeneration_pangea`
- Validated commit: `3c95bc7ca7afee362ab732efceb8fe86536aabc5` - test(worldgen): capture P03-A.4 mountain elevation reference
- .NET SDK: `10.0.402`
- Validation set: `UpliftReference`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | FAIL | 1 | 1.463 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-uplift-reference` | SKIPPED | N/A | 0.000 | [uplift-reference.log](uplift-reference.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
No diagnostic records were reached/emitted. No metrics are invented.

## Capture, privacy and retention
- Both streams are complete in separate labeled sections; cross-stream chronology is not asserted.
- Personal paths, URL credentials, recognizable secret assignments and emails are replaced by explicit markers; errors and numbers are retained.
- Sensitive-substring replacements across captured logs: 40.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A6-reference_20261106_213235533Z_2bb49306\commands.ps1 -ValidationSet UpliftReference -Label P03-A6-reference
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.