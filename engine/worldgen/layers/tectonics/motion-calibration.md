# WG-01-C — Motion policy decision gate

**Status:** M and this exact numerical policy were subsequently approved by the
owner in the WG-01-C finalization request. M is frozen as tectonic-motion-m-v1,
revision 1. A/B remain formally accepted. [Final C delivery](delivery-c.md).
**Branch/base:** world-tectonics, v0.0.38,
ba187fe933ad494a3b81992438d694fa40fed418.

The initial C authorization explicitly did not approve new kinematic constants. This
report makes that decision reviewable before a production implementation or
commit. Engine, Grid, CORE, the R profile/partition, Game and VERSION are
unchanged at that comparison gate. All proposed/pending/local-only statements
below preserve that historical preparation; the final C delivery supersedes
them. The numeric policy itself is unchanged. No D work starts.

## Proposed policies requiring selection

All candidates use independent uniform direction in [0,360) degrees, clockwise
with east=0 and south=90. Speeds are uniformly sampled in the intervals below;
the lower endpoint is included and the upper excluded. A and speed use cm/year;
M is dimensionless. These are stylized relative-kinematics diagnostics, not an
empirical Earth calibration, forces, stress, displacement per cell or simulation
time. Approved Grid units supply orientation only, without a cm-per-cell scale.

| Candidate | Speed cm/year | Activity A | Mixed ratio M | Controlled comparison |
| --- | --- | --- | --- | --- |
| H | [1,8) | .15 | .50 | Historical reference; previously unapproved. |
| Q | [1,8) | .50 | .50 | H with higher activity threshold only. |
| M | [1,8) | .15 | .75 | H with stricter Mixed classification only. |
| Z | [0,8) | .15 | .50 | H permitting slower plates only. No point mass at zero. |

H is a continuity proposal if the owner wants the historical diagnostics. M
provides more dominant-axis categories. Q increases Quiet edges, while Z
explores slow motion without an arbitrary stationary-plate probability. No
measured category distribution proves physical realism or determines the
owner's intended model. Other intervals/distributions remain possible and
would require another explicitly identified comparison.

## Exact proposed numeric contract

- Consume exactly the third reserved Tectonics stage draw; create its local
  DeterministicStageRandom without consuming partition/noise substreams.
  For each ascending PlateId 0..13 consume exactly two draws, direction then
  speed. Unit variate = (draw >> 11) * 2^-53. Direction = 360*u;
  speed = lower + (upper-lower)*u. No retries, ambient RNG or size/type inputs.
- Retain double direction/speed without decimal rounding or quantization.
  Radians = direction*(Math.PI/180); velocity = speed*(cos,sin), in double.
  Reject nonfinite direction/speed, directions outside [0,360), negative speed
  and nonfinite projections. Both zero signs mean zero; zero speed gives
  canonical positive-zero angle and components. Underflow to float zero is
  canonical positive zero. Tiny nonzero trigonometric components are retained;
  there is no implicit epsilon snapping at cardinal angles.
- Derive normals from accepted FlatHexGeometry.Center values, promoted from
  float to double. Correct X by one full PeriodWidth only if beyond half the
  period; retain Y. Opposite half-period ties cannot be actual neighbors in
  accepted presets. Normalize in double; tangent=(-ny,nx). Project second
  velocity minus first in double. Reverse both endpoints/normal consistently.
- Cast each component once to float (runtime IEEE-754 round-to-nearest,
  ties-to-even), reject nonfinite stored results and canonicalize signed zero.
  H/Q/M/Z activity bits are respectively 3E19999A/3F000000/3E19999A/3E19999A;
  mixed bits are 3F000000/3F000000/3F400000/3F000000. These exact stored floats,
  not decimal substitutes, determine classification.
- Promote stored components and thresholds to double before absolute values,
  comparisons and M*maximum. No classification epsilon. First Quiet when
  maximum<=A; then Mixed when minimum>=A and minimum>=M*maximum; then Transform
  when shear>axial; otherwise Convergent when normal<0, else Divergent.
  This gives explicit threshold and axis ties; preserve signed components.
- Canonical edge order is ascending (first CellId,second CellId), first<second.
  Only unlike-owner actual Grid neighbors are enumerated. No extra seam cells,
  pole links or changes to plate IDs. Influence stays absent.
- On selection, freeze a separate motion/classification revision 1 and explicit
  compatibility ID including the chosen policy, independently of schema 1 and
  frozen partition revision 2. Until approval these IDs/revision are proposals,
  not production-supported metadata. The experiment publishes no LayerData.
- Proposed reproducibility is for identical inputs and supported runtime/numeric
  policy. Math.Sin/Cos and floating-point code require executed compatibility
  vectors on other platforms before claiming cross-platform bit identity.

## Actual comparative evidence

[Tool](../../../../tests/worldgen/tectonics/experiments/MotionProfileComparison.cs),
[exact profiles](../../../../tests/worldgen/tectonics/experiments/motion-cal01/profiles.json),
[all cases](../../../../tests/worldgen/tectonics/experiments/motion-cal01/cases.json),
[raw summary](../../../../tests/worldgen/tectonics/experiments/motion-cal01/summary.csv)
and [environment](../../../../tests/worldgen/tectonics/experiments/motion-cal01/environment.json).

All 75 archived CAL01 R worlds were generated by the real accepted production
executor through WG-CORE, 25 per size. Their published Ownership buffers were
read, never changed. Four candidate policies per world and three full replays
each give 300 cases / 900 bit-identical motion-and-boundary replays. Candidate
fingerprints are explicitly provisional, not final C golden buffers. The 15
approved B buffers remain byte-for-byte unchanged and pass their complete-word
production comparisons. The 75 historical R fingerprints also still match.

Each candidate has 215,427 sampled boundary observations: Small 47,304, Medium
72,368, Large 95,755, over the respective 25 worlds. Counts include every unlike-
owner edge once. Independent test-owned Offset adjacency confirms exact set
equality; analytic normals, projection magnitude and endpoint reversal are
checked. Five shared seeds occur at each size; these observations are not
statistically independent estimates of a universal category distribution.

Percent of sampled boundary observations across all three sizes:

| Candidate | Quiet | Convergent | Divergent | Transform | Mixed |
| --- | ---: | ---: | ---: | ---: | ---: |
| H | .0775% | 14.4207% | 14.8240% | 30.0000% | 40.6778% |
| Q | .7817% | 14.2963% | 14.7660% | 30.0524% | 40.1036% |
| M | .0775% | 20.1934% | 20.5401% | 41.3871% | 17.8019% |
| Z | .1847% | 14.8231% | 14.8022% | 29.6560% | 40.5339% |

The activity threshold participates in Mixed as well as Quiet, so changing A
does not merely rename otherwise fixed categories. Z allows exact zero as an
endpoint, but does not force a stationary plate. Synthetic zero-speed checks
cover the canonical stationary case independently of its sampling probability.

## Measurements and limits

Release .NET 10.0.13, Windows 10.0.26200, X64, 16 logical processors,
Intel64 Family 6 Model 154 Stepping 3, GenuineIntel. Tiered compilation disabled.
One warmup per size/profile. Three serial full candidate builds per world:
75 measurement batches per size/profile. Measurements include motion generation,
full Grid-neighbor scan, projections, classification, sorting and result arrays.
They exclude the accepted partition, immutable view validation, independent
oracles, hashing, serialization and console output. Array/list allocations belong
to the operation; measurement records and digest scratch are outside its interval.

Median milliseconds [minimum,maximum]:

| Candidate | Small | Medium | Large |
| --- | --- | --- | --- |
| H | 3.1617 [2.6670,6.2592] | 6.6898 [6.0185,7.8755] | 11.8634 [10.7465,17.1028] |
| Q | 3.2179 [2.6896,4.1486] | 6.6487 [6.1723,7.6130] | 11.8501 [10.4756,43.0455] |
| M | 3.2868 [2.7894,4.0257] | 6.7158 [6.1267,7.6577] | 12.0243 [10.5897,32.3740] |
| Z | 3.2980 [2.7754,3.9918] | 6.7363 [6.0493,7.9690] | 11.8666 [10.3358,29.2698] |

Per-thread allocated ranges across these builds: Small 236,824–249,008 B,
Medium 436,808–453,880 B, Large 474,888–490,472 B. These are experimental typed
edge/list buffers, not the future packed publication/GPU buffers, peak live
memory or retained heap. Production packing/validation/LAB performance must be
measured after approval and implementation. No performance advantage, pass/fail
budget or hardware-independent threshold follows from these data. Fixed profile
order, thermal state, scheduling and GC may affect variation.

Synthetic checks pass for pure interactions, identical velocities, cardinal
conventions, stationary motion, signed zero/underflow, malformed/nonfinite input,
inclusive activity/mixed ties, the adjacent float on either side of a threshold,
and the nearest seam image. Independent geometric oracle tolerance 1e-4 covers
accepted float centers; squared projection error <=3e-7*max(1,relativeSpeed^2)
covers float storage. These are test error budgets, never classification epsilons.

## Executed regressions and remaining gate

[Retained outputs](../../../../tests/worldgen/tectonics/experiments/motion-cal01/validation/):
Tectonics 10/10 groups (15 complete frozen B vectors, all 75 archived R cases,
204 corruption checks and prior CORE lifecycle tests), complete Grid suite
(1,160,000 geometry BFS + 1,160,000 Grid API BFS comparisons, 1,015,000 triangle
checks, 10 integration checks), Laboratory 18/18, menu 16/16. Engine and Game
Release builds succeeded with zero warnings/errors. Initial sandboxed restore
could not read the user's NuGet.Config; the same build succeeded with approved
access. No dependency or source workaround was introduced.

These checks validate the existing production B/LAB and the experimental policy
comparison. They do not establish C production schema safety, full T07/T08/T12,
new packed C compatibility vectors, native visualization, DPI/input or GPU
lifecycle. Those remain pending after the owner selects the numeric policy.
OBS-01 crust and OBS-04 optional influences remain deferred; OBS-02 stays open
until a policy is selected and implemented. OBS-03 R remains closed.

Exact executed regression commands are the PowerShell commands below (slashes
in the execution logs may use the equivalent forward-slash paths):

```powershell
dotnet build .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release
dotnet run --project .\tests\worldgen\grid\Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release
dotnet run --project .\tests\laboratory\Evolve.Laboratory.Checks.csproj --configuration Release
dotnet run --project .\tests\menu\Evolve.Menu.Checks.csproj --configuration Release
dotnet build .\engine\worldgen\Evolve.Engine.WorldGeneration.csproj --configuration Release
dotnet build .\game\Evolve.Game.csproj --configuration Release
git diff --check
```

Reproduce the comparison after building, into a **new empty** output directory
(the tool refuses to overwrite evidence). The recorded run used motion-cal01:

```powershell
$previousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    dotnet run --project .\tests\worldgen\tectonics\Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --compare-motion .\tests\worldgen\tectonics\experiments\motion-cal01-replay
} finally {
    $env:DOTNET_TieredCompilation = $previousTiering
}
```

No commit, push, merge or VERSION increment has been performed at this gate.
Next required decision: select/approve one complete policy (including numeric
rules), or request a different comparison, before final C implementation and
its independently auditable delivery.
