# WG-01-D — Layer 01 final developer verification

**Date:** 2026-11-22. **Branch:** world-tectonics. **Delivery version:** v0.0.41.
**Verified starting HEAD:** 08099b6cd21fa13dcc90f25c7e1d4f5d7cc2c446,
owner-accepted FIX01 / v0.0.40; initially clean working tree.
**Status:** developer verification complete for independent audit submission.
This is not the independent Auditor's verdict or formal owner acceptance.

**Post-audit note:** the owner's D-FIX01 request reports a favorable independent
audit with observations. See [audit-fix01.md](audit-fix01.md) for the current
status; the report below remains the historical developer D record. Its frame
columns labeled Median used the upper central sample of 30, not the conventional
even-sample median. Original numbers/artifacts are preserved, not recalculated.

## 1. Executive assessment and authority

The executed automated and offscreen graphics checks pass, with no known
blocking functional defect found in their scope. Recommend submitting Layer 01
to independent audit. **Do not close Layer 01 unconditionally:** owner/native
interaction evidence and the independent verdict remain required.

Only tests, diagnostic artifacts, this documentation/roadmap status and VERSION
change. Engine, Grid, CORE, profiles R/M, Game/Laboratory production, State,
Saves, other layers and original compatibility fixtures remain unchanged.
No functional correction, influence algorithm, Layer 02 work or merge occurred.
Earlier increments/reports remain historical records, not rewritten results.

## 2. Requirements, executable mapping and evidence ownership

All PASS entries below mean **Codex developer evidence**, not owner or Auditor
approval. Raw outputs are in [wg01d/](../../../../tests/worldgen/tectonics/validation/wg01d/).

| Requirement / matrix | Executed evidence | Developer result | Owner / independent gate |
| --- | --- | --- | --- |
| T01 full ownership, exactly 14 plates/seeds/counts | Coverage; frozen R 75-case comparisons; histogram checks | PASS | Independent review pending |
| T02 connected regions | Test-owned Offset adjacency and per-plate BFS, all 75 matched R cases plus signed/extreme fixtures | PASS | Independent review pending |
| T03 seam/poles and unchanged Grid | Independent neighbor oracle, boundary set equality; complete Grid regressions | PASS | Independent review pending |
| T04 deterministic full results | All world types, modes/retry/rebuild, signed/extreme seeds; original 15 B and 15 M complete buffers | PASS | Independent reproduction pending |
| T05 exact frozen R constants/noise/queue policy | Existing numeric vectors, CAL01 exact-profile assertions, complete B words | PASS | Independent review pending |
| T06 schema, immutability and malformed input | 204 original corruptions plus C corruptions/zeros/sections/identity; defensive copies and historical views | PASS | Independent review pending |
| T07 real complete sorted unique interfaces | Test-owned neighbor set and analytic geometry over all sizes/signed seeds | PASS | Independent review pending |
| T08 M projections and five categories | Hand fixtures, endpoint reversal, 75 exact M fingerprints; new independent predicates, 324 signed/extreme pairs and exact binary midpoint ties | PASS | Independent review pending |
| T09 optional influence | Flag/metadata absence and no influence builder; no propagation is implemented | N/A for computation; absence PASS | Future decision, not silently passed |
| T10 real CORE lifecycle | Grid prerequisite, one uint publication, failure/no partial result, deterministic retry, suffix invalidation, history, contention/reentrancy | PASS | Independent review pending |
| T11 performance/dependencies | Separated CPU phases/queries, memory, graphics construction/upload/frames and buffer transitions; Engine references Definitions only | PASS as measured evidence | Hardware-specific interpretation; no universal budget |
| T12 LAB/FIX01 | 19 headless production-model checks; actual graphics across 3 sizes × seeds 0,1,-1; real manual-stage methods and unchanged INICIAR | PASS within stated scope | Native interaction/visual acceptance PENDING |

The D numeric oracle uses mutually exclusive predicates and exact
4*minimum>=3*maximum arithmetic, rather than production's .75 multiplication.
It checks 42,991 published interfaces in 15 signed/extreme worlds. Actual Quiet
counts happen to be zero in that subset; synthetic cases cover Quiet and all
five classes, and the wider 75-case M sample remains exactly matched. There is
no requirement that every generated world contain every category.

## 3. Frozen inventory and deterministic results

- Partition: **tectonic-partition-r-v2**, revision **2**, exactly 14 plates.
  Exact IEEE cost bits, 160 candidates, six frequencies, warp/noise/resistance
  remain frozen as recorded in calibration.md and the production profile.
- Motion/classification: **tectonic-motion-m-v1**, revision **1**; speeds [1,8)
  cm/year, activity bits **3E19999A**, Mixed ratio bits **3F400000**. Third
  reserved stream, double calculations, canonical zeros, single float cast and
  stored-float/double comparison policy unchanged.
- Publication: one **LayerData<uint>**, schema **1**, feature bits **7**;
  Influence flag/revision/offset/stride/radius/cutoff absent or zero.
- Original [15 B buffers](../../../../tests/worldgen/tectonics/vectors/manifest.json)
  and [15 M buffers](../../../../tests/worldgen/tectonics/motion-vectors/manifest.json)
  match every word. The test-only B projection removes only C-defined fields
  before restoring/comparing the exact original B schema. No fixture update.
- All **75 CAL01 R** fingerprints, metadata and fresh mode/type replays match;
  independent ownership BFS passes. All **75 M** complete candidate fingerprints
  (motions, normals, projections, identities/order/classes) match pre-approval
  evidence. Hashes supplement rather than replace complete buffer comparisons.

[Matched area summary](../../../../tests/worldgen/tectonics/validation/wg01d/matched-area-summary.csv)
is derived from those production-matched historical R records (25 worlds/size,
350 plate observations/size). It is not an unexecuted new generation sample.

| Size | Smallest observed plate | Largest observed plate | Observations below 1% |
| --- | ---: | ---: | ---: |
| Small | .695% | 30.475% | 7/350 |
| Medium | .580% | 22.022222% | 3/350 |
| Large | .63875% | 23.003750% | 4/350 |

These descriptive tails preserve irregular/unequal regions without new quotas,
minimum areas, rejection loops or acceptance thresholds. Extreme long seeds
are included in both frozen sets and fresh replay tests. Cross-platform bit
identity is not claimed without executing the compatibility checks there.

## 4. Executed regressions and exact reproduction

Final logs: [tectonics](../../../../tests/worldgen/tectonics/validation/wg01d/tectonics.txt)
**16/16**, [Laboratory](../../../../tests/worldgen/tectonics/validation/wg01d/laboratory.txt)
**19/19**, [menu](../../../../tests/worldgen/tectonics/validation/wg01d/menu.txt)
**16/16**. [Grid](../../../../tests/worldgen/tectonics/validation/wg01d/grid.txt)
passes all retained A/FIX01/B/C/D checks: 145,000 cells, 1,160,000 independent
geometry BFS comparisons, 1,160,000 Grid API comparisons, 1,015,000 metric
triangles and 10 integration checks. Both Release builds have zero warnings
and errors; compiled display metadata equals root VERSION **v0.0.41**.

Executed from repository root with already-restored dependencies:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release --no-restore
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release --no-restore
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release --no-restore
dotnet build .\game\Evolve.Game.csproj --configuration Release --no-restore
git diff --check
```

On a fresh checkout, omit --no-restore to restore existing dependencies. No new
package/project is added to Engine or Game. The normal test command executes
real production code; downstream neutral fixtures are test-only, never LAB
production capabilities. Project references are recorded in
[project-dependencies.txt](../../../../tests/worldgen/tectonics/validation/wg01d/project-dependencies.txt).

## 5. CPU phase, query and memory evidence

[Final raw measurements](../../../../tests/worldgen/tectonics/validation/wg01d/core-performance.txt),
[means/medians/ranges](../../../../tests/worldgen/tectonics/validation/wg01d/core-summary.csv).
Windows 10.0.26200, .NET 10.0.13, X64, Intel64 Family 6 Model 154 Stepping 3,
GenuineIntel, 16 logical CPUs. Release, tiering disabled, one full warmup per
size, three batches for seeds 0,1,-1,long.MinValue,long.MaxValue: **45 batches**.
All measured complete candidates equal real orchestrator output in full.
Measurements ran serially without simultaneous builds/tests. No clock/affinity
lock or universal pass threshold. Stopwatch frequency on this host: 10 MHz.

Median milliseconds per phase (15 batches/size):

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Region generation | 38.9378 | 79.6390 | 134.9844 |
| Fourteen motion records | .0007 | .0007 | .0007 |
| Boundary construction | 3.0046 | 6.0179 | 10.6102 |
| Validate/pack/defensive-copy candidate | 6.9360 | 13.8890 | 25.0653 |
| Validate/create immutable view | 6.7543 | 14.3713 | 24.7736 |
| Standalone array-source defensive copy | .0224 | .0287 | .0353 |

Motion generation stores directions/speeds; derived sin/cos velocities are
evaluated during projection. Sub-microsecond motion samples approach timer/
measurement overhead and are not high-confidence latency budgets. The codec
encapsulates packing plus validation and copying; pure packing cannot be
isolated without production instrumentation, which is intentionally absent.
Standalone copying uses the same known-array input path as production, not an
unknown-length enumerable. An initial enumerable-source diagnostic is retained
as core-performance-enumerable-probe.txt and **excluded from final metrics**.

Queries use 200,000 deterministic stride/polar/seam/interior lookups per phase
per batch, a live checksum sink and no input RNG. Total: **9 million owner and
9 million boundary queries**. Median ns/operation: owners **4.994 / 4.572 /
4.7605**, boundaries **59.284 / 57.374 / 55.5745** for Small/Medium/Large.
Both measured **0 allocated bytes/query**. Loop/index/checksum costs are included;
these are not isolated assembly-level getter costs.

Median allocated bytes by phase, including operation scratch/constructor work:

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Regions | 764,040 | 1,651,208 | 2,526,208 |
| Motions | 384 | 384 | 384 |
| Boundaries | 286,472 | 531,248 | 579,200 |
| Validate/pack/copy | 216,456 | 431,632 | 716,632 |
| Validate/view | 100,144 | 225,168 | 400,440 |
| Standalone copy | 58,296 | 103,360 | 158,336 |

Final payload ranges remain 56,916–59,696 / 101,936–104,916 / 154,416–159,456 B
for the five seeds/size. Median retained GC heap deltas were 110,752 / 183,896 /
266,848 B, with ranges 106,040–168,840 / 181,256–224,368 / 254,288–326,816 B.
They keep draft/candidate/view alive, exclude the later standalone-copy source
and clone, and include runtime/JIT/GC/diagnostic-list noise. They are not exact
world ownership, peak heap, physical driver memory or evidence of a leak.

## 6. Graphics construction, upload, frames and retention

The [graphics source/log/data/captures](../../../../tests/worldgen/tectonics/validation/wg01d/graphics/)
use the actual single EvolveWindow and production shaders/renderers against an
RGBA8 framebuffer at **1280x720**, VSync off. NVIDIA GeForce RTX 3050 Laptop
GPU/PCIe/SSE2, OpenGL **3.3.0 NVIDIA 592.00**. Same CPU/OS/runtime above; default
tiering for the graphics executable. This is real GPU execution, not native
mouse/keyboard interaction. Nine scenes cover sizes Small/Medium/Large × seeds
0,1,-1. PNG encoding/readback are excluded from timings.

Production BuildLines/TectonicVisualGeometry.Build are invoked directly through
test-only reflection: one warmup and three batches/scene, **27 measured batches**.
Uploads invoke the actual private Upload method into temporary diagnostic GL
buffers, separate from production mesh buffers, with Finish synchronization.
CPU measurements include reflection/dispatch; GPU elapsed uses GL_TIME_ELAPSED.
This does not add hooks or a renderer/generator to production.

Median ms over nine batches/size:

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Grid mesh CPU | 1.1201 | 1.8668 | 3.1511 |
| Tectonic mesh CPU | 3.1297 | 3.8303 | 6.7108 |
| Combined four-buffer upload CPU + Finish | 1.5748 | 4.5439 | 6.9017 |
| Combined upload GPU | .403456 | 1.301504 | 3.987456 |

Grid mesh allocation medians: 1,920,024 / 4,320,024 / 7,680,024 B; tectonic mesh:
2,978,952 / 6,659,168 / 11,715,424 B. Temporary arrays are diagnostic/mesh-build
allocation, not per-frame regenerated worlds. Raw geometry-upload.csv and its
summary include all min/max/mean values and exact uploaded section sizes.

Each scene/view has ten warmups and thirty synchronized samples: **540 measured
complete frames** across Grid and Tectonics, with UI/selection included. Size
summary below is the median of three scene **upper-middle statistics** (index
15 of 30 sorted samples), **not** a conventional even-sample median or a pooled
90-sample median. CPU includes reflection, render dispatch, SwapBuffers and Finish.

| View (historical upper-middle per scene) | Small GPU ms | Medium GPU ms | Large GPU ms |
| --- | ---: | ---: | ---: |
| Grid | .843776 | .863232 | .998400 |
| Tectonics + Grid | 1.380352 | 1.319936 | 1.551360 |

Tectonic GPU sample extrema were .822272–8.471552 / 1.117184–2.924544 /
1.387520–14.027776 ms. Do not infer an interactive FPS guarantee, a speedup over
earlier reports, or hardware-independent thresholds. Driver clocks, scheduling,
cache/thermal state, UI and synchronization differ between observations.

Tectonic CPU-finish median of scene upper-middle values: **6.4769 / 3.1430 / 3.6018 ms**.
CPU and GPU intervals measure different synchronized work and must not be
subtracted to estimate a renderer optimization or input latency.

Large retained logical buffers: Grid 7,680,000 B, fills 11,520,000 B, boundaries
59,104 B, arrows 672 B, selection 672 B. Clearing resets draw counts but retains
these stores. A new Small Grid replaces Grid storage with 1,920,000 B while
inactive Large fills remain; completing Small replaces fills with 2,880,000 B
and boundaries with 29,248 B. This matches the existing documented policy;
it is not an unbounded-growth or driver-memory conclusion. After explicitly
calling production OnUnload and diagnostically unbinding objects, GL.IsBuffer,
GL.IsVertexArray and GL.IsProgram confirm logical names are deleted. Actual
native-close dispatch and physical memory reclamation remain PENDING.

## 7. Visual evidence and native validation gate

The nine [plate captures](../../../../tests/worldgen/tectonics/validation/wg01d/graphics/)
plus three zoom views, independent B/V views and Start-unavailable image show
actual published maps. Developer inspection sampled Small/1, Medium/-1,
Large/1, Large zoom, B-off/V-on and the unavailable main-menu notice. Grid
remains visible; arrows and selection retain their roles. All fourteen colors,
fill-to-owner mapping, anchors/directions and selected text are also checked
headlessly. At fit, Large is dense; zoom is needed to distinguish small hexes.

The graphics scenarios assert separate Grid/Tectonics actions, no later stage,
unchanged Grid buffer, independent overlay flags, camera APIs, selection,
inspection reopening and unavailable INICIAR. They call real model/window APIs,
**not native input events**. No owner-native evidence was supplied in this task.
Use the [pending owner matrix](../../../../tests/worldgen/tectonics/validation/wg01d/native-validation.md)
to record exact resolutions, DPI, steps and evidence before changing status.
Captures/headless checks alone cannot approve those rows.

## 8. Reconciled observations and optional Influence

The owner supplied INFO meanings in the D request; this supersedes the earlier
unknown-description placeholder without rewriting that historical report.

| ID | Current disposition | Closure significance |
| --- | --- | --- |
| OBS-01 | Deferred: continental/oceanic classification belongs to its authorized geographic stage | No fake whole-plate crust; no new implementation |
| OBS-02 | Resolved numeric policy: approved M, exact compatibility/tie checks pass | Independent confirmation still required |
| OBS-03 | Resolved R calibration: frozen parameters and vectors unchanged | No recalibration |
| OBS-04 | Deferred future decision: Influence absent | No propagation or fictitious timing |
| INFO-01 | Informative: independent Auditor native execution limitation | Developer .NET/GL runs do not remove it |
| INFO-02 | Informative: independent reading of some binary buffers/captures limited | Decoder sources/full-word tests/manifests/text logs aid reproduction; no independent-reading claim |
| INFO-03 | Informative: diagnostics use restricted test access/reflection, timers/checksums | Measurement overhead and copy-source probe explicitly scoped; no production hooks |
| INFO-04 | Informative: GPU performance depends on environment | Raw variability/hardware/method recorded; no universal budget |
| INFO-05 | Pending owner native interactive confirmation | Blocks unconditional closure, not audit submission |

Influence has no implementation or published section, so computation/timing is
**N/A**, not zero or passed. Existing design's per-source bounded propagation
can approach O(S*(N+E)) work with reusable O(N) scratch; storing all-pairs fields
would be inappropriate. Large has N=80,000/E=239,200; optional four-word/cell
output alone would add **1,280,000 B**, before scratch/copies. Max-Gaussian
semantics cannot be silently replaced with nearest-boundary distance. No new
propagation was implemented, run or benchmarked; OBS-04 remains deferred.

## 9. Findings and recommendation

No new blocking functional defect was found in executed checks; production
remains untouched. D-F01 (acceptance gate): native interaction/DPI/presentation/
normal-close evidence remains incomplete and blocks an unconditional closure
recommendation. This is disclosed, not a newly proven algorithm defect.
D-F02 (informative): clear-session GPU stores remain allocated by design until
replacement/unload; actual physical driver reclamation is not established.
No fix is requested or applied under this verification-only authorization.

Submit this documentary/test delivery for independent audit. Final Layer 01
closure requires the Auditor's verdict and explicit owner acceptance after the
pending native matrix. No formal acceptance, roadmap retirement, Layer 02
development or branch integration follows from this report.

## 10. Reproduction and file inventory

Core benchmark (after Release checks build), serialized separately from tests:

```powershell
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --final-performance
} finally { $env:DOTNET_TieredCompilation = $previousTiering }
```

The retained graphics project/source are inert .txt diagnostics, not a new
production/test project dependency. Reproduce with Windows/OpenGL 3.3:

```powershell
$tools = '.\output\wg01d-capture'
$evidence = '.\tests\worldgen\tectonics\validation\wg01d\graphics'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
Copy-Item -LiteralPath "$evidence\Capture.csproj.txt" -Destination "$tools\Capture.csproj"
Copy-Item -LiteralPath "$evidence\FinalGraphicsEntryPoint.cs.txt" -Destination "$tools\FinalGraphicsEntryPoint.cs"
dotnet run --project "$tools\Capture.csproj" --configuration Release -- .\output\wg01d-replay
```

The recorded command used the committed graphics evidence directory as output;
use the separate replay path to preserve it. Source contains exact PNG encoding,
warmup/sample counts, buffer transitions and assertions. The complete changed
file inventory is [files.csv](../../../../tests/worldgen/tectonics/validation/wg01d/files.csv).
