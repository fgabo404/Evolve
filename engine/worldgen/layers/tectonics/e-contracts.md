# WG-01-E: Proposed integrated data contract

**Status: proposal for independent review and express owner approval.** This
document does not replace the active [schema-1 contract](contracts.md), enable
Influence, freeze new numeric parameters or authorize implementation. See the
[architecture](e-architecture.md) and [increment gates](e-roadmap.md).

E2-FINAL separately implements the internal owner-selected HISTORICAL candidate;
its [exact proposed revision-1 policy](influence-profile.md) and review vectors
do not approve or activate the schema-2 layout below. The current reader and
published payload remain schema 1/features 7; MINOR-01 compatibility is for E4.

## Recommendation and alternatives

Recommend schema **2**, still one immutable `LayerData<uint>` belonging to
`GenerationStage.Tectonics`. Keep all R/M records and their offsets unchanged;
append explicit Influence, mantle and parameter sections. A future complete
execution publishes features **31**: Ownership 1, Motion 2, Boundaries 4,
Influence 8 and Mantle 16. Do not publish partially computed schema-2 results.
E2/E3 can develop private builders without enabling a production result until
E4 integrates every real component. Zero-filled mantle is not an implementation.

| Alternative | Assessment |
| --- | --- |
| Schema 1 with reserved Influence bit 8 and four words per cell | Fits boundary distance and three deformation channels, but no mantle vectors, signed vertical field or profile metadata. Current readers reject this feature combination. |
| Pack mantle into the same four words | Loses independent channels or precision and changes meanings; reject. |
| Reuse schema-1 reserved header words without a new schema | Violates reserved-zero validation and historical readers; reject silent extension. |
| Managed objects or several published buffers | Violates the unmanaged single-result contract; no CORE change is needed or authorized. |
| Explicit schema 2 | Distinguishes new semantics, preserves legacy decoding and allows a checked, bounded layout; recommended subject to approval. |

## Word layout

All offsets and counts below are uint-word units. Floats use IEEE-754 binary32
bits, existing doubles retain their low/high words. Preserve the existing
canonical zero rule. Let N be the accepted Grid CellCount and B the exact number
of canonical unlike-owner edges. All products and sums are checked in `long`
before narrowing, indexing or allocating.

| Section | Offset | Words |
| --- | --- | --- |
| Header | 0 | 32 |
| Fourteen plate records | 32 | 14 * 8 = 112 |
| Packed owners | 144 | ceil(N / 4) |
| Existing boundary records | 144 + ceil(N / 4) | 5B |
| Influence | BoundaryOffset + 5B | 4N |
| Mantle | InfluenceOffset + 4N | 4N |
| Dynamics profile | MantleOffset + 4N | 12 |

TotalWordCount = ProfileOffset + 12 exactly; no overlapping sections, gaps or
trailing words. Existing plate, owner and boundary row encodings do not change.
The new per-cell sections use **eight words / 32 bytes per cell**, in addition
to packed ownership and sparse boundary records.

Header words 0..22 retain their existing roles, with these schema-2 values:
1 = schema 2; 2 = features 31; 13 = InfluenceOffset; 14 = InfluenceStride 4;
15 = total words; 16 = R revision 2; 17 = M revision 1; 18 = Influence revision;
21 = effective radius float bits; 22 = cutoff float bits. Words 19/20 retain
the frozen M thresholds. Words 3..12 retain Grid, plate and boundary metadata.

Only schema 2 assigns the following formerly reserved words:

| Header word | Meaning |
| --- | --- |
| 23 | MantleOffset |
| 24 | MantleStride = 4 |
| 25 | Mantle numeric revision |
| 26 | ProfileOffset |
| 27 | ProfileWordCount = 12 |
| 28..31 | Reserved, zero |

Influence and mantle revisions must be positive, explicitly supported and tied
to frozen profiles. E1 does not approve their first numerical revisions.

## Per-cell values and units

Influence row: `[DistanceToBoundary, Compression, Extension, Shear]`.
Distance is uint graph steps to the nearest endpoint of any boundary; endpoints
have distance zero. It is not the radius of the strongest source. The three
finite nonnegative floats are cm/year-equivalent kinematic diagnostics, not
stress or terrain height. They may coexist and cannot exceed the maximum
source amplitude in their respective channel. Gaussian radius and cutoff are
global metadata; a redundant per-cell reach value is unnecessary.

Mantle row for recommended model A:
`[Potential, FlowX, FlowY, SignedVertical]`. All four are finite dimensionless
diagnostics, not cm/year, temperature or pressure. Potential lies in [-1,1].
Flow X points east and Y south in accepted flat-grid coordinates. Positive
SignedVertical means upwelling, negative means downwelling. Accessors derive
`Upwelling = max(0, SignedVertical)`, `Downwelling = max(0, -SignedVertical)`
and vector magnitude without storing duplicate fields. This represents one net
vertical value, not simultaneous subcell upward and downward flows.

Potential is authoritative for reconstructing paired edge fluxes; cached X/Y
and vertical fields provide O(1) downstream queries. Their ideal component
bound is 12 (degree at most six, potential difference at most two). This is a
derived model bound, not an approved physical cap. E3 must freeze evaluation
order and a justified floating-point error budget before accepting stored
values or global circulation residuals. No arbitrary epsilon is approved here.

## Proposed twelve-word profile

| Relative word | Encoding and constraint |
| --- | --- |
| 0 | uint MantleModelKind = 1, graph-potential model A |
| 1 | uint broad periodic frequency, positive |
| 2 | uint regional frequency, greater than broad frequency |
| 3 | float boundary-potential weight, nonnegative |
| 4 | float broad-potential weight, nonnegative |
| 5 | float regional-potential weight, nonnegative; sum of weights positive |
| 6 | float positive kinematic reference, cm/year-equivalent |
| 7 | uint mantle seed-policy revision |
| 8 | uint Influence radius-policy revision |
| 9 | float positive radius fraction rho |
| 10 | float positive minimum radius in graph steps |
| 11 | Reserved zero |

All floats must be finite. Header radius must agree with the canonical
float computed from this profile and accepted Grid dimensions. Supported
revisions must identify exact approved parameter values, not arbitrary tuning
accepted merely because it is finite. Model identifiers and seed-policy revision
1 are proposals, not frozen runtime constants. Cutoff is header word 22.
E1 audit MINOR-02 clarification: radius and cutoff must both be finite and
strictly positive. Compute their product and its ceiling with checked
representability before narrowing to an integer, then cap traversal at N-1.
Reject nonfinite products or an unrepresentable integer reach with
OverflowException; reject invalid inputs before allocating a table. No numeric
cutoff interval is approved; historical limits must not become implicit defaults.
Choosing mantle alternative B requires a separate profile/layout review before
implementation; do not add a speculative generic solver parameter section.

## Immutable typed access

Proposed value-only API additions to `TectonicData`, subject to E4 review:

```text
bool HasInfluence; bool HasMantle;
uint InfluenceRevision; uint MantleRevision;
TectonicCellInfluence GetInfluence(int cellId);
MantleConvectionCell GetMantle(int cellId);
```

`TectonicCellInfluence` is a readonly value containing uint boundary distance
and three floats. `MantleConvectionCell` is a readonly value containing the four
stored floats and derived scalar properties above. A readonly profile value
exposes published revisions/parameters. Existing owner/plate/motion/boundary
accessors retain their contracts. Validate CellId first; an otherwise valid
query for an absent legacy feature throws `InvalidOperationException`.

Retain the immutable LayerData buffer and Grid view; do not escape arrays,
mutable spans, builders or context mutators. Do not regenerate partition,
noise or mantle in accessors. LayerData's existing defensive copy remains the
publication boundary; no change to its constraints or ownership is proposed.

## Validation and malformed data

Reject wrong stage/type, unknown schema/features/revisions, request/Grid size
mismatches, invalid section arithmetic, non-exact lengths, nonzero reserved
words and unsupported profile values before using offsets. Preserve all
current R/M ownership, connectivity, count, ordering and projection checks.
Reject NaN/infinity, negative deformation, noncanonical zeros, invalid distances
and mantle values inconsistent with the declared numeric policy.

Recompute nearest-boundary distance through accepted Grid neighbors. Validate
stored mantle fluxes against stored Potential in fixed canonical edge order;
the proposed production validator may recompute this O(N+E) calculation, but
must not rerun partition/noise generation. The separate global circulation
check uses E3's derived error budget. Range checks alone do not prove Gaussian
propagation: E2 tests require an independent all-source reference, exact stored
float comparisons and controlled seam/polar fixtures. Recommend full-field
Gaussian recomputation against the frozen policy at the checked decoding
boundary, comparing stored words. This can double propagation work for a new
candidate; include it in benchmarks. Cache the validated immutable view, never
rerun validation per accessor/frame. Any future trusted-builder shortcut needs
an explicit proof/review of which boundary replaces this check; it is not part
of this proposal.
Any builder/validation exception fails the candidate before publication, leaving
earlier Grid and historical immutable results intact under existing CORE rules.

## Compatibility and memory

Legacy schema-1 features 1 and 7 remain readable without rewriting buffers;
reserved feature 15 does not become a supported legacy format. Preserve all
fifteen original B and fifteen original M complete vectors. New integrated
buffers necessarily differ in header, length and added sections. Regression
tests must explicitly project their unchanged R/M prefix into canonical legacy
headers/lengths and compare every old word, while keeping new schema-2 goldens
separate. Do not replace P0, R, M or CAL01 historical evidence.

Payload bytes = **624 + 33N + 20B** (accepted sizes have N divisible by four).
The examples below are analytical, not measured E performance:

| Size | N | Seed-0 B | Example bytes | Conservative B <= all Grid edges | Upper payload bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small | 20,000 | 1,870 | 698,024 | 59,600 | 1,852,624 |
| Medium | 45,000 | 2,818 | 1,541,984 | 134,400 | 4,173,624 |
| Large | 80,000 | 3,944 | 2,719,504 | 239,200 | 7,424,624 |

The immutable publication copy can temporarily coexist with its draft payload.
Illustrative reusable Influence scratch: three source floats (12N bytes),
output distance/channels (16N), visited/distance/queue ints (12N): 40N bytes.
Illustrative mantle scratch: four float fields plus three double accumulators:
40N bytes. Each is 3,200,000 bytes at Large, before object overhead, builders,
noise, existing partition queues or the final packed copy. Scheduling/releasing
private buffers can reduce overlap; these are not a measured peak-heap claim.
No per-source N-sized retained arrays or duplicate published caches are needed.
Measure actual live/retained memory, allocations and costs in E5; no invented
acceptance threshold or hardware-independent performance guarantee.
