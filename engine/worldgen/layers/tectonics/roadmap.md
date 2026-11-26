# WG-01 — Tectonic Plates: Compact Roadmap

**Status:** A/B accepted; C + FIX01 formally accepted at v0.0.40. D delivered at v0.0.41 and audited favorably with observations per the owner's D-FIX01 request. Finding follow-up is recorded in audit-fix01.md. D acceptance and final Layer 01 closure remain pending; no roadmap closure or merge.
**Working branch:** `world-tectonics`, based on formally closed Grid `v0.0.35` (`503b3ec41a89b9f54cb47dede9f92506123b6a23`).  
**Target:** exactly **14 connected, irregular, unequal tectonic plates** for Small, Medium and Large.  
**Historical algorithm baseline:** weighted hex-region growth, multi-scale periodic noise, plate motion and boundary stresses.  
**Integration:** only after explicit owner authorization; no merge into `world` or `develop`.

## Delivery increments (four total)

### WG-01-A — Contracts and historical adaptation design

- Specify compact immutable tectonic data, exact published `LayerData<T>` representation, plate records and stage inputs without changing WG-CORE.
- Review the legacy tectonic algorithm and identify reusable formulas, topology adaptations, deterministic stream boundaries and memory risks.
- Specify 14-plate invariants, interfaces, parameter constraints, test oracles and implementation boundaries before writing production logic. Historical numeric values remain unapproved calibration proposals.

**Documentary delivery:** [readme.md](readme.md) records historical source review,
deterministic weighted growth, 14-seed connectivity guarantees, motion formulas,
parameter decisions, stress-propagation risks and planned tests T01–T12.
[contracts.md](contracts.md) defines one LayerData<uint> word buffer with a
checked schema and immutable typed view, proposed public/internal signatures,
future file layout and calculated memory bounds. No CORE extension, C# source,
test project or algorithm is implemented. Baseline v0.0.36 at
cdb00c3a6689d1e06fb25012b336b9a6a2da744d; delivery v0.0.37.

**Gate A:** the owner identifies the WG-01-A design as approved in the B request.
The referenced audit observations remain tracked; no new audit verdict is invented.
Source/history/contract, link/scope review and git diff --check are A evidence;
no builds or automated/GUI tests executed for documentation-only changes.

### WG-01-B — Fourteen-plate partition and CORE integration

- Implement farthest-candidate seeds, varied growth profiles, periodic domain-warped multiscale noise and competitive connected growth on real `HexGridData`.
- Ensure 14 nonempty connected regions, full unique cell assignment, deterministic replay, legal seam/pole neighbors and varied region sizes.
- Publish ownership-only schema features through the existing stage executor and orchestrator; exercise failure, retry, prerequisites and invalidation without creating a second generator. Resolve the numeric partition profile and fixed compatibility vectors before B publication; no motion or fake boundary data.

**Gate B:** exhaustive correctness/integration checks, independent audit and owner acceptance.

**B delivery:** schema-1 Ownership result and independent headless checks
implemented. R selected explicitly by the owner after CAL01; exact cost words
and constants frozen as tectonic-partition-r-v2 (revision 2), with 15 complete
production buffers and equivalence checks against all 75 archived R cases.
10/10 tectonic groups, Grid/LAB/menu regressions and Release builds passed.
[calibration.md](calibration.md) retains P0/CAL01 evidence and final measurements.
OBS-03 numeric calibration closed; OBS-01/02/04 remain deferred as documented.
No increment acceptance, C/D development or merge is implied.

### WG-01-C — Motions, boundaries and Laboratory display

- Assign deterministic per-plate directions and speeds; detect unique inter-plate edges and classify relative convergence, divergence, transform, mixed and quiet interactions.
- Determine and validate useful distance/stress influence fields; resolve units, speed/threshold calibration and measure propagation expense before adopting the legacy implementation. Optional influence output remains unavailable unless its semantics and cost are accepted; boundary kinematics alone must not masquerade as computed fields.
- Add a Laboratory view of the **real published** 14-plate map, boundaries, motion and selected-cell information without changing the normal `INICIAR` behavior.
- Preserve current LAB Grid display and accepted rendering contracts; native GPU/DPI caveats remain recorded.

**Gate C:** tectonic physics/geometry and LAB regression checks, visual review, independent audit and owner acceptance.

**C delivery:** the owner selected exactly M and the policy in
[motion-calibration.md](motion-calibration.md). Motion revision 1, schema 1,
flags 7; Influence disabled. [delivery-c.md](delivery-c.md) records real motion,
complete unique interfaces, immutable publication, Laboratory display, fifteen
new complete M vectors, preservation of all fifteen B buffers, exact agreement
with 75 M cases, 15/15 tectonic groups and regressions. OBS-02 numerical policy
closed for delivery. C + FIX01 were subsequently formally accepted at v0.0.40.
The later D authorization is verification-only; no broader fixes or merge is implied.

### WG-01-D — Full verification and final closure

- Verify all three sizes, deterministic seeds, 14 connected regions, boundary pairs, interactions, CORE lifecycle and LAB behavior.
- Benchmark generation, queries, allocations, stress propagation and large-map rendering as evidence, with no invented pass/fail thresholds.
- Independently audit the complete layer, reconcile observations and present WG-01 for formal owner closure.

**Gate D:** favorable independent audit and **explicit owner acceptance**. Branch integration requires a separate authorization.

**D evidence:** [verification-d.md](verification-d.md) maps all requirements to
executed checks, frozen vectors, CPU/query/memory and real offscreen GL metrics.
Tectonics 16/16, Laboratory 19/19, menu 16/16, complete Grid regressions and
Release builds pass. No production algorithm/profile/LAB change was made.
No blocking functional defect was found in the executed scope. The subsequent
favorable audit with observations is recorded in [audit-fix01.md](audit-fix01.md).
N01 is PASS by owner declaration; N02–N17 remain PENDING for native confirmation.
No formal D acceptance or Layer 01 closure is claimed. Influence remains absent
and unbenchmarked, pending a future decision.

## Governance

**Separate authorized E1 extension:** the owner requested documentation for
integrated dynamics at baseline v0.0.42. The [five-increment E roadmap](e-roadmap.md)
preserves the four historical increments above. E1 and E2-CAL01 were delivered;
the separately authorized [E2-FINAL](e2-final.md) is submitted for review.
Subsequent [E3-CAL01](e3-calibration.md) authorizes private Model A calibration
only; E3-FINAL and E4/E5 remain unauthorized. Frozen R/M/Influence and normal publication remain unchanged;
this extension does not close GATE-01 or accept D/Layer 01.

- Architect specifies; Dev implements only an authorized increment; independent Auditor reviews; owner accepts.
- Every authorized repository-changing commit increments root `VERSION`, and reports an English commit message plus its full SHA.
- A/B and the FIX01 baseline are accepted; R and M are frozen. D is explicitly authorized for verification, documentation and tests only. No changes to accepted production behavior, Grid, WG-CORE, State, Saves, other layers or normal creation are authorized; functional defects require separate owner authorization.
- Keep the Grid audit observations and `world-grid` history intact; do not delete or retire its roadmap.
- `world-tectonics` is a working branch based on the accepted Grid commit because `world-grid` has not been integrated into `world`. This does not authorize synchronization or merging.

**Layer responsibility:** [readme.md](readme.md)  
**Data/API contract:** [contracts.md](contracts.md)
**General roadmap:** [World Generation Roadmap](../../roadmap.md)
