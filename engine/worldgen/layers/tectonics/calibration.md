# WG-01-B — Frozen R Delivery and Historical Calibration Evidence

**Current status:** WG-01-A/B formally accepted according to the owner's C request;
tectonic-partition-r-v2 / revision 2 frozen, schema 1 unchanged. The separate
[C motion comparison](motion-calibration.md) was subsequently approved for M;
[delivery-c.md](delivery-c.md) records its implementation and verification.
Historical B audit/acceptance statements below describe their original delivery.
**Branch/base:** world-tectonics, v0.0.37,
63e968ff71dbbcaa61e1ebbba744e466e3a01332.
**Current delivery:** v0.0.38. The P0 and CAL01 sections below are historical
records, preserved unchanged in substance. Their old pending/local-only status
statements are superseded only by the final R delivery record at the end.

## Candidate revision 1 and approval decision

The candidate uses these 14 canonical float costs, before deterministic shuffle:

0.58, 0.6517028, 0.7408509, 0.8303338, 0.9343438, 1.0309718, 1.1328111,
1.2481337, 1.3789481, 1.5323541, 1.7094282, 1.9127674, 2.137958, 2.4.

They are the proposed log-space interpolation of the 16 historical costs at
positions k*15/13, rounded to float and promoted to double during growth.
No PlateId permanently receives a large/fast profile. The range ratio is about
4.14; it is a relative competition profile, not a geological size distribution.

Other candidate values retained for evaluation:
160 farthest candidates per seed; frequencies 3,5,9,13,21,29, three octaves with
amplitude halving; warp .055 of each dimension; broad/medium/fine weights
1.65/1.10/.44; irregularity 1.90; positive resistance clamp [.27,3.10].

These retain the historical model's contour complexity while isolating the
14-plate adaptation. The candidate count yielded minimum pairwise seed
distances 30–34 / 45–50 / 61–67 for Small/Medium/Large in five selected seeds.
That supports separation for this sample, not a mathematical universal lower
bound or evidence that 160 is optimal. Six fields occupy 24*N bytes; caching
them avoids resampling at every competitive frontier edge. Warp distorts fields,
not cells. Positive resistance bounds and explicit finite accumulated costs
preserve monotonic expansion. No benchmark-driven changes to Grid were made.

Changes from the old generator: 14 log-interpolated costs; distinct seeds and
deterministic fallback; all origins reserved before growth; total queue ordering;
64-bit stage/noise streams instead of old int salts; explicit 64-bit noise hash;
canonical float costs; no neighbor cache, LandmassState or external RNG.
The new hash changes contours: this is an adaptation, not bit-for-bit legacy
map reproduction. No geological realism is inferred from irregular borders.

## Area evidence and decision that remains open

Raw per-plate integer areas and six-decimal area fractions for all 15 cases:
[candidate-calibration.txt](../../../../tests/worldgen/tectonics/candidate-calibration.txt).
Seeds: 0, 1, -1, long.MinValue, long.MaxValue; each size was measured in three
batches, and repeated words were compared in full. Fractions are rounded
presentation values; integer areas and ownership words are authoritative.

| Size | Smallest observed plate | Largest observed plate | Unlike-owner edges | Boundary cells |
| --- | ---: | ---: | ---: | ---: |
| Small | 7 cells / .035% | 11,243 / 56.215% | 1,839–2,219 | 1,815–2,190 |
| Medium | 14 / .031111% | 13,221 / 29.38% | 2,966–3,204 | 2,943–3,170 |
| Large | 32 / .04% | 22,179 / 27.72375% | 3,891–4,531 | 3,863–4,496 |

Each region is nonempty and connected, but the profile permits **very small
plates and occasionally a dominant plate**. Example: Small/seed 0 assigns
56.215% to plate 8 and .18% to plate 1. Small/long.MaxValue has a 7-cell plate;
Medium/seed 0 has a 14-cell plate. These are not rejected by an invented minimum
area or repaired after growth. Unequal areas alone do not settle whether this
distribution meets the intended design.

Owner decision required before freeze/commit: accept this extreme size diversity
as candidate revision 1, or request a reviewed recalibration (for example a
narrower cost range and/or resistance/irregularity profile) with new evidence.
No additional profile or hidden retry policy has been selected as accepted.

Seam-edge boundary counts are 2–6 in these cases; polar boundary-edge counts
15–37. They are test-only diagnostics over real adjacency, not a published
boundary section or tectonic interaction classification. All cells retain the
real seam/polar topology and independent owner connectivity.

## Stable candidate compatibility vectors

CORE remains splitmix64-stage-v1. Exactly three stage draws seed partition,
noise and reserved future motion streams. Partition draws: first origin,
candidate samples for successive seeds (fallback scans consume no randomness),
Fisher–Yates costs, then three rotation angles per plate. Noise draws: two warp
seeds followed by six band seeds. No WorldType affects draws or algorithms.

Unsigned lattice hash: seed xor sign-extended X*0x9E3779B97F4A7C15 xor
sign-extended Y*0xD1B54A32D192ED03, then SplitMix64 finalization
(shift30/*0xBF58476D1CE4E5B9, shift27/*0x94D049BB133111EB, shift31), all modulo
2^64. Upper 53 bits map to [-1,1). Octave seeds add octave*gamma modulo 2^64.
Cubic interpolation uses x*x*(3-2*x); octave amplitudes 1,.5,.25 normalized by
1.75. Hash and interpolation are encoded in source and pinned in checks.

- Hash(0,0,0) = -1.
- Hash(13,-7,0x123456789ABCDEF0) double bits = 3FDA6049D98CADA4.
- Sample(12.125,15.75,200,100,9,0x123456789ABCDEF0) bits = BFA27206DA58A447.
- Small/seed 0 origins by PlateId:
  10030,2730,19329,19597,799,867,19269,9573,8887,11127,17233,17563,13400,1165.
- Its areas: 1119,36,1305,903,118,252,762,310,11243,604,800,1022,799,727.
- Payload SHA-256:
  12A0C7856BCC3E33A3FB79B4ABA262AA4CD695A208C4A7514B7BE28A6BEBFA98.
  Hash input is ASCII comma-separated uppercase eight-digit uint hex words;
  no newline/BOM. All 15 payload fingerprints are in the raw file.

Hashes supplement full word-by-word comparisons; they are not the sole
determinism checks. These are reproducible **candidate** vectors on the recorded
runtime. Final revision/profile compatibility is not frozen before approval.
Cross-runtime/platform floating-point identity remains unverified.
Noise periodicity tolerance 1e-12 applies to the bounded tested coordinates and
at most 116 lattice periods: it permits double rounding after translations,
not bit-identical periodic real-number arithmetic or a geological error budget.

## Performance methodology and results

Raw evidence contains 45 batches (5 seeds * 3 sizes * 3 repeats), timestamp,
environment, phase times, maximum queue, allocations, retained deltas, payload
sizes and per-plate distributions. .NET 10.0.13, Windows 10.0.26200, X64,
16 logical CPUs; Intel64 Family 6 Model 154 Stepping 3, GenuineIntel.
Release; tiered compilation disabled only for calibration; one complete
partition/codec/view warm-up per size, then three batches per seed.
No CPU affinity/frequency lock, latency budget or hardware-independent claim.

Seed timing brackets SelectSeeds only. Profile/shuffle/rotation setup belongs
to total partition time but is not mislabeled as seed selection. Noise and
growth are timed separately; peak queue includes reserved-origin expansion and
all live duplicate candidates. Measurements use actual production helpers;
every packed result is validated and all repeated word sequences compared.
Each measured case is also compared in full to the real orchestrator's output.

Codec time includes full semantic/connectivity validation, packing and the
mandatory LayerData defensive copy. View time includes schema/metadata/
connectivity validation and one wrapper, not plate regeneration. Construction
and validation use accepted Grid queries. No driver/GPU/rendering measurements.

Median [minimum, maximum] across 15 batches per size, in milliseconds:

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Seed selection | .4598 [.3978,.6879] | .4379 [.4061,.5735] | .4350 [.3978,.6717] |
| Noise fields | 14.8748 [14.6817,15.4996] | 33.2999 [31.9205,35.1243] | 58.8695 [57.1340,62.1461] |
| Competitive growth | 10.5434 [10.1087,11.0215] | 25.5936 [24.1682,27.1133] | 46.3092 [45.0746,47.8563] |
| Total partition | 26.0115 [25.2300,26.8862] | 58.8458 [57.9014,62.7186] | 105.7831 [103.6037,110.6875] |
| Validate/pack/copy | 2.1382 [1.9818,2.8825] | 4.8111 [4.5888,5.2645] | 8.4479 [7.9160,8.8824] |
| Validated view | 2.1106 [1.9779,2.3447] | 4.8026 [4.3442,5.1447] | 8.4967 [8.0395,8.8432] |

| Size | Peak queue range | Partition allocated | Codec allocated | View allocated | Published payload |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small | 2,485–3,363 | 764,288 B | 141,656 B | 100,136 B | 20,576 B |
| Medium | 4,251–5,127 | 1,651,456 B | 316,704 B | 225,160 B | 45,576 B |
| Large | 5,611–7,551 | 2,526,456 B | 561,752 B | 400,184 B | 80,576 B |

Per-thread allocation counters exclude log formatting, independent test-only
shape diagnostics, fingerprinting and output containers. Partition allocation
includes noise arrays, private owners/records, frontier and its growth, plus
stream/draft setup. The internal phase counter excludes final wrapper/metric
return overhead; the outer partition counter above includes that overhead.
Codec allocation includes both candidate words and their defensive copy plus
connectivity scratch. View allocation is transient traversal scratch, preset
validation and the small immutable wrapper, not a retained owner copy.

Full-GC heap deltas are recorded while draft, packed data and view are kept
alive: ranges 32,776–41,000 / 91,000–99,224 / 152,776–169,224 B.
They include runtime noise and overlapping prior-baseline lifetimes, not exact
result-only retained sizes or allocation peaks. Payload byte counts are exact;
array headers/alignment are additional. One-process short benchmarks and 15
samples do not establish confidence intervals, real-time budgets or physical
simulation accuracy. No motions/stress propagation has been benchmarked.

## Executed functional evidence

Nine named check groups passed. They exercise production code and compare
whole buffers: all three sizes, five signed seeds and four WorldTypes
(60 full comparisons plus independent BFS coverage), fixed vectors, reserved
stream draw count, seed fallback, 202 field corruptions plus wrong types/
lengths/indices/disconnected ownership, source mutation, real stage order,
failure/exception/wrong-stage/invalid-draft atomicity, deterministic retry,
both rebuild scopes, historical finalization and concurrency/reentrancy.

The independent connectivity oracle uses explicit Offset deltas/modulo/bounded
rows, not production neighbor/growth logic; its neighbor sets are compared at
every visited CellId. No production test hook or context mutation is exposed.
The friend assembly grant is required for actual noise/profile vectors and
phase metrics; tests restrict its use to tectonic internals. No simulated
geographic executor exists outside test code.

Existing Grid A/B/C/FIX01 and V01–V06 checks passed, retaining 1,160,000 geometry
BFS + 1,160,000 Grid API comparisons, 1,015,000 metric triangles and ten CORE
integration checks. Laboratory 18/18 and menu 16/16 passed. Engine and Game
Release builds succeeded with zero warnings/errors. Native UI/GPU validation,
cross-platform replay, motion/boundary/influence tests T07–T09 and WG-01-D
full-layer performance/audit remain unexecuted/out of B scope.

## Observation register (no silent closure)

The WG-01-B request references OBS-01..OBS-04, but the original independent
audit text was not included in repository documents or the attachment.
The owner has been asked to supply it; no invented finding or resolution.

| ID | Information available / current disposition |
| --- | --- |
| OBS-01 | Original text pending transcription from audit. Preserved, no closure asserted. |
| OBS-02 | Original text pending transcription from audit. Preserved, no closure asserted. |
| OBS-03 | Numeric calibration must be reviewed before freeze/publication. Evidence above is ready; owner decision pending. |
| OBS-04 | Original text pending transcription from audit. Preserved, no closure asserted. |

No Grid, WG-CORE contracts, Laboratory/INICIAR, State, Saves, Layer 02 or
tectonic motion implementation changed. No WG-01-C/D or branch merge started.


## WG-01-B-CAL01 — Local comparative recalibration (2026-11-21)

**Authority:** local experimentation only. No commit, push, VERSION increment,
constant approval, compatibility-vector replacement, merge or WG-01-C work.
The earlier P0 evidence above is historical and unchanged. Candidate revision 1
is still not frozen. The owner, not this experiment, selects an adopted profile.

### A. Preservation, isolated profiles and exact constants

The original candidate-calibration.txt remains byte-identical, SHA-256
A018FC13E4ADDB0E4B1CCF435508BC0EF24FC92C3D22A8EF23670AC3215795A3.
All 15 original P0 fingerprints and origin vectors matched the new P0 runs;
all new cases also use full word/ownership replay comparisons. The existing
fixed numeric/payload tests were not replaced and passed after CAL01.

Shared constants: exactly 14 plates; 160 separated-seed candidates with fallback;
frequencies 3,5,9,13,21,29; three octaves with amplitudes 1,.5,.25; weights
1.65/1.10/.44; unchanged 64-bit noise/CORE random streams, seed reservation,
neighbor queries and total queue ordering. No quotas, area repairs or Voronoi
replacement. Colors and random origins correspond across profiles.

| Variant | Growth costs | Irregularity | Resistance clamp | Warp fraction | Controlled comparison |
| --- | --- | ---: | --- | ---: | --- |
| P0 | Original 14 costs above, unchanged | 1.90 | [.27,3.10] | .055 | Reference. |
| P1 | float(sqrt(canonical P0 float cost)) | 1.90 | [.27,3.10] | .055 | Cost contrast alone; max/min cost ratio falls from about 4.14 to 2.03. |
| P2 | Same as P1 | .95 | [.27,3.10] | .055 | P1 with noise/resistance modulation amplitude halved, no clamp/warp change. |
| R | Same as P1 | .95 | [.50,2.00] | .055 | Additional resistance-only control relative to P2. |
| W | Same as P1 | .95 | [.27,3.10] | .025 | Additional warp-only control relative to P2. |

P1/P2/R/W canonical float costs:
0.7615773, 0.80728114, 0.86072695, 0.9112266, 0.9666146, 1.0153677,
1.064336, 1.1171991, 1.1742862, 1.237883, 1.307451, 1.3830284,
1.4621758, 1.5491934. R and W are two explicit one-factor controls rather than
a combined multi-parameter change that cannot separate clamp and warp effects.
The precise machine-readable profile definitions are in
[profiles.json](../../../../tests/worldgen/tectonics/experiments/cal01/profiles.json).

The only CAL01 production-source change is an internal experimental profile
factory in TectonicGenerationProfile. Candidate still constructs exactly P0.
The actual partition algorithm, production layer/codec/typed reader and their
default path are unchanged. Experiments invoke that same generator through a
test-only layer adapter in the real orchestrator, after the real Grid stage.

All results are copied into schema-1 Ownership LayerData<uint>. P1/P2/R/W use
explicit experimental tags CA010001..CA010004, **not supported production
algorithm revisions**. The unchanged production TectonicData correctly rejects
them. A test-only packer and independent validator keep temporary profiles
separate from the P0 codec/revision. Adopting a profile requires a later explicit
decision and final schema/revision/reader preparation; no experimental tag is
silently registered as a final contract. No public test hook or parallel
generator was added.

### B. Sampling, checks and diagnostic definitions

The first sample is the same 0,1,-1,long.MinValue,long.MaxValue. The second
sample is 20 additional distinct signed seeds drawn before evaluation from
DeterministicStageRandom(initialState=0x43414C30312D3230); duplicate/base draws
would be skipped. Ordered seeds and sample labels:
[seeds.json](../../../../tests/worldgen/tectonics/experiments/cal01/seeds.json).

375 cases = 5 variants * 3 sizes * 25 seeds. Every case has two complete
executions: sequential RunNext with Pangea versus RunUntil with RealEarth,
equal full buffers/owners, independent ownership BFS and schema-word ownership
agreement before atomic publication. Warmups and image regeneration are extra,
not counted as the 750 measured/check executions. All 75 size/seed worlds
retain exactly the same 14 origins across the five variants.

Every execution validates one owner per cell, exactly IDs 0..13, 14 nonempty
regions, coherent origin/count/cost records and independent connected-component
coverage. BFS uses test-owned Offset deltas with periodic columns and bounded
rows, not growth queue reachability. Real Grid neighbors drive growth and map
boundary masks. Same-owner and different-owner seam edge counts are recorded,
without adding cells or wrapping polar rows.

For each case, cases.json/csv record minimum and maximum percentage, median
area in cells (average of ordered ranks 7 and 8), largest/smallest ratio,
boundary counts, timing, allocation and payload digest. areas.csv has every
area, percentage, origin and cost by PlateId: 5,250 plate observations.
Experimental diagnostic cutoffs are **not acceptance rules**: tiny <.5%, <1%,
<2%; dominant >20%, >25%, >33%. No result is rejected or repaired by these
cutoffs. Effective plate count is exp(-sum(p*ln p)); it describes diversity,
not a minimization target or geological realism.

Raw data: [cases.json](../../../../tests/worldgen/tectonics/experiments/cal01/cases.json),
[cases.csv](../../../../tests/worldgen/tectonics/experiments/cal01/cases.csv),
[areas.csv](../../../../tests/worldgen/tectonics/experiments/cal01/areas.csv).
Full original/additional/all statistical tables with correct even-sample medians:
[summary.md](../../../../tests/worldgen/tectonics/experiments/cal01/summary.md),
[summary.csv](../../../../tests/worldgen/tectonics/experiments/cal01/summary.csv).

### C. Distribution summary — all 25 seeds per size

Min/max below are worst observed percentages across each 25-world group.
The ratio is the median per-world largest/smallest area ratio.
Small/dominant counts refer to plate observations, not approved world budgets.

| Size/variant | Worst min % | Worst max % | Median largest/smallest | Plates <1% (of 350) | Plates >25% (of 350) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small/P0 | .0350 | 56.2150 | 168.30 | 67 | 11 |
| Small/P1 | .0950 | 51.4400 | 43.55 | 47 | 4 |
| Small/P2 | .2500 | 43.2200 | 14.25 | 17 | 2 |
| Small/R | .6950 | 30.4750 | 9.53 | 7 | 1 |
| Small/W | .2400 | 43.7100 | 15.74 | 19 | 3 |
| Medium/P0 | .0311 | 34.2400 | 123.58 | 69 | 10 |
| Medium/P1 | .0800 | 27.1356 | 54.01 | 42 | 4 |
| Medium/P2 | .2844 | 26.2911 | 15.59 | 9 | 1 |
| Medium/R | .5800 | 22.0222 | 7.30 | 3 | 0 |
| Medium/W | .2222 | 26.0622 | 14.83 | 11 | 1 |
| Large/P0 | .0400 | 37.6525 | 128.84 | 74 | 12 |
| Large/P1 | .0950 | 34.0075 | 70.18 | 51 | 5 |
| Large/P2 | .2888 | 29.0188 | 16.24 | 19 | 2 |
| Large/R | .6388 | 23.0038 | 10.49 | 4 | 0 |
| Large/W | .2938 | 28.5750 | 16.95 | 17 | 2 |

Across 1,050 plate observations per variant (75 worlds):

| Variant | <.5% | <1% | <2% | >20% | >25% | >33% |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| P0 | 130 | 210 | 299 | 65 | 33 | 6 |
| P1 | 79 | 140 | 252 | 45 | 13 | 3 |
| P2 | 13 | 45 | 133 | 29 | 5 | 1 |
| R | 0 | 14 | 68 | 8 | 1 | 0 |
| W | 13 | 47 | 134 | 27 | 6 | 1 |

The improvement is not confined to the original five seeds. On the 20
additional worlds per size, P0/P1/P2/R have respectively 170/109/36/8 plates
below 1% across 840 observations per variant. R has no >25% plate in that
additional sample; P2 still has four, and W has not shown a consistent
advantage over P2. These are sampled observations, not universal guarantees.

Examples deliberately include failures to eliminate extremes: Small/seed 0
is still 51.44% under P1 and 43.22% under P2; R reduces it to 30.475%, not an
equal-area partition. Medium/additional seed -6193655624985058374 still has
P2 regions from .4622% to 26.2911%. Large/additional seed
-3400830380307605163 has a P2 maximum of 29.01875%.
No favorable-only image or hidden rejection policy changes the sample.

### D. Required offline visual comparison

[Full index: 23 PNG triptychs](../../../../tests/worldgen/tectonics/experiments/cal01/image-index.md).
Fifteen images cover all original 5 seeds * 3 sizes under P0/P1/P2.
Five further triptychs cover the additional-sample P2 minimum/maximum tails
(the two criteria select the same Medium case). Three control images compare
P0/P2/R at seed 0 for every size.

Primary seed-zero comparisons:
[Small](../../../../tests/worldgen/tectonics/experiments/cal01/images/Small-0-original.png),
[Medium](../../../../tests/worldgen/tectonics/experiments/cal01/images/Medium-0-original.png),
[Large](../../../../tests/worldgen/tectonics/experiments/cal01/images/Large-0-original.png).
Resistance-control comparisons:
[Small P0/P2/R](../../../../tests/worldgen/tectonics/experiments/cal01/images/Small-0-control-P0-P2-R.png),
[Medium P0/P2/R](../../../../tests/worldgen/tectonics/experiments/cal01/images/Medium-0-control-P0-P2-R.png),
[Large P0/P2/R](../../../../tests/worldgen/tectonics/experiments/cal01/images/Large-0-control-P0-P2-R.png).

Every panel uses identical extent, uniform geometric scale, orientation and
PlateId palette for that world. Real engine centers and six unit corners
supply regular flat-top shapes; exported boundary masks use actual Grid
neighbors. All 14 areas/percentages, minimum/maximum PlateIds and median/ratio
appear in the legends. East/west adjacency is logical across the canonical
strip; no duplicate display cells. Polar edges and real plate interfaces are
dark. No fictitious boundaries, motion or influence fields.

The offline renderer uses Windows GDI+ through PowerShell/.NET Framework,
not OpenTK or the Laboratory. It introduces no NuGet/production dependency.
23 PNG files were decoded and checked for dimensions; selected Small/Medium/
Large and the resistance control were visually inspected for alignment,
legibility and matching legends. This is developer inspection of experiments,
not formal visual approval. Colors are distinct but no universal color-vision
accessibility claim is made.

### E. Performance and memory

Environment and full phase counters:
[environment.json](../../../../tests/worldgen/tectonics/experiments/cal01/environment.json)
and the raw case tables. .NET 10.0.13, Windows 10.0.26200, X64, 16 logical
processors; Intel64 Family 6 Model 154 Stepping 3, GenuineIntel.
Release, DOTNET_TieredCompilation=0, one warmup per size/profile.
Each world has one recorded partition measurement and one complete replay.
Counters exclude independent BFS, packing/publication, diagnostics, JSON/CSV,
PNG drawing, and output formatting; those are not represented as generation
speedups. The phase timers still record actual seed, noise and growth costs.

Median generation milliseconds over 25 measured worlds:

| Size | P0 | P1 | P2 | R | W |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small | 28.827 | 30.375 | 29.404 | 29.154 | 28.999 |
| Medium | 65.089 | 65.459 | 63.189 | 64.850 | 63.956 |
| Large | 118.398 | 120.952 | 119.463 | 119.352 | 117.639 |

Per-thread partition allocation ranges are the same across profiles:
Small 764,024 B; Medium 1,389,024–1,651,192 B; Large 2,526,192 B.
Medium variation reflects frontier backing-array growth, not a retained
neighbor cache. Exact Ownership payloads stay 20,576/45,576/80,576 bytes,
plus CLR array/object overhead. Published values remain unmanaged and copied.

The summaries include each phase median, complete per-case timings, peak queue
and area/edge distributions. Boundaries remain irregular real hex interfaces;
median edges across profiles range 1,889–2,081 / 2,903–3,152 / 3,831–4,242.
R's lower interface count is a measured contour tradeoff, not proof of greater
geological quality. Render/image export timing is intentionally not generation.

Times vary by world, thermal state, scheduling and allocation/GC; the fixed
P0/P1/P2/R/W order is not a randomized benchmark. Samples are different seeds,
not 25 repeated timings of one world; replay timing is not included: it is a correctness check,
not an independent timing batch. There is no exclusive CPU reservation or
frequency lock. No statistically established profile latency advantage,
hardware-independent budget or acceptance threshold is inferred. Previous P0
timings remain in their original log rather than being retroactively replaced.

### F. Recommendation and limits

**Technical recommendation: present R as the next adoption candidate for
explicit owner selection**, keeping P2 as the less constrained alternative.
This is not selection by minimum inequality alone: R removes the observed
sub-.5% microplates while retaining median largest/smallest ratios about
7.3–10.5, substantial area diversity, irregular connected boundaries and an
occasional 30.475% large plate. It preserves the seeded weighted-growth model,
normal noise bands and warp. Paired controls identify reduced cost contrast,
modulation and finally the clamp as the relevant levers; lowering warp alone
does not show a reliable improvement and is not recommended.

If the owner wants rare very dominant plates and more microplates deliberately,
P2 is defensible; Small/seed 0 makes that tradeoff visible. Neither R nor P2
imposes hard quotas or proves geophysical realism. Twenty additional seeds
support generalization beyond the initial five but cannot rule out unseen
extreme maps. No universal minimum area or maximum dominance is approved.

All 375 comparisons passed coverage/connectivity/full replay. Post-experiment
regressions: Tectonics 9/9 groups; complete Grid A/B/C/FIX01 plus V01–V06,
including 2,320,000 retained BFS assertions, 1,015,000 metric triangles and
10/10 CORE integration checks. No new LAB, motion or field tests are claimed.
No protected production module or default P0/vector/VERSION changed. Original
OBS-01..OBS-04 register above remains unresolved, not silently closed.

### G. Reproduction and complete local-file inventory

[Experiment instructions](../../../../tests/worldgen/tectonics/experiments/readme.md)
provide exact build/run/render/summary commands and the Windows script-policy
limitation. [local-files.csv](../../../../tests/worldgen/tectonics/experiments/cal01/local-files.csv)
lists every changed/new local file relative to the authorized Git baseline,
including the pre-existing WG-01-B work. Its categories distinguish existing
future-delivery code, temporary experimental edits, and experiment-only
tools/data/scenes/images. A file being listed is not authorization to commit it.

CAL01 modified the internal profile experiment factory, the checks CLI/project
(excluding the offline GDI+ renderer from net10 compilation), and this report.
Experiment-only additions are the comparative driver, offline C# renderer,
PowerShell render/summary tools and readme, raw/summarized data, scenes, PNGs
and manifest. Existing uncommitted B implementation remains available; no files
were deleted or promoted to an accepted/frozen profile.

Final state: world-tectonics, HEAD 63e968ff71dbbcaa61e1ebbba744e466e3a01332,
VERSION v0.0.37, no commit/push/merge. Stop for the owner's explicit profile
selection before preparing the final WG-01-B delivery.

## Final WG-01-B delivery — owner-approved R (v0.0.38)

This record supersedes all earlier pending-profile/local-only statements while
preserving the original P0 and CAL01 data. The owner explicitly selected R and
authorized its final implementation/publication. This is **not** independent
audit approval or formal acceptance of WG-01-B.

### Frozen constants, schema and revision

Algorithm compatibility ID: **tectonic-partition-r-v2**.
PartitionRevision: **2**; SchemaVersion: **1**; feature bits: Ownership **1**.
Historical P0 revision 1 and CAL01's CA010003 tag are rejected by the production
reader, not interpreted as R. Root game VERSION is independently v0.0.38.
Revision 2 identifies the definitive R mapping, numeric profile, RNG draw order,
noise hash and queue policy together.

The exact CAL01 R profile was read from profiles.json and its original source,
not reconstructed from the preceding summary tables. Production reads these
canonical IEEE-754 single-precision words directly; it no longer runs legacy
interpolation, square roots/powers, or an experimental profile factory.

| Cost rank before deterministic shuffle | Round-trip float | Exact uint bits |
| ---: | ---: | --- |
| 0 | .7615773 | 3F42F6BB |
| 1 | .80728114 | 3F4EA9FA |
| 2 | .86072695 | 3F5C589A |
| 3 | .9112266 | 3F694625 |
| 4 | .9666146 | 3F77740E |
| 5 | 1.0153677 | 3F81F792 |
| 6 | 1.064336 | 3F883C29 |
| 7 | 1.1171991 | 3F8F0061 |
| 8 | 1.1742862 | 3F964F03 |
| 9 | 1.237883 | 3F9E72F3 |
| 10 | 1.307451 | 3FA75A8E |
| 11 | 1.3830284 | 3FB10713 |
| 12 | 1.4621758 | 3FBB2894 |
| 13 | 1.5491934 | 3FC64BF8 |

All other constants are unchanged from R: 14 plates, 160 candidate samples,
frequencies 3,5,9,13,21,29; three octaves, amplitudes 1,.5,.25; warp frequency 3
and fraction .055; band weights 1.65/1.10/.44; irregularity .95; resistance
clamp [.50,2.00]. The same separated/reserved seeds, positive weighted
competitive frontier and real Grid neighbors are used. No quota, area repair,
new geographic algorithm or permanent PlateId advantage.

Compared with P0, only the reviewed numeric profile differs: balanced costs,
irregularity .95 instead of 1.90 and resistance [.50,2.00] instead of [.27,3.10].
Warp/noise frequencies and spatial algorithm remain as calibrated. Production
equivalence against all 75 CAL01 R cases proves that freezing the bits has not
changed the selected partition beyond its documented revision word.

Determinism policy remains CORE splitmix64-stage-v1, exactly three initial
draws for partition/noise/reserved motion, fixed internal consumption, rejection
sampling, unsigned modulo-2^64 noise hashing and total lexicographic queue order
(cost,PlateId,CellId,sequence). WorldType is ignored by the partition; negative
64-bit seeds preserve their bits. Clock/GC metrics do not enter payloads or RNG.
Floating-point compatibility vectors are pinned on the recorded runtime; bit
identity on other runtime/platform combinations is not claimed without execution.

### Definitive production compatibility vectors and areas

[Fifteen complete production buffers and manifest](../../../../tests/worldgen/tectonics/vectors/manifest.json)
were generated explicitly through the registered real Grid and Tectonics
executors, not by rewriting P0 vectors. They cover all three sizes and seeds
0,1,-1,long.MinValue,long.MaxValue. Little-endian uint32 .words files total
733,640 payload bytes. Default tests compare every word and every plate's
origin/count/cost bits; digests are supplementary.

Canonical-text SHA-256 examples for seed 0:

| Size | Definitive R digest |
| --- | --- |
| Small | 2C00A1D967A2F9A8D04341B74E8C3D294B1FC7333BFDBAB323262280D63B9060 |
| Medium | F9A2587614AEC8D9A22F78DF622E50306791140936A53A18A3B51A6ED626643A |
| Large | C12B5D4FE87B40F7CFE575BAE2BD8B03CE28FB7B783B50D8C597A2767B3A3DDA |

Seed-zero CellCounts by PlateId 0..13, generated from final production:

- Small: 1854,507,1255,796,575,731,921,1087,6095,1520,996,1338,860,1465.
- Medium: 3130,1024,892,2955,2452,5487,3300,1086,6131,4232,5902,3147,261,5001.
- Large: 1747,3339,4182,3584,4928,11996,6877,9000,11628,4394,5859,6331,1756,4379.

Each set sums exactly to Grid.CellCount and passes independent owner BFS.
The wider R distribution remains the 25-seed sample above: smallest observed
fractions .695%/.580%/.63875%, largest 30.475%/22.02222%/23.00375%.
No area limits or universal spread guarantees are invented.

All 75 archived R case metadata and full-payload fingerprints match final
production after normalizing only header word 16 back to the old experiment
tag in a private test copy. Each case also has a complete fresh mode/type
replay and independent connectivity check. CORE/noise arithmetic vectors are
unchanged shared vectors; P0-specific plate outputs are retained only in their
historical records, not as definitive R tests.

### Final executed validation

[Retained command output](../../../../tests/worldgen/tectonics/validation/)
records: 10/10 tectonic groups, complete Grid regressions, Laboratory 18/18,
menu 16/16 and Release Engine/Game builds with zero warnings/errors.
The Game ProductVersion matched VERSION v0.0.38. Whitespace and scope review
passed before the single delivery commit.

B checks cover T01–T06 and applicable T10–T12: exhaustive ownership, independent
BFS/seam/pole topology, 60 full fresh WorldType comparisons, 15 complete golden
buffers, 75 additional complete mode/type replays against the CAL01 R sample,
204 corrupted fields plus malformed inputs, defensive copies, actual CORE
prerequisites/failure/retry/rebuild/finalized history and contention/reentrancy.
Grid retains 1,160,000 geometry BFS plus 1,160,000 API comparisons, 1,015,000
metric triangles and ten CORE integration checks. No retired CORE suite is
claimed present. T07–T09, tectonic LAB and C/D full-layer verification are not
implemented or claimed approved.

### Final serial performance/memory evidence

[Raw final R measurement](../../../../tests/worldgen/tectonics/r-performance.txt)
and [machine-readable summary](../../../../tests/worldgen/tectonics/r-performance-summary.csv)
contain 45 batches: three repetitions of five signed seeds per size, one full
warmup per size. Measurement ran serially after validation/build processes
finished; an earlier overlapping trial was excluded from this final record.
Release, .NET 10.0.13, Windows 10.0.26200, X64, 16 logical CPUs, Intel64
Family 6 Model 154 Stepping 3, GenuineIntel, tiered compilation disabled.

Milliseconds: median [minimum,maximum] over 15 batches per size.

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Seed selection | .6099 [.5403,.7000] | .6581 [.5773,.7753] | .7610 [.6564,1.4706] |
| Noise | 22.4611 [20.2052,24.3277] | 48.7515 [46.7124,53.2518] | 93.6125 [89.2554,117.5121] |
| Growth | 15.1935 [13.8359,16.6702] | 35.0612 [31.4438,41.0523] | 78.4434 [75.3041,101.3419] |
| Partition total | 37.4468 [34.6420,41.0930] | 84.7120 [80.1733,93.9756] | 177.3185 [168.5556,211.7035] |
| Validate/pack/copy | 3.1957 [2.8518,3.6145] | 6.9209 [6.1110,10.0131] | 19.2537 [17.9028,21.1711] |
| Validated immutable view | 3.0629 [2.7271,3.8286] | 7.2149 [6.4688,12.1184] | 19.7500 [17.2543,22.6918] |

| Size | Peak queue range | Partition allocated | Codec allocated | View allocated | Exact payload |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small | 2,982–3,336 | 764,288 B | 141,656 B | 100,136 B | 20,576 B |
| Medium | 4,543–4,960 | 1,651,456 B | 316,704 B | 225,160 B | 45,576 B |
| Large | 5,411–7,640 | 2,526,456 B | 561,752 B | 400,184 B | 80,576 B |

Timed/allocation intervals exclude console output, shape/hash diagnostics and
full collections. Partition includes actual draft/streams/scratch/frontier;
codec includes packing, validation and the unavoidable LayerData copy. View
cost includes transient BFS validation, not regeneration. Per-thread bytes
are allocations, not peak live memory. Retained heap deltas while draft,
payload and view are alive range 41,000–49,224 / 91,000–99,224 /
152,776–169,224 B and include runtime/GC noise. Refer to the raw counters for
every batch. No CPU affinity/frequency lock or hardware-independent threshold;
different runtimes, background activity and thermal state can change timings.
Earlier CAL01/P0 timings were not retroactively changed or presented as these
final measurements.

The previously recorded Auditor limitation for native .NET 10 execution is not
cleared by these developer runs. Native UI/DPI/GPU lifetime and cross-platform
bit identity remain unverified here. Scoped test-data Git attributes preserve
raw evidence/vector bytes, including historical file fingerprints.

### Current observation dispositions and delivery boundaries

| Observation | Current disposition |
| --- | --- |
| OBS-01 | Continental/oceanic crust classification remains out of WG-01-B; no classification implemented or resolution claimed. |
| OBS-02 | Numerical policy for actual tectonic movement remains WG-01-C work; motion output and algorithms remain absent. |
| OBS-03 | Calibration closed for B by explicit owner selection of exact R, frozen revision/constants and definitive production vectors. Independent B audit/acceptance still pending. |
| OBS-04 | Optional influence propagation remains disabled; flag/offset/revision zero, no propagation implementation or fake fields. |

Production contains only the selected ownership generator/codec/profile.
Historical executable experiment sources/tools were preserved as inert text
snapshots under experiments/archive; compare/control CLI options and the
production factory were removed. All 23 PNGs, raw/summarized CAL01 cases,
exact profiles/seeds, original P0 data and source provenance remain.
The 23 regenerable binary scene files were deleted locally to avoid carrying
redundant geometry. Historical local-files.csv describes the old experiment;
[delivery-files.csv](../../../../tests/worldgen/tectonics/delivery-files.csv)
is the complete current delivery inventory.

No generated images/scenes are production assets or Engine/Game dependencies.
Golden buffers are small test fixtures; the retained evidence/source set is
under 7 MB before final text/inventory additions. No dependency on old
GEvolve models, GDI+, extra UI libraries or test projects is added to production.
No change to accepted Grid/WG-CORE, LAB/INICIAR, State/Saves or Layer 02.
No movement, dynamic boundary classification, influences, tectonic UI, C/D
development or merge. The four-increment roadmap is retained.

Delivery commit: feat(worldgen): generate deterministic tectonic plate regions.
Publication is exclusively origin/world-tectonics, with local/remote SHA and
clean-tree verification reported with the delivery. This remains a submission
for independent audit, not automatic formal acceptance of WG-01-B.
