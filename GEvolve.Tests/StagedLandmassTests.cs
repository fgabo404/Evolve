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
                Check(landmass.Climate is null && landmass.SlopeByCellId is null &&
                    landmass.Biomes is null,
                    "Temperature, precipitation, slopes and biomes are still separate stages");

                if (type != LandmassType.Pangaea)
                {
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
        Console.WriteLine("PASS: all four stage 02 types, unique shapes, relief, tectonics, persistence");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
