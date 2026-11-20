# Layer 00 — Hex Grid: Implementation Roadmap

**Repository:** `fgabo404/Evolve`  
**Integration branch:** `world`  
**Delivery branch:** `world-grid`\
**Status:** WG-00-A/FIX01, WG-00-B and WG-00-C formally accepted; WG-00-D implemented pending independent technical audit and formal owner acceptance; WG-00-E not started.\
**Baseline:** WG-CORE accepted and integrated; `world` at `v0.0.26` before this documentation change  
**Scope:** WG-00 only; no other geographical layers or Laboratory UI implementation

## 1. Goal and authority

Deliver one immutable, deterministic, compact flat-top hexagonal grid compatible with the existing `WorldGenerationOrchestrator` and `GenerationContext`. The grid is the shared spatial foundation of the eight-stage pipeline. Reuse and adapt verified mathematical behavior from the historical `WorldGenerationOLD` source; do not restore the old generator, parallel orchestration, state model, renderer, or persistence system.

This roadmap organizes already discussed preliminary decisions D01–D11 into implementation increments. A preliminary design decision is not a verified algorithm. Each unresolved technical detail must be resolved and validated at its corresponding gate, with owner approval before advancing. This document does not authorize code changes.

References:
- [Repository standards](../../../../AGENTS.md)
- [World generation architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
- [WG-CORE contracts](../../core-contracts.md)
- [Grid responsibility](readme.md)
- [General world generation roadmap](../../roadmap.md)

## 2. Design traceability: D01–D11

| ID | Owner decision / constraint | Implementation gate | Verification |
| --- | --- | --- | --- |
| D01 | Regular flat-top hexagons, no geometric distortion | A | V03, V04 |
| D02 | Canonical column/row Offset addresses; derived Axial coordinates | A | V02, V03 |
| D03 | Cylindrical horizontal topology: east/west wrap, bounded north/south; no spherical polar joins | A | V04, V05 |
| D04 | Rectangular Offset matrix with equal row count for every column and stepped physical polar outline | A, B | V01, V04 |
| D05 | Deterministic center longitude/latitude as normalized cylindrical references, not geodesic/spherical distances | A, B | V03 |
| D06 | Preserve regular grid geometry; target a 2:1 *presentation frame* without stretching or changing cell counts | A, B (geometry only) | V03; UI work deferred |
| D07 | Small 200×100, Medium 300×150, Large 400×200; only these three official sizes | B | V01, V07 |
| D08 | Stable row-major CellId, compact immutable spatial result, computed queries, later-stage data separately indexed | B, C | V02, V06, V07 |
| D09 | Seven headless validation groups V01–V07 | D | All |
| D10 | Formalize flat-top invariants, odd-column shift, periodic neighbors and shortest-path distances; prove or independently validate assumptions | A, D | V03–V05 |
| D11 | Reuse *adapted* legacy grid mathematics/tests where compatible; modern WG-CORE remains authoritative | A–D | Code review and regression tests |

All design approvals are preliminary unless and until the layer-specific technical contract and audit evidence are accepted.

## 3. Fixed grid configuration and mathematical invariants

| Size | Columns (C) | Rows (R) | Cell count | CellId range | Expected undirected adjacency edges |
| --- | ---: | ---: | ---: | --- | ---: |
| Small | 200 | 100 | 20,000 | 0–19,999 | 59,600 |
| Medium | 300 | 150 | 45,000 | 0–44,999 | 134,400 |
| Large | 400 | 200 | 80,000 | 0–79,999 | 239,200 |

- Offset ranges: `0 <= column < C`, `0 <= row < R`. CellId is `row * C + column`; inverse uses integer quotient and remainder. CellId is stable **within a grid configuration**, not a cross-world global identifier.
- Orientation: regular flat-top hexagons, with odd columns shifted downward by half the vertical row spacing. Axial conversion proposal: `q = column`, `r = row - floor(column / 2)` (mathematical floor for signed coordinates); exact inverse must be checked.
- Center spacing for radius/side length `s`: horizontal `3s/2`, vertical `sqrt(3)s`; preserve uniform shape and size.
- All C values are even. The periodic axial translation is `(q,r) ~ (q + k*C, r - k*C/2)` for integer `k`. Canonical normalization must retain the correct Offset row across the seam.
- Exactly six neighbors for interior rows. With the stated odd-column-down convention: north/south boundary cells have either three or five neighbors, alternating with column parity. No north/south wrap, self-neighbor, duplicate neighbor, or invalid identifier.
- Expected counts for all supported sizes: `C` cells with three neighbors, `C` with five, `C*(R-2)` with six. Undirected edges equal `3*C*R - 2*C`. Prove these counts against explicit neighbor enumeration.
- Normalized longitude proposal: `-180 + 360*(column + 0.5)/C`, with a half-open periodic interval `[-180,180)`.
- Normalized latitude proposal: `90 - 180*(row + (column mod 2)/2 + 0.5)/(R+0.5)`; centers remain strictly inside (-90,90). These values represent a cylinder's abstract latitude/longitude references, not physical distances or cell areas on a sphere.
- The regular grid's geometric envelope is approximately 1.73:1 at the approved sizes. The 2:1 viewport is a later presentation requirement, not a Grid transform; no artificial columns, padding cells, distortion, or changes to C/R.
- The cylindrical shortest distance must equal the minimum graph path using valid cell neighbors. Candidate computation minimizes the axial hex distance across periodic target images; verify its correctness at polar boundaries and the seam against independent breadth-first search (BFS) before accepting a bounded-image optimization.
- All geometry and topology depend only on approved grid configuration, not seed or world type. A Grid implementation must not consume random values needlessly.

These equations preserve the design baseline. WG-00-A including FIX01 is now
formally accepted; WG-00-B reuses its implemented conventions without changes.

## 4. Reuse inventory and exclusions

Historical reference: `WorldGenerationOLD` (also available as `worldgeneration` at the inspected baseline).

| Historical path | Intended reuse | Required adaptation |
| --- | --- | --- |
| `GEvolve.Engine/Grid/FlatHexGeometry.cs` | Flat-top centers, Offset/Axial conversion, periodic wrapping, point lookup and distance candidate | Verify mathematical correctness with current dimensions, signed coordinates, boundaries, and new ownership rules |
| `GEvolve.Engine/Grid/HexGrid.cs` | Row-major IDs, spatial query semantics, neighbor enumeration | Replace the per-cell precomputed `GridCell[]` design with the approved compact immutable data model; integrate WG-CORE |
| `GEvolve.Engine/Geometry/HexCoord.cs` | Axial addition, directions and distance arithmetic | Use English naming and repository comment conventions; avoid duplicate coordinate types |
| `GEvolve.Engine/World/worldgenerationold/LandmassGeneration/Shared/HexTopology.cs` | Reference for seam-aware adjacency and compact neighbor iteration | Prefer computed queries; only permit a neighbor cache if evidence justifies its memory/performance cost |
| `GEvolve.Tests/GridTests.cs` | Exhaustive cell invariants, seam tests, BFS distance oracle and measurement patterns | Remove old save/state/UI dependencies, update sizes and exact assertions, use the current test harness |

Do not import the old `WorldGenerationService`, `PangaeaGenerationSession`, `DeterministicRandom`, old State/Saves, pointy-top `HexGeometry`, legacy renderer, tectonics, continents, elevations, climate or biomes as part of WG-00. Future-stage reuse requires separate review and authorization. The old code's existing tests are historical evidence only: they are not claimed to pass against the new design.

## 5. Proposed code organization (subject to contract review)

```text
engine/worldgen/layers/grid/
    readme.md                     # Existing
    roadmap.md                    # This document
    HexGridLayer.cs               # Proposed WG-CORE stage executor
    HexGridData.cs                # Proposed immutable spatial result
    geometry/
        HexCoordinates.cs         # Proposed Offset/Axial value types
        FlatHexGeometry.cs        # Proposed geometry and periodic arithmetic
        HexTopology.cs            # Optional; create only if justified

tests/worldgen/grid/                # Proposed new headless checks
    HexGridGeometryChecks.cs
    HexGridTopologyChecks.cs
    HexGridIntegrationChecks.cs
    HexGridPerformanceChecks.cs
```

Names and exact namespaces are proposals, not commitments to unnecessary classes. Keep the existing lowercase module boundaries. The current CORE contracts, not historical class names, determine integration. No second generation orchestrator, new GUI window, OpenGL dependency in `engine/`, or automatic `WorldState` mutation.

## 6. Increment plan and approval gates

### WG-00-A — Geometry and coordinates

**Scope:** inspect and adapt legacy geometry/coordinate algorithms; specify flat-top centers, Offset/Axial conversion, direction order, canonical wrap, normalized lat/lon, bounds and cylindrical shortest-distance mathematics.

**Deliverables:**
- Explicit geometry and coordinate contract, including signed/invalid input behavior and rounding/point-picking policy if exposed.
- Verified periodic seam and polar-boundary math; a documented proof or BFS-backed correction of the shortest-distance formula.
- Focused headless geometry/topology checks derived from legacy tests, without starting any other layer.

**Gate A:** owner reviews API/conventions and evidence; independent validation of seam, corner, inverse conversions and distances before permission to proceed to B.

**WG-00-A-FIX01:** the owner reports audit approval with observations at
`7db3c2c70fa11173702640a232f4e909423d07a1` (`v0.0.28`), following
1,160,000 independent BFS comparisons. The minimal correction uses long
periodic-image/cube-distance intermediates with a checked final int conversion,
and rejects non-finite computed extents. Focused overflow regressions extend
the existing headless suite. No change to approved geometry, topology, public
signatures or dimensions. The owner subsequently formally accepted WG-00-A
including FIX01 at `0631717ed123089da83e3b864f187ab0a2a60bbc` (`v0.0.29`)
and authorized WG-00-B. The Auditor's remaining .NET 10 native-execution
limitation is preserved; developer-native checks do not imply the Auditor
performed native .NET 10 reproduction.

### WG-00-B — Compact immutable grid data

**Scope:** implement only the approved three presets, row-major CellId, immutable grid result and computed spatial queries using A's validated mathematics.

**Deliverables:**
- Dimension/ID validation and deterministic normalized geographic lookup.
- Compact ownership with no mutable array or list escaping consumers; no per-cell terrain/biome/plate properties.
- Memory allocation assessment for Small, Medium and Large. An optional adjacency cache requires measurements and owner approval.

**Gate B:** the owner reviews immutable data contract, exhaustive cell coverage, size/ID invariants and measured memory before permission to proceed to C.

**Implementation:** `HexGridData.cs` stores only two readonly dimensions, selects
the three approved presets using existing MapSize, calculates IDs/coordinates
and delegates spatial mathematics to accepted WG-00-A geometry. No per-cell
objects, neighbor caches or WG-CORE changes. Exact API, developer validation
and measured construction/memory evidence are recorded in [readme.md](readme.md).
The owner subsequently formally accepted WG-00-B at
`3115c6362d46d0340ca4ec72902ea0edd4d17a45` (`v0.0.30`) and authorized
WG-00-C. The Auditor's native .NET 10 execution limitation remains recorded.

### WG-00-C — WG-CORE integration

**Scope:** adapt Grid to the existing `IWorldGenerationLayer` / `LayerData` contracts, with orchestrator-controlled validation, atomic publication, status and rebuild lifecycle.

**Deliverables:**
- Single Grid executor, no new orchestration path or parallel generator.
- Tests using the registered eight-stage contract with test-only placeholders for unfinished layers as needed.
- Verify `RunNext`, `RunUntil` / `RunAll` when applicable, failure atomicity, immutable access, deterministic reexecution and `RebuildFrom(Grid)` suffix invalidation.

**Gate C:** owner review of integration and regressions; WG-CORE must remain functionally unchanged unless a minimal separately approved extension is demonstrated necessary.

**Implementation:** HexGridLayer publishes one unmanaged HexGridDescriptor
through the existing LayerData contract. The descriptor validates via accepted
HexGridData construction and stores only MapSize; ToGridData reconstructs the
same approved dimensions and calculated queries. No WG-CORE source or public
contract changed, no random consumption and no per-cell storage. Exact APIs
and developer evidence are recorded in [readme.md](readme.md).
The owner formally accepted WG-00-C before authorizing WG-00-D, based on
`v0.0.34` / `7ae354f16d602b59eaec5c2c6b3c5637e9d638a8`.
LAB-GRID-02 including FIX01 is also formally accepted and remains a separate
visual delivery; its two non-blocking observations remain documented in LAB.

### WG-00-D — Complete test suite and performance evidence

**Scope:** consolidate the seven D09 groups, perform exhaustive topology checks for all sizes, independent BFS comparison, regression checks and controlled benchmarks.

| Group | Acceptance target |
| --- | --- |
| V01 — Dimensions | Exact C, R and C×R values for all three presets; no missing/extra cells |
| V02 — Identity | Unique contiguous IDs, exact round-trip coordinates, stable rebuild |
| V03 — Coordinates | Exact Offset/Axial round-trips and bounded normalized geographic centers |
| V04 — Topology | Valid unique reciprocal neighbors; known 3/5/6-neighbor counts; seam and polar checks; correct edge totals |
| V05 — Distance | Zero, symmetry, adjacent distance 1, triangle inequality, seam routes and comparison to BFS |
| V06 — Core integration | Correct lifecycle and publication, immutability, failure behavior, deterministic replay and rebuild invalidation |
| V07 — Performance | Record build time, retained/allocated memory, neighbor and distance-query throughput per preset |

**Gate D:** all applicable functional checks pass, with no unexplained failures. Performance measurements must report hardware/runtime, methodology and actual values; no unmeasured numerical budget is deemed approved. Audit regressions against retained WG-CORE contracts; no claim that retired CORE check files remain present.

### WG-00-E — Independent audit and formal acceptance

**Scope:** independent review of mathematical correctness, source and dependency boundaries, tests, documentation, versioning and measured performance.

**Deliverables:**
- Auditor verdict and evidence; correction loop if required.
- Inventory of created/modified/deleted files; exact English Git commit messages and full SHAs.
- PowerShell verification commands, recorded outputs and explicit limitations.
- Owner's formal acceptance before integration into `world`, followed by any separately authorized synchronization with `develop`.
- Update/retire this roadmap only under owner authorization.

**Gate E:** no acceptance or integration without auditor approval and explicit owner decision.

## 7. Execution governance

- Architect specifies; Codex implements the authorized increment; Auditor independently verifies; the owner accepts.
- Implement only one authorized increment at a time, on the explicitly authorized `world-grid` working branch. Its existing implementation does not authorize later increments.
- Respect `AGENTS.md`: project content and commit messages in English; Spanish owner-facing reports; English C# file-purpose `//` comments and no XML/block comments.
- Every authorized repository-changing commit increments root `VERSION` in that same commit, including documentation edits. Report commit name, complete SHA and PowerShell validation instructions.
- Do not alter other layers, the old source branch, `game/` rendering, `state/` or `saves/`; do not import older algorithms merely because they exist.
- Do not interpret test definitions as executed evidence. WG-00-A/FIX01, WG-00-B and WG-00-C are formally accepted. WG-00-D developer evidence does not replace independent audit or owner acceptance. WG-00-E, GUI inspection and branch integration require their respective authorizations.

## 8. Current status

- [x] Owner selected the D01–D11 preliminary design direction, historical algorithm reuse and the A–E roadmap structure.
- [x] Detailed implementation roadmap documented.
- [x] Authorize the `world-grid` working branch and WG-00-A implementation.
- [x] Implement WG-00-A geometry/coordinates; audit approved with observations.
- [x] Implement authorized FIX01 numeric corrections and focused regressions.
- [x] Owner formally accepted WG-00-A including FIX01 and authorized WG-00-B.
- [x] Record the Auditor's remaining .NET 10 native-execution limitation without claiming independent native reproduction.
- [x] Implement WG-00-B and run exhaustive identity/topology, BFS and construction-memory checks.
- [x] Owner formally accepted WG-00-B and explicitly authorized WG-00-C.
- [x] Implement WG-00-C using the real Grid executor and unchanged public WG-CORE contracts.
- [x] Owner confirmed formal WG-00-C acceptance before authorizing WG-00-D.
- [x] Implement WG-00-D V01–V07 validation and reproducible performance evidence.
- [ ] Independently audit WG-00-D and obtain formal owner acceptance.
- [ ] Complete WG-00-E independent audit and owner acceptance.
- [ ] Integrate into `world` only with explicit authorization.

**Document status:** WG-00-A/FIX01, WG-00-B and WG-00-C formally accepted;
WG-00-D implemented at v0.0.35, pending independent audit and owner acceptance.
WG-00-E has not started. This does not close WG-00 or authorize integration into
`world` or synchronization with `develop`.

FIX01 developer evidence at `v0.0.29`: the complete WG-00-A headless suite
passed, including new numeric-limit regressions and the existing 1,160,000 BFS
comparisons across 200×100, 300×150 and 400×200. The engine Release build
completed with zero warnings/errors and whitespace verification passed.
That FIX01 developer record predates the owner's subsequent formal acceptance.

WG-00-B developer evidence at `v0.0.30`: the complete headless grid project
passed all retained A/FIX01 checks and new B assertions over 145,000 cells.
The 1,160,000 existing BFS comparisons remain, with another 1,160,000 comparing
the grid API directly against BFS. Exact edge totals and invalid inputs passed.
Engine Release build: zero warnings/errors. Construction batches allocated
24 B/grid for all sizes, with actual retained heap/timing figures and methodology
in [readme.md](readme.md); no acceptance thresholds are invented.
That WG-00-B developer record predates the owner's subsequent formal acceptance.

WG-00-C developer evidence at `v0.0.31`: complete headless grid suite passed,
including all A/FIX01/B regressions and ten named production-core integration
checks. The 2,320,000 existing geometry/grid-API BFS assertions remain intact.
Grid outputs match all sizes and remain independent of seed/world type;
failure/retry, rebuilding, finalization and operation exclusivity are exercised
through the existing orchestrator. Engine Release build: zero warnings/errors;
whitespace verification passed. D01–D11 traceability is preserved.
That historical C evidence predates formal acceptance and the WG-00-D
measurements below. The Auditor's native .NET 10 limitation remains documented.

## 9. WG-00-D delivery evidence

Authorized baseline: v0.0.34 / 7ae354f16d602b59eaec5c2c6b3c5637e9d638a8.
Delivery: v0.0.35. Only tests, evidence, documentation and VERSION changed;
accepted Grid/WG-CORE source, interfaces, geometry and D01–D11 are unchanged.

All correctness groups V01–V06 passed: 145,000 cells, 1,160,000 geometry BFS
comparisons plus 1,160,000 Grid API comparisons, ten core integration checks
and 1,015,000 deterministic metric triangles. BFS now has independent explicit
Offset adjacency instead of calling production neighbor mathematics.
No exhaustive Large all-pairs search was introduced.

V07 is a separate --performance mode: all nine requested operations, all three
presets, 20,000 warm-up calls per operation/preset and five 200,000-call batches.
Raw results, variation, environment, methodology and the V01–V07 executable
mapping are in [readme.md](readme.md) and its linked performance evidence.
Performance has no invented pass/fail budget. Existing construction measurement
was moved out of the functional B checks into the dedicated performance mode.

Regression evidence: Laboratory 18/18; menu 16/16; Engine and Game Release
builds with zero warnings/errors; compiled display version v0.0.35;
git diff --check passed. No new native UI/GPU validation is claimed.
Submit WG-00-D for independent technical audit. No WG-00-E or merge is authorized.
