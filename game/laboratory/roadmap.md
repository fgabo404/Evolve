# Laboratory Visual Deliveries

**Repository:** `fgabo404/Evolve`
**Working branch:** `world-grid`
**Authority:** explicit project-owner request for LAB-GRID-01.
**Engine baseline:** `v0.0.31`, `4f0049dba50f68b426c99bb7bafb749648c16d42`.
**Delivery version:** `v0.0.32`.
**LAB-GRID-01 status:** initial implementation delivered but not visually accepted; its visual design is superseded by corrective LAB-GRID-02 below.

## Independent scope

This separately authorized visual delivery neither advances nor closes
[WG-00](../../engine/worldgen/layers/grid/roadmap.md). D01–D11, accepted
WG-00-A/B/C and WG-CORE contracts remain unchanged. WG-00-D/E, other geographic
layers, saves and active WorldState integration are excluded.
The existing EvolveWindow presents menu and LAB; no second window or generator.

## Delivery gates

- [x] Inspect approved architecture, Grid and WG-CORE contracts.
- [x] Connect LAB and return to the menu within the existing window.
- [x] Configure existing size/type enums and a validated signed 64-bit seed.
- [x] Create contexts via CreateGeneration and execute only Grid via RunNext.
- [x] Inspect actual GenerationProgress and LayerData<HexGridDescriptor>.
- [x] Guard unavailable stages without producing candidate geography.
- [x] Cache regular outlines, use uniform zoom/pan, initial fit and canonical
      selection with engine IDs and geographic information.
- [x] Run production-model headless checks, menu/Grid regressions and Release builds.
- [ ] Complete manual visual/native-event/GPU-lifetime validation.
- [ ] Obtain independent audit and formal owner acceptance.

## Evidence and remaining limits

Developer evidence: 11/11 LAB checks, 16/16 menu checks and the complete
A/FIX01/B/C Grid suite passed, including 10/10 integration checks and 2,320,000
BFS assertions. Engine and Game Release builds reported zero warnings/errors.
See [readme.md](readme.md) for commands, file responsibilities, capability-guard
registration, rendering tradeoffs and the pending manual checklist.

No visual run, runtime shader approval, framerate budget, native DPI/focus
approval or actual graphics-resource-release observation is claimed.
Rendering work remains separate from pending WG-00 query-performance work.
This roadmap authorizes no follow-on delivery or branch merge.

## LAB-GRID-02 — Corrective redesign and world creation

**Authority:** explicit owner request and supplied principal reference image;
latest owner clarification keeps Evolve's original blue colors and uses the
reference for structure.
**Baseline:** `v0.0.32`, `db1900966bf49f22aa1c91131ef3cb4d2519d2d8`.
**Delivery:** `v0.0.33`.
**Historical status:** Laboratory design visually approved by the owner; the
INICIAR scope below is superseded by LAB-GRID-02-FIX01.

LAB-GRID-01 was not visually accepted. This correction replaces its full-width
toolbar with a scrollable compact left sidebar and large right canvas. LAB and
INICIAR reuse the same configuration, dropdowns, stage cards, map renderer,
camera and cell-information components. Accepted mathematical/core sources,
D01–D11, State and Saves remain unchanged. No WG-00-D/E or merge is authorized.

- [x] Shared enum-backed dropdowns, validated seed editing and explicit randomization.
- [x] Connect both LAB and INICIAR using tested production navigation.
- [x] LAB manual context creation and next eligible capability execution.
- [x] INICIAR automatic progression via the same core RunNext path, stopping at
      unavailable capability or failure, with a clearly partial map.
- [x] Show all eight canonical stages and distinguish actual completion/failure
      from unavailability; inspect only real completed Grid output.
- [x] Return to configuration/menu without finalization, playable-world claims,
      State promotion or persistence.
- [x] Preserve uniform-scale cached Grid visualization and add empty-map guidance.
- [x] Headless shared-screen/configuration/capability/layout tests and all menu/
      Grid regressions; clean Engine/Game Release builds.
- [ ] Compare native UI against the reference across sizes and DPI settings.
- [ ] Independently audit, resolve authorized observations and obtain owner acceptance.

Developer evidence: 18/18 shared-screen checks, 16/16 menu checks and the complete
A/FIX01/B/C Grid suite passed, including 10/10 integration checks and 2,320,000
BFS assertions. Both Release builds reported zero warnings/errors.
The corrected DPI test setup uses logical client sizes and physical framebuffer
dimensions consistently. See [readme.md](readme.md) for exact commands, UI/core
boundaries, remaining performance limits and requested manual verification.
LAB-GRID-02 does not retroactively approve LAB-GRID-01 or advance other increments.

## LAB-GRID-02-FIX01 — Laboratory-only interface

**Authority:** owner approval of the Laboratory appearance and explicit scope
correction: INICIAR must remain unavailable.
**Baseline:** `v0.0.33`, `ceaa289018b1a56eafe3417a0d64e6b4fb3c5b70`.
**Delivery:** `v0.0.34`.
**Status:** formally accepted by the owner, as confirmed in the WG-00-D authorization.

This correction supersedes only the earlier normal-creation scope. The approved
LAB presentation, layout, configuration, map and manual controls remain intact.
The normal-creation mode and automatic creation entry point are removed; reusable
configuration and presentation components remain available. LAB creates a pending
session and explicitly advances through RunNext once per manual request.

- [x] Restore INICIAR's original notice without opening or generating anything.
- [x] Preserve generated LAB data when INICIAR is activated after returning.
- [x] Preserve manual LAB controls and remove automatic normal creation.
- [x] Add production-model regression coverage and run Release checks/builds.
- [x] Owner confirmed formal acceptance of LAB-GRID-02 including FIX01 before WG-00-D.

Developer evidence: 18/18 Laboratory checks, 16/16 menu checks and the complete
A/FIX01/B/C Grid suite, including 10/10 integration checks and 2,320,000 BFS
assertions; Engine and Game Release builds with zero warnings/errors.
No new native visual/input/GPU verification is claimed. The owner's visual
approval and formal acceptance are separate from headless execution. The two
non-blocking observations remain visible in
[readme.md](readme.md#non-blocking-observations-retained-after-acceptance): native
input/DPI/GPU lifecycle evidence and retained GPU buffer allocation. Acceptance
does not claim those observations resolved; WG-00-D changes no Laboratory code.
WG-CORE, accepted Grid algorithms, D01–D11 and WG-00-D/E remain unchanged.
