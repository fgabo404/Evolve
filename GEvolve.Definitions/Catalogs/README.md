# Evolve game definitions — imported snapshot

This directory contains exact copies of the eight JSON definition/catalog files from [fgabo404/EvolveDoc](https://github.com/fgabo404/EvolveDoc), retaining their original relative paths under `data/`.

**Source of design decisions and upstream catalog edits:** EvolveDoc.  
**Runtime snapshot for Evolve:** this directory.

Do not change assigned IDs, tags, references, translated values or catalog structure independently. Update the source in EvolveDoc first, run its validators, then sync the approved JSON files here. **No automatic synchronization is currently configured.**

Source blob SHA values (at import time):

| Path under `data/` | Git blob SHA |
| --- | --- |
| animals/animals.biological.catalog.json | `03ae24beda50b9bca90343065297b6edb895f5c8` |
| animals/animals.catalog.guide.json | `becb025f4e8d46a2423eb7495a7501b61b1647bf` |
| minerals/minerals.catalog.guide.json | `0d6368eeb69214a0dbe30c89f28a5f8fa154123c` |
| plants/plants.biological.catalog.json | `2f30aa243b2fa077426e551a3bfe455eb6d07759` |
| plants/plants.catalog.guide.json | `30fab97a7c6f21df8d8ba00dc48407ad2fb03dfc` |
| resources/resources.catalog.guide.json | `f5c1e8673072008001e858c49dfa73bfedc808e7` |
| technologies/agriculture.technologies.json | `a3480bfa32f8eda9c56fffc71c9d5c8f021920ff` |
| world/world.catalog.guide.json | `d50cae3f91571c16438c036c8f04fe71bc4c0e85` |

The imported catalogs include **design guides and optional biological background**. Many explicitly mark `runtimePolicy.loadAsGameDefinitions: false`; copying does **not** approve them for execution or turn them into completed rules. Preserve `null` as unresolved, not as a default numeric value. Do not invent yields, spawn rates or transformations.

JSON Schemas, educational example states, validators and generated Markdown documentation remain in EvolveDoc and have not been moved. The existing `DefinitionCatalog` loader can read this folder explicitly, but no application has yet been wired to load it automatically. A repeatable version-pinned sync/build packaging workflow and C# schema/domain validation are future tasks.
