# World Generation Tests

**Status:** Documentation scaffold; no world-generation test project created.

## Responsibility

Verify the generator and each layer **without requiring the graphical interface** where possible.

Planned coverage:

- Orchestrator state transitions, prerequisite enforcement, failure behavior and rebuild invalidation.
- Reproducibility from seed/settings and consistent `GenerationContext` data ownership.
- Hex-grid topology and seamless horizontal wrapping with polar boundaries.
- Plate assignment and boundary validity.
- World-type landmass connectivity, including seam-aware checks.
- Elevation and landform consistency; physically meaningful hydrology.
- Climate response and environmental biome constraints.
- Laboratory eligibility rules, without duplicating generator logic.

Exact tests and performance targets belong to each separately approved internal layer roadmap. Preserve and continue running existing `tests/menu/` checks when authorized changes may affect UI integration.

**General roadmap:** [World Generation Roadmap](../../engine/worldgen/roadmap.md)  
**Architecture:** [World Generation Architecture](../../WORLD_GENERATION_ARCHITECTURE.md)
