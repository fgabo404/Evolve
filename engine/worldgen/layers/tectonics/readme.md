# Layer 01 — Tectonic Plates

**Status:** Architecture and roadmap prepared; implementation not started.  
**Working branch:** `world-tectonics` (branched from accepted Grid v0.0.35).  
**Prerequisite:** Layer 00 — Hex Grid, formally closed.

## Purpose

Generate a deterministic, geologically useful mosaic of **exactly 14 tectonic plates** for every supported map size. Every Grid cell must belong to one connected plate region. Plates vary in size, outline and movement according to the main seed, not the selected landmass world type.

The tectonic layer describes plate ownership, movement and boundary interactions. It **does not** assign final continents, oceans, elevation, biomes or game state.

## Historical reference and reuse policy

Review the legacy `GEvolve.Engine/World/Tectonics/` algorithms in repository history:

- `TectonicPlateGenerator.cs`: farthest-candidate seed placement, weighted competitive region growth, periodic multi-scale noise and domain warping.
- `PlateBoundaryMap.cs`: unique inter-plate edges, boundary cells and distance-to-boundary.
- `PlateMotionGenerator.cs`: per-plate direction and speed.
- `TectonicMotionGenerator.cs`: relative velocity, convergence/divergence/shear and influence fields.

Reuse the **algorithmic ideas**, not legacy `HexTopology`, `LandmassState`, random-stream implementation or separate generation orchestration. Adapt to accepted `HexGridData`, WG-CORE contracts and stable per-stage deterministic randomness. The historical fixed count of 16 is replaced by **14**; prior growth-cost samples are reference parameters only, not approved constants for 14 plates.

## Architectural boundaries

- `TectonicPlatesLayer` will implement the existing `IWorldGenerationLayer` contract for `GenerationStage.Tectonics`.
- Read the published, completed Grid descriptor from the existing generation context through approved stage inputs.
- Use a compact cell-to-plate mapping indexed by `CellId`, with immutable externally visible published data. Determine its exact `LayerData<T>` representation in WG-01-A.
- Respect cylindrical horizontal wrap and bounded north/south neighbors.
- Use the same `WorldGenerationOrchestrator`; do not create another world generator, window, or UI-based tectonic algorithm.
- Laboratory uses real completed tectonic output and manually advances via `RunNext()`; normal `INICIAR` remains unavailable.
- No mutation of Layer 00 or WG-CORE; no landmass generation as part of this layer.

## Directory responsibilities (planning only)

- `generation/`: seed selection, weighted region growth and periodic multi-scale fields.
- `motion/`: plate velocities and relative boundary interactions.
- `boundaries/`: detection of shared edges and optional stress/distance fields.
- Root tectonics folder: stage executor, immutable publication contracts, documentation and roadmap.

Directories contain placeholders only until their individual increments are authorized. No source file or algorithm is implemented by this preparation.

## Acceptance principles

Cover every cell exactly once; guarantee 14 nonempty connected regions and valid IDs; enforce seam/pole topology; reproduce complete results for identical requests; classify interactions consistently; preserve publication/invalidation rules; measure CPU allocation and runtime without arbitrary thresholds. Independently audit each delivered increment and obtain owner acceptance.

**Internal roadmap:** [roadmap.md](roadmap.md)  
**General roadmap:** [World Generation Roadmap](../../roadmap.md)  
**Architecture:** [World Generation Architecture](../../../../WORLD_GENERATION_ARCHITECTURE.md)
