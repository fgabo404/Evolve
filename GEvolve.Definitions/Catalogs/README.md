# Evolve catalogs: JSON design references + C# contracts

Catalogs are organized by domain. Each folder contains original EvolveDoc JSON reference data (unchanged) and an **initial strongly typed C# contract**:

- `Animals/`: animal guide, biological guide, `AnimalsDefinitions.cs`
- `Plants/`: plant guide, biological guide, `PlantsDefinitions.cs`
- `Minerals/`: geological candidate guide, `MineralsDefinitions.cs`
- `Resources/`: resource guide, `ResourcesDefinitions.cs`
- `Technologies/`: agriculture technologies, `TechnologiesDefinitions.cs`
- `World/`: world guide, `WorldDefinitions.cs`

Upstream JSON authority: [fgabo404/EvolveDoc](https://github.com/fgabo404/EvolveDoc). Maintain IDs and relationships; update JSON upstream, then synchronize the reference copies. There is no automatic synchronization yet.

**Status:** The C# files currently type the primary collections and selected frequently accessed fields. Nested details, advanced technology expressions, taxonomy and processing rules are **not yet modeled 1:1**. Do not mistake these initial contracts for complete catalog translations. Do not substitute missing rules with defaults.

**Runtime:** Current JSON files are design references, not a required runtime parsing pipeline. No compiled C# data instances/generated static catalog have been implemented yet. The Engine is not wired to these definitions. Existing `runtimePolicy.loadAsGameDefinitions: false` flags remain authoritative. To run exclusively from C#, the next work will be a complete typed mapping, C# catalog population, reference validation and runtime integration. The new C# types by themselves do not populate game data.

No JSON contents or upstream documents were changed during regrouping. Original imported JSON blob hashes remained identical after the move.
