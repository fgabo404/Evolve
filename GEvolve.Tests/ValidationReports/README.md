# Validation reports

Run from the repository root on `worldgeneration_pangea`:

```powershell
.\GEvolve.Tests\ValidationReports\commands.ps1
```

Windows PowerShell 5.1 or PowerShell 7 and the project's .NET SDK are sufficient. If the local script execution policy blocks a reviewed script, invoke `powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1`. No external modules are needed.

The script runs `dotnet build`, then the elevation, mountain and complete suites in that order. Build failure skips the three dependent commands. Suite failures do not prevent later suites from running. Each UTC execution directory contains four complete logs, `SUMMARY.md` and the exact `commands.ps1` snapshot. Each native exit code and elapsed duration is preserved. The script's aggregate exit code is 1 on any failure, 0 otherwise.

For the targeted P03-A.5 audit (only the five requested Large seeds), use:

```powershell
.\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet TectonicAudit -Label P03-A5
```

This set runs build and `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit`, saving `build.log`, `tectonic-audit.log`, `SUMMARY.md` and the script snapshot. Repeating a snapshot requires the validation-set/label arguments shown in its summary. The audit compares complete persisted geography with tracing enabled and disabled; historical reports and snapshots are never overwritten. Human findings are published separately as `REPORT.md` after reading the actual results.

Each stream is drained asynchronously and preserved in its own labeled section; this prevents deadlocks and makes no claim about ordering between stdout and stderr. Local repository/user/temp paths and recognizable credentials are replaced by explicit privacy markers. All remaining output, including failures, is retained. Review the resulting files for other sensitive information before any Git publication. The script never pushes, commits, deletes reports, changes generation code or runs the interactive game.

For P03-A.6 use `-ValidationSet TectonicCalibration -Label P03-A6`. This runs build, elevations, mountains, the five-seed tectonic audit, the twenty-case `--pangaea-uplift` comparison and all suites. `pangaea-uplift.log` contains per-system and regional before/after JSON metrics, checked against `PangaeaUpliftReference.txt`, captured before the vertical change at commit `3c95bc7ca7afee362ab732efceb8fe86536aabc5`. This reference adds diagnostic coverage; it does not replace or approve historical baselines. The historical capture command is available only in that commit and its report script snapshot.

Summaries distinguish reported suite totals from individual assertions, whose count is not emitted by this console test harness. Missing diagnostic metrics stay absent. The validated SHA identifies the source before the operational report commit; tracked source changes outside this reports directory are flagged. Untracked files are not certified by that SHA. A report is execution evidence, not architectural or visual approval.

## Retention

- Keep every milestone/reference report and every report with a failure or new regression.
- Keep one canonical routine success per validated commit and SDK. Repeat only when code/environment changes or a failure investigation needs another execution.
- After five routine successful reports, review for redundancy before publishing more. Older redundant routine successes may be removed in an explicit maintenance commit after reviewing their summaries; they remain in Git history. Preserve important reference/failure reports regardless of age.
- No automatic pruning or history rewriting. No scheduled unlimited report generation.

Only report text, Markdown and PowerShell belong here. Stage explicit paths; never stage `bin`, `obj`, executables, unrelated swap files or private raw logs. Existing repository ignore rules exclude build output.

## Interactive game

`GEvolve.Game/Program.cs` creates an OpenTK window and enters `window.Run()`. Run `dotnet run --project GEvolve.Game` manually after a successful build, inspect the reference worlds and their existing views, then close the window normally. Unattended validation neither launches nor forcibly closes it.
