# World generation validation report

- Run: `P03-A5_20261106_143813456Z_ed0d8bb9`
- Started UTC: 2026-11-06T14:38:13.4560446Z
- Finished UTC: 2026-11-06T14:38:30.8959698Z
- Branch: `worldgeneration_pangea`
- Validated commit: `74db94f6a4bc4f848245bb0bcf0e882aa71d7b1d` - test(worldgen): quantify sub-summit mountain visibility
- .NET SDK: `10.0.402`
- Validation set: `TectonicAudit`
- PowerShell: 5.1.26100.9711; OS: Microsoft Windows NT 10.0.26200.0; 64-bit OS: True
- Tracked source differs from validated HEAD: False
- The report/script changes are operational and excluded from the tracked-source comparison.

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 5.543 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` | PASS | 0 | 11.464 | [tectonic-audit.log](tectonic-audit.log) |

## Reported suite totals
Individual assertion/test totals are not inferred from PASS messages; this console runner does not report a framework test count.

## Compilation errors, failed suites and exception reasons
No matching error/exception lines were emitted; see exit codes and full logs.

## Warnings emitted
```text
    0 Warning(s)
```

## Actual diagnostic and regression evidence
Selected actual records; full decisions, distributions, prominence and H04 metrics are in tectonic-audit.log.
```text
AUDIT_ENV {"Utc":"2026-11-06T14:38:23.5314702Z","Framework":".NET 10.0.13","OS":"Microsoft Windows 10.0.26200","Architecture":"X64","Grid":"Large 300x200 flat-top; E/W wrap; N/S open","Audit":"P03-A.5 observational only"}
AUDIT_CASE Large/-714216669 reference=Favorable
AUDIT_SURFACE seed=-714216669 biomesPresent=False nonWhiteLandColors=1 axisBelow85=218/268 axisInUniformLandColor=218
AUDIT_COUNTS seed=-714216669 reference=Favorable land=24018 convergentLand=417 eligible=318 candidates=50 accepted=15 axes=268 influence=3650 meanAxisFinal=60.593 meanLocalProminence=3.772
AUDIT_CASE Large/-2024691550 reference=Favorable
AUDIT_SURFACE seed=-2024691550 biomesPresent=False nonWhiteLandColors=1 axisBelow85=75/220 axisInUniformLandColor=75
AUDIT_COUNTS seed=-2024691550 reference=Favorable land=24000 convergentLand=349 eligible=326 candidates=22 accepted=5 axes=220 influence=3217 meanAxisFinal=83.291 meanLocalProminence=3.821
AUDIT_CASE Large/-174327693 reference=Unfavorable
AUDIT_SURFACE seed=-174327693 biomesPresent=False nonWhiteLandColors=1 axisBelow85=210/242 axisInUniformLandColor=210
AUDIT_COUNTS seed=-174327693 reference=Unfavorable land=24881 convergentLand=354 eligible=354 candidates=29 accepted=7 axes=242 influence=3069 meanAxisFinal=53.017 meanLocalProminence=4.089
AUDIT_CASE Large/-1979952984 reference=Unfavorable
AUDIT_SURFACE seed=-1979952984 biomesPresent=False nonWhiteLandColors=1 axisBelow85=92/117 axisInUniformLandColor=92
AUDIT_COUNTS seed=-1979952984 reference=Unfavorable land=24000 convergentLand=168 eligible=134 candidates=18 accepted=7 axes=117 influence=1743 meanAxisFinal=52.034 meanLocalProminence=2.120
AUDIT_CASE Large/2076374193 reference=Unfavorable
AUDIT_SURFACE seed=2076374193 biomesPresent=False nonWhiteLandColors=1 axisBelow85=218/264 axisInUniformLandColor=218
AUDIT_COUNTS seed=2076374193 reference=Unfavorable land=28127 convergentLand=379 eligible=379 candidates=24 accepted=7 axes=264 influence=3351 meanAxisFinal=54.534 meanLocalProminence=2.872
AUDIT_PASS: five required seeds; traced/untraced complete geography identical; trace and persistence consistency verified.
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
.\GEvolve.Tests\ValidationReports\P03-A5_20261106_143813456Z_ed0d8bb9\commands.ps1 -ValidationSet TectonicAudit -Label P03-A5
```
Each invocation creates a new UTC directory. Individual native exit codes are in each log and the table; this script exits 1 if any validation failed, otherwise 0.