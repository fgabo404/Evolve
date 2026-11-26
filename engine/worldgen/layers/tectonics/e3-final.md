# E3-FINAL: Historical BALANCED Mantle candidate

**Implementation/reference delivery for review; not formally frozen or accepted.**
Base world-tectonics, v0.0.47 / 6ef56576b861b072403e798b6c60e40af43498e9.
Local/remote HEAD, clean tree and VERSION were confirmed before edits.
The owner's selected identity is `tectonic-mantle-balanced-historical-v1`,
candidate revision 1. No E4/E5, normal activation or Layer 01 closure.

## Selected numerical contract

Model A, operator R0, spatial policy A0. Broad/regional frequency 2/6; all three
weights 2 (binary32 40000000), Vref 1 (3F800000). Domain ASCII WG01MNT1 is
little-endian 31544E4D31304757. Fresh private SplitMix64 from the existing
Tectonics-derived seed XOR domain: first draw broad, second regional.

MantleHistoricalProfile is internal, validates exact identity/revision/model/
operator/sampling/numeric policy/domain and parameter words, and calls the
unchanged CAL01 builders. No public tuning or alternative operator is enabled.
The unchanged sampler has three octaves, cubic interpolation and fixed octave
weights. A0 uses physical float centers with X/PeriodWidth*Columns and
Y/MapHeight*Rows in the existing arithmetic order. R0 traverses ascending CellId
and canonical directions, handles each real i<j pair once, accumulates double
and casts only final U/V/W. Potential retains the exact existing z and weighted
double expression before its final float cast. Zeros are canonical positive zero.

All four channels are finite dimensionless diagnostics. Potential is in [-1,1];
U/V are east/south directional indicators; W positive/negative denotes
compensating up/down. The owner accepts R0's resolution dependence and A0's
anisotropy as part of this candidate identity. No R1/R2/A1, RMS/quantile scaling,
physical velocity/temperature/pressure interpretation or new noise is adopted.
The complete byte references and independent oracles guard formula/rounding
changes that metadata validation alone cannot detect.

Conservation retains CAL01's outward-rounded gamma(6)/gamma(N) budgets,
separating ideal cancellation, double accumulation, float rounding and computed
stored-value sums. Finite residues need not be zero. E3 MINOR-01 remains resolved.

## Reference matrix declared before generation

A: 3 MapSizes x seeds {0,1,-1,long.MinValue,long.MaxValue} = **15 unique maps**,
matching the complete B/M/Influence matrix. B: the 12 BALANCED CAL01/CAL02 maps
use {0,-1,long.MinValue,long.MaxValue}, all included in A; overlap is twelve,
not an additional twelve buffers. The extra positive-seed maps are already in
A. C: execute every map under all four existing WorldTypes, yielding **60
logical cases**, forty-five additional type cases without duplicate data.
Each equivalent type is actually generated and compared, not merely labeled.

Files under `tests/worldgen/tectonics/mantle-candidate-vectors/` explicitly
identify **candidate** references, not a frozen profile. Fifteen losslessly
compressed full buffers contain CellId-ascending Potential/FlowX/FlowY/
SignedVertical, binary32 little-endian, positive zero, no padding/header,
16 bytes per cell. Raw lengths Small/Medium/Large: 320000/720000/1280000 bytes.

The manifest records identity/revision/parameters/domain/numeric order, all
sixty size/type/signed-seed cases, cell counts, exact lengths, raw/compressed
SHA-256, filenames, links to unchanged B/M/Influence hashes and the historical
overlap. Independent seed vectors record all five input seeds, the private
domain state and ordered broad/regional draws. Compressed hashes protect
physical files; raw hashes define logical compatibility. All references are
verified independently before export and are not silently regenerated on failure.

## Verification and delivery evidence

### Numerical order and oracle independence

The fixed potential sequence is:

```text
z = ((double)Extension - Compression) / (((double)Vref + Extension) + Compression)
numerator = ((double)wt * z + (double)wb * Nb) + (double)wr * Nr
denominator = ((double)wt + wb) + wr
phi = (float)(numerator / denominator)
```

For each real i<j edge, R0 computes f=(double)phi[i]-phi[j], adds/subtracts
f to W[i]/W[j], and adds f*normal.X and f*normal.Y to both U/V endpoints.
No FMA, reordered reductions, parallelization, physical scaling or map-wide
normalization is introduced. Potential and final U/V/W are canonical binary32.

`MantleFinalChecks` independently derives stage/domain seeds and both private
draws using specified modulo-2^64 arithmetic. Its expected A0 coordinates,
lattice values, cubic interpolation, three octave accumulation and potential
do not call the production sampler or signal/potential builders. Every signal
is compared by binary64 words, then every potential by binary32 words.
The reused `MantleIndependentOracle` independently enumerates Offset neighbors,
periodic normals and all canonical edges; it compares all U/V/W words and
checks local/global derived error budgets. The production profile only
supplies actual values. Frozen-reference hashes are a separate regression
mechanism and are not described as independent numerical oracles.

All fifteen unique maps receive this full oracle. Sixty equivalent WorldType
cases are generated and compared against complete references, with all old
input bindings checked. Distinct seeds must have distinct complete hashes.
Existing controlled fields and malformed input/zero/balance tests remain.
New tests reject wrong identity, revision, parameter words, ordering/encoding,
seed vectors, missing/duplicate cases, unsafe paths, dimensions, checksums,
truncated buffers, wrong lengths and corrupted content. Export requires an
empty directory; normal verification never regenerates expected outputs.

The [candidate reference inventory](../../../../tests/worldgen/tectonics/mantle-candidate-vectors/readme.md)
lists all fifteen raw hashes and five seed vectors. The
[manifest](../../../../tests/worldgen/tectonics/mantle-candidate-vectors/manifest.json)
contains all sixty rows, physical hashes and frozen-input links. The fifteen
files total 10,725,787 compressed bytes versus 11,600,000 raw logical bytes.
No output references or historical CSVs were replaced.

### Measured performance and memory

Release, .NET 10.0.13, Windows build 10.0.26200, process/OS X64,
Intel64 Family 6 Model 154 Stepping 3 (GenuineIntel), 16 logical processors,
10 MHz Stopwatch. Regressions/builds finished before the separate benchmark.
No dedicated-machine isolation, cross-runtime bit identity or performance
threshold is claimed. Tiering was not overridden for this run.

Each of twelve cases (all sizes x seeds 0,-1,MinValue,MaxValue) has one warmup
and three measured batches, one operation/phase/batch. Seven separately timed
phases yield 336 raw rows (84 warmup, 252 measured), 84 per-case summaries.
Summaries use conventional sorted medians, minimum and maximum, mean allocated
bytes and signed retained-delta ranges. No average is mislabeled as median.

The [raw measurements](../../../../tests/worldgen/tectonics/experiments/mantle-final/raw/timings.csv),
[all summaries](../../../../tests/worldgen/tectonics/experiments/mantle-final/raw/summary.csv),
[environment](../../../../tests/worldgen/tectonics/experiments/mantle-final/raw/environment.json)
and [control](../../../../tests/worldgen/tectonics/experiments/mantle-final/raw/control.json)
are retained. Representative **seed 0** medians in milliseconds (3 samples;
full min/max for every seed is in the summary):

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Signals | 7.8582 | 10.6597 | 19.3933 |
| Potential | 0.6924 | 0.4296 | 0.7404 |
| Edges | 6.6210 | 6.3195 | 12.1199 |
| Structural validation | 0.1311 | 0.2407 | 0.2740 |
| Independent full oracle | 19.1523 | 29.3928 | 58.5726 |
| Serialization | 0.1659 | 0.2090 | 0.4061 |
| Hash | 0.2248 | 0.4571 | 0.7985 |

Delegates, input R/M/Influence preparation, row recording and forced GC occur
outside the timed/allocation interval. Oracle time includes independent seed,
noise, complete potential/edge checks, assertions and structural validation;
its allocations belong to test machinery, not production execution. Hash
measures SHA-256 plus hex conversion, separately from buffer serialization.
The no-op control recorded 0 allocated bytes, 0.0405 ms and +24 retained-GC
bytes; no blanket overhead subtraction is applied.

Representative seed-0 bytes allocated per operation:

| Phase | Small | Medium | Large |
| --- | ---: | ---: | ---: |
| Signals | 320392 | 720392 | 1280392 |
| Potential | 80048 | 180048 | 320048 |
| Edges | 800176 | 1800176 | 3200176 |
| Structural validation | 0 | 0 | 0 |
| Independent full oracle | 9148904 | 20077696 | 37486728 |
| Serialization | 320024 | 720024 | 1280024 |
| Hash | 208 | 208 | 208 |

`GC.GetAllocatedBytesForCurrentThread` measures new bytes, not a peak.
`GC.GetTotalMemory(true)` before/after gives a signed retention observation
with collection noise (including negative oracle deltas); it is not an exact
object-size or simultaneous live-set measurement. The measured output flow
tuple stride is 16 bytes; the separate potential adds 4 bytes/cell, excluding
headers. The logical reference stays exactly 16 bytes/cell. Edge construction
allocates three temporary double arrays plus the retained tuple array
(40*N plus observed overhead); this does not establish integrated peak memory.
No compact representation was adopted and no arithmetic was changed for speed.
MEM-01 remains OPEN pending complete simultaneous pipeline characterization.

### Executed validation and reproducibility

All commands below were executed locally in Release. Exit status was zero:

| Command / evidence file under tests/worldgen/tectonics/validation/mantle-final/ | Result |
| --- | --- |
| dotnet run --project tests/worldgen/tectonics/Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release -- --export-mantle-candidate-vectors tests/worldgen/tectonics/mantle-candidate-vectors / export.txt | 15 complete files, 60 generated logical cases, 12 exact CAL01/CAL02 overlaps, independent oracles before export. Explicit one-time authoring; do not rerun into existing references. |
| dotnet run --project tests/worldgen/tectonics/Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release / tectonics.txt | 33/33 groups. Run again after adding malformed-buffer tests; final log includes those tests. All 45 original R/M/Influence buffers and all 60 historical controls passed. |
| dotnet run --project tests/worldgen/grid/Evolve.WorldGeneration.Grid.Checks.csproj --configuration Release / grid.txt | V01-V06 passed: 145000 cells, 1160000 geometry BFS + 1160000 Grid API BFS comparisons; metric/topology/invalid-input checks retained. V07 is a separate existing tool, not rerun for E3-FINAL. |
| dotnet run --project tests/laboratory/Evolve.Laboratory.Checks.csproj --configuration Release / laboratory.txt | 19/19 headless checks. |
| dotnet run --project tests/menu/Evolve.Menu.Checks.csproj --configuration Release / menu.txt | 16/16 checks. |
| dotnet build engine/worldgen/Evolve.Engine.WorldGeneration.csproj --configuration Release / engine-build.txt | Success, 0 warnings, 0 errors. |
| dotnet build game/Evolve.Game.csproj --configuration Release / game-build.txt | Success, 0 warnings, 0 errors. Game ProductVersion independently read as v0.0.48, FileVersion 0.0.48.0. |
| dotnet run --project tests/worldgen/tectonics/Evolve.WorldGeneration.Tectonics.Checks.csproj --configuration Release --no-build -- --mantle-final-performance tests/worldgen/tectonics/experiments/mantle-final/raw / performance.txt | 336 raw rows; every measured full oracle/replay passed. |

For independent PowerShell validation, use the same build/run commands with
Windows path separators if preferred. `dotnet run` executes these custom
headless checks; `dotnet test` does not run their assertions. To reproduce
performance, replace the output with a **new empty directory** and run only
after other checks finish. Do not overwrite committed measurement evidence.
`git diff --check` and post-commit `git diff HEAD^ HEAD --check` verify whitespace;
publication additionally requires matching local/remote world-tectonics SHA.
No graphical window or native GPU/DPI validation was performed for this task.

The [file inventory](../../../../tests/worldgen/tectonics/validation/mantle-final/files.csv)
enumerates every added/modified path. Scope review confirms the only new Engine
code is the internal candidate profile; the four canonical Mantle builders,
R/M/Influence sources/references, WG-CORE, Grid, Game, State and Saves are unchanged.

### Observation follow-up

| Observation | E3-FINAL disposition |
| --- | --- |
| E3 MINOR-01 | Remains resolved. Ideal cancellation does not imply an exactly zero finite double sum; existing gamma budgets are preserved. |
| CAL02-INFO-01 | Automated 60/60 historical comparisons: BALANCED/TECTONIC-DOM/BROAD-DOM plus 24 LOW/HIGH cases. LOW/HIGH remain experimental and are not accepted by the profile. |
| CAL02-MINOR-01 | Not applicable to selected A0. A1 was not selected; no additional complete A1 oracle is claimed. Historical experimental A1 checks remain unchanged. |
| Seam documentation INFO | Existing CAL02 shifted-probe maximum is 6.106226635438361E-16 across 216 nonzero-shift rows. Across all 288 rows, including 72 zero-shift rows, the maximum is 6.6613381477509392E-16. Different scopes, not contradictory measurements; original CSV/report retained. |
| MEM-01 | OPEN. Isolated phase allocations/retention do not prove the simultaneous complete pipeline peak. No claim of closure. |
| E1 MINOR-01 | Pending E4: schema-1/schema-2 reader compatibility. No codec change here. |
| PERF-01 | Pending integration: full validation and integrated total cost remain relevant. |
| GATE-01 | OPEN. N01 owner-declared PASS; N02-N17 PENDING. No native/DPI/GPU lifecycle evidence is added here. |

R/M/Influence remain frozen inputs. E3-FINAL is an implementation/reference
submission, subject to architectural review, independent audit and express
owner acceptance. Mantle is not formally frozen; E3 is not formally accepted.
No schema 2/features 31, normal Influence/Mantle publication, E4/E5, Laboratory
changes, other layers, active WorldState, saves or branch merge are included.
