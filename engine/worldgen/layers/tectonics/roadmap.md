# WG-01 — Tectonic Plates: Compact Roadmap

**Status:** Planning prepared; no WG-01 implementation authorized yet.  
**Working branch:** `world-tectonics`, based on formally closed Grid `v0.0.35` (`503b3ec41a89b9f54cb47dede9f92506123b6a23`).  
**Target:** exactly **14 connected, irregular, unequal tectonic plates** for Small, Medium and Large.  
**Historical algorithm baseline:** weighted hex-region growth, multi-scale periodic noise, plate motion and boundary stresses.  
**Integration:** only after explicit owner authorization; no merge into `world` or `develop`.

## Delivery increments (four total)

### WG-01-A — Contracts and historical adaptation design

- Specify compact immutable tectonic data, exact published `LayerData<T>` representation, plate records and stage inputs without changing WG-CORE.
- Review the legacy tectonic algorithm and identify reusable formulas, topology adaptations, deterministic stream boundaries and memory risks.
- Approve 14-plate invariants, interfaces, parameter ranges, test oracles and implementation boundaries before writing production logic.

**Gate A:** independent design review and formal owner acceptance.

### WG-01-B — Fourteen-plate partition and CORE integration

- Implement farthest-candidate seeds, varied growth profiles, periodic domain-warped multiscale noise and competitive connected growth on real `HexGridData`.
- Ensure 14 nonempty connected regions, full unique cell assignment, deterministic replay, legal seam/pole neighbors and varied region sizes.
- Publish tectonic region ownership through the existing stage executor and orchestrator; exercise failure, retry, prerequisites and invalidation without creating a second generator.

**Gate B:** exhaustive correctness/integration checks, independent audit and owner acceptance.

### WG-01-C — Motions, boundaries and Laboratory display

- Assign deterministic per-plate directions and speeds; detect unique inter-plate edges and classify relative convergence, divergence, transform, mixed and quiet interactions.
- Determine and validate useful distance/stress influence fields; measure propagation expense before adopting the legacy implementation.
- Add a Laboratory view of the **real published** 14-plate map, boundaries, motion and selected-cell information without changing the normal `INICIAR` behavior.
- Preserve current LAB Grid display and accepted rendering contracts; native GPU/DPI caveats remain recorded.

**Gate C:** tectonic physics/geometry and LAB regression checks, visual review, independent audit and owner acceptance.

### WG-01-D — Full verification and final closure

- Verify all three sizes, deterministic seeds, 14 connected regions, boundary pairs, interactions, CORE lifecycle and LAB behavior.
- Benchmark generation, queries, allocations, stress propagation and large-map rendering as evidence, with no invented pass/fail thresholds.
- Independently audit the complete layer, reconcile observations and present WG-01 for formal owner closure.

**Gate D:** favorable independent audit and **explicit owner acceptance**. Branch integration requires a separate authorization.

## Governance

- Architect specifies; Dev implements only an authorized increment; independent Auditor reviews; owner accepts.
- Every authorized repository-changing commit increments root `VERSION`, and reports an English commit message plus its full SHA.
- No implementation is implied by this planning commit. Do not start WG-01-A code, alter Grid, WG-CORE, State, Saves, other geographic layers, or normal game creation without separate authorization.
- Keep the Grid audit observations and `world-grid` history intact; do not delete or retire its roadmap.
- `world-tectonics` is a working branch based on the accepted Grid commit because `world-grid` has not been integrated into `world`. This does not authorize synchronization or merging.

**Layer responsibility:** [readme.md](readme.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)
