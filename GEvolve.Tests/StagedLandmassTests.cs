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
        VerifyPublicGenerationEntryPoint(service);
        var relief = new PlateAwareElevationGenerator();
        var finishedTypes = new[] { LandmassType.Pangaea, LandmassType.SmallContinents, LandmassType.Archipelagos };
        foreach (var size in Enum.GetValues<GridSize>())
        {
            var generated = new Dictionary<LandmassType, GeneratedWorld>();
            var pendingConfig = LandmassProfiles.Create(size, LandmassType.Continents, 2026);
            var pending = service.Generate(pendingConfig);
            generated[LandmassType.Continents] = pending;
            Check(pending.Statistics is null && pending.Landmass.SurfaceGenerationPending &&
                pending.Landmass.SurfaceByCellId.Length == 0 && pending.Landmass.ElevationByCellId is null &&
                pending.Landmass.TectonicMotion is not null,
                "Continents stop after universal tectonics without a surface");

            foreach (var type in finishedTypes)
            {
                var config = LandmassProfiles.Create(size, type, 2026);
                var platesPreview = service.GeneratePlatePreview(config).Landmass.PlateByCellId!;
                byte[] immutableSnapshot = platesPreview.ToArray();
                var world = service.Generate(config);
                generated[type] = world;
                var landmass = world.Landmass;
                var topology = new HexTopology(world.Grid);
                var ids = landmass.PlateByCellId ?? throw new InvalidOperationException("Missing stage 01 plates");
                var heights = landmass.ElevationByCellId ?? throw new InvalidOperationException("Missing stage 02 elevations");
                var stats = world.Statistics ?? throw new InvalidOperationException("Finished world lacks surface statistics");

                Check(!landmass.TectonicPreviewOnly && ids.Distinct().Count() == TectonicPlateGenerator.NumberOfPlates &&
                    ids.SequenceEqual(platesPreview) && platesPreview.SequenceEqual(immutableSnapshot),
                    "Every finished map keeps the exact stage 01 plates");
                Check(landmass.SurfaceByCellId.Length == topology.Count && heights.Length == topology.Count,
                    $"Surface and elevation arrays match topology ({size}/{type}: surface={landmass.SurfaceByCellId.Length}, elevation={heights.Length}, expected={topology.Count})");
                Check(landmass.SurfaceByCellId.All(value => value is 0 or 1),
                    $"Surface is binary ({size}/{type})");
                Check(heights.Max() == 100,
                    $"Elevation reaches level 100 ({size}/{type}: max={heights.Max()})");
                Check(heights.Where((value, id) => landmass.SurfaceByCellId[id] == 1)
                    .Distinct().Count() > 20,
                    $"Land elevation variety exceeds 20 levels ({size}/{type})");
                int nonzeroOcean = 0, zeroLand = 0;
                for (int id = 0; id < topology.Count; id++)
                {
                    if (landmass.SurfaceByCellId[id] == 0 && heights[id] != 0) nonzeroOcean++;
                    if (landmass.SurfaceByCellId[id] == 1 && heights[id] == 0) zeroLand++;
                }
                Console.WriteLine($"ELEVATION DIAGNOSTIC {size}/{type}: ocean above zero={nonzeroOcean}, land at zero={zeroLand}");
                Check(nonzeroOcean == 0,
                    $"Ocean cells have elevation zero ({size}/{type}: violations={nonzeroOcean})");
                Check(zeroLand == 0,
                    $"Land cells have positive elevation ({size}/{type}: violations={zeroLand})");
                Check(LandmassProfileValidator.MeetsProfile(type, stats) &&
                    stats.LandCells > 0 && stats.LandCells < topology.Count,
                    "Finished types retain their original geographic profiles");
                Check(landmass.Climate is null && landmass.SlopeByCellId is null && landmass.Biomes is null,
                    "Climate, slopes and biomes remain separate stages");
                if (type != LandmassType.Pangaea)
                {
                    if (size == GridSize.Small)
                    {
                        IPlateAwareLandmassGenerator surfaceGenerator = type == LandmassType.SmallContinents
                            ? new SmallContinentsGenerator() : new ArchipelagosGenerator();
                        var landWithPlates = surfaceGenerator.Generate(topology, config, landmass.DeterministicAttempt, ids);
                        var legacyLand = surfaceGenerator.Generate(topology, config, landmass.DeterministicAttempt);
                        Check(landWithPlates.SequenceEqual(landmass.SurfaceByCellId) &&
                            !legacyLand.SequenceEqual(landWithPlates) && ids.SequenceEqual(platesPreview),
                            "Non-continental staged generators retain plate-aware and legacy entry points");
                    }
                    byte[] platesCopy = ids.ToArray();
                    var recalculated = relief.Generate(topology, landmass.SurfaceByCellId,
                        platesCopy, type, config.Seed, landmass.DeterministicAttempt);
                    Check(recalculated.SequenceEqual(heights) && platesCopy.SequenceEqual(ids),
                        "Shared elevation remains deterministic and cannot modify plates");
                }
                if (size == GridSize.Small)
                {
                    var repeated = service.Generate(config).Landmass;
                    Check(repeated.PlateByCellId!.SequenceEqual(ids) &&
                        repeated.SurfaceByCellId.SequenceEqual(landmass.SurfaceByCellId) &&
                        repeated.ElevationByCellId!.SequenceEqual(heights) &&
                        repeated.DeterministicAttempt == landmass.DeterministicAttempt,
                        "Finished worlds repeat land, relief and tectonics");
                    string path = Path.Combine(Path.GetTempPath(), $"evolve-stage02-{type}-{Guid.NewGuid():N}.json");
                    try
                    {
                        GameSaveStore.Save(path, new GameState { HexGrid = new(world.Grid.Definition), Landmass = landmass });
                        var restored = GameSaveStore.Load(path).Landmass!;
                        Check(restored.SurfaceByCellId.SequenceEqual(landmass.SurfaceByCellId) &&
                            restored.PlateByCellId!.SequenceEqual(ids) &&
                            restored.ElevationByCellId!.SequenceEqual(heights),
                            "Finished landmass survives save and load");
                    }
                    finally { if (File.Exists(path)) File.Delete(path); }
                }
                Console.WriteLine($"STAGE 02 {size}/{type}: {stats.LandFraction:P1} land; " +
                    $"{stats.ComponentCount} components; attempt {landmass.DeterministicAttempt}");
            }

            var pangaea = generated[LandmassType.Pangaea].Statistics!;
            var small = generated[LandmassType.SmallContinents].Statistics!;
            var arch = generated[LandmassType.Archipelagos].Statistics!;
            Check(pangaea.ComponentCount == 1 && pangaea.DominantLandShare == 1 &&
                small.ComponentCount > 8 && arch.ComponentCount > small.ComponentCount,
                "The three finished world types keep distinct structures");
            byte[] firstPlates = generated[LandmassType.Pangaea].Landmass.PlateByCellId!;
            Check(generated.Values.All(world => world.Landmass.PlateByCellId!.SequenceEqual(firstPlates)),
                "Pending and finished map types share identical tectonic plates");
            Check(finishedTypes.Select(type => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                generated[type].Landmass.SurfaceByCellId))).Distinct().Count() == finishedTypes.Length,
                "The three finished types retain distinct land distributions");
        }
        foreach (var type in finishedTypes)
        {
            var config = LandmassProfiles.Create(GridSize.Small, type, 42);
            var world = service.Generate(config);
            Check(world.Landmass.PlateByCellId!.SequenceEqual(
                    service.GeneratePlatePreview(config).Landmass.PlateByCellId!) &&
                LandmassProfileValidator.MeetsProfile(type, world.Statistics!),
                "Alternate seed retains finished type and universal tectonics");
        }
        Console.WriteLine("PASS: pending Continents and unchanged finished staged generators");
    }

    private static void VerifyPublicGenerationEntryPoint(WorldGenerationService service)
    {
        foreach (var type in Enum.GetValues<LandmassType>())
        {
            var configuration = LandmassProfiles.Create(GridSize.Small, type, 2026);
            var viaConfiguration = service.Generate(configuration);
            var viaParameters = service.Generate(GridSize.Small, type);
            var baseline = viaConfiguration.Landmass;
            var actual = viaParameters.Landmass;

            Check(actual.Configuration == baseline.Configuration &&
                actual.DeterministicAttempt == baseline.DeterministicAttempt &&
                actual.TectonicPreviewOnly == baseline.TectonicPreviewOnly &&
                actual.SurfaceGenerationPending == baseline.SurfaceGenerationPending &&
                actual.HasSurface == baseline.HasSurface &&
                actual.PlateByCellId!.SequenceEqual(baseline.PlateByCellId!) &&
                actual.SurfaceByCellId.SequenceEqual(baseline.SurfaceByCellId) &&
                (actual.ElevationByCellId is null && baseline.ElevationByCellId is null ||
                 actual.ElevationByCellId is not null && baseline.ElevationByCellId is not null &&
                 actual.ElevationByCellId.SequenceEqual(baseline.ElevationByCellId)) &&
                (viaParameters.Statistics is null && viaConfiguration.Statistics is null ||
                 viaParameters.Statistics is { } actualStats &&
                 viaConfiguration.Statistics is { } baselineStats &&
                 actualStats.LandCells == baselineStats.LandCells &&
                 actualStats.TotalCells == baselineStats.TotalCells &&
                 actualStats.IslandComponents == baselineStats.IslandComponents &&
                 actualStats.ComponentSizes.SequenceEqual(baselineStats.ComponentSizes)),
                $"Public facade matches existing generation for {type}");
            if (type == LandmassType.Continents)
                Check(actual.SurfaceGenerationPending && actual.TectonicPreviewOnly &&
                    !actual.HasSurface && actual.SurfaceByCellId.Length == 0 &&
                    actual.ElevationByCellId is null && viaParameters.Statistics is null,
                    "Public facade does not invent a continental surface");
        }

        foreach (var type in Enum.GetValues<LandmassType>())
        {
            var alternateConfig = LandmassProfiles.Create(GridSize.Small, type, -731);
            var alternate = service.Generate(GridSize.Small, type, -731);
            var baselineAlternate = service.Generate(alternateConfig);
            Check(alternate.Landmass.Configuration == alternateConfig &&
                alternate.Landmass.PlateByCellId!.SequenceEqual(
                    baselineAlternate.Landmass.PlateByCellId!) &&
                alternate.Landmass.SurfaceByCellId.SequenceEqual(
                    baselineAlternate.Landmass.SurfaceByCellId) &&
                (alternate.Landmass.ElevationByCellId is null &&
                 baselineAlternate.Landmass.ElevationByCellId is null ||
                 alternate.Landmass.ElevationByCellId is not null &&
                 baselineAlternate.Landmass.ElevationByCellId is not null &&
                 alternate.Landmass.ElevationByCellId.SequenceEqual(
                     baselineAlternate.Landmass.ElevationByCellId)),
                $"Explicit alternate seed stays deterministic for {type}");
        }
        Console.WriteLine("PASS: H01-A public generation facade equivalence");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
