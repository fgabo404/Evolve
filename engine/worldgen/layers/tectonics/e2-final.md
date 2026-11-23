# WG-01-E2-FINAL: Influence verification delivery

**Submitted for architectural review, independent audit and express owner
acceptance.** Baseline world-tectonics, v0.0.44,
b11856fffbb78f860567520ee8f73f0b8241c9a0; branch/HEAD/VERSION matched and the
initial tree was clean. Delivery version **v0.0.45**. HISTORICAL was selected
for implementation/validation; this delivery does not declare its final freeze,
activate Influence, implement Mantle/E3, integrate schema 2/E4 or close Layer 01.

## Proposed profile and implementation

`tectonic-influence-historical-v1`, proposed numeric revision **1**, has exact
binary32 words: relative radius `3CA3D70A`, minimum `3F800000`, cutoff `40400000`.
Effective radii are 2/3/4 and inclusive reaches 6/9/12. The
[numeric contract](influence-profile.md) specifies exact values, rounding,
operation order, deterministic scope, zero/overflow policy and units.

One new internal profile class supplies the selected parameters. The existing
private source/distance/propagation components remain unchanged; only
TectonicInfluenceFields adds `ValidateComplete`. It performs structural checks,
fresh boundary-distance/production Gaussian recomputation and all-word
comparison. It neither publishes nor mutates fields. The normal executor,
codec, R/M algorithms/streams, Grid and CORE source remain unchanged.
Tectonics still publishes **schema 1/features 7**, InfluenceRevision zero and
HasInfluence false. No public API, package or test project is introduced.

## TEST-01: independent and complete verification

The normal Tectonics executable passes **24/24 groups**, retaining all original
21. The three additions exercise actual internal Engine code and complete
review fixtures, with no OpenTK/GPU dependency:

| Requirement | Executed evidence |
| --- | --- |
| Exact selected policy | Identifier/revision/constant bits and effective radius/reach for every MapSize. |
| Complete real M source extraction | Direct decoding of stored normal/tangential words into independent per-endpoint maxima; compare every source channel and boundary-membership bit on fifteen maps. No production Convergence/Divergence/Shear accessor used as expected-value logic. |
| Exact distances | Independent explicit Offset multi-source BFS; compare all uint distances. Zero membership includes quiet edges, independent of channel amplitude. |
| Complete real envelopes | Target-centered BFS, opposite traversal order, independent Offset neighbors, direct per-source exp/Pow without production weight tables or traversal. All targets, all channels and exact float words for 3 sizes x 5 seeds: 725,000 cells and 2,175,000 floats. |
| Dense case | Every Small cell is a source with varied positive channel amplitudes; all 20,000 distances and 60,000 float values match. This is a test-only controlled field, not an extra plate/world model. |
| Sparse/edge/numeric cases | Preserved CAL01 full sparse envelopes and independent BFS; stronger farther source, shared endpoints, simultaneous channels, permutation, seam/poles, final inclusive/following shell, invalid/nonfinite/negative inputs, checked overflow and canonical underflow zeros. |
| Immutability/corruption | Changing original sample arrays does not change retained outputs; deliberate reflection injection of an in-range wrong candidate passes structural checks but fails complete recomputation; NaN is rejected. No mutator is exposed to facilitate tests. |
| Complete references | Every decompressed byte and SHA-256 matches all 60 size/seed/world-type cases; same raw fields across equivalent world types and unchanged R/M full-buffer bindings. |

Full real oracles use seeds 0,1,-1,long.MinValue,long.MaxValue. The dense fixture
adds 20,000 cells to the unique all-target coverage above. No dense/real target
sampling is substituted for these checks. Preserved CAL01 six-target dense-map
checks remain additional sampled evidence and are not mislabeled exhaustive.
Same-production recomputation is explicitly distinct from the independent
oracle; both are measured and validated separately.

## Review vectors and hashes

[Manifest](../../../../tests/worldgen/tectonics/influence-vectors/manifest.json):
60 explicit size/seed/world-type cases referencing fifteen unique complete
buffers. Each decoded row is uintLE distance plus three float32LE channel words
in ascending CellId order, exactly 16N bytes. No production schema header or
LayerData publication is added. Data totals **11,600,000 raw bytes**, compressed
losslessly into **757,644 bytes** across fifteen Brotli files (metadata excluded).

Each manifest case records profile/revision/parameter words, dimensions, raw
and compressed SHA-256, and original Ownership/Motion full-buffer SHA-256.
The exporter verified policy, complete independent oracles and all-world-type
replays before pinning the review references. The normal suite verifies exact
bytes against fresh production fields; it never regenerates historical vectors.
All fifteen B and fifteen M fixtures, 75 archived R and 75 M cases remain intact.
All nine archived CAL01 HISTORICAL full-field hashes (three sizes x seeds
0,-1,long.MaxValue) also match these new reference hashes exactly.

Seed-zero raw field hashes (all four world types):

| Size | SHA-256 |
| --- | --- |
| Small | 100616616A27C2EC741F504245867F7BD648855CA4651957C65FF89D73A77DBE |
| Medium | 48B527ED647830908F2A0B89A34C890313CF38E3E53E756A9D15929D6B9D538A |
| Large | 4D6C0ABCCB01773A3F018C7A7C9532B953695F748E569155B0E520BF96217DD3 |

The manifest supplies all remaining seeds and compressed/R/M hashes. Raw hashes
identify the numeric contract; compressed hashes protect the fixture files and
do not promise identical compressor output on other runtime versions.

## PERF-01: separated costs

[Environment](../../../../tests/worldgen/tectonics/validation/influence-final/performance/environment.json):
.NET 10.0.13, Windows 10.0.26200, x64 OS/process, Intel64 Family 6 Model 154
Stepping 3 (GenuineIntel), 16 logical processors, Stopwatch 10 MHz. Release,
one thread, DOTNET_TieredCompilation=0. No dedicated-machine claim.

Nine cases: three sizes x seeds 0,-1,long.MaxValue. One warmup then three
measured batches per case, one whole-map operation per phase per batch.
[216 raw phase rows](../../../../tests/worldgen/tectonics/validation/influence-final/performance/timings.csv)
retain 54 warmup and 162 measured rows. Each case checks complete replay hashes,
source extraction and the independent full-field oracle. Per-case
[summaries](../../../../tests/worldgen/tectonics/validation/influence-final/performance/summary.csv)
show min/conventional median/max across repeated batches, allocations and
signed retained-GC ranges. Warmups are excluded, not discarded from evidence.

Median milliseconds [minimum, maximum], pooling the nine measured samples
per size/phase (three seeds x three batches):

| Phase | Small | Medium | Large |
| --- | --- | --- | --- |
| Extract sources | .559 [.514,.620] | 1.291 [.955,1.638] | 2.183 [2.066,2.621] |
| Boundary distance | 2.442 [2.183,2.675] | 8.807 [4.939,11.511] | 15.729 [15.269,16.477] |
| Gaussian | 20.931 [19.155,22.567] | 128.613 [71.183,164.411] | 311.899 [294.049,323.711] |
| Structural validation | 2.760 [2.475,2.891] | 8.911 [5.624,11.445] | 15.793 [15.488,19.018] |
| Production recomputed validation | 25.875 [24.450,28.641] | 143.853 [80.772,149.793] | 343.105 [327.297,396.846] |
| Independent complete-oracle validation | 30.408 [28.509,33.204] | 228.780 [132.733,231.321] | 695.825 [692.734,744.358] |

For Large Gaussian, within-seed min/median/max ms: seed 0,
317.007/319.031/323.711; seed -1, 294.049/299.084/301.659; long.MaxValue,
311.659/311.899/315.754. Seed-dependent workload and repeated-run variation
are visible separately in raw rows; no outlier trimming or universal threshold.

The paired per-run added cost of replacing structural-only checks with full
production recomputation has median **23.217 / 134.583 / 327.571 ms** S/M/L.
Gaussian construction plus recomputed validation, paired per run, has median
**47.450 / 273.363 / 654.792 ms**. The validation includes structural scanning,
fresh distance BFS, a fresh Gaussian and all-bit comparison; these totals do
not include initial source extraction/distance creation or future schema packing.
Independent-oracle validation separately includes independent distances/target
envelopes and exact comparison, using independent M source decoding prepared
outside its timing. It is not called by the production validator.

### Allocation and retention

| Phase | Allocated bytes per operation S / M / L |
| --- | --- |
| Sources | 350304 / 722520 / 1224720 (means across seeds) |
| Boundary distance | 160072 / 360072 / 640072 |
| Gaussian | 480200 / 1080208 / 1920224 |
| Structural validation | 0 / 0 / 0 |
| Production recomputed validation | 640272 / 1440280 / 2560296 |
| Independent complete-oracle validation | 560160 / 1260160 / 2240160 |

Production recomputation allocates about 32N additional bytes (new Gaussian
output/traversal plus distance result/queue), but releases its temporary result;
Large retained delta was +4,152..+8,184 bytes, a noisy GC observation. Gaussian
Large retained delta ranged 930,960..968,232 bytes near its 960,000-byte channel
payload. The independent oracle retains its returned four-word result during
measurement: Large +1,280,024 bytes, with additional temporary queues/marks.
These deltas are not peak heap, process memory or leak proof. Sources/distances,
reference inputs, R/M and future packed copies can coexist and are excluded
from those per-phase retention interpretations.

Delegates are created before timing; row objects, hashes, CSV/JSON output and
analysis are outside operation allocation/time windows. GC.GetTotalMemory(true)
runs before/after outside the timer; allocated bytes use the current thread.
An empty-operation diagnostic after warming the measurement wrapper reports
zero allocated bytes, .0529 ms and -40 retained bytes; its separate delegate
may still include first-call JIT. It is recorded, not treated as a calibrated
timing floor or subtracted. Source extraction includes
its existing GetBoundary normal reconstruction and boundary materialization.

Active source ranges S/M/L: 1,802..1,884 / 2,788..2,845 / 3,665..3,918.
Source-centric Gaussian visits: 225,661..235,971 / 743,972..760,894 /
1,698,829..1,814,541. Cached table exponentials are K+1 = 7/10/13;
the oracle does not use a table. The raw Visits column counts Gaussian
source-cell pairs, oracle target-neighborhood visits, or N scanned cells for
linear phases; it is not a claim that these different operations have equal cost.

**Recommendation for E4 review:** quantify and explicitly approve the one-time
complete-validation cost and temporary copies before integration; reuse the
validated immutable view instead of revalidating per frame/accessor. No faster
model, trusted-builder shortcut, budget or profile change is adopted here.

## Regressions, observations and limitations

[Executed logs](../../../../tests/worldgen/tectonics/validation/influence-final/tectonics.txt):
Tectonics 24/24; Grid complete with 1,160,000 geometry + 1,160,000 Grid API BFS
comparisons; Laboratory 19/19; menu 16/16. Release Engine, Game and check project
builds have zero warnings/errors. VERSION and compiled Game display metadata
are v0.0.45; Engine's existing SDK assembly-version policy is unchanged.
No GUI validation was needed or performed for this headless-only change.

| Observation | Delivery status |
| --- | --- |
| TEST-01 | Expanded complete oracles and review vectors implemented/executed; independent assessment and profile acceptance pending. |
| PERF-01 | Complete structural/recomputation/independent-oracle costs and allocations measured; no approved performance budget or E4 strategy inferred. |
| E1 MINOR-02 | Previously resolved finite-positive cutoff/checked reach policy preserved, with old regression evidence retained. |
| E1 MINOR-01 | Schema-1/schema-2 reader compatibility remains pending for E4; active codec untouched. |
| GATE-01 | N01 PASS by owner declaration; N02-N17 PENDING, no native/DPI/GPU closure. |

The owner's specifications communicate CAL01 acceptance with observations;
a standalone CAL01 independent audit report is not tracked or supplied here.
The reported observations are followed without inventing a new audit verdict.
Only the recorded .NET/OS/CPU environment and five correctness/three performance
seeds are verified; no cross-platform bit identity, geophysical realism or
hardware-independent timing guarantee is claimed. Historical CAL01/D/FIX01
evidence and original R/M fixtures are preserved.

The proposed profile and complete references are ready for independent review
without another numeric change being recommended. Definitive freezing remains
the owner's decision after architectural review and independent audit.
Stop after E2-FINAL publication; no E3/E4, Layer 02, merge or Layer 01 closure.

## Reproduction and inventory

See [PowerShell test/performance/export instructions](../../../../tests/worldgen/tectonics/readme.md).
Normal verification never exports or rewrites goldens. Use a new directory for
any expressly reviewed export or benchmark rerun. The
[delivery inventory](../../../../tests/worldgen/tectonics/validation/influence-final/delivery-files.csv)
lists all 48 added/modified files; there are no deletions. Checks verify 79
local Markdown targets, headers/comment syntax for seven affected C# files,
fixture checksums, raw row counts and protected-path scope. Git whitespace
checks include staged new files before commit; commit/remote SHA is reported
from Git. New console logs are UTF-8 transcriptions verified against their
original decoded content; historical evidence is unchanged.
