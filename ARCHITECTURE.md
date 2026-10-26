# Evolve — shared simulation architecture (initial scaffold)

- **GEvolve.Core**: existing hex world geometry and deterministic terrain generator. Kept unchanged for compatibility.
- **GEvolve.Definitions**: read-only loader for versioned JSON snapshots from fgabo404/EvolveDoc. JSON source of truth remains EvolveDoc; guide-only catalogs are **not** activated as runtime rules.
- **GEvolve.State**: per-save mutable state and persistence. Definitions are referenced by stable IDs. The initial save has world generator settings, turn, civilization/region placeholders and a save format version.
- **GEvolve.Engine**: deterministic world initialization and rule execution; currently only advances an empty turn. No invented production or research balance.
- **GEvolve.Simulator**: experimental strategic hex 2D viewer, exported as interactive self-contained HTML; it supports zoom, pan and inspect. It is **not yet a live simulation UI**. Re-run console to advance turns and regenerate the view.
- **GEvolve.Desktop**: existing 3D OpenTK renderer, kept unchanged.
- **GEvolve.Tests**: console smoke tests for turn, persistence and world reproducibility; keep existing GEvolve.Core.Tests.

Dependency direction: Desktop/Simulator -> Engine -> State/Definitions/Core. State -> Core. Definitions is standalone. Do not import graphical dependencies into simulation.

## Run
From the Evolve repository root:

```sh
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Simulator -- --new --turns 5 --save saves/prototype.json --map output/strategic-map.html
```

Open `output/strategic-map.html` in a browser to inspect the 2D hex map.
Do not commit `saves/` or `output/`. Save files use a provisional schema; migrations and content compatibility are pending.

## Next phases
1. Establish pinned EvolveDoc content package/version and schema validation; the catalog loader currently parses JSON only.
2. Define and approve per-turn phases, actions, event log and transactional updates.
3. Define per-hex state, ownership and civilization mechanics with proper stable instance IDs.
4. Convert the strategic 2D viewer into a live map host connected to Engine (and eventually 2D/3D switch in Desktop).
5. Implement real gameplay only from approved operational rules in EvolveDoc.
