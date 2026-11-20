# LAB-GRID-01 — First Grid Visualization

**Repository:** `fgabo404/Evolve`
**Working branch:** `world-grid`
**Authority:** explicit project-owner request for LAB-GRID-01.
**Engine baseline:** `v0.0.31`, `4f0049dba50f68b426c99bb7bafb749648c16d42`.
**Delivery version:** `v0.0.32`.
**Status:** implemented, pending manual visual validation, independent audit and owner acceptance.

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
