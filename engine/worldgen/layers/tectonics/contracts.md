# WG-01-A — Tectonic Data and API Contracts

**Status:** documentary implementation proposed for independent audit and owner
acceptance; no C# implementation or approval of calibration implied.
**Baseline:** v0.0.36 / cdb00c3a6689d1e06fb25012b336b9a6a2da744d.
**Delivery:** v0.0.37. [Design and validation matrix](readme.md).

## 1. Verified CORE compatibility and representation decision

The inspected production LayerData constructor is private protected and
LayerData<T> is sealed with T : unmanaged. Its values are defensively copied;
each stage slot and LayerExecutionResult hold exactly one LayerData. Inputs
expose earlier data only and a fresh isolated DeterministicStageRandom. Context
mutation/operation exclusivity belongs to CORE, not to layer consumers.

| Option | Compatibility / cost | Decision |
| --- | --- | --- |
| Multiple typed LayerData buffers published separately | Context has one stage slot, not a multi-output transaction | Reject; no CORE extension. |
| Managed descriptor containing arrays/lists/LayerData references | Violates unmanaged constraint and risks aliases; an external composite LayerData subclass is also blocked | Reject. |
| Uniform tagged union record per cell/plate/edge | Unmanaged, but all entries pay the largest record stride, tag and padding; type/index validation still needed | Reject for avoidable per-cell waste. |
| Seed/settings-only reconstructible descriptor | Fits unmanaged but hides full regeneration in every consumer, loses direct published ownership, and complicates historical algorithm revisions | Reject as the authoritative result. |
| One packed word buffer plus a typed read-only view | One copied unmanaged buffer, compact ownership, heterogeneous sections decoded by checked schema | Select: LayerData<uint>(Tectonics, words). |

Packing complexity is deliberate and limited to one codec/view, with explicit
schema validation and corruption tests. No unsafe reinterpretation, pointer,
mutable array handle, global result dictionary or independent serialization
framework. This is an in-memory schema, not a Save format or host-endian wire
protocol. Root VERSION remains separate from schema/algorithm revisions.

The builder owns temporary byte/word arrays, validates complete candidate
semantics, then creates LayerData<uint>. CORE copies the words and alone
publishes the candidate. Do not return success until all required fields and
invariants pass. TectonicData retains only the immutable LayerData<uint> and
immutable HexGridData; all accessor results are values, never backing buffers.
No lazy regeneration, per-cell object allocation or full-world copy in readers.

## 2. Word layout, schema 1

All offsets/counts below are **uint word indices**, not byte addresses; a word
is 4 bytes. Use checked long arithmetic for products/range sums, then verify
against buffer.Count before indexing/casting. Reject excessive or overlapping
sections and unknown versions; never allocate based on unvalidated counts.

N is the validated Grid.CellCount. Exactly 14 fixed plate rows follow the
32-word header. Ownership uses one byte per CellId, four cells per word,
least-significant byte first: word=ownerOffset+CellId/4, shift=8*(CellId%4).
Each decoded owner must be 0..13; sentinel 255 is private scratch only.
All approved N are divisible by four; for any future supported nonmultiple,
unused high bytes must be zero (new sizes still require separate authorization).

| Header word | Meaning / canonical constraint |
| ---: | --- |
| 0 | Magic 0x54454354 (numeric schema marker). |
| 1 | Schema version 1. |
| 2 | Feature bits: Ownership=1, Motion=2, Boundaries=4, Influence=8. Supported combinations: 1 (B), 7 (C), 15 (C with approved fields). Unknown combinations/bits rejected. |
| 3 | Existing MapSize numeric value, validated against supplied Grid dimensions. No new size enum. |
| 4 | N, equal to Grid.CellCount. |
| 5 | PlateCount=14. |
| 6 | PlateOffset=32. |
| 7 | PlateStride=8 words. |
| 8 | OwnerOffset=144 (32+14*8). |
| 9 | OwnerWordCount=ceil(N/4). |
| 10 | BoundaryOffset=OwnerOffset+OwnerWordCount, even if boundary count is zero. |
| 11 | BoundaryCount=B, zero without Boundaries. |
| 12 | BoundaryStride=5 words. |
| 13 | InfluenceOffset=BoundaryOffset+5*B when Influence present; otherwise zero. |
| 14 | InfluenceStride=4 words when present; otherwise zero. |
| 15 | TotalWordCount=BoundaryOffset+5*B+(Influence ? 4*N : 0), exactly buffer.Count. No trailing data. |
| 16 | Positive partition algorithm revision, identifying frozen parameters/hash/draw order; B must define its first revision and vectors before publishing. |
| 17 | Positive motion/classification revision when Motion present; otherwise zero. |
| 18 | Positive influence revision when Influence present; otherwise zero. |
| 19 | Activity threshold A as finite nonnegative float bits when Motion present; otherwise zero. |
| 20 | Mixed ratio M as finite float bits, 0<M<=1 when Motion present; otherwise zero. |
| 21 | Finite positive influence radius in graph steps as float bits when Influence present; otherwise zero. |
| 22 | Finite positive cutoff multiplier as float bits when Influence present; otherwise zero. |
| 23..31 | Reserved zero. |

Revision numbers identify an immutable internal profile and interpretation, not
just an incrementing counter disconnected from settings. No actual numeric
algorithm profile is approved in A; B/C must document and freeze it. Header
revisions/active thresholds make published interpretation explicit. A schema
version change is required if future features cannot obey this exact layout.

### Plate section: 14 rows, ascending implicit PlateId

| Row word | Value |
| ---: | --- |
| 0 | SeedCellId, validated against Grid and matching this owner. |
| 1 | CellCount, positive, exactly matching ownership histogram; all rows sum to N. |
| 2 | Finite positive GrowthCost encoded as float bits; the B profile defines these canonical values. |
| 3,4 | DirectionDegrees double bits, low then high uint word. |
| 5,6 | SpeedCmPerYear double bits, low then high uint word. |
| 7 | Reserved zero. |

Row index is PlateId; do not duplicate it in storage. When Motion is absent,
words 3..6 must be zero and GetMotion rejects the query: zero bits are not a
claim of stationary/Quiet plates. When present, angle is finite in [0,360),
speed finite >=0, zero speed requires zero angle. Decode via BitConverter bit
conversions, not numeric casts; use the same canonical treatment of zero.
Growth uses the stored canonical float costs promoted to double, avoiding
a hidden mismatch between published profile and growth calculations.

### Boundary section: B rows, canonical sorted real inter-plate edges

| Row word | Value |
| ---: | --- |
| 0 | FirstCellId. |
| 1 | SecondCellId; FirstCellId < SecondCellId, both valid actual neighbors with distinct owners. |
| 2 | SignedNormal, finite float bits. |
| 3 | SignedTangent, finite float bits. |
| 4 | TectonicInteractionKind: Quiet=0, Convergent=1, Divergent=2, Transform=3, Mixed=4. |

Pairs are strictly lexicographically ordered. The full set equals exactly all
unlike-owner Grid edges; no omitted interface, duplicate/reverse edge or polar
wrap. Plate IDs, unit normal and convergence/divergence/shear are derived from
the stored owners, Grid geometry and signed components by the typed reader.
The class must equal the ordered formula in readme.md using stored components,
A and M. Reverse endpoint fixtures must preserve relative-kinematics meaning.
A decoder need not retain another edge set; validate completeness by a canonical
Grid scan or bounded temporary validation scratch, not a persistent duplicate.

### Optional influence section: N rows by CellId

Words: uint DistanceToBoundary, float Compression, float Extension, float Shear
(bit-encoded). All amplitudes finite and nonnegative; distance zero exactly on
a boundary cell. Defined as kinematic influence, not force/stress in pascals.
Radius/cutoff and revision describe the approved propagation semantics.
If not enabled, the entire section is absent and GetInfluence throws.
Boundary-only C output is valid with flags 7; enabling flags 15 requires the
separately reviewed C calibration/performance decision, not fabricated zeros.
Final Layer 02/03 algorithms decide how these inputs influence geography.

## 3. Proposed public surface (signatures, not implemented source)

Namespace: Evolve.Engine.WorldGeneration.Layers.Tectonics.
All records below are readonly value records with get-only fields/properties;
no collection or reference field. The view is managed, but is never the T in
LayerData<T>. No new public context mutator, constructor of stage inputs,
additional orchestrator or changes to CORE/Grid signatures.

| Type | Proposed public members / contract |
| --- | --- |
| TectonicPlatesLayer : IWorldGenerationLayer | GenerationStage Stage => Tectonics; LayerExecutionResult Execute(GenerationLayerInputs inputs). Stateless executor; no public tuning knobs. |
| sealed TectonicData | static TectonicData FromLayerData(LayerData data, HexGridData grid); private constructor; validated immutable view. |
| TectonicData metadata | int CellCount; int PlateCount; MapSize Size; uint SchemaVersion; uint PartitionRevision; uint MotionRevision; uint InfluenceRevision; bool HasMotion; bool HasBoundaries; bool HasInfluence; int BoundaryCount. Get-only, read from validated header. |
| TectonicData queries | byte GetPlateId(int cellId); TectonicPlate GetPlate(byte plateId); PlateMotion GetMotion(byte plateId); TectonicBoundary GetBoundary(int boundaryIndex); TectonicCellInfluence GetInfluence(int cellId). |
| readonly record struct TectonicPlate | byte PlateId; int SeedCellId; int CellCount; float GrowthCost. |
| readonly record struct PlateMotion | double DirectionDegrees; double SpeedCmPerYear; calculated TectonicVector Velocity (get only). |
| readonly record struct TectonicVector | double X; double Y. |
| enum TectonicInteractionKind | Explicit values Quiet=0, Convergent=1, Divergent=2, Transform=3, Mixed=4. |
| readonly record struct TectonicBoundary | int FirstCellId; int SecondCellId; byte FirstPlateId; byte SecondPlateId; TectonicVector Normal; float SignedNormal; float SignedTangent; TectonicInteractionKind Kind; calculated float Convergence, Divergence, Shear. |
| readonly record struct TectonicCellInfluence | uint DistanceToBoundary; float Compression; float Extension; float Shear. |

Consumer path: obtain inputs.GetLayerData(Tectonics) in a later stage (or a
completed context result in LAB), reconstruct the validated Grid from its real
descriptor, then call TectonicData.FromLayerData. Retain/reuse a validated view
rather than repeatedly validating all N cells in a render loop. Owner/plate
queries are O(1); boundary row decode is O(1); one-time full validation is
O(N+E), plus connectivity traversal. No full-world copy is made by the view.

FromLayerData verifies stage=Tectonics, runtime type=LayerData<uint>, supported
schema/revisions/features and exact consistency with Grid. It validates
ownership/connectivity, plate statistics, required features and boundaries/
influences according to the active schema. Unknown algorithm revisions must
reject interpretation rather than silently use another profile. View validation
checks shape/ranges, connectivity, the complete edge set, projected components
and classification, and optional boundary distance using multi-source BFS.
It does not rerun partition randomness or the expensive influence envelope.
Correct influence amplitudes depend on the validated builder and independent
T09 oracle; finite nonnegative values alone do not prove that physical/model
semantics are correct. No hidden generation is performed by a data accessor.

Errors: null arguments -> ArgumentNullException; malformed input, wrong
stage/type/schema, nonfinite values or incompatible Grid -> ArgumentException;
invalid CellId/PlateId/boundary index -> ArgumentOutOfRangeException.
For optional queries, validate the supplied index first, then throw
InvalidOperationException when the feature is unavailable. B has zero
BoundaryCount; no valid boundary index exists, so GetBoundary rejects any index.
No mutable partial view escapes on a validation failure.

Execute rejects null/wrong-stage inputs using CORE's existing argument policy.
Malformed/mismatched Grid or invalid generated content returns an English
LayerExecutionResult.Failed diagnostic before success; unexpected ordinary
exceptions are handled as Failed by the real orchestrator. CORE checks stage
identity, not tectonic schema: semantic validation is the layer's responsibility.

Old views remain stable historical observations after RebuildFrom(Tectonics)
or Grid. New context lookups expose no invalidated data. Mutating a private
source array after LayerData construction cannot alter its copied payload.
No reader calls execution, modifies progress or consults a global cache.

## 4. Proposed internal responsibilities and physical files

No files below are created by A. Add only when their authorized B/C behavior
exists; no empty classes, new projects or general-purpose framework.

| Future path relative to tectonics/ | Gate | Responsibility / internal contract |
| --- | --- | --- |
| TectonicPlatesLayer.cs | B | Validate inputs, call partition builder/validator, pack and return exactly one immutable candidate; no publication. |
| TectonicData.cs | B, extend C | The sole schema codec, checked offsets, immutable validated view and readonly value record declarations; no public word-array accessor. |
| TectonicGenerationProfile.cs | B, extend C | Internal immutable calibrated profile, algorithm revision and validation; internal constants only once approved. |
| generation/TectonicPartitionGenerator.cs | B | Internal static TectonicDraft Generate(HexGridData grid, DeterministicStageRandom partitionRandom, DeterministicStageRandom noiseRandom, TectonicGenerationProfile profile). Returns ownership and 14 seed/cost/count records; no context reference. |
| generation/PeriodicTectonicNoise.cs | B | Internal static double Sample(double column, double row, int columns, int rows, int frequency, ulong seed). Periodic value lattice/octaves; warp composition stays in partition generation. Fixed vectors and explicit numeric policy; no alternate geometry. |
| motion/PlateMotionGenerator.cs | C | Internal static PlateMotion[] Generate(DeterministicStageRandom random, TectonicGenerationProfile profile). Exactly 14 finite motion values, no State or ambient RNG. |
| boundaries/PlateBoundaryBuilder.cs | C | Internal static TectonicBoundary[] Build(HexGridData grid, TectonicDraft draft, TectonicGenerationProfile profile). Requires actual motions; produces canonical sorted edges and projections. |
| boundaries/TectonicInfluenceBuilder.cs | C, conditional | Internal static TectonicCellInfluence[] Build(HexGridData grid, TectonicDraft draft, TectonicGenerationProfile profile). Requires actual boundaries; enabled only after semantics/calibration/cost approval, with reusable scratch. |
| readme.md, contracts.md, roadmap.md | A onward | Responsibilities/design, precise publication contract and four increment gates. |

Internal sealed TectonicDraft is declared with the codec, not in a new project
or general-purpose result abstraction. It contains typed private-execution
buffers: byte[] Owners, TectonicPlate[] Plates (length 14), and nullable
PlateMotion[] Motions, TectonicBoundary[] Boundaries, TectonicCellInfluence[]
Influences. In B all three optional buffers are null; a null feature is distinct
from generated empty/zero data. No object-typed field or public draft exposure.

The codec's internal static LayerData<uint> CreateCandidate(HexGridData grid,
TectonicDraft draft, TectonicGenerationProfile profile) validates, packs exact
feature sections and constructs the defensive-copy candidate. The layer sets
optional buffers only after the corresponding C builder succeeds. Failed
builders discard the entire private draft; no incremental CORE publication.

Draft arrays may be mutable **internally during execution** and passed between
these helpers, never inside the published T or through public getters.
Partition validation/packing lives with the layer/codec, not a new public
validator service. Numeric random helpers stay internal in the generation
implementation unless actual reuse justifies a separate file. No future file
layout licenses implementing B/C in A.

## 5. Memory model and performance risks

Let E be all undirected Grid edges and B the inter-plate subset (B <= E).
Payload excludes CLR headers/alignment; measured allocations must distinguish
private scratch, candidate, defensive copy, readers and historic references.

- Header plus plate table: 128+448=576 bytes.
- Ownership: ceil(N/4)*4 bytes, exactly N for the three approved sizes.
- Boundary rows: 20*B bytes; normals, duplicate PlateIds and derived magnitudes
  are not stored per edge.
- Optional influences: 16*N bytes, only when enabled.
- B ownership-only payload: 576+N bytes; C: 576+N+20*B, plus 16*N if enabled.

| Size | N | Ownership-only payload | Safe C upper bound using B=E | With optional influences |
| --- | ---: | ---: | ---: | ---: |
| Small | 20,000 | 20,576 B | 1,212,576 B | 1,532,576 B |
| Medium | 45,000 | 45,576 B | 2,733,576 B | 3,453,576 B |
| Large | 80,000 | 80,576 B | 4,864,576 B | 6,144,576 B |

E uses accepted totals 59,600 / 134,400 / 239,200. These are conservative
formula-based payload bounds, **not benchmark results or expected actual B**.
Fourteen connected regions generally yield fewer edges than the bound; measure
rather than promise a boundary count.

Six cached float noise fields would cost 24*N (1,920,000 bytes on Large);
private owners cost N, queue entries/costs and visited/validation arrays add
O(N+E) scratch. At most six expansions per claimed cell bounds queued candidates
by a constant multiple of E, not 14 full per-plate world copies. Track actual
frontier peak and allocations; do not import a persistent six-neighbor cache.
Recomputing versus caching noise is a B measurement decision.

LayerData makes an unavoidable defensive copy of the candidate words. At
publication, source and destination payloads coexist, in addition to builder
scratch. Reuse/release private scratch promptly; do not modify CORE to steal an
array. View validation may need O(N) temporary connectivity scratch, not a
second permanent ownership copy. Keeping historical finalized results alive
intentionally retains old buffers; distinguish this from a leak.

Stress propagation is the major C/D risk: per-source bounded BFS may approach
O(S*(N+E)); no per-source all-pairs table. Preserve semantics or review a
replacement as documented in readme.md. C/D must report runtime/environment,
repeated timings, allocation, peak frontier/source visits and optional influence
cost for all sizes without invented numerical acceptance thresholds.

## 6. Gate conditions

A is documentation only. Audit this schema, API, historical adaptation,
validation matrix and open calibration list before accepting A. B may not
begin until explicit acceptance/authorization; required profile values and
compatibility vectors must be resolved before B publication. C adds only real
approved motion/edge/optional influence features without altering B partition
random streams. D verifies the full implementation; no branch merge is implied.

No automated tests, compile result, geological output or GUI approval is
claimed for these prospective contracts.
