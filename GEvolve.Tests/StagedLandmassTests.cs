using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class StagedLandmassTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        var relief = new PlateAwareElevationGenerator();
        var allTypes = Enum.GetValues<LandmassType>();

        // Every value between two and six must be reachable, but a given seed
        // must produce one count across the four map resolutions.
        var possibleCounts = Enumerable.Range(-80, 161)
            .Select(LandmassProfiles.ContinentCountForSeed).Distinct().Order().ToArray();
        Check(possibleCounts.SequenceEqual([2, 3, 4, 5, 6]),
            "Seeds can produce any number from two through six continents");
        foreach (int continentSeed in new[] { -731, 0, 42, 2026, int.MaxValue })
        {
            int expected = LandmassProfiles.ContinentCountForSeed(continentSeed);
            Check(expected is >= 2 and <= 6 &&
                Enum.GetValues<GridSize>().All(size =>
                    LandmassProfiles.Create(size, LandmassType.Continents, continentSeed)
                        .Continents!.CoreCount == expected),
                "Number of main continents depends on seed, never map size");
        }

        foreach (var size in Enum.GetValues<GridSize>())
        {
            var generated = new Dictionary<LandmassType, GeneratedWorld>();
            foreach (var type in allTypes)
            {
                var config = LandmassProfiles.Create(size, type, 2026);
                var platesPreview = service.GeneratePlatePreview(config).Landmass.PlateByCellId!;
                byte[] immutableSnapshot = platesPreview.ToArray();
                var world = service.Generate(config);
                generated[type] = world;
                var landmass = world.Landmass;
                var topology = new HexTopology(world.Grid);
                var ids = landmass.PlateByCellId
                    ?? throw new InvalidOperationException("Missing stage 01 plates");
                var heights = landmass.ElevationByCellId
                    ?? throw new InvalidOperationException("Missing stage 02 elevations");

                Check(!landmass.TectonicPreviewOnly &&
                    ids.Distinct().Count() == TectonicPlateGenerator.NumberOfPlates &&
                    ids.SequenceEqual(platesPreview) &&
                    platesPreview.SequenceEqual(immutableSnapshot),
                    "All four map types keep the exact same 16 tectonic plates");
                Check(landmass.Configuration.Type == type &&
                    landmass.SurfaceByCellId.Length == topology.Count &&
                    heights.Length == topology.Count &&
                    landmass.SurfaceByCellId.All(value => value is 0 or 1) &&
                    heights.Max() == 100 &&
                    heights.Where((value, id) => landmass.SurfaceByCellId[id] == 1)
                        .Distinct().Count() > 20 &&
                    Enumerable.Range(0, topology.Count).All(id =>
                        landmass.SurfaceByCellId[id] == 0
                            ? heights[id] == 0 : heights[id] > 0),
                    "Every world type has oceans, land and 0..100 elevation");
                Check(LandmassGenerationService.MeetsProfile(type, world.Statistics) &&
                    world.Statistics.LandCells > 0 &&
                    world.Statistics.LandCells < topology.Count,
                    "Each type retains its own original geographic landmass profile");
                if (type == LandmassType.Continents)
                {
                    int expectedContinents = config.Continents!.CoreCount;
                    int actualContinents = world.Statistics.ComponentSizes.Count(cells => cells >= 100);
                    Check(expectedContinents is >= 2 and <= 6 &&
                        actualContinents == expectedContinents &&
                        world.Statistics.DominantLandShare < .75 &&
                        world.Statistics.LandFraction is >= .27 and <= .43,
                        "Seed-selected continent count, separated mainlands and ocean coverage");

                    // The plate mosaic MUST decide where the continental
                    // nuclei and crust go, not merely tweak coast noise.
                    var crustGenerator = new ContinentalCrustGenerator();
                    var crust = crustGenerator.Generate(
                        topology, ids, expectedContinents, config.Seed);
                    Check(crust.MainContinentCoreCells.Length == expectedContinents &&
                        crust.MainContinentCorePlates.Distinct().Count() == expectedContinents &&
                        crust.CrustByCellId.Length == topology.Count &&
                        crust.EmergenceAffinityByCellId.Length == topology.Count &&
                        crust.ProvinceByCellId.Length == topology.Count &&
                        crust.MainContinentCoreCells.Select((cell, i) =>
                            ids[cell] == crust.MainContinentCorePlates[i]).All(value => value) &&
                        crust.MainContinentCoreCells.All(cell =>
                            crust.CrustByCellId[cell] == 1) &&
                        crust.MainContinentCoreCells.Select((cell, i) =>
                            crust.ProvinceByCellId[cell] == i).All(ok => ok) &&
                        crust.CrustByCellId.Contains((byte)0) &&
                        crust.CrustByCellId.Contains((byte)1),
                        "Plate interiors seed continents and both crust types exist");
                    int continentalLand = Enumerable.Range(0, topology.Count)
                        .Count(id => landmass.SurfaceByCellId[id] == 1 &&
                            crust.CrustByCellId[id] == 1);
                    int submergedContinentalCrust = Enumerable.Range(0, topology.Count)
                        .Count(id => landmass.SurfaceByCellId[id] == 0 &&
                            crust.CrustByCellId[id] == 1);
                    Check(continentalLand > world.Statistics.LandCells / 2 &&
                        submergedContinentalCrust > 0 &&
                        Enumerable.Range(0, topology.Count).Any(cell =>
                            crust.CrustByCellId[cell] == 0 &&
                            landmass.SurfaceByCellId[cell] == 0),
                        "Most emergent land follows continental crust, but shelves can be submerged");
                    var repeatedCrust = crustGenerator.Generate(
                        topology, ids, expectedContinents, config.Seed);
                    Check(repeatedCrust.CrustByCellId.SequenceEqual(crust.CrustByCellId) &&
                        repeatedCrust.ProvinceByCellId.SequenceEqual(
                            crust.ProvinceByCellId) &&
                        repeatedCrust.EmergenceAffinityByCellId.SequenceEqual(
                            crust.EmergenceAffinityByCellId) &&
                        repeatedCrust.MainContinentCoreCells.SequenceEqual(
                            crust.MainContinentCoreCells),
                        "Plate-based crust and continent roots are deterministic");
                    int shorelineWithinPlate = 0, shorelineCrossingPlates = 0;
                    int internalPlateBorderOnLand = 0;
                    for (int cell = 0; cell < topology.Count; cell++)
                    {
                        if (landmass.SurfaceByCellId[cell] == 0) continue;
                        foreach (int nb in topology.Neighbors(cell))
                        {
                            if (landmass.SurfaceByCellId[nb] == 0)
                            {
                                if (ids[cell] == ids[nb]) shorelineWithinPlate++;
                                else shorelineCrossingPlates++;
                            }
                            else if (ids[cell] != ids[nb])
                                internalPlateBorderOnLand++;
                        }
                    }
                    Check(shorelineWithinPlate > shorelineCrossingPlates / 3 &&
                        internalPlateBorderOnLand > 0,
                        "Coasts need not trace plate boundaries and continental " +
                        "land crosses some tectonic borders");
                }
                Check(landmass.Climate is null && landmass.SlopeByCellId is null &&
                    landmass.Biomes is null,
                    "Temperature, precipitation, slopes and biomes are still separate stages");

                if (type != LandmassType.Pangaea)
                {
                    // The staged generator must consume tectonics while preserving
                    // the legacy no-plates mode for older callers.
                    if (size == GridSize.Small)
                    {
                        IPlateAwareLandmassGenerator surfaceGenerator = type switch
                        {
                            LandmassType.Continents => new ContinentsGenerator(),
                            LandmassType.SmallContinents => new SmallContinentsGenerator(),
                            LandmassType.Archipelagos => new ArchipelagosGenerator(),
                            _ => throw new InvalidOperationException("Unexpected world type")
                        };
                        var landWithPlates = surfaceGenerator.Generate(
                            topology, config, landmass.DeterministicAttempt, ids);
                        var legacyLand = surfaceGenerator.Generate(
                            topology, config, landmass.DeterministicAttempt);
                        Check(landWithPlates.SequenceEqual(landmass.SurfaceByCellId) &&
                            !legacyLand.SequenceEqual(landWithPlates) &&
                            ids.SequenceEqual(platesPreview),
                            "Tectonics influence the coastline while the original legacy path remains available");
                    }
                    byte[] platesCopy = ids.ToArray();
                    var recalculated = relief.Generate(
                        topology, landmass.SurfaceByCellId, platesCopy,
                        type, config.Seed, landmass.DeterministicAttempt);
                    Check(recalculated.SequenceEqual(heights) &&
                        platesCopy.SequenceEqual(ids),
                        "Shared elevation routine is deterministic and cannot modify plates");
                }
                if (size == GridSize.Small)
                {
                    var repeated = service.Generate(config);
                    Check(repeated.Landmass.PlateByCellId!.SequenceEqual(ids) &&
                        repeated.Landmass.SurfaceByCellId.SequenceEqual(landmass.SurfaceByCellId) &&
                        repeated.Landmass.ElevationByCellId!.SequenceEqual(heights) &&
                        repeated.Landmass.DeterministicAttempt == landmass.DeterministicAttempt,
                        "Same seed and type reproduce land, relief and tectonics");

                    var path = Path.Combine(Path.GetTempPath(),
                        $"evolve-stage02-{type}-{Guid.NewGuid():N}.json");
                    try
                    {
                        GameSaveStore.Save(path, new GameState
                        {
                            HexGrid = new HexGridState(world.Grid.Definition),
                            Landmass = landmass
                        });
                        var restored = GameSaveStore.Load(path).Landmass!;
                        Check(restored.Configuration.Type == type &&
                            restored.SurfaceByCellId.SequenceEqual(landmass.SurfaceByCellId) &&
                            restored.PlateByCellId!.SequenceEqual(ids) &&
                            restored.ElevationByCellId!.SequenceEqual(heights),
                            "Every landmass type survives save and load");
                    }
                    finally { if (File.Exists(path)) File.Delete(path); }
                }
                Console.WriteLine($"STAGE 02 {size}/{type}: " +
                    $"{world.Statistics.LandFraction:P1} land; " +
                    $"{world.Statistics.ComponentCount} components; " +
                    $"attempt {landmass.DeterministicAttempt}");
            }

            var pangaea = generated[LandmassType.Pangaea].Statistics;
            var continents = generated[LandmassType.Continents].Statistics;
            var small = generated[LandmassType.SmallContinents].Statistics;
            var arch = generated[LandmassType.Archipelagos].Statistics;
            Check(pangaea.ComponentCount == 1 && pangaea.DominantLandShare == 1 &&
                continents.LargeComponents >= 2 &&
                small.ComponentCount > continents.ComponentCount &&
                small.DominantMassSize < continents.DominantMassSize &&
                arch.ComponentCount > small.ComponentCount,
                "Pangaea, continents, small continents and archipelagos have distinct structure");

            byte[] firstPlates = generated[LandmassType.Pangaea].Landmass.PlateByCellId!;
            Check(allTypes.All(type =>
                generated[type].Landmass.PlateByCellId!.SequenceEqual(firstPlates)),
                "Map types never regenerate or replace stage 01 tectonic plates");
            Check(allTypes.Select(type =>
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    generated[type].Landmass.SurfaceByCellId))).Distinct().Count() == 4,
                "All four map types produce visibly distinct land distributions");
        }

        // Different seeds must alter all world types, but not break plate sharing.
        foreach (var type in allTypes)
        {
            var config = LandmassProfiles.Create(GridSize.Small, type, 42);
            var world = service.Generate(config);
            Check(world.Landmass.PlateByCellId!.SequenceEqual(
                    service.GeneratePlatePreview(config).Landmass.PlateByCellId!) &&
                LandmassGenerationService.MeetsProfile(type, world.Statistics),
                "Alternate seed preserves landmass identity and universal tectonics");
        }
        Console.WriteLine("PASS: plate-born continental/oceanic crust and seed-selected " +
            "2..6 continents across sizes; relief, tectonics and persistence");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
