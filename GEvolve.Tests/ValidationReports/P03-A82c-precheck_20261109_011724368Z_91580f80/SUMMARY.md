# World generation validation report

- Run: `P03-A82c-precheck_20261109_011724368Z_91580f80`
- Started UTC: 2026-11-09T01:17:24.3688732Z
- Finished UTC: 2026-11-09T01:17:33.6112899Z
- Branch: `worldgeneration_pangea`
- Validated commit: `5fcd34307b1f35ab16820a6b9782ea38c92c5b63` - docs(worldgen): publish P03-A82b base calibration findings
- .NET SDK: `10.0.402`
- Validation set: `BaseComparatorControls`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-precheck_20261109_011724368Z_91580f80` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: True
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-precheck_20261109_011724368Z_91580f80` | FAIL | 1 | 8.978 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-precheck_20261109_011724368Z_91580f80 -- --pangaea-base-comparator` | SKIPPED | N/A | 0.000 | [base-comparator.log](base-comparator.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
```text
<REPO>\GEvolve.Tests\PangaeaBaseComparatorTests.cs(151,33): error CS0246: The type or namespace name 'GeneratedWorld' could not be found (are you missing a using directive or an assembly reference?) [<REPO>\GEvolve.Tests\GEvolve.Tests.csproj]
```

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
No diagnostic records were reached/emitted. No metrics are invented.

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
.\GEvolve.Tests\ValidationReports\P03-A82c-precheck_20261109_011724368Z_91580f80\commands.ps1 -ValidationSet BaseComparatorControls -Label P03-A82c-precheck -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.