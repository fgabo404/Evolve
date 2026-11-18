# World Generation Layers

**Status:** Documentation scaffold. No layer implementations exist.

Stages run in strict order. The Laboratory may inspect completed layers but can only execute the next eligible unfinished layer. The orchestrator, not individual layers or the UI, enforces prerequisites. Changing or rebuilding a stage invalidates dependent downstream results.

| Stage | Directory | Purpose |
| --- | --- | --- |
| 00 | [`grid/`](grid/readme.md) | Hex Grid |
| 01 | [`tectonics/`](tectonics/readme.md) | Tectonic Plates |
| 02 | [`landmasses/`](landmasses/readme.md) | Landmasses |
| 03 | [`elevation/`](elevation/readme.md) | Elevation |
| 04 | [`landforms/`](landforms/readme.md) | Terrestrial Landforms |
| 05 | [`hydrology/`](hydrology/readme.md) | Base Hydrology |
| 06 | [`climate/`](climate/readme.md) | Climate |
| 07 | [`biomes/`](biomes/readme.md) | Biomes |

Each directory contains a `readme.md` describing its responsibility and a **preliminary internal `roadmap.md`**. The internal roadmaps are placeholders for separately approved plans; they do not authorize implementation.

**Overall roadmap:** [World Generation Roadmap](../roadmap.md)
