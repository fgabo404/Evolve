# World generation validation report

- Run: `TextInputs_20261108_214309259Z_0a9b0a9c`
- Started UTC: 2026-11-08T21:43:09.2591891Z
- Finished UTC: 2026-11-08T21:43:48.7803411Z
- Branch: `worldgeneration_pangea`
- Validated commit: `49f24f350ce45498a0c076b1055cf7895b6036c2` - fix(game): support clipboard and text editing shortcuts in inputs
- .NET SDK: `10.0.402`
- Validation set: `TextInputs`
- Isolated build artifacts: True
- Ignored artifact directory: `GEvolve.Tests/bin/ValidationArtifacts/TextInputs_20261108_214309259Z_0a9b0a9c` (never published).
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/TextInputs_20261108_214309259Z_0a9b0a9c` | PASS | 0 | 12.417 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/TextInputs_20261108_214309259Z_0a9b0a9c -- --all-suites --suite=Game` | PASS | 0 | 12.224 | [game-inputs.log](game-inputs.log) |
| `dotnet run --project GEvolve.Tests --artifacts-path GEvolve.Tests/bin/ValidationArtifacts/TextInputs_20261108_214309259Z_0a9b0a9c -- --pangaea-laboratory` | PASS | 0 | 14.579 | [pangaea-laboratory.log](pangaea-laboratory.log) |

## Reported suite totals
- game-inputs: 1 suites passed; 0 suites failed.
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; complete emitted output is in game-inputs.log.
```text
INPUT_EDIT_PASS: positive/negative int32; selection replacement; clipboard copy/cut/paste; cursor/delete/home/end; invalid/overflow; focus isolation; no duplicate key/text insertion; full laboratory invalidation and restart. No window/GL/native clipboard.
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
.\GEvolve.Tests\ValidationReports\TextInputs_20261108_214309259Z_0a9b0a9c\commands.ps1 -ValidationSet TextInputs -Label TextInputs -IsolatedArtifacts
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.