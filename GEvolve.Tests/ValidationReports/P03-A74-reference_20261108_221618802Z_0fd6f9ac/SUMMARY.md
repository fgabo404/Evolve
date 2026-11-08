# World generation validation report

- Run: `P03-A74-reference_20261108_221618802Z_0fd6f9ac`
- Started UTC: 2026-11-08T22:16:18.8020696Z
- Finished UTC: 2026-11-08T22:16:50.7838634Z
- Branch: `worldgeneration_pangea`
- Validated commit: `84932b9396001f0c883ce2d4c6a4d4d3dcd29f45` - docs(game): publish text input validation report
- .NET SDK: `10.0.402`
- Validation set: `BaseAuditReference`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A74-reference_20261108_221618802Z_0fd6f9ac` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: True
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A74-reference_20261108_221618802Z_0fd6f9ac` | PASS | 0 | 12.636 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A74-reference_20261108_221618802Z_0fd6f9ac -- --pangaea-base-reference` | PASS | 0 | 18.972 | [base-reference.log](base-reference.log) |

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
- Sensitive-substring replacements across captured logs: 10.
- Review all files for additional sensitive data before publishing; this script does not commit or push.
- Keep milestone/failure reports and one canonical report per commit/SDK. Review routine successes manually after five; remove only redundant successes after review, never automatically. Git retains published history. See [retention policy](../README.md).

## Manual game validation and repeat command
The Game entry point creates EvolveWindow and calls window.Run(); it is interactive and was not launched. After a successful build, run `dotnet run --project GEvolve.Game`, inspect the required worlds and BASE/APORTE/FINAL/RELIEVE/PLACAS views, then close the window normally. Visual approval is not implied by these reports.

From the repository root, repeat this exact script snapshot:
```powershell
.\GEvolve.Tests\ValidationReports\P03-A74-reference_20261108_221618802Z_0fd6f9ac\commands.ps1 -ValidationSet BaseAuditReference -Label P03-A74-reference -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.