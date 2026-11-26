# E3-CAL02: Experiment declared before execution

Baseline v0.0.46 / aadfb5775cdb95c449beb8b18fd922d59af19b8f, clean
world-tectonics. All new numerical alternatives live exclusively in the
headless test project. Canonical Engine code, sampler, RNG, geometry, normals,
R/M/Influence and CAL01 CSVs remain unchanged. BALANCED is the conceptual
direction, not an accepted numerical profile. See [report](e3-calibration-02.md).

## Operators and units

Let P=PeriodWidth, s the length-unit conversion, ell=s the regular hex side,
A=(3*sqrt(3)/2)*s*s the full regular hex area, d the actual periodic distance
between approved float centers times s, n the approved unit normal and
f=double(phi[i])-phi[j]. Use the same canonical i<j edge order and paired signs.

| Operator | Horizontal coefficient multiplying f*n | Vertical coefficient multiplying f | Units |
| --- | --- | --- | --- |
| R0 | 1 | 1 | Original dimensionless graph diagnostics; call unchanged CAL01 builder. |
| R1 | 1/(3*d) | 1/(3*d) | Potential per length; horizontal negative-gradient reconstruction in a regular six-neighbor interior. Vertical is a summed gradient proxy, not a physical divergence. |
| R2 | ell/(2*A) | ell/(A*d) | Horizontal potential/length, vertical potential/length squared; equal-area finite-volume negative Laplacian. |

The factor 3 in R1 follows from sum of the six n*n-transposes=3I. R2 follows
from shared-edge length and cell area; its horizontal reconstruction reproduces
an affine negative gradient in a regular interior. Missing polar faces are
closed/no external flux; boundary gradient consistency is not claimed.
All cells use the same full-hex area, so paired vertical terms conserve the
global sum (and area-weighted sum) ideally. No per-degree renormalization.

Compare s=1 (native hex-side units) and s=1/P (width-one cylindrical units).
This is an explicitly declared coordinate-unit choice, not rescaling by
observed RMS. Native maps grow in physical extent as dimensions grow; native
R1/R2 alone cannot guarantee resolution-independent magnitudes. Fixed-width
R2 derives its scale from ell, A and d, rather than an empirical map-size factor.

## Geometry and sampling

A0 calls the unchanged MantleNoiseFields.Build. A1 calls the same immutable
sampler/seeds with xSampler=X/P*Columns and ySampler=Y/P*Rows (instead of
Y/H*Rows). Thus wavelengths P/f match on the two physical axes, while X remains
periodic and Y open. No new topology/projection or sampler change.

For every actual octave k=f,2f,4f record axis samples per wavelength and the
largest edge advance |dx|*kx+|dy|*ky, where kx=k/P and ky=k/H (A0) or k/P (A1).
Classify the modeled octave envelope: advance >=.5 insufficient (Nyquist
boundary), .25<advance<.5 warning, <=.25 twofold Nyquist margin. This is a
proposed verifiable base-octave screen, not proof of alias-free cubic noise,
which is not strictly band-limited. A frequency-24 diagnostic must exercise
the insufficient classification; it is not an additional Mantle candidate.

Measure noise/potential responses to shifts (0,0),(.375,0),(0,sqrt(3)/4),
(.375,sqrt(3)/4): quarter-column/quarter-row offsets in native coordinates.
Influence stays fixed at the original cells; no invented interpolation of
tectonic inputs. Record seam roundoff and shifted full-field differences.

## Isolated candidate matrix

Sizes: Small/Medium/Large. Seeds: 0,-1,long.MinValue,long.MaxValue. All weights
2/2/2, frequencies 2/6, Vref=1, A0 and R0 unless the row changes them.

| Row | Only intended change |
| --- | --- |
| CONTROL | Exact BALANCED CAL01 hash reproduction. |
| R1-NATIVE / R2-NATIVE | Operator R1/R2 with s=1. |
| R1-WIDTH / R2-WIDTH | Operator R1/R2 with s=1/P, explicitly different units. |
| A1 | Geometry policy only, R0. |
| LOW / HIGH | Frequencies 1/3 or 4/12 only. |
| VREF-Q25 / VREF-Q50 / VREF-Q75 | Vref only, derived as described below. |
| TECTONIC-DOM / BROAD-DOM | CAL01 weight controls 4/1/1 and 1/4/1, checked against archived hashes. |
| COMBINED-A1-R2 / COMBINED-A1-LOW-R2 | Prespecified interaction probes: A1 + R2 width units, optionally low frequencies; no parameter selected from apparent visual uniformity. |

Fifteen rows rather than a Cartesian parameter search. First survey the frozen
C+E distributions on the twelve real worlds. Report zero fraction and positive
cell quantiles. Derive each Vref as float(median of twelve per-world positive
quantiles at .25/.5/.75), giving every world equal weight. No aesthetic rounding;
record decimal values and binary32 words before the main experiments. Retain
Vref=1 as the separate control. Save the fully resolved design JSON before runs.

## Controlled resolution and oracles

In addition to real worlds, sample one continuous field
phi(x,y)=(sin(2*pi*x)+.5*cos(2*pi*y))/1.5 in width-one coordinates at every approved
Grid size. Its known negative gradient/Laplacian provide independent reference
metrics; compare interior rows only for derivative accuracy, all cells for
topology/conservation. No expectation of equal fields from different plate
realizations. Report truncation, input/output rounding and geometry separately.
The fixed analytic amplitude divisor enforces the existing [-1,1] potential
contract; it was present in the initial control code, not fitted to measured RMS.

Independent Offset edge enumeration, float-center reconstruction and scalar
coefficient evaluation compare every alternative output word. Constant/affine
controls, seam/poles, seed replay, invalid inputs and ownership checks apply.
Conservation bounds reuse the mathematical gamma(6)/gamma(N) derivation with
the *represented scaled vertical edge terms*, observed float rounding and
outward-rounded positive bounds. Component limits become 12*maximum edge
coefficient, not the R0 constant 12. No arbitrary geological epsilon.

Metrics: per-phase elapsed time, allocated bytes and signed retained-GC deltas;
all-channel ranges/RMS, z statistics/correlations, boundary/interior summaries,
paired differences from CONTROL, full replay hashes and per-case conservation.
One warmup then three measured batches, with analysis/output outside timers;
separate regression and benchmark execution. No performance budget or new
frozen Mantle vectors. Critical failures prevent publication.
