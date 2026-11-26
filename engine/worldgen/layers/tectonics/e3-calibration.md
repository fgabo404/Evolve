# WG-01-E3-CAL01: Model A preliminary mantle calibration

**Experimental delivery for architectural review, independent audit and owner
selection. No Mantle profile is selected or frozen.** Base world-tectonics,
v0.0.45 / 4fb88e93b73bbecf678689584a108e6e138bee2a, initially clean and verified
before changes. Delivery version v0.0.46. The owner's authorization identifies
R revision 2, M revision 1 and Influence revision 1 as frozen inputs. Their
code, parameters, streams, fifteen original buffers each and manifests remain
unchanged. Normal Tectonics still publishes schema 1/features 7, with no
Influence/Mantle publication or new stage. E3-FINAL/E4/E5 are not authorized.

## Historical reuse and private components

The [pre-implementation historical review](e3-history-review.md) inspects the
pinned worldgenerationold snapshot, including periodic noise, RNG, geometric
normals, coast influence, Pangaea pressure/potential and relief. No mantle
circulation implementation was identified, including an all-tracked-text
mantle-term search. Pangaea's Potential/Pressure are relief fields and do not
replace Model A. No historical source file or old project is imported.

Direct unchanged reuse: PeriodicTectonicNoise.Sample, DeterministicStageRandom
and DeriveSeed, HexGridData.TryGetNeighbor, FlatHexGeometry.Center/PeriodWidth/
MapHeight and PlateBoundaryBuilder.Normal. The already adapted modern sampler
retains its three octaves, cubic interpolation, amplitude halving and fixed
amplitude normalization; no duplicate noise implementation is added.

| New internal Engine file under mantle/ | Responsibility |
| --- | --- |
| MantleCalibrationParameters.cs | Validate candidate integer frequencies, float weights and positive Vref; no approved profile/default. |
| MantleNoiseFields.cs | Adapt physical centers into the existing sampler domain, with an isolated experimental seed stream; retain two private double arrays. |
| MantlePotentialField.cs | Combine immutable Influence/signals into private canonical float potential; provide controlled owned-value fixtures. |
| MantleConvectionField.cs | Canonical paired all-edge flux accumulation, immutable four-channel queries and derived conservation diagnostics; no LayerData publication. |

The new Model A combination, paired graph fluxes and error diagnostics have no
compatible legacy mantle implementation to reuse. They are newly implemented
exactly within the approved conceptual model. Fixed division by weight sum is
part of Model A; per-world relief normalization is excluded. No Landmasses,
Elevation, Landforms, crust, temperature, pressure, State, Saves or GPU inputs.
No package, project, public executor or public context mutator is added.

## Exact experimental numerical and seed policy

Physical centers are the approved float Vector2 coordinates. Sample at
`xSampler=(double)center.X/PeriodWidth*Columns` and
`ySampler=(double)center.Y/MapHeight*Rows`, using the existing double sampler.
Offset column is never substituted for physical X. X wraps through the existing
periodic lattice; Y remains open. Frequencies obey positive broad < regional
and the sampler's frequency <= int.MaxValue/4 limit. Noise is validated in [-1,1].

Promote stored Influence floats and candidate floats before arithmetic:

```text
z = (double(Extension)-Compression) / ((double(Vref)+Extension)+Compression)
numerator = (double(wt)*z + double(wb)*Nb) + double(wr)*Nr
denominator = (double(wt)+wb)+wr
phi = float(numerator/denominator)
```

Validate finite nonnegative weights, positive double weight sum, finite positive
Vref and nonnegative finite C/E; reject invalid inputs, zero sums and unsupported
frequencies. Store only finite phi in [-1,1], canonicalizing zero. No clamping
or map-dependent rescaling masks an invalid result. Vref is diagnostic
cm/year-equivalent; all four mantle channels are dimensionless proxies.

Candidate domain: ASCII bytes `57 47 30 31 4D 4E 54 31` (`WG01MNT1`), explicitly
little-endian ulong **0x31544E4D31304757**. Construct a fresh current SplitMix64
stream with `DeriveSeed(request.Seed,Tectonics) XOR domain`; its first draw
seeds Nb and second seeds Nr. No draw is made from the original stage stream.
Independent packing/stream checks cover seeds 0,1,-1,long.MinValue,long.MaxValue;
the original three R/M draws remain unchanged. This is a candidate seed policy,
not a frozen Mantle revision. World type does not enter this calculation.

Traverse CellId ascending, then the existing North/NE/SE/S/SW/NW direction order,
handling only i<j. Reuse the approved periodic unit normal for every real edge,
including same-plate edges. Compute `f=double(phi[i])-phi[j]`, `x=f*n.X`, `y=f*n.Y`;
add f/-f to W and the same x/y to both endpoint U/V accumulators. Accumulate
in separate double arrays; cast each final value once to float and canonicalize
zero. No fake polar edges, reverse-edge duplicates or change to plate motion.
Positive W denotes compensating upwelling, negative W downwelling; U/V are
map-direction proxies (east/south), not physical velocity or fluid-solver output.

## Candidate matrix: all provisional

All values below are experimental, local to the test harness. Integer weights
1/2/4 have exact float words 3F800000/40000000/40800000; zero is 00000000.
All Vref values are exactly 1 (3F800000). The matrix varies one structural
choice at a time rather than searching a large parameter space.

| Candidate | Broad / regional frequency | wt / wb / wr | Purpose |
| --- | --- | --- | --- |
| TECTONIC-DOM | 2 / 6 | 4 / 1 / 1 | Two-thirds forcing weight; retains interior noise. |
| BALANCED | 2 / 6 | 2 / 2 / 2 | Equal normalized contributions, comparison anchor. |
| BROAD-DOM | 2 / 6 | 1 / 4 / 1 | Broad-pattern predominance. |
| REGIONAL-DOM | 2 / 6 | 1 / 1 / 4 | Regional-pattern predominance. |
| TECTONIC-ONLY | 2 / 6 | 1 / 0 / 0 | Control isolating frozen Influence response. |
| NOISE-ONLY | 2 / 6 | 0 / 1 / 1 | Control isolating multiscale anomalies. |
| LOW-FREQUENCY | 1 / 3 | 2 / 2 / 2 | Balanced weights, longer spatial wavelengths. |
| HIGH-FREQUENCY | 4 / 12 | 2 / 2 / 2 | Balanced weights, shorter spatial wavelengths and alias-risk probe. |

Two base scales and a 3:1 frequency ratio give separable broad/regional roles;
weights expose directional sensitivity without changing other inputs. Vref=1
sets an explicit diagnostic reference, not an approved physical constant. No
Vref sensitivity sweep is claimed. Frequencies are cycles in the normalized
domain; the sampler's highest octave is 4*frequency. At Small, the high-frequency
case reaches 48 cycles per normalized Y height, about 2.09 vertical row samples
per shortest wavelength. The standard regional case has about 4.19. This is
an analytical sampling-risk observation, not an alias-free certification.

## Conservation and error budget

Use u=2^-53 and gamma(k)=k*u/(1-k*u). For each represented edge f the opposite
vertical contributions cancel exactly in real arithmetic, regardless of the
rounding used to form f. Each vertex accumulates at most six terms. Let
A=2*sum_edges(abs(f)), D=sum_cells(abs(Wdouble)), F=sum_cells(abs(Wfloat)), and
R=sum_cells(abs(double(Wfloat)-Wdouble)). Then conservative bounds on the
**computed sequential double sums** are:

```text
Bdouble = gamma(6)*A + gamma(N)*D
Bfloat  = gamma(6)*A + R + gamma(N)*F
```

The standard accumulation bound follows by bounding the product of at most k
rounding factors by gamma(k). Positive sums/products and subtraction-error
magnitudes are outward-rounded with Math.BitIncrement; gamma's denominator is
rounded downward. Thus computed metadata does not silently underestimate the
bound. Underflow contributions are conservatively covered by outward steps.
These are numerical error bounds, not tunable geological tolerances. Ideal
per-component bound 12 follows from degree <=6 and |phi difference|<=2; validation
allows its next float to cover final rounding, not a calibrated physical cap.

E3 MINOR-01 correction: ideal paired cancellation does not imply an exactly
zero computed global double sum. Medium / seed 9223372036854775807 /
TECTONIC-ONLY records DoubleBalance=1.7763568394002505E-15, within its derived
budget 2.0063475155697538E-08. Finite accumulation can leave nonzero residuals.
This documentary correction does not change the calculation or original CSVs.

| Size | Maximum absolute float residual | Float budget range across candidates/seeds | Maximum budget fraction used |
| --- | ---: | --- | ---: |
| Small | 1.640e-6 | 7.889e-7 .. 4.964e-5 | .06738 |
| Medium | 2.117e-6 | 4.372e-7 .. 6.884e-5 | .06164 |
| Large | 2.687e-6 | 2.960e-7 .. 7.446e-5 | .06149 |

Ranges are across different cases; each residual is checked against its own
budget, not against the minimum budget in another row/candidate. No residual
exceeded its derived case bound. Double budget maxima S/M/L were
6.880e-9 / 2.216e-8 / 4.878e-8. Full signed balances, R bounds and metadata are
retained in [results.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/results.csv).

## Executed oracles and coverage

Tectonics passes **27/27 groups**, preserving the previous 24 and all R/M/
Influence compatibility checks. New E-T05/E-T06 checks exercise actual private
components without calling the same mantle builder as expected-value logic:

- Independent explicit Offset neighbor enumeration, canonical unique pair set,
  float-center reconstruction and periodic unit normals; complete expected U/V/W
  accumulation with exact final float-bit comparison. The oracle preserves
  specified order while independently constructing geometry/edges.
- A second per-vertex divergence calculation uses all incident neighbors in its
  own order and a rounding bound (cast error plus 32*u*sum(abs(terms)), covering
  the two at-most-six-term sums). Vertical edge antisymmetry, exact edge totals,
  pole/seam behavior and derived global budgets are checked.
- Complete potential combination comparisons use independent scalar expression
  evaluation. The unchanged sampler itself is reused and protected by prior
  frozen tests; a second competing noise implementation is not introduced.
- Invalid frequencies/defaults/weights/zero sum/Vref/nonfinite signals, exact
  potential endpoint bounds, evaluation order, zero canonicalization, constant
  potential, convergent/divergent forcing, isolated signed anomalies, noise-only
  forcing without tectonic signal, input ownership and malformed output injection.
- Seed-domain byte order and independent SplitMix64 first/second draws over five
  signed/extreme seeds. Twenty broad-signal center/seam probes at frequency 2
  use a conservative 256*binary64-epsilon guard for coordinate/interpolation
  roundoff; this sampled signal check is not an exhaustive aliasing proof.

Complete candidate tests cover **96 maps** (3 sizes x 4 seeds x 8 candidates):
4,640,000 cells, 13,920,000 U/V/W float comparisons plus complete potential
checks, and 13,862,400 canonical edge instances. Replays compare every cell
between fresh Pangea and RealEarth requests. Other world types retain prior
R/M/Influence exhaustive coverage; new Mantle replay explicitly tests these
two types, not a claimed four-type matrix. Seed variation is measured and
recorded separately from repeatability. No target/edge sampling substitutes
for the complete potential/flux oracles.

Structural production validation checks dimensions, finite/range/canonical-zero
fields, edge counts, balance diagnostics and recomputed stored W sum. It does
not claim to recompute noise or the complete potential/flow algorithm at a
future untrusted codec boundary. Such a boundary is not implemented here;
independent complete oracles provide the present semantic evidence.

## Measurements and spatial distributions

[Environment](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/environment.json):
.NET 10.0.13, Windows 10.0.26200, x64 OS/process, Intel64 Family 6 Model 154
Stepping 3, GenuineIntel, 16 logical processors, Stopwatch 10 MHz. Release,
single thread, DOTNET_TieredCompilation=0; no dedicated-machine guarantee.

Seeds 0,-1,long.MinValue,long.MaxValue; 96 cases, one full warmup followed by
three measured batches, one complete operation per phase. Raw evidence:

- [timings.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/timings.csv):
  1,536 rows, 384 warmup and 1,152 measured; signals, potential, edges and structural
  validation separately. Upstream R/M/Influence generation is excluded.
- [distributions.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/distributions.csv):
  1,152 measured rows, all four channels with min/max/mean/RMS and lower empirical
  order statistics at floor(q*(N-1)) for q=.01,.5,.99.
- [results.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/results.csv):
  288 rows: full four-channel replay hashes, conservation, up/down proportions,
  interior population/potential RMS and Pearson correlation with z.
- [summary.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/raw/summary.csv):
  per-size/candidate/phase min/conventional median/max over twelve measured
  operations (four seeds x three batches), allocation and signed retention ranges.

Below, RMS/correlation/proportions are means over four seeds at Large, using
batch 1 only because repeated batches have identical fields. All channels
remain dimensionless and no per-map normalization is performed.

| Candidate | Potential RMS | FlowX RMS | FlowY RMS | W RMS | Interior potential RMS | phi/z correlation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| TECTONIC-DOM | .18143 | .08463 | .08967 | .09236 | .06426 | .94573 |
| BALANCED | .14660 | .04392 | .05037 | .04629 | .12852 | .55139 |
| BROAD-DOM | .19071 | .02316 | .02893 | .02315 | .19271 | .22820 |
| REGIONAL-DOM | .18929 | .03247 | .05070 | .02396 | .19066 | .12199 |
| TECTONIC-ONLY | .26365 | .12686 | .13346 | .13853 | 0 | 1.00000 |
| NOISE-ONLY | .18749 | .01967 | .03588 | .00483 | .19278 | -.11056 |
| LOW-FREQUENCY | .16170 | .04271 | .04620 | .04618 | .13015 | .61763 |
| HIGH-FREQUENCY | .15647 | .05025 | .06497 | .04784 | .12798 | .56575 |

Interior means boundary distance greater than the complete Influence reach;
C/E are therefore zero there. Counts range S 9,013..9,448; M 20,401..21,669;
L 37,428..39,973. Nonzero interior RMS in noise-weighted candidates demonstrates
modeled intraplate anomalies; TECTONIC-ONLY is identically zero there. This is
not a hotspot, crust or temperature classification. Negative sample correlation
in NOISE-ONLY is incidental in four worlds, not coupling back into tectonics.

Large up/down percentages were near 50/50 for all noise-weighted candidates
(up 49.70..50.21%). TECTONIC-ONLY had 27.36% up and 27.29% down, leaving quiet
cells. Equal global balance does not require equal positive/negative cell counts.
For BALANCED, complete extrema across the four Large worlds were phi
[-.54660,.55856], X[-.40755,.35404], Y[-.46667,.41301], W[-.45618,.45457].
TECTONIC-ONLY's Small W extrema reached [-2.49813,2.49214]; raw distributions
preserve every candidate/size/channel range and quantile.

### Resolution/frequency limitations

| Candidate | W RMS Small / Medium / Large |
| --- | --- |
| BALANCED | .09586 / .06264 / .04629 |
| TECTONIC-DOM | .19020 / .12482 / .09236 |
| BROAD-DOM | .04801 / .03134 / .02315 |
| REGIONAL-DOM | .05350 / .03315 / .02396 |
| TECTONIC-ONLY | .28514 / .18719 / .13853 |
| NOISE-ONLY | .01847 / .00847 / .00483 |
| LOW-FREQUENCY | .09510 / .06241 / .04618 |
| HIGH-FREQUENCY | .10478 / .06599 / .04784 |

The unscaled graph difference/divergence operators are not resolution-invariant
physical derivatives: finer samples of the same normalized noise reduce edge
differences and vertical divergence. No hidden h or h-squared correction was
introduced. Normalized-domain frequencies also do not guarantee physically
isotropic wavelengths on the actual aspect ratio; noise-only Y/X RMS differs.
HIGH-FREQUENCY increases short-scale gradients and approaches the Small sampling
limit. Neither resolution behavior nor aliasing is considered calibrated away.

## Performance and memory

BALANCED median milliseconds [minimum,maximum] over twelve measured runs:

| Phase | Small | Medium | Large |
| --- | --- | --- | --- |
| Two signals | 6.741 [6.212,7.142] | 12.991 [10.997,15.764] | 19.576 [18.817,22.174] |
| Potential | .268 [.257,.369] | .545 [.466,1.160] | .830 [.781,.989] |
| Edge accumulation + balance diagnostics | 5.525 [5.215,6.357] | 11.511 [10.089,15.880] | 18.286 [17.121,19.713] |
| Structural/balance validation | .253 [.244,.553] | .535 [.439,1.081] | .884 [.826,1.075] |

All candidates compute both signals, even when a control weight is zero, so
control timings do not silently use a different optimized path. Large signal
medians across the eight candidates were 19.576..21.392 ms; individual raw
observations extended to 36.730 ms. No performance budget or universal threshold.
Exact cell counts are 20,000/45,000/80,000 and edge counts 59,600/134,400/239,200.

| Phase | Allocated bytes per run S / M / L | Large retained-GC interpretation |
| --- | --- | --- |
| Signals | 320920 / 720920 / 1280920 | About 16N signal data plus object overhead; request creation/validation included. |
| Potential | 80048 / 180048 / 320048 | About 4N potential data. |
| Edges/diagnostics | 800176 / 1800176 / 3200176 | About 16N retained tuple rows; three temporary double arrays add 24N allocation. |
| Validation | 0 / 0 / 0 | Signed small GC noise, not an independent retained result. |

The current runtime's measured tuple-row allocation is 16N despite three
logical floats, separate from 4N potential storage. Combined retained mantle
values therefore use about 20N bytes in this prototype; no 16N compact-output
claim is made. A future compact storage review can address representation
without changing the mathematical model, but no new publication layout is
implemented by CAL01. Noise/earlier fields and future packing copies coexist;
phase deltas are not peak heap, process memory or proof of absence of leaks.

GC before/after operations is outside timers; allocated-byte deltas are
current-thread. Delegates are constructed before timing. Distribution sorting,
hashing, row objects and file output are outside measured windows. A reused,
warmed empty delegate recorded .0002 ms, zero allocated bytes and -40 retained
bytes; no subtraction is applied. Validation timing is structural/balance only;
independent all-edge test oracles are not included in those timing numbers.

## Regression evidence and next decisions

[Tectonics log](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/validation/tectonics.txt):
27/27 groups, including all fifteen B, fifteen M and fifteen Influence buffers,
all reference manifests, 75 archived R and 75 M cases and sixty complete
Influence size/seed/world-type cases. Grid's 1,160,000 geometry plus 1,160,000
Grid API BFS comparisons pass; Laboratory 19/19 and menu 16/16 pass. Engine
and Game Release builds have zero warnings/errors. No critical validation failed.
No GUI session was required or executed for this headless calibration.

The [file inventory](../../../../tests/worldgen/tectonics/experiments/mantle-cal01/delivery-files.csv)
enumerates additions/modifications. Protected-path scope, local links, source
headers, numeric row counts, replay hashes and Git whitespace are checked before
publication. Raw hashes document experiments, not frozen Mantle golden vectors.
Historical evidence and all accepted numeric source/fixture paths remain intact.
The final inventory contains 29 paths, with no deletions. Checks verify 84 local
Markdown targets and header/comment conventions for eight affected C# files.
VERSION and the compiled Game display metadata are v0.0.46. Seven new console
logs are UTF-8 transcriptions with identical decoded contents; no old log is
rewritten. All 96 repeated hashes are stable and each size/candidate has four
distinct tested-seed results.

**Review shortlist, not a selection:** compare TECTONIC-DOM, BALANCED and BROAD-DOM
to decide the intended balance between boundary forcing and interior structure;
keep REGIONAL-DOM and frequency controls visible for sensitivity review.
TECTONIC-ONLY/NOISE-ONLY are explanatory controls. Decide resolution semantics,
directional anisotropy, acceptable sampling density, Vref sensitivity and output
representation before any definitive profile decision. Four seeds and one
recorded platform do not establish geological realism or cross-runtime bit identity.

The owner's E3 authorization supplies the current frozen-input status; prior
E2 documents remain historical review records rather than being rewritten to
invent an audit verdict. MINOR-01 stays for E4; GATE-01 remains N01 owner-declared
PASS and N02-N17 PENDING. E3-FINAL, E4/E5, LAB views, Layer 02, branch integration
and final Layer 01 closure remain outside this delivery. Stop after publication
for architectural review, independent audit and owner parameter selection.
