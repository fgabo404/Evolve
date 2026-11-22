# WG-01-D-FIX01 — Audit findings follow-up

**Branch:** world-tectonics. **Version:** v0.0.42. **Date:** 2026-11-22.
**Verified clean baseline:** v0.0.41,
04aea2ac457868a6ba01e2a364c404026df43733.

## 1. Audit reference, authority and disposition

The owner supplied findings **GATE-01, MINOR-01 and MINOR-02** in the request
“WG-01-D-FIX01: Resolución integral de hallazgos del Auditor”. That request
reports a favorable independent D audit with observations and explicitly
authorizes documentation, the bounded statistical correction and validation.
The complete original Auditor report was not separately attached here; no
additional verdict, signature, report identifier or native approval is invented.
The original [D verification](verification-d.md) remains the developer record.

| Finding | Disposition | Evidence / remaining work |
| --- | --- | --- |
| MINOR-01 | RESOLVED | Initial README and related current references correctly distinguish accepted A/B/C+FIX01, favorable D audit, and pending D acceptance/Layer 01 closure |
| MINOR-02 | RESOLVED | Actual capture template uses the shared tested conventional median; original measurements unchanged and correctly labeled upper-middle; new summaries match retained individual samples |
| GATE-01 | PARTIALLY RESOLVED | N01 PASS by owner declaration; complementary automation extended; N02–N17 remain PENDING for equivalent native evidence/express owner decisions |

No automatic Layer 01 closure, final D acceptance, algorithm/profile change,
Influence, Layer 02 implementation or branch merge follows from this delivery.

## 2. MINOR-01 — Documentary status

The tectonic README now states A accepted, B accepted, C + FIX01 formally
accepted at **v0.0.40**, D delivered at **v0.0.41** and audited favorably with
observations. Formal D acceptance and definitive Layer 01 closure remain
pending. Roadmap/current test references and later-status notes in the C/D
reports agree. Earlier reports retain their dated historical content; raw
historical results are not rewritten as new evidence.

## 3. MINOR-02 — Actual statistical implementation

The real source is
tests/worldgen/tectonics/validation/wg01d/**graphics**/FinalGraphicsEntryPoint.cs.txt;
the request's path omits the graphics directory. No duplicate source path was
created. MeasureFrames previously used gpu[15]/cpu[15] after sorting 30 samples:
the **upper central observation**, not the conventional even-sample median.

The template now calls GraphicsTimingStatistics.MedianOfSortedSamples for both
GPU and CPU. That exact source is compiled into the temporary capture executable
and the existing headless test project; tests do not duplicate its implementation.
Odd counts select their central value; even counts average the two centers
(indices **14 and 15** for 30). Finite-overflow-safe averaging preserves finite
extremes; the normal sum path preserves subnormal rounding. Empty, unsorted or
nonfinite inputs are rejected. No production renderer or tectonic calculation
uses this diagnostic helper.

Executed cases cover odd/even/30 counts, repeated values, opposite signs,
positive/negative double.MaxValue, subnormals and invalid inputs. The default
tectonic suite now passes **17/17** groups, including those checks.

Historical frames.csv and its numbers are unchanged. Documentation explicitly
identifies its Median-named GPU/CPU fields as upper-middle statistics, including
the original per-size summary. Core's 15-batch and geometry's 9-batch medians
were odd-count calculations and are not changed. Old per-frame samples were
not retained, so the missing lower-central observations cannot be reconstructed
from published min/upper-middle/max summaries. No corrected historical values
are fabricated. [historical-hashes.csv](../../../../tests/worldgen/tectonics/validation/wg01d-fix01/historical-hashes.csv)
verifies **35 unchanged original D data/log/image artifacts**.

## 4. N01 — Owner-provided evidence

**N01 PASS.** The owner personally created a **Pangea / Small / seed 0** session
without automatically executing Grid or Tectonics. Observations: **none**.
Provenance: the owner's declaration communicated to the Architect and supplied
in the D-FIX01 authorization. This is not independent Codex observation. No
owner-provided capture is asserted; exact personal test date/build/monitor
metadata was not supplied. Separate Codex screenshots are not substituted for
that declaration. The existing native matrix records this provenance explicitly.

## 5. Individual native states N02–N17

All statuses here refer to the **native/owner gate**, not the separate API or
offscreen checks. Prior formal C + FIX01 acceptance does not automatically fill
these rows.

| ID | Status | Complementary evidence and remaining native requirement |
| --- | --- | --- |
| N02 | PENDING | Real session staging runs Grid then Tectonics separately; native clicks/confirmation still needed |
| N03 | PENDING | No actual native N/G/held-key delivery executed; owner keyboard sequence required |
| N04 | PENDING | Production fills/Grid/colors captured for three sizes/seeds; owner visible-window inspection needed |
| N05 | PENDING | Independent boundary/motion flags and frames checked; real B/V key interaction required |
| N06 | PENDING | Headless picking/metadata and real selection frames; native seam/polar clicks and sidebar inspection required |
| N07 | PENDING | Actual camera APIs and headless transforms pass; native wheel/drag/arrows/F still required |
| N08 | PENDING | Actual reopen/model/window methods retain context; native navigation confirmation required |
| N09 | PENDING | Real unavailable INICIAR path and frame checked; owner native activation still required |
| N10 | PENDING | Actual GL captures at 800x600, 1280x720 and 1920x1061; exact requested 1920x1080 and native hit alignment not validated |
| N11 | PENDING | Cross-monitor DPI setup and actual transitions not exercised; owner hardware/scaling evidence needed |
| N12 | PENDING | Programmatic client/framebuffer resize checked; no manual maximize/continuous drag with pending input |
| N13 | PENDING | No equivalent native minimize/restore run supplied |
| N14 | PENDING | No real focus/Alt-Tab sequence with held input supplied |
| N15 | PENDING | Production GLSL compiles/renders on recorded NVIDIA context; responsible person's normal window/driver confirmation still required |
| N16 | PENDING | Logical retention/replacement transitions verified; native/physical driver accounting remains unproven |
| N17 | PENDING | Logical GL names deleted after explicit OnUnload and diagnostic unbinding; SALIR/native window-close dispatch and effective physical release still require evidence |

The [updated N01–N17 matrix](../../../../tests/worldgen/tectonics/validation/wg01d/native-validation.md)
contains the detailed actions. The opt-in [PowerShell launcher](../../../../tests/worldgen/tectonics/validation/wg01d-fix01/StartNativeValidation.ps1)
records branch/SHA/version/runtime/build/exit output and copies the checklist
under ignored output/. It never marks rows PASS. Its syntax was checked; it
was **not** run interactively by Codex. If script policy prevents execution,
use the explicit build/run commands rather than weakening machine policy.

## 6. Complementary execution and environment limitations

New results are stored only in
[wg01d-fix01/](../../../../tests/worldgen/tectonics/validation/wg01d-fix01/),
separate from original D artifacts. The approved temporary project/rendering
approach is reused: one real hidden EvolveWindow, production shaders/renderers,
real generation APIs and an RGBA8 offscreen framebuffer. No alternative UI,
new production hook, renderer or geographic generator is introduced.

Nine scenarios cover all sizes × seeds 0,1,-1. Each Grid/Tectonics view has ten
warmups and thirty timed frames: **540 new frame samples**. Raw GPU/CPU values
are retained in graphics/frame-samples.csv; all **18** new summary rows were
independently recomputed from indices 14/15 and match within six-decimal output
precision (verification tolerance .000001 ms). See median-verification.csv.
These are new conventional medians, never replacements for old D values.

The new timings are highly variable: per-scene Tectonics GPU medians range
**2.209792–342.456320 ms**. The interval wraps the whole actual frame, including
SwapBuffers and driver scheduling/synchronization effects; it is not isolated
shader cost. No causal performance regression/improvement, universal budget or
native responsiveness approval is inferred. The cause of this variability was
not established. All raw values are preserved, including outliers.

The capture run records .NET 10.0.13, Windows 10.0.26200, X64, 16 logical CPUs,
Intel64 Family 6 Model 154 Stepping 3, GenuineIntel; NVIDIA GeForce RTX 3050
Laptop GPU/PCIe/SSE2, OpenGL 3.3.0 NVIDIA 592.00. Release/default tiering,
VSync off; measured frames start at 1280x720. Benchmarks ran separately from
the regression/build jobs. Reflection/diagnostic overhead and synchronization
are not normal interactive FPS measurements.

Programmatic resizing rendered successfully at **800x600** and **1280x720**.
A **1920x1080** request produced actual client/framebuffer **1920x1061** in this
run. The exact requested resolution was **not validated**; its cause is not
diagnosed or fixed in production. The 800x600 and actual 1920x1061 captures
were visually inspected. This is neither DPI migration nor native resize input.
The final logical GL cleanup checks passed; physical memory reclamation remains
outside their proof.

Two diagnostic attempts are retained: a temporary namespace collision during
compilation was corrected with a GLFW alias; an exact-size assertion then
stopped at the unapplied 1920x1080 request. The final diagnostic explicitly
records requested/actual dimensions and their limitation instead of declaring
the unavailable target passed. Those attempts are not successful benchmark
results and do not establish an accepted production defect requiring a fix.

## 7. Regression and scope results

Executed exactly as requested, with existing package restore enabled:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Tectonics **17/17**, Laboratory **19/19**, menu **16/16**, complete Grid suite
(2,320,000 retained BFS comparisons; 1,015,000 triangles; 10 integration checks).
Engine/Game Release: zero warnings/errors; display metadata equals VERSION
v0.0.42. Both fifteen-buffer R/M sets and all 75 historical R/M cases remain
matched. No WG-CORE/Grid/R/M/schema-1/flags-7/production-LAB/menu/INICIAR/State/
Saves change; no Influence, Layer 02, roadmap deletion or merge.

The [file inventory](../../../../tests/worldgen/tectonics/validation/wg01d-fix01/files.csv)
identifies all additions/modifications. A preservation/scope review and exact
command outputs accompany the evidence. No new deterministic functional defect
requiring production changes was established by these checks.

## 8. Recommendation and owner procedure

Submit MINOR-01 and MINOR-02 as resolved for independent re-review. Keep
**GATE-01 PARTIALLY RESOLVED**, with N01 PASS and N02–N17 PENDING. GATE-01 and
Layer 01 cannot be closed merely because automated/offscreen checks pass.
Final D acceptance and Layer 01 closure require the responsible person's
remaining evidence/express decisions and independent finding follow-up.

```powershell
powershell -NoProfile -File .\tests\worldgen\tectonics\validation\wg01d-fix01\StartNativeValidation.ps1
```

Record real mouse/keyboard, DPI/monitor combinations, maximize/resize,
minimize/restore, focus, normal-driver shader behavior, GPU accounting and both
SALIR/window-close paths. Unsupported configurations stay PENDING with their
limitation and the evidence needed. No automated row approval is performed.

For diagnostic replay without overwriting evidence:

```powershell
$tool = '.\output\wg01d-capture'
$source = '.\tests\worldgen\tectonics\validation\wg01d\graphics'
New-Item -ItemType Directory -Path $tool -Force | Out-Null
Copy-Item -LiteralPath "$source\Capture.csproj.txt" -Destination "$tool\Capture.csproj"
Copy-Item -LiteralPath "$source\FinalGraphicsEntryPoint.cs.txt" -Destination "$tool\FinalGraphicsEntryPoint.cs"
dotnet run --project "$tool\Capture.csproj" --configuration Release -- .\output\wg01d-fix01-replay
```

The shared median source is included directly by that temporary project. No
separate production dependency is required. This delivery is pending independent
review and formal acceptance; it does not declare automatic Layer 01 closure.
