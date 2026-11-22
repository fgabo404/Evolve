# WG-01-E2-CAL01: Preliminary deformation calibration

**Delivery for Architect review, owner numeric selection and subsequent independent
audit. No candidate is approved or frozen.** Baseline v0.0.43,
49565c260bd21a045c2338d9677331d48b921680, world-tectonics, initially clean.
Delivery version v0.0.44. Normal Tectonics still publishes **schema 1/features 7**.
No codec, executor, R/M profile, Grid, CORE, Laboratory, INICIAR, State or Saves
behavior changed. No mantle generation, E2-FINAL or E3 is implemented.

## Internal implementation

Files under `influence/` are internal and use the existing restricted friend
assembly for headless tests. No new project, public API or package is added.

| File | Responsibility |
| --- | --- |
| TectonicInfluenceParameters.cs | Finite positive float radius/cutoff, checked integer reach; experimental radius resolution only. |
| TectonicInfluenceSources.cs | Extract stored M Convergence/Divergence/Shear at both boundary endpoints; own immutable per-cell channel maxima and independent boundary membership. |
| TectonicBoundaryDistances.cs | One multi-source BFS with uint distances through real Grid neighbors; private array, value-only lookup. |
| TectonicInfluenceFields.cs | Bounded stamped BFS, shared channel traversal, float attenuation table and immutable private output; separate structural/range validation. |

The normal TectonicPlatesLayer never invokes these components. The calibration
entry point explicitly obtains the real published R/M result before calling
them; none of these fields is a published LayerData result. Source extraction
uses TectonicData.GetBoundary, including its existing normal reconstruction
overhead, then materializes only boundary records for extraction. Private
samples/boundary factories support controlled independent fixtures, not another
geographic generator. Input enumerables are consumed into owned storage; no
mutable source or output array escapes.

For each cell/channel, source amplitude is the maximum of all incident stored
M projections. Compression uses Convergence, Extension uses Divergence, Shear
uses absolute tangential speed. A cell can carry all three channels. Quiet
boundary endpoints remain distance sources even when every amplitude is zero.
There is no plate barrier, crust classification, sum, Euclidean metric or
per-world normalization. Units remain diagnostic cm/year-equivalent, not
pascals, terrain height or a physical strain-rate model.

## Numerical policy and historical relation

The formula is `max_v a[v,c] * exp(-(d(v,x)/R)^2)` for integer Grid distance
`d <= K`, with inclusive `K=min(N-1,ceil(double(R)*double(cutoff)))`.
Use actual Grid neighbor queries, horizontal wrapping and bounded poles.
Distance to any boundary endpoint is computed separately and is independent
of amplitude/channel. An empty boundary set is rejected rather than returning
a fabricated finite distance; actual fourteen-region M results have boundaries.

E1 audit **MINOR-02** is addressed in the design and internal parameter checks:
both stored float radius and cutoff must be finite and strictly positive.
Validate product/ceiling representability as int before clipping to N-1;
throw OverflowException for excessive reach or an unrepresentable resolved
float radius. Invalid/nonfinite inputs throw ArgumentOutOfRangeException.
No numeric cutoff interval has been approved. Values below 1 and above 5 are
tested as valid when their reach is representable; legacy limits are not adopted.

Proposed experimental evaluation order, not a frozen Influence revision:

1. Resolve `float(max(double(minimum),double(fraction)*min(columns,rows)))`.
2. Promote stored radius/cutoff to double for reach/kernel evaluation.
3. Store `(float)Math.Exp(-Math.Pow(distance/(double)radius,2))` once per shell.
4. Multiply each source float by that stored float weight, rounding to float
   before maximum reduction. No fused accumulation or double-product shortcut.
5. Traverse ascending CellId sources and canonical Grid directions. Maximum
   reduction with finite nonnegative amplitudes is independent of input order.
   Canonical positive zero is retained, including underflow; zero fields remain
   zero. No output normalization is performed.

This preserves the operation shape of historical Spread reviewed in
[e-history-review.md](e-history-review.md), without importing HexTopology,
ushort distance arrays, mutable State or legacy settings. The N-1 traversal cap
omits no shortest path on this connected N-cell graph. Unlike the old unchecked
cast, integer overflow is rejected before allocation. Huge radii can still be
valid but expensive; representability is not a performance acceptance criterion.

## Candidate set (all preliminary)

All candidates have minimum radius 1. Fraction/cutoff values below are local
test-harness inputs, absent from normal production execution and not assigned
an approved Influence revision.

| Candidate | Fraction | Cutoff | Radius Small/Medium/Large | Inclusive reach Small/Medium/Large |
| --- | ---: | ---: | --- | --- |
| NARROW | .01 | 3 | 1 / 1.5 / 2 | 3 / 5 / 6 |
| HISTORICAL | .02 | 3 | 2 / 3 / 4 | 6 / 9 / 12 |
| WIDE | .04 | 3 | 4 / 6 / 8 | 12 / 18 / 24 |
| SHORT-CUTOFF | .02 | 2 | 2 / 3 / 4 | 4 / 6 / 8 |
| LONG-CUTOFF | .02 | 4 | 2 / 3 / 4 | 8 / 12 / 16 |

These isolate narrower/wider radius and shorter/longer tails. The same source
amplitudes and fourteen R/M plates are used for every candidate in each case.
The Gaussian reaches 1/e at distance R; integer shell rounding can include a
last shell beyond the continuous cutoff multiple. This is deliberate historical
semantics, not an unrecorded distance approximation.

## Reproducible measurements

[Raw evidence](../../../../tests/worldgen/tectonics/experiments/influence-cal01/raw/environment.json):
.NET 10.0.13, SDK 10.0.402, Windows 10.0.26200, x64 OS/process,
Intel64 Family 6 Model 154 Stepping 3, GenuineIntel; 16 logical processors.
Stopwatch frequency 10,000,000 Hz. Release, one process/thread,
DOTNET_TieredCompilation=0. No dedicated-machine/isolation guarantee.

Three sizes x seeds {0,-1,long.MaxValue} x five candidates = **45 cases**.
Each has one complete warmup (batch 0) followed by three measured batches,
one operation per phase per batch: **135 measured runs**. All 180 warmup/measured
runs hash the complete distance/C/E/S fields and require identical hashes for
equivalent case settings. All four phases warm up before measured batches.

GC.GetTotalMemory(true) before/after each phase is outside its timer. Allocation
uses current-thread allocated-byte deltas. Delegates are constructed before
timing; phase rows, hashes, distribution analysis and serialization are outside
timing/allocation windows. Source extraction's own enumerators/boundary copy
are included. Each operation builds a whole-map component; costs below are not
per-cell nanoseconds. Retained deltas are signed noisy managed-heap observations,
not peak memory, leak proof or process working set. The cold empty-operation
control recorded 0 allocated bytes, 0.1084 ms and -64 retained bytes; it includes
first-call JIT and is not subtracted from warmed measurements.

- [timings.csv](../../../../tests/worldgen/tectonics/experiments/influence-cal01/raw/timings.csv):
  720 phase rows, explicitly marked warmup vs measured; phase time/allocation/
  retention, radius/cutoff/reach, active sources, source-cell visits, attenuation
  evaluations and channel products.
- [fields.csv](../../../../tests/worldgen/tectonics/experiments/influence-cal01/raw/fields.csv):
  405 measured channel rows, full-result SHA-256, maxima/means/p50/p90/p99,
  coverage, channel overlap, seam/polar/interior summaries.
- [summary.csv](../../../../tests/worldgen/tectonics/experiments/influence-cal01/raw/summary.csv):
  nine measured samples per size/candidate/phase (three seeds x three batches),
  min/conventional median/max and allocation/retention ranges. Raw per-seed
  batches permit separating machine variation from seed-dependent workloads.

Field quantiles use `sorted[floor(q*(N-1))]`; timing summaries use the conventional
median (averaging the two central samples for an even count).

### Gaussian time and allocation

Median milliseconds [minimum, maximum] over nine measured runs; bytes allocated
per run (fixed within each row). These are actual local measurements, not
universal thresholds or claims of acceptance.

| Candidate | Small ms | Medium ms | Large ms | Allocated bytes S / M / L |
| --- | --- | --- | --- | --- |
| NARROW | 5.462 [4.925,5.936] | 37.198 [23.329,40.251] | 78.378 [65.753,98.716] | 480184 / 1080192 / 1920200 |
| HISTORICAL | 28.046 [24.307,40.449] | 147.140 [135.537,159.183] | 337.884 [283.067,403.138] | 480200 / 1080208 / 1920224 |
| WIDE | 109.167 [105.527,182.948] | 586.431 [545.609,685.804] | 1409.070 [1317.069,1550.772] | 480224 / 1080248 / 1920272 |
| SHORT-CUTOFF | 10.831 [9.036,17.655] | 63.768 [56.112,74.771] | 160.330 [149.227,198.262] | 480192 / 1080200 / 1920208 |
| LONG-CUTOFF | 49.846 [43.655,75.640] | 259.048 [222.176,285.941] | 637.700 [570.978,709.079] | 480208 / 1080224 / 1920240 |

Within-case Large/HISTORICAL batch variation (minimum / median / maximum ms)
was 283.067 / 346.257 / 358.166 for seed 0; 316.485 / 320.540 / 336.119
for -1; and 337.884 / 344.101 / 403.138 for long.MaxValue. Variation is
reported, not removed as an outlier or hidden by a pooled average.

Gaussian allocations are approximately 24N bytes: 12N retained channel values,
12N reusable traversal scratch, plus attenuation table/objects. There is no
N-sized allocation per source. Large retained Gaussian deltas ranged
951,784..968,232 bytes across candidates/batches, near the 960,000-byte value
payload, with GC observation noise. This does not include concurrently live
sources/distances/R/M/decoder/harness or future packed publication copies.

| HISTORICAL phase | Median ms S / M / L | Mean allocated bytes S / M / L |
| --- | --- | --- |
| Source extraction | .710 / 1.529 / 2.422 | 350304 / 722520 / 1224720 |
| Boundary-distance BFS | 3.044 / 9.453 / 16.713 | 160072 / 360072 / 640072 |
| Structural/range validation | 3.457 / 9.930 / 17.288 | 0 / 0 / 0 |

Distance allocation is approximately 8N (uint result plus int queue), with 4N
retained data. Source retained data is approximately 13N (three floats plus
boundary bool); extraction also includes temporary boundary records. See raw
summary for actual signed GC ranges. Structural validation verifies finite
nonnegative/canonical values, source-channel bounds and exact nearest-boundary
distance invariants through zero membership, neighbor Lipschitz bounds and
descent paths. **It does not recompute the Gaussian envelope**. E4's proposed
full-field decoding validation may roughly double Gaussian work and is not
implemented or benchmarked here.

Large had 3,665..3,918 active source cells. Source-cell visits per run ranged:
NARROW 462,744..494,304; HISTORICAL 1,698,829..1,814,541;
WIDE 6,442,018..6,881,976; SHORT 789,170..842,918;
LONG 2,947,266..3,148,217. Each visit performs three channel products.
Actual exponential evaluations are only K+1 per field: Large 7/13/25/9/17
for the table's candidate order, cached across all source traversals. BFS still
dominates; this is not a constant-cost nearest-source approximation.

## Field comparison and implications

Coverage below is the fraction with Compression >=1% of that run's maximum
Compression source amplitude, averaged over the three seeds (batch 1 only;
repeated batches have identical fields). Overlap means at least two channels
exceed their respective 1% source maximum. Thresholds are analysis labels only,
not physical cutoffs, new approved settings or per-world value normalization.

| Candidate | Compression coverage % S / M / L | Channel overlap % S / M / L | Nonzero Compression % S / M / L |
| --- | --- | --- | --- |
| NARROW | 15.81 / 15.48 / 14.80 | 19.42 / 18.92 / 19.03 | 29.24 / 29.59 / 25.95 |
| HISTORICAL | 29.81 / 29.53 / 28.32 | 34.69 / 34.26 / 34.45 | 49.87 / 48.03 / 46.78 |
| WIDE | 54.28 / 54.11 / 52.58 | 59.67 / 59.39 / 59.88 | 80.17 / 78.82 / 77.88 |
| SHORT-CUTOFF | 29.81 / 29.53 / 28.32 | 34.69 / 34.26 / 34.45 | 36.45 / 34.44 / 33.20 |
| LONG-CUTOFF | 29.81 / 29.53 / 28.32 | 34.69 / 34.26 / 34.45 | 61.83 / 60.01 / 58.91 |

Wider radius broadens the modeled boundary signal and increases competing
channels inside plates; it does not create more physical force or classify
crust. Narrower radius leaves more quiet interior. Approximate scale consistency
across sizes is visible in these samples, not a guarantee over all seeds.

For Large, mean Compression was .429789 / .813054 / 1.585418 for
NARROW/HISTORICAL/WIDE. At fixed radius, SHORT and LONG gave .811500 and .813058:
cutoff 4 mainly preserves extremely weak tails at substantially greater cost.
Equal 1% coverage does not imply bit-identical fields or downstream equivalence;
tiny tails can matter to future response models that are not yet implemented.

Seam means use first/last columns; polar means use first/last rows. HISTORICAL
Compression seam means S/M/L were .459268/.859252/.754320 and polar means
.376750/.239579/.334212. Topology correctness is established by independent
fixtures, not by demanding equal geographic-band means. Interior here means
boundary distance >=2R, so each candidate can select a different population;
its mean is conditional, not a same-cell controlled difference. HISTORICAL
interior Compression means were .007494/.006483/.005897. Raw rows preserve the
other channels and distributions for review.

**Technical recommendation:** review radius/coverage first, then the tail
dynamic range needed by later geology. Retain HISTORICAL as the comparison
anchor, examine SHORT against it for tail/cost tradeoffs, and keep NARROW/WIDE
as explicit narrower/broader alternatives. Do not select solely by appearance
or runtime. No winner, numeric revision, compatibility golden or final profile
is approved by this report.

## Executed correctness and regressions

[Validation logs](../../../../tests/worldgen/tectonics/experiments/influence-cal01/validation/tectonics.txt)
record **21/21 Tectonics groups**: all original seventeen plus four new groups.
The new tests exercise actual internal Engine components, without GPU:

- Endpoint maxima, shared edges, simultaneous channels, quiet/unequal sources,
  input ownership and canonical zero.
- Invalid amplitudes/radius/cutoff, default struct rejection, checked overflow,
  float extremes/underflow and valid values outside historical cutoff bounds.
- Complete 20,000-cell sparse-source envelopes using independent Offset BFS,
  direct reversed all-source maxima and exact float-bit comparisons; separate
  complete nearest-boundary oracle. Successive/inclusive/following shells,
  stronger farther source, reordered inputs, seam and polar cases.
- All sizes x five seeds {0,1,-1,long.MinValue,long.MaxValue} x all four world
  types: complete replay equality, stable distance fields, unchanged R/M payload,
  fourteen plates, schema 1/features 7 and absent published Influence. Real dense
  fields additionally compare six seam/polar/interior targets per size/seed
  against all-source expectations using independent target BFS. This is not
  an exhaustive all-target oracle for every dense field.

Original fifteen B and fifteen M full buffers remain untouched and match;
all 75 archived R and all 75 M cases pass existing regressions. Grid passes
1,160,000 geometry BFS plus 1,160,000 Grid API comparisons and existing A/B/C/D
checks. Laboratory **19/19**, menu **16/16**, Release Engine and Game **zero
warnings/errors**. git diff --check and the staged diff whitespace check passed.
Initial sandbox
restore could not read the user NuGet.Config; the authorized rerun with access
completed successfully. That access failure is not a passed build.
The compiled Game ProductVersion is v0.0.44, read from the existing centralized
game version configuration. Engine retains its existing SDK assembly-version
policy; changing that unrelated configuration is outside E2-CAL01.

The [file inventory](../../../../tests/worldgen/tectonics/experiments/influence-cal01/delivery-files.csv)
lists all 26 added/modified paths. Documentary checks verified 40 local Markdown
targets (zero missing), headers/comment syntax in all six new C# files,
calibration row/hash counts and an empty diff for protected CORE/Grid/R/M/UI/
vector paths. No files are deleted. Console logs were transcribed from the
PowerShell default encoding to UTF-8 with exact decoded-content comparisons.

## Reproduction and pending decisions

See [test instructions](../../../../tests/worldgen/tectonics/readme.md) for exact
PowerShell regression/calibration commands. Use a fresh directory for reruns;
the harness refuses to overwrite evidence. Runtime/CPU/source-seed variation,
GC retention noise, only three benchmark seeds and absent native GPU evidence
limit the measurements. No universal threshold, peak-heap claim, physical model
validation or independent audit approval is inferred.

E1 audit **MINOR-01** schema-reader compatibility stays open for E4. GATE-01
remains partially open (N01 owner-declared PASS, N02-N17 PENDING). All frozen
R/M, Grid/CORE contracts and historical audit evidence remain intact. Owner
selection and Architect review are next; no E2-FINAL, E3, schema 2, Laboratory
view, Layer 02 or merge is authorized by this delivery.
