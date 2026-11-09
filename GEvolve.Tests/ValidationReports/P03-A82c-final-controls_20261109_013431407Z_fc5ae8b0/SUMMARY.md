# World generation validation report

- Run: `P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0`
- Started UTC: 2026-11-09T01:34:31.4077304Z
- Finished UTC: 2026-11-09T01:36:06.5729986Z
- Branch: `worldgeneration_pangea`
- Validated commit: `8d20b2522713b71dddaceede7376fdef02c9cb78` - fix(tests): validate completed comparator snapshot fields
- .NET SDK: `10.0.402`
- Validation set: `BaseComparatorControls`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: True
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0` | PASS | 0 | 8.985 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0 -- --pangaea-base-comparator` | PASS | 0 | 85.813 | [base-comparator.log](base-comparator.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in base-comparator.log.
```text
COMPARATOR_CASE_PASS Small/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Small/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Medium/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Large/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/-1387338765: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/-132790047: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/2026: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_CASE_PASS Enormous/42: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility
COMPARATOR_PASS: 64 exact historical fields; 16 worlds; four sizes; detached asynchronous cache; official DAG/BASE/FINAL/ecology/persistence intact; labels/inspector/other views; reset and stale-result protection. NO window/GL/visual approval.
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
.\GEvolve.Tests\ValidationReports\P03-A82c-final-controls_20261109_013431407Z_fc5ae8b0\commands.ps1 -ValidationSet BaseComparatorControls -Label P03-A82c-final-controls -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.