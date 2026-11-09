# Validación oficial R02

Ejecutar localmente desde la raíz, en worldgeneration:

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .GEvolve.TestsValidationReportscommands.ps1 -ValidationSet GridLaboratory -Label R02 -IsolatedArtifacts
~~~

El script activo captura build y las cuatro suites nuevas de GEvolve.Tests/Active, con streams completos, códigos reales, privacidad, directorio UTC y artefactos aislados. Codex no lo ejecuta. No inicia Game ni realiza validación visual. [Aislamiento y pendientes R02](R02_LEGACY_ISOLATION.md).

Las pruebas geográficas anteriores permanecen intactas pero no se compilan ni ejecutan. Sus baselines e informes tampoco se alteran. Los resultados 17 PASS/2 FAIL del legado no se convierten en PASS ni se declaran corregidos al ejecutar la suite nueva. El runner rechaza expresamente los flags de suites históricas.

La copia pre-R02 del script se conserva en ../Legacy/commands.ps1 como referencia, fuera del flujo activo; los snapshots de informes son inmutables. Usar únicamente el script actual para validar R02. La política de conservación y privacidad que sigue continúa vigente.

## Referencia histórica de validaciones anteriores

Los sets y comandos descritos a continuación pertenecen al motor archivado; no están disponibles en el script activo R02.

# Validation reports

Run from the repository root on `worldgeneration`:

The [single active roadmap](../../GEvolve.Engine/World/WorldGeneration/ROADMAP.md) governs R00–R01. See the [Git/static audit](R00-R01_GIT_AUDIT.md); no .NET commands were run for that audit. Historical report scripts retain their original branch guards; use the current top-level script on the active branch.

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

For P03-A.7.1 use `-ValidationSet StagedPangaea -Label P03-A71`. It adds `--pangaea-staged`: all seven acceptance seeds at all four sizes, 12 other-type controls, original/automatic/manual byte equivalence, dependency invalidation, asynchronous reset/cancellation, isolated snapshots and completed-world save/load. The immutable `PangaeaStagedReference.txt` was captured before refactoring at `9220a1da789936c63b6fb143743d57ed9dbb48b5`. Full per-stage hashes, input revisions, durations and actual attempt histories are in `pangaea-staged.log`. Historical baselines are unchanged. The reference capture command exists only in the pre-refactor commit/snapshot.

Summaries distinguish reported suite totals from individual assertions, whose count is not emitted by this console test harness. Missing diagnostic metrics stay absent. The validated SHA identifies the source before the operational report commit; tracked source changes outside this reports directory are flagged. Untracked files are not certified by that SHA. A report is execution evidence, not architectural or visual approval.

## Retention

P09.1 uses `-ValidationSet PangaeaEcology -Label P09-1 -IsolatedArtifacts`: build, `--pangaea-ecology`, `--pangaea-laboratory`, `--pangaea-staged`, then `--all-suites`. The ecology suite covers 16 worlds across four sizes with the original catalog, exact component composition, unchanged geology, determinism, FINAL-driven climate, current/historical save contracts and manual ecological invalidation. Each `ECOLOGY_WORLD` JSON record includes biome/subbiome percentages, latitude/elevation diagnostic groups, climate statistics, aridity and priority examples. These groups never influence classification. `EcologyControls` runs only the first three commands for focused work; it does not claim full regression coverage. Source SHA and dirty status in each summary distinguish exploratory prechecks from a canonical committed validation. Technical success does not approve biome distribution or visuals. No game window or GL context is created.

For P03-A.7.2 use `-ValidationSet LaboratoryPangaea -Label P03-A72`: build, `--pangaea-laboratory`, `--pangaea-staged` and `--all-suites`. Laboratory tests exercise the Game controller, partial data, dependency controls, revision caching, configuration/reset/cancellation and numeric layout bounds without creating a window or GL context. They do not certify appearance or geography. Codex must not launch Game, run graphics smoke, inspect screenshots or use visual automation for this milestone. Visual execution and approval belong exclusively to the user.

- Keep every milestone/reference report and every report with a failure or new regression.
- Keep one canonical routine success per validated commit and SDK. Repeat only when code/environment changes or a failure investigation needs another execution.
- After five routine successful reports, review for redundancy before publishing more. Older redundant routine successes may be removed in an explicit maintenance commit after reviewing their summaries; they remain in Git history. Preserve important reference/failure reports regardless of age.
- No automatic pruning or history rewriting. No scheduled unlimited report generation.

Only report text, Markdown and PowerShell belong here. Stage explicit paths; never stage `bin`, `obj`, executables, unrelated swap files or private raw logs. Existing repository ignore rules exclude build output.

## Interactive game

P03-A.7.2.3 uses `-ValidationSet LaboratoryPangaea -Label P03-A723 -IsolatedArtifacts`. The existing laboratory suite also runs pure bitmap/convex-geometry checks: measured glyph ink, per-cell fit at a readable font size, both-axis centering, no distant sampling, no label overlap/off-viewport text, contrast, exact fields, pan/zoom/seams and inspector independence. It never constructs UiRenderer or a GL context; the new static measurement reads the existing glyph table only. Detailed `LAB_LABELS_PASS` records are retained in the full laboratory log.

For the sidebar/sequential-flow redesign use `-ValidationSet LaboratoryPangaea -Label P03-A721-sidebar -IsolatedArtifacts`. The optional flag adds `--artifacts-path` consistently to build and run, using a unique directory under the already ignored `GEvolve.Tests/bin/ValidationArtifacts`. It avoids replacing DLLs held by a user's open game, without starting, inspecting or closing that process. The summary and repeat command record the option; no compiled artifact is published. Preserve any earlier file-lock failure report rather than hiding it. Tests cover sidebar/viewport geometry, sequential Game gates over the unchanged Engine DAG, immediate resets and integrated numeric annotations; visual approval remains user-only.

P03-A.7.2.1 reuses `-ValidationSet LaboratoryPangaea -Label P03-A721`. The laboratory suite additionally checks automatic grid, five visible actions, sequential plates/H04, signed random seeds, numeric annotation fields/zoom limits, strict laboratory-only surface white above 90 and immediate FINAL-to-BASE fallback. Numeric palette assertions exercise pure code and never open a window or inspect pixels; clipboard/graphics appearance remain manual-only.

`-ValidationSet LaboratoryControls` runs build and the laboratory controller/representation tests only. Use this focused set after a change confined to those contracts; it does not claim to run staged equivalence or all suites. Every report lists the actual commands and source SHA. Preserve broader reports separately when their historical failures/equivalence evidence remain relevant.

`GEvolve.Game/Program.cs` creates an OpenTK window and enters `window.Run()`. Run `dotnet run --project GEvolve.Game` manually after a successful build, inspect the reference worlds and their existing views, then close the window normally. Unattended validation neither launches nor forcibly closes it.

## Text input editing

Use `-ValidationSet TextInputs -Label TextInputs -IsolatedArtifacts` for input-only changes. It runs build, `dotnet run --project GEvolve.Tests -- --all-suites --suite=Game` (only the existing Game suite, including TextFieldEditingTests), and `--pangaea-laboratory`. This does not run the full general suite. Tests use OpenTK key identifiers and the production editor bound to real GameSession/PangaeaLaboratory fields, with a memory clipboard callback; they never create a window/GL context or touch the OS clipboard. Native clipboard delivery, focus and appearance remain pending interactive user verification. All native codes, complete streams and the source SHA are retained in each report. Existing report retention rules apply.

## P03-A.7.4 — BASE source/morphology audit

`-ValidationSet BaseReliefAudit -Label P03-A74 -IsolatedArtifacts` runs build, `--pangaea-base-audit`, `--pangaea-staged` and all suites. Eight worlds: −1387338765 and −132790047 at all four sizes. BASE_REFERENCE/BASE_WORLD/BASE_MORPH JSON records retain normal-path timing/allocation, actual source distributions, transformations, H04 relations, maxima, components and conservative annular candidates. Fields are opt-in through the existing official session; no generation/calibration rule or historical baseline is changed. The test reads the immutable pre-instrumentation reference log in P03-A74-reference_20261108_221618802Z_0fd6f9ac and must run from repository root. `BaseAuditControls` is build+audit only. `BaseAuditReference` captures normal current outputs/timing and does not reconstruct old source or overwrite the prior reference. These are numerical reports, not visual/geographical approval. Diagnostic graph/width/skeleton/ring metrics have explicit limitations in the milestone report. Performance samples are evidence, not a new acceptance threshold.

## P03-A.8 — Isolated regional BASE variants

`-ValidationSet BaseRegionalVariants -Label P03-A8 -IsolatedArtifacts` runs build, `--pangaea-base-variants`, staged/session equivalence, laboratory and all suites. Twelve configurations × five variants (V0, V1, V1T bridge, V2, V3), with complete numerical JSON in base-variants.log. It reuses the P03-A.7.4 historical log without replacing it, verifies exact V0 and immutable official outputs/persistence, and measures multiple fixed/relative thresholds, slopes, coastal jumps and kernel allocations. Experimental code lives only in Tests; production is unchanged. BASE_VARIANT_RESULT contains per-case metrics; BASE_VARIANT_WORLD contains protected official hashes and contextual normal-generation/capture timings. BaseRegionalComparison is build+comparison only; BaseVariantControls is build+focused controls. The earlier CS0246 failed build is retained separately. No variants are selected, persisted or rendered; no Game/GL/visual automation is run. See GEvolve.Tests/PANGAEA_BASE_VARIANTS.md and the milestone report for formulas, reflection coupling, profiling boundaries and limits.

## P03-A.8.2b — Regional lowlands and coastal controls

Use BaseLowlandControls for build+scalar controls, BaseLowlandComparison for build+176 comparisons, or BaseLowlandVariants for build+comparison+staged/session equivalence+laboratory+all suites. Example: commands.ps1 -ValidationSet BaseLowlandVariants -Label P03-A82b -IsolatedArtifacts. No Game/window/GL execution.

[Canonical findings and per-world/coastal/performance tables](P03-A82b_20261108_234756449Z_a6c3ecc0/REPORT.md), [summary](P03-A82b_20261108_234756449Z_a6c3ecc0/SUMMARY.md). Six new hypotheses plus five unchanged references; 16 worlds including independent seed 42; 60 historical result hashes and metrics retained exactly. Build/comparison/equivalence/laboratory PASS; all suites 17 PASS/2 historical FAIL, unchanged elevation mismatch records. Read GEvolve.Tests/PANGAEA_LOWLAND_VARIANTS.md for formulas and limits. V4O/V4U merit future visual comparison; no variant adopted, no visual approval or P03-A.8.3. Preliminary dirty-source runs remain separately labelled; existing retention policy applies.
## P03-A.8.2c — Laboratory BASE comparator

Use commands.ps1 -ValidationSet BaseVisualComparator -Label P03-A82c -IsolatedArtifacts from the repository root. Runs build, --pangaea-base-comparator, --pangaea-staged (28+12), --pangaea-laboratory and --all-suites. BaseComparatorControls runs build+comparator only. These are headless code/geometry checks; no Game/GL/visual automation.

[Canonical report, files and limits](P03-A82c_20261109_012037805Z_58713af4/REPORT.md). The comparison covers 64 historical fields in 16 worlds/four sizes, V0/V1/V4O/V4U equality against original experimental implementations and P03-A.8.2b hashes, determinism, visual-data/label/inspector contracts, official continuation and ecology, cache invalidation and save compatibility. It records kernel, cold dispatch/ready and warm-switch timings plus allocation/payload measurements, without claiming visual approval or GPU/peak memory measurements. The failed initial missing-import build and dirty-source controls remain separate. No historical logs/baselines are overwritten; existing retention policy applies.
