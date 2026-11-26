# WG-01-B/C — Frozen R and M headless verification

The .NET 10 executable references actual production Engine code; no OpenTK,
native window or duplicate generator. Use dotnet run, not dotnet test.
Nonzero exit means a failed assertion.

## E3-CAL01: experimental Model A calibration

The current suite passes **27/27 groups**, retaining all 24 E2-FINAL groups and
adding parameter/seed checks, controlled fields and complete candidate oracles.
See the [calibration report](../../../engine/worldgen/layers/tectonics/e3-calibration.md)
and [historical reuse matrix](../../../engine/worldgen/layers/tectonics/e3-history-review.md).
No Mantle candidate is selected/frozen and normal publication stays schema 1/7.

Complete new candidate tests cover 96 maps: 3 sizes x 4 signed/extreme seeds x
8 candidates, all potentials and canonical edges, independent Offset geometry,
periodic normals and final float values. Additional controls exercise zero/
constant/tectonic/noise/anomaly cases, malformed fields and derived balance
bounds. Replay compares Pangea and RealEarth; prior frozen R/M/Influence tests
continue to cover all four world types. No GPU/window validation is claimed.

After the regression/build commands below finish, run measurements separately
with an empty output directory and tiering disabled:

```powershell
dotnet build .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --mantle-calibration .\tests\worldgen\tectonics\experiments\mantle-cal01\rerun
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

Archived [environment/candidates](experiments/mantle-cal01/raw/environment.json),
[phase timings](experiments/mantle-cal01/raw/timings.csv),
[distributions](experiments/mantle-cal01/raw/distributions.csv),
[balance/interior results](experiments/mantle-cal01/raw/results.csv) and
[summaries](experiments/mantle-cal01/raw/summary.csv) preserve warmup separately.
No old evidence is overwritten. The harness refuses a nonempty output directory.
Hashes are experimental replay evidence, not new frozen Mantle vectors.

## E2-FINAL: selected-profile validation and review vectors

The historical E2-FINAL suite passed **24/24 groups**: the prior 21 plus exact selected
policy/dense corruption checks, complete real-map oracles and complete
Influence references bound to original R/M buffers. The proposed profile is
`tectonic-influence-historical-v1`, revision 1; definitive acceptance remains
pending independent audit and the owner's decision. Normal schema 1/features 7
remain unchanged. See the [delivery report](../../../engine/worldgen/layers/tectonics/e2-final.md)
and [reference manifest](influence-vectors/manifest.json).

Complete real-map oracles cover 725,000 cells / 2,175,000 float values over
all sizes and five signed/extreme seeds; a separate fully dense Small fixture
covers 20,000 cells. They independently decode stored M projections, traverse
explicit Offset topology from each target and evaluate the complete envelope.
References compare all bytes for 60 size/seed/world-type cases, sharing only
physically identical fixture files. No target sampling is used in those new
oracles; the preserved CAL01 sampled checks remain additional historical coverage.

Run the full regressions with the independent-audit commands below. Measure
PERF-01 separately after builds/tests finish, using a new empty directory:

```powershell
dotnet build .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --influence-final-performance .\tests\worldgen\tectonics\validation\influence-final-rerun
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

The explicit authoring switch below verifies independent full oracles before
export. It never overwrites existing references and is unnecessary for normal
audit verification. Do not use it to replace failing goldens without approval.

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release -- --export-influence-vectors .\tests\worldgen\tectonics\influence-review-export
```

[PERF-01 raw rows](validation/influence-final/performance/timings.csv) separate
source extraction, boundary distance, Gaussian propagation, structural
validation, production recomputation and independent full-oracle validation.
Warmup is explicit; CPU/runtime/batch settings and signed GC-retention caveats
are recorded. No budget or hardware-independent acceptance threshold is implied.

## E2-CAL01: private deformation experiments

The historical CAL01 suite passed 21/21 groups: the seventeen B/C/D-FIX01 groups plus
four internal deformation groups in TectonicInfluenceChecks.cs. See the
[calibration report](../../../engine/worldgen/layers/tectonics/e2-calibration.md)
for numerical policy, candidates, complete/sampled independent oracles, actual
measurements and remaining decisions. No Influence profile is selected/frozen;
normal publication remains schema 1/features 7. No Laboratory view or mantle
calculation is added. E1 MINOR-01 reader compatibility remains pending for E4;
E1 MINOR-02 finite-positive cutoff/checked reach is addressed experimentally.

Run correctness/regressions with the commands below. Run calibration separately,
after other checks/builds finish, to avoid concurrent test load. The harness
uses five explicit candidates, all sizes, seeds 0/-1/long.MaxValue, one warmup
then three measured batches per case. Phase times/allocations exclude analysis
and output; signed retained-GC deltas are not peak working set. Raw batch zero
is retained and excluded from summaries. Existing evidence is never overwritten.

```powershell
dotnet build .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    # Choose a new empty directory; this path is intentionally not the archived raw evidence.
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --influence-calibration .\tests\worldgen\tectonics\experiments\influence-cal01\rerun
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

Archived [raw evidence](experiments/influence-cal01/raw/environment.json),
[phase samples](experiments/influence-cal01/raw/timings.csv),
[field distributions](experiments/influence-cal01/raw/fields.csv),
[summaries](experiments/influence-cal01/raw/summary.csv) and
[regression log](experiments/influence-cal01/validation/tectonics.txt) belong
only to E2-CAL01. They do not supersede historical R/M vectors or approve E2-FINAL.

**Current audit follow-up:** D-FIX01 executes 17/17 groups including the shared
conventional graphics timing median (even/odd/repeated/extreme finite samples).
Historical D's 16-group logs and upper-middle frame summaries are preserved.
[audit-fix01.md](../../../engine/worldgen/layers/tectonics/audit-fix01.md) records
MINOR-01/02 resolution and the partially resolved GATE-01. N01 is owner-declared
PASS; N02–N17 remain PENDING. New runs/samples reside only in validation/wg01d-fix01/.

The historical WG-01-D delivery executed **16/16 groups**, adding an independent Boolean category
oracle over 324 signed/extreme pairs and 42,991 published interfaces, plus
exact positive/negative binary midpoint rounding. Previous checks/fixtures are
preserved. [Final verification](../../../engine/worldgen/layers/tectonics/verification-d.md)
and [native pending matrix](validation/wg01d/native-validation.md) separate
developer evidence from owner/independent acceptance.

Run --final-performance separately after building, with tiering disabled, for
isolated actual region/motion/boundary/codec/view/array-copy/query measurements.
It validates full output against the real orchestrator and reports allocation
and retained-GC deltas. Source and raw results explain diagnostic overhead and
why the initial enumerable-copy probe is excluded from final metrics.

The original B evidence below is historical; the C section records its
historical 15-group suite and approved M publication. No historical claims of
absent motion should be read as the current C capability inventory.

## Independent audit commands

From the repository root in PowerShell with .NET 10:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Measure R separately after tests/builds finish:

```powershell
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --calibration
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

That historical CLI name now measures only frozen production R. Recorded final
stdout is [r-performance.txt](r-performance.txt), with phase/memory summary
[r-performance-summary.csv](r-performance-summary.csv). Original P0
[candidate-calibration.txt](candidate-calibration.txt) is preserved unchanged.

## Executed coverage

10/10 named groups passed. T01–T03 cover all 145,000 cells, 14 owners,
independent Offset BFS and seam/pole adjacency. T04 compares complete words for
five signed/extreme seeds, three sizes and all four WorldTypes (60 fresh
comparisons). T05 pins the CORE/noise vectors, warp periodicity, seed fallback,
three stage draws, and definitive R buffers.

[TectonicFrozenVectors.cs](TectonicFrozenVectors.cs) checks all words of 15
production-generated [golden buffers](vectors/manifest.json), not only hashes.
It also verifies exact cost bits/constants against archived CAL01 R and checks
all 75 R sample cases: metadata plus the archived full-payload fingerprint after
normalizing only the revision word. Each of those 75 cases has another complete
mode/type replay and independent connectivity. This is equivalence to R, not
P0 interpreted as R. Old revision 1 and CAL01's experiment tag are rejected.

T06 rejects 204 field corruptions plus malformed types/stages/lengths/Grid,
indices, nonfinite costs, disconnected owner swaps and mutable-source changes.
T10 exercises actual prerequisite order, atomic failures, retries, suffix and
Grid rebuilds, historical finalization, contention and reentrancy. T11/T12 B
scope covers dependencies/absence of C features and existing LAB/menu
regressions, not a new tectonic UI. T07–T09 and C/D full-layer/visual acceptance
remain deferred.

The specifically named friend assembly accesses production tectonic vectors/
metrics only. No public test hooks or CORE mutation are used. Test-only
downstream executors provide neutral buffers for lifecycle checks, not real
geography. No motion, dynamic boundary classification or influences are built.

## Frozen fixture policy and provenance

Profile: owner-selected R, tectonic-partition-r-v2, partition revision 2.
Schema stays 1. The 15 .words files are complete little-endian uint32 payloads
exported explicitly from real registered production layers through WG-CORE.
manifest.json records all origins, areas, cost bits and canonical-text digests.
They total 733,640 payload bytes; nothing is reconstructed from area summaries.

Normal checks never overwrite goldens. Updating them requires explicit
algorithm/revision authorization and review. The initial authorized export was:

```powershell
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release -- --export-vectors .\tests\worldgen\tectonics\vectors
```

It rejects an existing manifest. This is provenance, not a routine audit command.
CORE/noise vectors remain shared because their arithmetic did not change;
P0-specific partition fixtures are historical evidence only.

## Evidence and limitations

[validation/](validation/) retains test/build/version outputs.
The local .gitattributes preserves raw evidence/vector bytes across Git checkouts;
line-ending conversion must not invalidate historical file fingerprints.
The immutable historical CAL01 summary also retains its original empty final
line via one file-specific attribute; source whitespace checks are unchanged.
[calibration report](../../../engine/worldgen/layers/tectonics/calibration.md)
contains the exact constants, observations, areas, final timing methodology and
limits. [Historical experiments](experiments/readme.md) contain text snapshots,
raw/summarized CAL01 evidence and PNGs; historical CAL01 sources remain inert.
The separately authorized C numeric comparison below is executable test-only
code. Discarded .scene geometry is regenerable; the PNGs/data remain.

No numerical performance threshold, geological realism, cross-platform bit
identity, native input/DPI/GPU cleanup or formal B acceptance is inferred.

## WG-01-C approval and verification

The owner has formally accepted A/B and authorized C. Before adopting kinematic
constants, the test-only experiments/MotionProfileComparison.cs compares four
provisional policies using the third reserved stream and real frozen production
R Ownership data. Run with --compare-motion <new-empty-output-directory>.
It refuses to overwrite results and never publishes C data or changes the LAB.

[Motion policy report](../../../engine/worldgen/layers/tectonics/motion-calibration.md)
contains exact policies, commands, measurements and limitations.
[motion-cal01/](experiments/motion-cal01/) contains all 300 cases, 900 full
replays, exact profiles/environment, CSV summary and regression outputs.
These fingerprints are historical comparative evidence, not the new frozen C
vectors. The owner selected M and its exact numerical policy. Production now
matches all 75 M fingerprints exactly, with fifteen separate complete buffers
in motion-vectors/. Existing vectors/ and P0/CAL01 evidence remain unchanged.

The C delivery passed 15/15 groups, retaining all B assertions and adding
production M precision/ties, complete independent interfaces/projections,
expanded malformed schema/canonical-zero checks, C candidate atomicity/retry,
historical rebuilding, full output WorldType independence and complete M
goldens. The test-only OwnershipOnly projection removes exactly the added C
fields to preserve every original B assertion/vector. It does not rewrite
fixtures or duplicate partition generation. Unsupported Influence remains absent.

[C delivery](../../../engine/worldgen/layers/tectonics/delivery-c.md) records
contracts, final logs, measurement methodology and native limitations. Run
--motion-performance for separate serial R+M phase/allocation measurements.
--export-motion-vectors <empty-directory> documents the explicit production
fixture export and refuses to overwrite an existing directory's contents.
Normal checks never export or update compatibility vectors. Native visual
review and formal C acceptance remain pending.
The historical B delivery was an audit submission; the later C request records owner acceptance of A/B.
