# WG-01-E3-CAL02: Complementary spatial calibration

**Experimental delivery; no Mantle policy or profile selected/frozen.** Base
world-tectonics, v0.0.46 / aadfb5775cdb95c449beb8b18fd922d59af19b8f, verified
clean before changes. Delivery v0.0.47. The owner selected BALANCED only as
the conceptual direction. No E3-FINAL/E4/E5, production activation, Layer 02,
merge or Layer 01 closure is implemented or implied.

## Preservation and implementation boundary

All new numerical code is **test-project-only**: MantleSpatialAlternatives,
MantleSpatialDesign, MantleSpatialChecks and MantleSpatialRunner. No Engine
C# source, sampler, seed generator, Grid, geometry/normal method, R/M/Influence
profile/revision or canonical CAL01 builder changed. The test project copies
the original CAL01 result CSV for read-only comparison; no old CSV is rewritten.
Every affected file is listed in the
[inventory](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/delivery-files.csv).
It contains 29 paths and explicitly labels the E3 MINOR-01 paragraph correction.
Final checks verify 72 local Markdown targets, header/comment conventions in
five affected C# files, raw row counts and balance/replay invariants. Eight new
console logs are UTF-8 transcriptions with identical decoded contents. Root
VERSION and compiled Game display metadata are v0.0.47; no files are deleted.

R0 calls the original builder. A0 calls the original signal builder and original
potential combination. Full hashes match **all twelve BALANCED worlds**, plus
the same twelve TECTONIC-DOM and BROAD-DOM worlds: 36 exact archived controls.
The LOW/HIGH frequency controls also match their twelve CAL01 worlds each,
for 60 complete historical hashes checked in the final evidence review.
The existing modern sampler is the approved historical adaptation; no competing
noise implementation, smoothing, uniformization or per-world normalization is
introduced. The [historical review](e3-history-review.md) remains applicable:
no historical mantle circulation is invented.

Alternative typed flow rows retain three floats separately from the immutable
potential, within test-only results. New helper factories never publish
LayerData, change stage state or modify old results. Normal Tectonics remains
schema 1/features 7, with Influence/Mantle publication absent.

## Declared design and exact resolved inputs

[Pre-execution design](e3-cal02-design.md) specifies formulas, units, independent
oracles, controls, seeds and criteria. Its bounded analytic field was in the
initial test code; the fixed /1.5 amplitude divisor is not fitted to outcomes.
The runner first surveys frozen inputs and persists the resolved
[design.json](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/design.json)
before executing alternatives. Seeds are 0,-1,long.MinValue,long.MaxValue;
all three accepted sizes. Fifteen cases per world, with one-factor comparisons
and two prespecified interaction probes, rather than a Cartesian search.

| Cases | Variable isolated; all unspecified settings remain CONTROL |
| --- | --- |
| CONTROL | Original BALANCED, A0, R0, frequencies 2/6, weights 2/2/2, Vref=1. |
| R1-NATIVE, R2-NATIVE | Geometric operator, native length units. |
| R1-WIDTH, R2-WIDTH | Geometric operator, explicitly width-one length units. |
| A1 | Spatial frequency interpretation only. |
| LOW, HIGH | Frequencies 1/3 or 4/12 only. |
| VREF-Q25/Q50/Q75 | Derived Vref only, BALANCED weights/frequencies unchanged. |
| TECTONIC-DOM, BROAD-DOM | Exact CAL01 weight controls 4/1/1 and 1/4/1. |
| COMBINED-A1-R2 | A1 plus R2 in width-one units. |
| COMBINED-A1-LOW-R2 | Same interaction plus low frequencies; exploratory, not an accepted replacement. |

Candidate seed policy remains the existing experimental WG01MNT1 domain,
with broad/regional draws ordered and isolated from R/M. Float/double potential
arithmetic remains CAL01's exact sequence. All candidate weights and frequencies
remain experimental; no seed policy or profile is newly frozen.

## RES-01: geometric meaning before RMS comparisons

Let s be 1 in native units, or 1/PeriodWidth in width-one units. Let ell=s,
A=(3*sqrt(3)/2)*s*s, d=periodic distance between accepted float centers times s,
f=double(phi_i)-phi_j and n the unchanged approved unit normal. Each edge is
handled once, in original CellId/direction order with i<j.

| Operator | Horizontal term at both endpoints | Opposite vertical terms | Interpretation |
| --- | --- | --- | --- |
| R0 | f*n | f | Original dimensionless graph proxies. |
| R1 | (f/(3*d))*n | f/(3*d) | Negative-gradient reconstruction in the regular interior; W is a summed gradient proxy, not a true divergence. |
| R2 | (f*ell/(2*A))*n | f*ell/(A*d) | Equal-area orthogonal finite-volume negative gradient/Laplacian. |

Implementation computes each reciprocal coefficient first, multiplies by f,
then multiplies the horizontal value by n. Record exact parentheses in the
source/oracles; no reassociation with the unmodified R0 expression.
R1 channels have potential/length units. R2 U/V have potential/length and W
potential/length squared. These are arbitrary declared coordinate units, not
physical mantle velocity, pressure or geophysical length calibration.

The factor 3 follows from six regular directions with sum(n*n^T)=3I. R2 uses
regular full hex cell areas and shared face lengths, while respecting actual
float-center distances/normals. Float coordinate perturbations are not silently
treated as a new Voronoi mesh. Polar missing faces represent closed external
flux; interior derivative consistency is not claimed at polar boundaries.

### Controlled same-function experiment

The same continuous bounded field `(sin(2*pi*x)+.5*cos(2*pi*y))/1.5` is sampled
at every size in width-one coordinates. There is no plate/noise realization
change in this control. Native physical extents differ; normalized Y extent
also changes slightly through the approved Rows+.5 height. Derivative errors
are measured on rows excluding the first/last two; all cells still participate
in edge/conservation checks.

R2-WIDTH errors against known negative gradient/Laplacian:

| Size | Horizontal vector RMS error | Vertical RMS error | Maximum error / derived Taylor bound |
| --- | ---: | ---: | ---: |
| Small | .000540125 | .00301495 | .03119 |
| Medium | .000239839 | .00545948 | .09507 |
| Large | .000134995 | .00949260 | .18349 |

Horizontal consistency improves, while second derivatives amplify stored
potential float quantization as spacing shrinks. The independent bound includes
actual stencil first/second moments, analytic third-derivative remainder,
measured input rounding, final float ULP and arithmetic roundoff. It does not
hide growth in vertical error with an arbitrary geology tolerance. Transcendental
reference values are evaluated on the recorded runtime; no cross-platform
accuracy theorem for Math.Sin/Math.Cos is claimed.

All fifteen operator/size control rows, including native-unit errors, are in
[controlled-resolution.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/controlled-resolution.csv).
Those controls isolate operator behavior; the real worlds below do not.

### Real-world comparison

Means over four seeds, batch 1 only (repeated fields are identical):

| Operator | W RMS Small / Medium / Large | Large X / Y RMS |
| --- | --- | --- |
| R0 | .09586 / .06264 / .04629 | .04392 / .05037 |
| R1-NATIVE | .01845 / .01206 / .00891 | .00845 / .00969 |
| R2-NATIVE | .02130 / .01392 / .01029 | .00845 / .00969 |
| R1-WIDTH | 5.53464 / 5.42501 / 5.34468 | 5.07094 / 5.81661 |
| R2-WIDTH | 1917.254 / 2818.916 / 3702.904 | 5.07094 / 5.81661 |

Native center spacing stays about sqrt(3) as map dimensions increase, so
native R1/R2 largely change constants/units rather than cure resolution effects.
Width-one R1's relatively similar RMS is not evidence that its W is a correct
divergence. Width-one R2's larger real-world W shows the importance of changing
plate realizations, boundary roughness, stored-potential quantization and units.
Do not infer convergence or physical suitability from aggregate RMS similarity.
None of these operators replaces R0.

### Limits and conservation

For represented scaled vertical edge terms q, use Aflux=2*sum(abs(q)), at most
six additions per cell, D=sum(abs(Wdouble)), F=sum(abs(Wfloat)), and observed
float rounding bound R. The same mathematical derivation gives
`Bdouble=gamma(6)*Aflux+gamma(N)*D` and
`Bfloat=gamma(6)*Aflux+R+gamma(N)*F`, with outward-rounded positive arithmetic.
Bounds therefore track coefficients/units rather than reuse an R0 epsilon.
U/V component limits are 12*max(horizontal coefficient); W uses 12*max(vertical
coefficient), with a final-float outward allowance. Areas are equal within each
case, so ideal unweighted and area-weighted vertical sums cancel; no per-degree
renormalization breaks antisymmetry.

Maximum measured absolute float residuals by operator were R0 2.511e-6,
R1-NATIVE 1.541e-7, R2-NATIVE 2.105e-7, R1-WIDTH 8.217e-5 and R2-WIDTH .04539.
Each is checked in its own units against its own budget. Maximum fractions of
budget used were .06633/.03544/.03959/.01890/.02971 respectively. No critical
balance check failed. Raw signed balances and budgets accompany every metric row.

## ANISO-01: A0/A1 comparison

A0 is unchanged: xSampler=X/P*Columns, ySampler=Y/H*Rows. A1 changes only the
Y denominator to P. Thus a frequency f means native wavelengths P/f on both
axes under A1, versus P/f and H/f under A0. Horizontal periodicity and open
polar sampling remain intact. No new projection or second production topology.

The effective aspect P/H is about 1.723..1.728 across sizes; A0's normalized
axes therefore do not imply equal physical wavelengths. A1 is an alternative
interpretation, not a correction declaring historical anisotropy wrong.

At Large, A0 X/Y RMS were .04392/.05037 and A1 .04391/.04632. A1 potential RMS
was .15832 versus .14660, with paired potential delta RMS .14545; these are
substantial pattern changes even when flow aggregate ratios look closer to one.
Interior potential RMS changed from .12852 to .13141. Fewer vertical cycles,
open boundaries and different noise phases alter realizations. The owner's
historical-pattern preference cannot be satisfied merely by choosing the most
isotropic-looking aggregate. A0 remains the exact control.

## ALIAS-01: octaves, shifts and proposed screen

The unchanged sampler uses frequencies f,2f,4f. The
[sampling table](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/sampling.csv)
contains all three octaves for seven frequencies and both policies at all sizes
(126 rows). A frequency-24 diagnostic exercises insufficient sampling; it is
not an additional candidate or a change to the sampler.

The proposed screen uses maximum directional envelope advance
`max(|dx|*kx+|dy|*ky)` over real hex edge orientations. Advance >=.5 is
INSUFFICIENT; .25<advance<.5 is WARNING; <=.25 has a twofold Nyquist margin for
the modeled octave envelope. Axis counts alone can overlook diagonal modes.
The quarter-cycle worst-direction advance is an explicit review margin, not an approved production
frequency budget. Cubic interpolated value noise is not strictly band-limited,
so passing this screen does not prove absence of aliasing.

Worst regional octave axis samples X/Y (Small):

| Pair | A0 | A1 | Screen Small / Medium / Large |
| --- | --- | --- | --- |
| 1/3 | 16.667 / 8.375 | 16.667 / 14.434 | Margin / Margin / Margin for both policies |
| 2/6 | 8.333 / 4.1875 | 8.333 / 7.2169 | Margin / Margin / Margin for both policies |
| 4/12 | 4.167 / 2.09375 | 4.167 / 3.60844 | Warning / Warning / Margin for both policies |

Subcell offsets are zero, quarter-column .375, quarter-row sqrt(3)/4, and both.
Influence is held fixed at original cells: this probes noise/combined-potential
phase sensitivity, not an invented subcell interpolation of tectonics. Across
four seeds and the three nonzero shifts, Small A0 W-delta RMS means were
.0003335 / .0020038 / .0118871 for low/balanced/high. A1 gave .0001187 /
.0007109 / .0044550. Increased high-frequency phase sensitivity is measurable,
but phase sensitivity alone is not a unique diagnosis of aliasing. Maximum
measured seam-probe difference was 6.107e-16. All four channels/differences,
hashes and the 288 shift rows are preserved in
[shifts.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/shifts.csv).
No candidate is declared alias-free or rejected by finiteness alone.

## Vref: measured sensitivity without changing Influence

The twelve frozen C+E surveys had 45.065..49.966% zero cells. Positive-cell
Q25/Q50/Q75 are measured per world, then their twelve-world medians are cast
once to float, avoiding size-weighted domination by Large. These are proposed
coupling scales, not new amplitudes or physically fitted constants.

| Label | Exact round-trip float | Binary32 word |
| --- | ---: | --- |
| Reference | 1 | 3F800000 |
| VREF-Q25 | .046172723 | 3D3D1F9C |
| VREF-Q50 | .9367168 | 3F6FCCAC |
| VREF-Q75 | 4.445543 | 408E41E3 |

Large four-seed means at fixed BALANCED 2/2/2, A0, R0:

| Vref | z RMS | \|z\|>=.9 percent | phi/z correlation | Boundary mean \|z\| | W RMS |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | .26365 | .2431 | .55139 | .43905 | .04629 |
| Q25 | .38418 | 7.6969 | .70681 | .50287 | .07960 |
| Q50 | .26745 | .3563 | .55758 | .44264 | .04709 |
| Q75 | .16280 | 0 | .34980 | .31083 | .02769 |

The .9 saturation label is an analysis band only, not a model clamp or approved
acceptance threshold. Signed z quantiles, deformation correlations and all-channel
ranges are retained. Q25 strengthens coupling into weak tails; Q75 attenuates
it; Q50 is close to the reference. All four variants have the identical far-
interior potential RMS .12852058, because C=E=0 beyond the complete Influence
reach and the noise/weights there are unchanged. No Vref winner is selected.

## Combined cases and interpretation

A1 plus R2-WIDTH gave W RMS 1904.686/2809.928/3696.009 across sizes. With low
frequency it gave 1901.175/2808.093/3694.295. Similar aggregate values do not
justify choosing either combination: boundary forcing/realization and the
different operator units dominate. The combinations are prespecified interaction
probes only, not a hidden parameter search or selection.

## Oracles, coverage and regression results

Tectonics passes **30/30 groups**, retaining all 27 earlier groups. New tests
cover the common continuous field/constant field, exact geometric operator
oracles, ownership, invalid policies/inputs, octaves/shifted seams and the full
fifteen-case real-world matrix. R1/R2 comparisons independently enumerate Offset
edges, reconstruct float centers/normals, evaluate coefficients and compare
every output float word; R0 retains the prior complete CAL01 oracle plus exact
archived hashes. No scalar coefficient helper or builder is recalled as the
expected geometric result.

Real tests cover 180 configurations (3 sizes x 4 seeds x 15 rows) and repeat
every full result. Exact geometric all-cell oracles cover the four R1/R2 rows
and two combined R2 rows: 72 real configurations, 3,480,000 cells plus their
three flow channels. Controlled tests add all five operators at each size.
Sampling coordinate/periodicity tests are explicitly sampled: 3 sizes x 2
policies x 4 frequencies x 4 cells; shift metrics compare all cells, while seam
roundoff probes use four cells per shifted case. Taylor controls compare
interior derivatives only; topology/conservation checks still include poles.

All 45 frozen B/M/Influence buffers and manifests pass existing full regressions.
Grid passes 1,160,000 geometry + 1,160,000 Grid API BFS comparisons; Laboratory
19/19, menu 16/16. Release Engine, Game and check project builds report zero
warnings/errors. Normal publication stays schema 1/features 7. No GUI/GPU
validation is claimed for this test-only change.

## Runtime and memory evidence

Environment in design.json: .NET 10.0.13, Windows 10.0.26200, x64, Intel64 Family
6 Model 154 Stepping 3/GenuineIntel, 16 logical processors; Stopwatch 10 MHz,
single-threaded Release, tiering disabled. No dedicated-machine guarantee.
One warmup then three measured batches per configuration yields 2,880 phase
rows (720 warmup, 2,160 measured), 540 paired metric rows, 288 shift rows and
15 controlled-resolution rows. Upstream surveys/generation, scalar analysis,
hashing and file output are outside measured phase windows.

Large medians over twelve measurements per case/phase:

| Case | Signals ms | Potential ms | Edges ms | Validation ms |
| --- | ---: | ---: | ---: | ---: |
| CONTROL | 18.136 | .789 | 16.062 | .837 |
| R2-WIDTH | 18.040 | .792 | 24.279 | 1.221 |
| A1 | 18.638 | 1.047 | 16.280 | .812 |

CONTROL edge observations ranged 15.086..52.490 ms, so medians are not a
universal budget. Exact min/median/max and signed retention ranges for every
case are in [summary.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/summary.csv);
[timings.csv](../../../../tests/worldgen/tectonics/experiments/mantle-cal02/raw/timings.csv)
retains per-seed/batch variation. GC runs before/after outside the timer and
allocations use current-thread deltas. Delegate creation, analysis and row
serialization are excluded. Retained deltas are noisy GC observations, not
peak heap, process memory or leak proof.

Large per-operation allocation: CONTROL signals 1,281,232 bytes, potential
320,048, edges 3,200,536, validation zero. R2-WIDTH edges allocate 2,880,456
bytes and retain about 960,000 bytes of compact three-float rows, plus the
already owned 320,000-byte potential. R0's original tuple rows retain about
1,280,000 bytes plus potential. A1 potential allocates 640,072 bytes because
the experimental adapter uses the existing defensive FromValues factory;
it does not change the canonical implementation to eliminate that temporary
copy. Extra geometric distance work also contributes to R2's edge time.

The compact experimental rows demonstrate a possible temporary representation
independently of which operator is selected. They do not approve a production
schema/ownership change. Future packing copies, input arrays and simultaneous
historical views are not included in per-phase retention interpretations.

## E3 MINOR-01 and outstanding decisions

Only the incorrect double-zero paragraph in `e3-calibration.md` is corrected:
Medium / long.MaxValue / TECTONIC-ONLY had DoubleBalance
1.7763568394002505E-15 within 2.0063475155697538E-08. Ideal cancellation and
computed finite sums are distinguished. No CAL01 calculation or raw CSV was
changed. The inventory identifies this documentary correction explicitly.

Recommendations for review, **not automatic selections**:

- Preserve R0/A0/BALANCED as the exact historical-behavior reference. R1/R2
  native units do not solve resolution by themselves; width units require an
  explicit semantic decision, and R2 amplifies float quantization.
- A1 supplies equal-axis wavelength interpretation but changes patterns
  substantially. Closer RMS ratios are not evidence that A0 was wrong.
- Low/balanced frequencies have more sampling margin; high needs Small/Medium
  warnings under this proposed screen. No cubic-noise alias-free guarantee.
- Keep Vref=1 and measured Q50 as close comparison points; Q25/Q75 expose
  stronger/weaker coupling. The owner must choose any definitive value.
- Review compact temporary storage separately from mathematical selection.

Only four measured seeds and one platform are covered. No physical calibration,
cross-runtime bit identity, new frozen Mantle vectors or performance budget is
claimed. MINOR-01 reader compatibility remains for E4, and GATE-01 remains
partially open. Stop after CAL02 for architectural review, independent audit
and owner decisions; no E3-FINAL/E4/E5, Layer 02 or branch merge is authorized.
