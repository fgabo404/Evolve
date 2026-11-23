# Selected Influence profile: exact review contract

**Proposed identifier:** `tectonic-influence-historical-v1`.
**Proposed initial numeric revision:** 1, independent from R revision 2 and M
revision 1. HISTORICAL was selected by the owner for E2-FINAL implementation
and validation. Definitive freezing requires architectural review, independent
audit and express owner acceptance. No `Approved` production setting is added.

The internal `TectonicInfluenceProfile` supplies explicit parameters to existing
builders. It is not called by TectonicPlatesLayer, does not consume randomness
and does not modify the published schema-1/features-7 payload. The future
schema-2 codec and Mantle remain outside E2-FINAL.

## Constants and resolution

| Parameter | Stored binary32 word | Exact value |
| --- | --- | --- |
| Relative radius fraction | 0x3CA3D70A | 0.0199999995529651641845703125 |
| Minimum radius, graph steps | 0x3F800000 | 1 |
| Cutoff multiplier | 0x40400000 | 3 |

Resolve `float(max(double(minimum), double(fraction)*min(columns,rows)))`.
The casts promote already stored binary32 values exactly to binary64; do not
replace fraction with a different binary64 literal `0.02`. Round the result
to binary32 before using it in any reach/kernel calculation.

| MapSize | Effective radius / binary32 word | Inclusive integer reach |
| --- | --- | ---: |
| Small | 2 / 0x40000000 | 6 |
| Medium | 3 / 0x40400000 | 9 |
| Large | 4 / 0x40800000 | 12 |

Radius/cutoff must be finite and strictly positive. Calculate their promoted
double product and ceiling, require int representability, then cap to N-1.
Reject an unrepresentable radius/reach with OverflowException before allocation.
Reject invalid/nonfinite inputs with ArgumentOutOfRangeException. There is no
new generally approved cutoff interval: generic internal parameters still
accept representable positive values outside historical [1,5]. This selected
profile's cutoff is exactly 3. E1 MINOR-02 evidence remains intact.

## Source and numerical policy

- Use only stored M boundary components. Compression is max(0,-SignedNormal),
  Extension is max(0,SignedNormal), Shear is abs(SignedTangent).
- Both endpoints are sources. At a shared endpoint retain the maximum per
  channel, never sum; channels coexist. All real boundary endpoints, including
  zero-amplitude/quiet ones, seed the independent distance field.
- Use minimum integer distance through approved HexGridData neighbors, with
  east-west continuity and bounded poles. Plates do not obstruct propagation.
- For each shell d through inclusive reach K, compute
  `weight[d]=(float)Math.Exp(-Math.Pow(d/(double)radius,2))`. A float source
  multiplied by this float weight rounds to float before maximum reduction.
  Do not fuse another operation, sum contributions, substitute linear/Euclidean
  attenuation or reduce to a nearest-source amplitude.
- Traverse sources by ascending CellId and canonical Grid directions. Finite
  nonnegative maxima are commutative; permuted input enumeration does not
  change values. Cached attenuation and shared three-channel BFS preserve the
  historical expression order. No per-source whole-world allocation.
- Reject negative/nonfinite amplitudes. Accept negative input zero as zero and
  retain canonical positive output zeros, including underflow. No per-map
  normalization. Units are cm/year-equivalent kinematic diagnostics, not
  pressure, force, elevation or strain-rate predictions.

The stored-radius and float-weight/product order are pinned by exact-bit
tests and complete fixtures. Reproducibility is demonstrated on the recorded
.NET 10 Windows x64 environment, including positive/negative/zero/extreme
64-bit seeds and every world type. Math.Exp/Math.Pow are runtime math functions;
cross-platform/runtime bit identity is not claimed without reproduction.
New outputs must fail reference checks rather than silently replace vectors.
Any intentional numerical change requires reviewed revision/reference changes.

## Validation boundaries

`Validate` checks dimensions, finite/range/zero constraints and exact distance
invariants (zero iff boundary, neighbor differences <=1, descent to zero).
`ValidateComplete` additionally builds fresh production distance/Gaussian fields
and compares every uint/float bit. It measures a proposed E4 trust-boundary
cost, not an enabled reader or independent oracle. It does not extract new M
sources, mutate original fields or publish anything.

The independent test oracle decodes raw stored M words, computes its own Offset
neighbors and target-centered BFS shells, and directly evaluates all applicable
source envelopes without production traversal, weight tables or extraction.
Complete real-map/dense/sparse comparisons complement production recomputation;
same-code replay alone is not proof of mathematical correctness.

## Reference data

[Influence fixtures](../../../../tests/worldgen/tectonics/influence-vectors/readme.md)
contain fifteen unique complete distance/C/E/S buffers and sixty explicit
size/seed/world-type cases. Compression is lossless Brotli. The authoritative
algorithm comparison uses all decompressed bytes and raw SHA-256. A separate
compressed SHA-256 protects the physical file; compression byte identity on a
different runtime is not an algorithm determinism promise.

Each case also binds the original B ownership and M motion full-buffer hashes;
all those historical files remain unchanged. These are pinned **review
references**, not owner acceptance or published schema-2 snapshots. See
[E2-FINAL delivery](e2-final.md) for executed checks, PERF-01/TEST-01 evidence and
unresolved governance/native limitations.
