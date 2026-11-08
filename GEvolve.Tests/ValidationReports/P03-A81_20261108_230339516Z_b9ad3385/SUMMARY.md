# World generation validation report

- Run: `P03-A81_20261108_230339516Z_b9ad3385`
- Started UTC: 2026-11-08T23:03:39.5164247Z
- Finished UTC: 2026-11-08T23:04:10.1175461Z
- Branch: `worldgeneration_pangea`
- Validated commit: `393da16d604d7e8e12b4f3d1bb45d10c2eca4bfb` - test(worldgen): introduce isolated Pangaea base calibration variants
- .NET SDK: `10.0.402`
- Validation set: `BaseVariantControls`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A81_20261108_230339516Z_b9ad3385` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A81_20261108_230339516Z_b9ad3385` | PASS | 0 | 18.434 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A81_20261108_230339516Z_b9ad3385 -- --pangaea-base-variants-controls` | PASS | 0 | 11.860 | [base-variant-controls.log](base-variant-controls.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in base-variant-controls.log.
```text
BASE_VARIANT_CONTROLS_PASS: V0 byte-exact F/G/H; immutable shared inputs; one-factor parent chain; fixed amplitude; monotone C1 summit treatment; ocean/land validity; eight laboratory actions; no official mutation or adoption
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
.\GEvolve.Tests\ValidationReports\P03-A81_20261108_230339516Z_b9ad3385\commands.ps1 -ValidationSet BaseVariantControls -Label P03-A81 -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.