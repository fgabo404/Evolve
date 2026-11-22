# WG-01-C — Approved M implementation and Laboratory delivery

**Status:** implemented for independent audit and owner visual review; no formal
C acceptance or D work. **Branch:** world-tectonics. **Version:** v0.0.39.
**Baseline:** v0.0.38, ba187fe933ad494a3b81992438d694fa40fed418.
A/B are formally accepted according to the owner's C authorization.

**Later status:** C + FIX01 were formally accepted at v0.0.40. D was delivered
at v0.0.41 and audited favorably with observations, per the owner's D-FIX01
request. [audit-fix01.md](audit-fix01.md) records current finding follow-up;
the C-era pending/no-D statements below retain their historical meaning.

## Frozen numeric policy and compatibility

The owner approved exactly M and the numerical policy in
[motion-calibration.md](motion-calibration.md). No post-approval recalibration:
uniform speed [1,8) cm/year; activity float bits **3E19999A** (.15);
Mixed ratio bits **3F400000** (.75). Compatibility ID
**tectonic-motion-m-v1**, motion/classification revision **1**. The exact third
reserved stage draw seeds the motion stream; two draws per ascending PlateId
(direction then speed), upper-53-bit unit variates. Directions are clockwise,
east=0/south=90. Double motion/projection calculations, one float component
cast, canonical positive zeros, no epsilon classification. Comparisons and
ratio multiplication promote stored floats to double, with Quiet/Mixed/
Transform/Convergent/Divergent precedence as approved. Tiny nonzero trigonometric
components are not snapped. Units are relative kinematic diagnostics, not
physical Grid displacement, force, geological crust or Earth calibration.

R remains **tectonic-partition-r-v2**, revision **2**; profile, partition generator
and noise source are unchanged. All 75 pre-approval M fingerprints match actual
production motions, double normals, projections, complete edge order and classes.
All 75 Ownership fingerprints also match. Fifteen new full flags-7 production
[buffers](../../../../tests/worldgen/tectonics/motion-vectors/manifest.json)
are frozen separately from the fifteen unchanged B buffers. Both sets are
compared word for word; the B checks explicitly project only added C fields out
of the production result, restoring its exact original B schema. No historical
fixture, P0 result, CAL01 source snapshot or comparison evidence was overwritten.

## Actual production contract

- TectonicPlatesLayer validates Grid and builds R ownership, M motions and
  PlateBoundaryBuilder output, then returns one fully validated candidate.
  Publication/progress/exclusivity/failure/rebuild remain owned by WG-CORE.
- TectonicData accepts schema 1 flags **1** (historical B) and **7** (full M).
  Unknown flags/revisions, including Influence 15, are rejected. Header words
  17/19/20 identify exact M; influence metadata remains zero/absent.
- Existing ownership queries are unchanged. GetMotion(byte) returns readonly
  PlateMotion: PlateId, DirectionDegrees, SpeedCmPerYear and derived Velocity.
  TectonicVector exposes get-only X/Y. GetBoundary(int) returns readonly
  endpoints, PlateIds, normal, signed float components, interaction Kind and
  derived Convergence/Divergence/Shear. No mutable references escape.
- GetMotion validates PlateId before rejecting absent B motion with
  InvalidOperationException. Boundary indices outside BoundaryCount throw
  ArgumentOutOfRangeException. Malformed data throws ArgumentException before
  a view/candidate escapes. No Influence API or fake fields are introduced.
- The view retains only immutable LayerData<uint> and HexGridData references.
  The codec validates long section sums/checked offsets before indexing,
  canonical zeros/ranges, exact profile metadata, connected ownership and every
  boundary. A canonical Grid scan with a six-ID scratch span proves completeness
  and sorted unique order; stored projection bits and classes must match motion.
  No persistent neighbor/edge cache or second world copy in readers.
- Boundary normals use accepted centers and nearest-periodic X; north/south
  remain bounded. All unlike-owner Grid edges appear exactly once. Invalid
  kinematics discard the attempt; prior Grid data and historical C views remain
  unchanged through failed attempts/rebuilds. No CORE/Grid APIs were modified.

## Laboratory implementation and native review

The existing single EvolveWindow and GridCamera are reused. The default session
inventory registers real Grid and Tectonics only. Each explicit G/stage click
calls RunNext once; no future layers execute. Completed Tectonics selects the
real published view. Grid/Tectonics remain selectable without regenerating or
resetting the camera. Reopening LAB retains inspection; INICIAR stays unavailable.

TectonicVisualGeometry builds static grouped fill triangles, interface lines
and fourteen arrows once per publication. Colors depend only on PlateId. The
accepted origin of each plate anchors its arrow stably inside that region;
arrow orientation follows published velocity and display length indicates speed.
The same Grid corners/centers supply outlines, fills and picking. Seam interface
lines appear at both physical cuts without adding cells, IDs or topological
edges. Selected-cell information reads actual PlateId, count/fraction, direction
and speed. Only the information card extends its scroll range for this view.

Controls: N creates a pending session; G advances Grid then Tectonics; select a
completed stage or the view dropdown to inspect it. Wheel zooms, right drag and
arrow keys pan, F fits. Click a hex for details. B toggles interfaces; V toggles
motion arrows. Escape/back returns to menu. These are display toggles, not
generation or influence controls. No unreliable boundary-edge hit selection is
claimed; categories remain accessible through the engine API.

Large fill positions require 11,520,000 bytes; the retained original Grid lines
require 7,680,000 bytes, plus interface/arrow/selection buffers and temporary CPU
upload arrays. Fourteen uniform-color draws avoid per-vertex color storage.
Meshes are not rebuilt on zoom/pan or every frame. Native driver memory, FPS,
input, DPI transitions, minimization/restore and disposal require manual evidence.
Existing buffer-retention behavior and the Auditor's .NET 10 native limitation
remain open; source inspection/headless runs do not resolve them.

## Executed validation and performance

[Final logs](../../../../tests/worldgen/tectonics/validation/wg01c/):
Tectonics **15/15** groups (original B regression checks plus T06/T07/T08/T10),
Laboratory **19/19** (T12 actual session and static geometry), menu **16/16**.
Complete Grid checks pass: 1,160,000 independent geometry BFS comparisons,
1,160,000 Grid API comparisons, 1,015,000 triangles and 10 integration checks.
Engine/Game Release builds: zero warnings/errors. Game ProductVersion is
v0.0.39. No native visual approval or independent audit is claimed.

The final commands used --no-restore with existing restored dependencies;
the performance command additionally used --no-build and tiering=0. A preliminary
checks build found an extra parenthesis in new test source; it was corrected
before these final successful runs. Full standalone reproduction commands:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
```

Manual review must cover Small/Medium/Large, varied seeds/types, Grid then
Tectonics, all fourteen colors/arrows, seam/polar selection, overlay toggles,
zoom/pan/fit, resizing/DPI, minimization/restore, LAB reopening, unavailable
INICIAR and clean shutdown. Owner confirmation and independent C audit remain
required before formal acceptance.

[Raw performance](../../../../tests/worldgen/tectonics/validation/wg01c/m-performance.txt)
and [summary with means/ranges](../../../../tests/worldgen/tectonics/validation/wg01c/m-performance-summary.csv):
.NET 10.0.13, Windows 10.0.26200, X64, 16 logical CPUs,
Intel64 Family 6 Model 154 Stepping 3, GenuineIntel. Release, tiering disabled,
one warmup per size, three serial batches per five signed/extreme seeds per size
(45 batches total). No simultaneous builds/tests. Each measured full payload
matches actual orchestrator output outside the measured intervals.

Median milliseconds [min,max], 15 batches per size:

| Phase | Small | Medium | Large |
| --- | --- | --- | --- |
| R partition | 27.1409 [25.3959,29.3751] | 60.3410 [58.8788,66.5426] | 106.7302 [103.4847,124.6699] |
| M motion/boundaries | 2.2261 [2.0266,2.6316] | 4.8255 [4.4065,6.2113] | 8.5080 [7.9503,8.9358] |
| Validate/pack/copy | 4.9386 [4.5346,6.3354] | 10.8237 [10.2468,12.7587] | 19.1492 [18.1188,20.8898] |
| Validated view | 4.8810 [4.5197,8.6809] | 10.7605 [9.9925,11.6770] | 18.4174 [17.6147,20.7479] |

Motion/boundary time is full draft elapsed minus partition's internal phase
sum; it includes stream/draft setup and timing overhead, not a pure microbenchmark.
Packing includes validation and LayerData's unavoidable copy. GC collections,
output, hashing and sequence comparisons are outside timed intervals.

Build allocations: Small 1,048,704–1,055,376 B; Medium 2,180,552–2,187,704 B;
Large 3,097,504–3,109,600 B. Codec: 214,344–219,896 / 429,424–435,392 /
709,432–719,512 B. View: 100,144 / 225,168 / 400,440 B. Payload:
56,916–59,696 / 101,936–104,916 / 154,416–159,456 B respectively.
Per-thread allocations include actual operation scratch/copies, excluding
measurement records. Retained GC heap deltas (raw log) keep draft/candidate/view
alive and include runtime noise, not peak live memory or GPU usage. No affinity,
frequency lock, cross-platform guarantee or invented acceptance threshold.

Run --motion-performance on the Release tectonic checks with
DOTNET_TieredCompilation=0; restore that environment variable afterwards.

## Observation register and scope

- OBS-01: continental/oceanic crust remains pending, without classifications.
- OBS-02: M policy approved, implemented, exactly compared and frozen; numerical
  policy closed for delivery, subject to independent verification.
- OBS-03: R resolved and unchanged.
- OBS-04: Influence remains disabled and unavailable.
- INFO-01/INFO-02/INFO-03: retained for documentary follow-up. Their descriptions
  were not present in repository audit records or the supplied finalization
  request; no meaning, implementation change or resolution is invented. The
  Auditor's corresponding source record is needed to reconcile those details.

No Grid/CORE, State, Saves, other layers or normal world creation changes;
no D implementation, roadmap retirement or branch merge. The file inventory
for this delivery is [delivery-c-files.csv](../../../../tests/worldgen/tectonics/delivery-c-files.csv).
