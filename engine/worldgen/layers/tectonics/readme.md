# Layer 01 — Tectonic Plates

**Status:** WG-01-A and WG-01-B accepted. WG-01-C + FIX01 formally accepted at
v0.0.40. WG-01-D delivered at v0.0.41 and independently audited favorably with
observations, as communicated in the owner's D-FIX01 authorization. Formal
WG-01-D acceptance and final Layer 01 closure remain pending. R revision 2 and
M revision 1 are unchanged. [audit-fix01.md](audit-fix01.md) records finding
resolutions and the outstanding native gate; [verification-d.md](verification-d.md)
retains the developer D evidence and [delivery-c.md](delivery-c.md) the C record.
**Working branch:** `world-tectonics`.
**Historical B baseline:** `v0.0.37`, `63e968ff71dbbcaa61e1ebbba744e466e3a01332`.
**Historical B delivery:** `v0.0.38`.
**Prerequisite:** formally closed Layer 00 — Hex Grid.

## WG-01-E documentary extension

The owner authorized E1 architecture only at v0.0.42. See the
[historical review](e-history-review.md), [integrated design](e-architecture.md),
[proposed schema-2 contract](e-contracts.md), [five-increment E roadmap](e-roadmap.md)
and [documentary verification](e1-validation.md). These proposals do not replace
the active R/M profiles or schema 1/features 7. Published Influence and mantle
remain unavailable; E2 requires independent audit and express owner acceptance.
GATE-01 remains partially open and Layer 01 is not formally closed.
Subsequent [E2-FINAL](e2-final.md) implements and verifies private Influence
components with the owner-selected HISTORICAL candidate. Normal published
Influence remains absent; definitive profile acceptance and E4 integration
remain pending. Its [numeric contract](influence-profile.md) is separate from
the active schema-1 R/M publication.

## 1. Responsibility and boundaries

Generate exactly **14 nonempty connected regions**, with one owner in 0..13
for every existing row-major CellId. Plate size, irregular outline and motion
depend on seed and algorithm revision, not on landmass WorldType. This is
ownership and kinematic information, not continents, oceanic crust assignments,
elevation, physical stress simulation, State or Saves.

The [contracts](contracts.md) specify the single immutable publication buffer,
typed API, memory accounting and errors. The A design is retained below; the
ownership-only B subset is now implemented as recorded in section 10.

- Depend only on Engine, Grid and existing Definitions; no OpenTK, Laboratory,
  Landmasses, State or Saves dependencies.
- TectonicPlatesLayer implements IWorldGenerationLayer for Tectonics. Read the
  completed single HexGridDescriptor through GenerationLayerInputs; validate
  its size against Request.Settings.MapSize and use ToGridData(). Never mutate
  Grid, read context internals or implement another topology.
- Return one validated candidate through LayerExecutionResult.Success. Only the
  real orchestrator publishes, changes progress, retries and invalidates.
  Helpers receive immutable Grid/values, never GenerationContext.
- LAB integration belongs to C under separate authorization and uses RunNext.
  INICIAR remains unavailable. A changes documentation and VERSION only.

## 2. Historical review and adaptation

Inspected `worldgeneration` at
`ef35e9f8865b02081c8f532a298a314e01091a8d`.
The requested conceptual GEvolve.Engine/World/Tectonics directory is stored
there as GEvolve.Engine/World/worldgenerationold/Tectonics/. No old classes
are copied or imported.

| Historical source | Actual behavior and recoverable idea | Required adaptation / exclusion |
| --- | --- | --- |
| TectonicPlateGenerator.cs | 16 seeds, 160 farthest candidates, shuffled growth costs, six periodic fields, warp and priority-queue growth | 14 distinct seeds, reserve seeds before competition, stable total queue order, reviewed 14-profile calibration; discard legacy RNG and fixed-16 assumptions. |
| PlateBoundaryMap.cs | Canonical unlike-owner edges using first ID < second ID; multi-source boundary-distance BFS | Accepted Grid neighbors, immutable word sections; remove LandmassState, escaped bool/ushort arrays and unproven ushort distance bound. |
| PlateMotionGenerator.cs | Separate historical random stream; uniform direction and speed interval | Stable local stream from CORE, finite value records; do not inherit old settings/state dependencies or default speeds as approved physics. |
| TectonicMotionGenerator.cs | Seam-aware normal, relative velocity, five classes, maximum Gaussian envelope, interior anchor | Retain projection/sign semantics and optional anchor; remove hardcoded 16 validation and unconditional per-boundary-source stress BFS. |
| LandmassGeneration/Shared/PeriodicNoise.cs | X-periodic value lattice, cubic interpolation, three octaves, amplitude halving; open Y | Specify fixed integer hash widths/overflow, 64-bit seed adaptation and periodic warp; no dependency on LandmassGeneration. |
| LandmassGeneration/Shared/HexTopology.cs | Cached six-neighbor array and counts | Use HexGridData queries; no duplicate adjacency cache or legacy HexGrid graph. |
| Definitions/WorldGeneration/Configuration/TectonicMotionSettings.cs; State/World/TectonicMotionState.cs | Clockwise map direction, cm/year, threshold categories, mutable field arrays | Preserve explicit diagnostic units; remove State/JSON/mutable publication dependencies and unreviewed defaults. |

Historical motion fields are explicitly kinematic diagnostics, not pascals,
forces, uplift or a spherical plate-rotation model. Source review does not
establish physical realism or claim execution of historical tests.

## 3. Determinism and profile ownership

Use only the existing DeterministicStageRandom (splitmix64-stage-v1).
Proposed local stream rule: consume exactly three inputs.Random.NextUInt64
values in partition/noise/motion order; construct one local instance per value.
Reserve all three draws in B before C exists, so C cannot change the partition's
random sequence. Version the call order and domains under the layer revision.
No CORE extension, System.Random, seed truncation to int, process hash or clock.

Bounded integer n > 0: reject ulong draws below (-n mod 2^64) mod n, then
reduce modulo n, using explicit unchecked unsigned arithmetic.
Uniform u in [0,1): upper 53 bits times 2^-53. Noise hash widths, overflow and
domain separation need fixed B vectors; no legacy implicit hash behavior.

Iterate ascending CellId/PlateId and canonical HexDirection. Priority keys
compare (accumulatedCost, plateId, cellId, enqueueSequence) lexicographically,
not PriorityQueue stability. Require finite positive step costs; reject
non-finite accumulated costs and checked sequence overflow.

Keep tunables in an internal validated algorithm profile; do not extend
WorldGenerationSettings or invent another enum/catalog. Changing numeric
profiles changes the algorithm revision. Ignore Request.WorldType deliberately.
Reproducibility is for equal inputs, revisions and supported numeric/runtime
policy; cross-runtime/platform floating-point equivalence needs D evidence.

## 4. WG-01-B partition algorithm

1. Select the first seed from the partition stream. For each later ID sample
   unused cells, maximize minimum Grid.GetDistance to existing origins, and
   resolve equal scores by smaller CellId. If a sample finds no unused cell,
   scan unused CellIds with the same score/order. No arbitrary separation floor.
2. Reserve all 14 distinct origins as owned before enqueueing their neighbors.
   This guarantees nonempty plates, including slow profiles.
3. Assign positive varied growth costs, deterministically shuffled across IDs.
   A candidate 16-to-14 adaptation interpolates legacy sorted log costs at
   positions k*15/13 for k=0..13, then exponentiates. This retains the old range
   without simply discarding small-plate profiles; B calibration remains pending.
4. Compute broad/medium/fine pairs of periodic multiscale fields on x/C, y/R,
   with per-plate pair rotations and periodic domain warp. Noise stays periodic
   in X and open in Y; sample outside Y when warped, never wrap/clamp polar
   topology. Warp(x+C,y)=warp(x,y) preserves the composed period.
5. A private owner buffer starts at sentinel 255. The queue holds candidates
   reached from owned neighbors. Pop in total order, skip already-owned cells,
   claim and expand only through real Grid neighbors. Add the plate cost times
   a bounded strictly positive resistance derived from its rotated fields.
6. Validate all invariants before packing/publishing. No post-growth relabeling
   that disconnects regions, erases seeds or silently retries for aesthetics.

Connectivity follows by induction: each claimed cell has an already-owned
neighbor of the same plate. Connected finite Grid and finite positive costs
allow full coverage. This is competitive connected growth, not a proof of
globally optimal independent per-plate Dijkstra regions.

Periodic seam tests compare fields at x and x+C, including warped/negative
coordinates; columns 0 and C-1 are distinct neighboring samples, not identical
samples. Geography/adjacency still comes exclusively from Grid.

Nonempty regions alone imply unequal counts here because none of 20,000,
45,000 or 80,000 is divisible by 14. That does not demonstrate useful diversity:
B must report sorted area fractions/spread across sizes and representative
seeds. No unapproved minimum-area fraction, fixed quota or hidden retry loop.
Inadequate diversity requires explicit profile review.

Expected partition cost is O(14^2*K) seed distance queries plus O(N) field
evaluation for a fixed band count and O(E log E) competitive frontier work.
Fallback seed scans add O(14^2*N) only when candidate sampling cannot supply an
unused cell. Boundary collection is O(E), with O(B log B) canonical sorting.

## 5. WG-01-C boundaries and motion

Enumerate all real unlike-owner neighboring pairs with firstCellId <
secondCellId, then sort lexicographically. Emit each once. Derive PlateIds from
ownership; do not store another cell-owner table or add seam/polar cells.

Direction is finite in [0,360), 0 degrees east/+X and 90 south/+Y, clockwise
in map coordinates. Speed is finite and nonnegative in proposed cm/year;
zero-speed direction canonicalizes to zero. No physical cell length, spherical
rotation, timeline or approved empirical speed interval is implied.

Compute the unit normal n from first cell to its neighbor using accepted
flat-top centers and the nearest periodic X image; keep Y bounded. Use double
precision normalization, never longitude differences or floating-point neighbor
search. Set tangent t=(-n.Y,n.X), dv=velocity(secondPlate)-velocity(firstPlate).

- signedNormal=dv dot n; negative means closing, positive opening.
- signedTangent=dv dot t.
- convergence=max(0,-signedNormal), divergence=max(0,signedNormal),
  shear=abs(signedTangent).

Project in double, store finite float components and canonicalize signed zero.
Classify using those stored values and stored thresholds, not a different
precision computation hidden from consumers. With axial=abs(signedNormal),
shear as above, max=max(axial,shear), min=min(axial,shear), A>=0, 0<M<=1:

1. Quiet if max<=A.
2. Mixed if min>=A and min>=M*max.
3. Otherwise Transform if shear>axial.
4. Otherwise Convergent if signedNormal<0, else Divergent.

This precedence defines equality. Reversing endpoints reverses dv, n and t
together, leaving projections/class invariant and swapping endpoint identities.
Preserve components for Layer 02/03; a category alone loses useful information.
No subduction polarity, final crust type, sea level or uplift is assigned here.

## 6. Conditional influence fields and propagation risk

Multi-source BFS from boundary cells can compute distance in O(N+E). Calculate
in int, publish checked uint, rather than assuming ushort capacity. A future
interior display anchor can select maximal boundary distance per plate,
smallest CellId on ties; no extra published anchor table is necessary.

Historical spread is maximum over sources of amplitude*exp(-(distance/r)^2)
per channel, truncated to ceil(r*cutoff). A bounded BFS for every active source
costs O(S*(N+E)) worst case, with reusable O(N) scratch. Large has 80,000 cells
and 239,200 undirected edges. Measure active-source count, visited cells,
allocation and time before adopting this expensive algorithm.

One nearest-boundary distance cannot reproduce differently weighted envelopes.
Do not silently substitute sums or simple priority-based attenuation for the
maximum-Gaussian semantics; prove equivalence with a small independent
all-source BFS oracle or present an approved model change.

The schema reserves optional fields, but B omits them. C must first resolve
algorithm, numeric calibration and measured cost. Without acceptance publish
boundary kinematics only and explicitly report influence unavailable, never
fabricated zero fields. Ownership, plate records, boundaries and motion already
give Layer 02/03 geological inputs without requiring a terrain generator.

## 7. Historical A proposals and remaining C decisions

This table preserves A's historical proposals. B now uses the exact frozen
R constants in section 10/calibration.md; historical P0 values below are not
current defaults. Motion/classification/influence parameters remain C decisions.

| Parameter | Historical value / proposal | Effect and required evidence |
| --- | --- | --- |
| Seed candidates | 160 historically | O(14^2*K) distance queries versus separation quality; B measures spread/time. Distinct-seed fallback is mandatory for any K. |
| Growth costs | Legacy .58,.64,.72,.79,.88,.97,1.05,1.14,1.24,1.35,1.48,1.62,1.79,1.97,2.17,2.40; propose 14 log-interpolated/shuffled costs | Controls relative expansion, not guaranteed area; B reviews actual distributions. |
| Noise | Legacy frequencies 3,5,9,13,21,29, three octaves, amplitude halving | World-relative lobes/bays/bends; B verifies seam behavior, numeric vectors, scale and cost. |
| Warp | Legacy .055 domain dimension at frequency 3 | Distorts fields, not Grid; B assesses artifacts and periodicity. |
| Bands/irregularity | Legacy 1.65,1.10,.44 / 1.90 | Contour complexity; B calibration pending. |
| Resistance | Legacy [.27,3.10] | Positive bounded increments; exact bounds not geological evidence. |
| Speed | Legacy [1,8] cm/year, arbitrary validation cap 100 | C must justify interval/distribution and stylized versus empirical meaning; A approves no speed cap/default. |
| Activity/mixed | Legacy .15 cm/year / .5 | C must justify unit-aware classification and synthetic equality cases. |
| Influence | Legacy .02*min(C,R) radius, cutoff 3 | C reviews size consistency, error and propagation cost before enabling. |
| Minimum region | At least one cell required; no further floor | B must present diversity evidence for owner decision; do not invent a percentage. |

Profiles require finite parameters, positive costs/resistance, an ordered
nonnegative speed interval, A>=0, 0<M<=1 and positive optional radius/cutoff.
Tolerance budgets, final profiles, physical interpretation and algorithm
compatibility vectors remain explicit B/C decisions, not approval of arbitrary
legacy values.

## 8. Planned independent validation matrix

This is the original A validation matrix. The applicable B subset is now
executed in tests/worldgen/tectonics/ as detailed in section 10 and the test
readme; C/D entries remain planned. Tests call production code and use only
test-local downstream executors. Keep accepted Grid and LAB/menu regressions.

| ID / gate | Invariant | Independent oracle / adversarial evidence |
| --- | --- | --- |
| T01 / B | N owners in 0..13; all 14 present; seed retained | Exhaustive histogram/sum and seed fixtures; reject sentinel, missing/duplicate seed or wrong count. |
| T02 / B | One connected component per plate | Flood-fill by test-owned odd-column Offset adjacency/wrap, not growth logic; reached counts equal owner histogram on all sizes. |
| T03 / B | Seam/pole topology, Grid unchanged | Independent seam/polar fixtures; retain original descriptor/reference and compare all IDs/neighbors/geographic values; Grid regression suite. |
| T04 / B,D | Determinism and continent-type independence | Signed/extreme/zero seeds, all sizes/types; canonical word equality across fresh contexts, retry, RunNext/Until/All and rebuild. Variation across seeds is evidence, not a universal no-collision rule. |
| T05 / B | Weighted competition, profiles, periodic fields | Hand-worked equal-priority frontier; RNG/hash vectors; x versus x+C including warped/negative samples; distinct origins; report area distributions with justified numeric tolerances. |
| T06 / B,C | Safe schema and immutable data | Invalid stage/type/version/flags/lengths/offset arithmetic/padding/owners/nonfinite values; mutate source before/after LayerData copy; no mutable refs escape; view remains stable after rebuild. |
| T07 / C | Exactly all canonical real unlike-owner edges | Independent adjacency set equality; sorted pairs, no duplicates, self/reversed/non-neighbor pairs; seam and both poles. |
| T08 / C | Finite motion, signs and five classifications | Hand-worked aligned/opposed/parallel/identical velocities; seam normal; endpoint reversal; zero speeds and exact threshold/mixed ties. Do not reuse production projector as oracle. |
| T09 / C | Optional distance/influence fields | Independent multi-source distances and small all-source max-Gaussian oracle with unequal amplitudes/cutoffs; source-order independence; reject absent-field queries. |
| T10 / B,C,D | Real CORE lifecycle | No Tectonics before Grid or running partial data; inject validation failures/exceptions/wrong-stage candidates; preserve Grid and retry stream; suffix invalidation, historical snapshots and exclusivity/reentrancy. |
| T11 / D | Memory/performance/dependencies | All presets/seeds; generation/query times, frontier peak, allocation/copies, optional spread scaling; environment/warmup/repeated batches, no invented thresholds; no UI/State/GPU dependencies. |
| T12 / C,D | Real LAB integration/regressions | C authorization only; display completed real data and unavailable channels honestly; preserve Grid/menu, native input/DPI/GPU and retention observations; INICIAR stays unavailable. |

Historically, A executed source/history/contract and document/link/scope review
plus git diff --check only. It created no test project, generator or benchmark
and ran no builds or automated/GUI tests for its documentation-only delivery.
The B execution evidence is separate below. Neither delivery claims independent
acceptance or geological validation from developer checks alone.

## 9. Gates and progression

- **A:** these documents, contracts, compact roadmap and VERSION only; independent
  audit and explicit owner acceptance required before B.
- **B:** approved R profile and Ownership-only CORE publication implemented;
  formally accepted by the owner in the C authorization.
- **C:** approved motions/boundaries and conditional measured influences; actual
  LAB view implemented with M, B partition stable; influences remain absent.
  C + FIX01 were formally accepted at v0.0.40; D native gate/final closure remain pending.
- **D:** complete correctness/performance evidence, independent audit and formal
  owner closure. Branch integration remains separately authorized.

## 10. WG-01-B definitive R implementation

The real TectonicPlatesLayer, TectonicData codec/view, frozen profile,
TectonicPartitionGenerator and PeriodicTectonicNoise implement Ownership only.
Coverage, metadata and connectivity are validated before publication by CORE.
No motion, boundary/influence section, LAB integration or INICIAR change.

The owner explicitly selected CAL01 R. Exact cost bits and constants are frozen
in TectonicGenerationProfile: no experimental factory, legacy runtime cost
interpolation or profile selector remains. Schema 1 is unchanged; partition
revision 2 distinguishes R from P0 revision 1 and unsupported CAL01 tags.

[calibration.md](calibration.md) preserves historical P0/CAL01 evidence and records
the final constants, 15 complete production vectors, R results, measured phase/
memory evidence and current OBS dispositions. Calibration OBS-03 is closed by
owner profile approval and the frozen vectors; OBS-01 crust, OBS-02 motion
policy and OBS-04 optional influences remain deferred/out of B scope.

Developer checks: 10/10 tectonic groups, all Grid regressions, Laboratory 18/18
and menu 16/16; Release Engine/Game builds with no warnings/errors. These are
developer results, not the independent audit or formal B acceptance.
See [test instructions](../../../../tests/worldgen/tectonics/readme.md).
Grid, WG-CORE, UI, State and Saves production code remain unchanged.

**Internal roadmap:** [roadmap.md](roadmap.md)
**Data/API contract:** [contracts.md](contracts.md)
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
