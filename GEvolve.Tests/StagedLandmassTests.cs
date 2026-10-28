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
        var pangaea = new PangaeaFromPlatesGenerator();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Pangaea, seed);
            var stageOne = service.GeneratePlatePreview(config);
            byte[] originalPlates = stageOne.Landmass.PlateByCellId!;
            byte[] plateSnapshot = originalPlates.ToArray();
            var world = service.Generate(config);
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            byte[] plates = terrain.PlateByCellId
                ?? throw new InvalidOperationException("Missing universal tectonic plates");
            byte[] heights = terrain.ElevationByCellId
                ?? throw new InvalidOperationException("Missing land elevations");

            Check(!terrain.TectonicPreviewOnly &&
                plates.SequenceEqual(originalPlates) &&
                originalPlates.SequenceEqual(plateSnapshot),
                "Pangaea reuses universal stage 01 plates without changing any ID");
            Check(terrain.SurfaceByCellId.Length == topology.Count &&
                terrain.SurfaceByCellId.All(value => value is 0 or 1) &&
                heights.Length == topology.Count &&
                heights.Max() == 100 &&
                heights.All(value => value <= 100) &&
                Enumerable.Range(0, topology.Count).All(id =>
                    terrain.SurfaceByCellId[id] != 0 || heights[id] == 0),
                "Land, oceans and 0..100 elevations are generated consistently");
            Check(LandmassGenerationService.MeetsProfile(LandmassType.Pangaea, world.Statistics) &&
                world.Statistics.ComponentCount == 1 &&
                world.Statistics.DominantLandShare == 1 &&
                world.Statistics.LandCells > 0 &&
                world.Statistics.LandCells < topology.Count,
                "Pangaea remains one continuous continental landmass");
            Check(terrain.Climate is null && terrain.Biomes is null &&
                terrain.SlopeByCellId is null,
                "Later layers remain disabled until their respective stages");

            // This call receives the existing plate array, never a new plate map.
            byte[] directInput = plates!.ToArray();
            var direct = pangaea.Generate(topology, config, directInput,
                terrain.DeterministicAttempt);
            Check(directInput.SequenceEqual(plates) &&
                direct.Surface.SequenceEqual(terrain.SurfaceByCellId) &&
                direct.Elevation.SequenceEqual(terrain.ElevationByCellId),
                "Adapted generator is deterministic and does not rewrite plate IDs");

            if (size == GridSize.Small && seed == 2026)
            {
                var repeated = service.Generate(config);
                Check(repeated.Landmass.PlateByCellId!.SequenceEqual(plates) &&
                    repeated.Landmass.SurfaceByCellId.SequenceEqual(terrain.SurfaceByCellId) &&
                    repeated.Landmass.ElevationByCellId!.SequenceEqual(terrain.ElevationByCellId),
                    "All terrain arrays repeat with the same seed and size");

                string savePath = Path.Combine(Path.GetTempPath(),
                    $"evolve-stage02-{Guid.NewGuid():N}.json");
                try
                {
                    GameSaveStore.Save(savePath, new GameState
                    {
                        HexGrid = new(world.Grid.Definition), Landmass = terrain
                    });
                    var loaded = GameSaveStore.Load(savePath).Landmass!;
                    Check(!loaded.TectonicPreviewOnly &&
                        loaded.SurfaceByCellId.SequenceEqual(terrain.SurfaceByCellId) &&
                        loaded.PlateByCellId!.SequenceEqual(plates) &&
                        loaded.ElevationByCellId!.SequenceEqual(terrain.ElevationByCellId),
                        "Stage 02 survives save/load without regeneration");
                }
                finally { if (File.Exists(savePath)) File.Delete(savePath); }
            }
            Console.WriteLine($"STAGE 02 {size}/{seed}: " +
                $"{world.Statistics.LandFraction:P1} land, " +
                $"{world.Statistics.ComponentCount} landmasses, " +
                $"attempt {terrain.DeterministicAttempt}");
        }

        var unsupported = LandmassProfiles.Create(
            GridSize.Small, LandmassType.Archipelagos, 2026);
        try
        {
            service.Generate(unsupported);
            throw new InvalidOperationException("Non-integrated map mode should not be silently generated");
        }
        catch (NotSupportedException) { }

        Console.WriteLine("PASS: stage 02 Pangaea, unchanged plates, deterministic land and heights, save/load");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
