# World generation validation report

- Run: `P03-A8_20261108_230933671Z_4194a94a`
- Started UTC: 2026-11-08T23:09:33.6716923Z
- Finished UTC: 2026-11-08T23:09:41.7978873Z
- Branch: `worldgeneration_pangea`
- Validated commit: `ef53b0b343e3ac158d5cc31569c8711b07db15af` - test(worldgen): compare regional Pangaea base relief variants
- .NET SDK: `10.0.402`
- Validation set: `BaseRegionalVariants`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a` | FAIL | 1 | 7.805 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a -- --pangaea-base-variants` | SKIPPED | N/A | 0.000 | [base-variants.log](base-variants.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a -- --pangaea-staged` | SKIPPED | N/A | 0.000 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a -- --pangaea-laboratory` | SKIPPED | N/A | 0.000 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A8_20261108_230933671Z_4194a94a -- --all-suites` | SKIPPED | N/A | 0.000 | [all-suites.log](all-suites.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
```text
<REPO>\GEvolve.Tests\PangaeaBaseVariantTests.cs(133,56): error CS0246: The type or namespace name 'GeneratedWorld' could not be found (are you missing a using directive or an assembly reference?) [<REPO>\GEvolve.Tests\GEvolve.Tests.csproj]
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
.\GEvolve.Tests\ValidationReports\P03-A8_20261108_230933671Z_4194a94a\commands.ps1 -ValidationSet BaseRegionalVariants -Label P03-A8 -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.