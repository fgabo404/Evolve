using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class TectonicPlateTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        {
            var configuration = LandmassProfiles.Create(size, LandmassType.Pangaea, 2026);
            var result = service.Generate(configuration);
            var terrain = result.Landmass;
            byte[] plates = terrain.PlateByCellId
                ?? throw new InvalidOperationException("Every world needs tectonic plates");
            int count = result.Grid.Cells.Count;
            Check(terrain.TectonicPreviewOnly && terrain.ElevationByCellId is null &&
                terrain.Biomes is null && terrain.Climate is null &&
                terrain.SurfaceByCellId.Length == count &&
                terrain.SurfaceByCellId.All(x => x == 0),
                "Phase 01 must not generate oceans, land, elevations or climate");
            Check(plates.Length == count &&
                plates.Distinct().Count() == TectonicPlateGenerator.NumberOfPlates &&
                plates.All(id => id < TectonicPlateGenerator.NumberOfPlates),
                "Each world contains all 16 plates regardless of resolution");
            var topology = new HexTopology(result.Grid);
            var sizes = new int[TectonicPlateGenerator.NumberOfPlates];
            foreach (byte plate in plates) sizes[plate]++;
            Check(sizes.Max() > sizes.Min() * 1.5,
                "Plates have unequal areas instead of uniform Voronoi sizes");
            for (byte plate = 0; plate < TectonicPlateGenerator.NumberOfPlates; plate++)
            {
                int start = Array.IndexOf(plates, plate);
                var visited = new bool[count];
                var queue = new Queue<int>();
                visited[start] = true;
                queue.Enqueue(start);
                int found = 0;
                while (queue.TryDequeue(out int cell))
                {
                    found++;
                    foreach (int neighbor in topology.Neighbors(cell))
                    {
                        if (visited[neighbor] || plates[neighbor] != plate) continue;
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
                Check(found == sizes[plate],
                    "Each irregular plate must remain one contiguous region");
            }
            Check(plates.SequenceEqual(TectonicPlateGenerator.Generate(topology, 2026)),
                "Plate generation is deterministic and independent of map type");
            var alternate = service.Generate(
                LandmassProfiles.Create(size, LandmassType.Archipelagos, 2026));
            Check(plates.SequenceEqual(alternate.Landmass.PlateByCellId!) &&
                alternate.Landmass.TectonicPreviewOnly,
                "The same tectonic layer precedes every continent type");

            if (size == GridSize.Small)
            {
                byte[] otherSeed = service.Generate(
                    LandmassProfiles.Create(size, LandmassType.Pangaea, 42))
                    .Landmass.PlateByCellId!;
                Check(!plates.SequenceEqual(otherSeed),
                    "The same grid with a different seed changes plates");

                string path = Path.Combine(Path.GetTempPath(),
                    $"evolve-tectonic-{Guid.NewGuid():N}.json");
                try
                {
                    GameSaveStore.Save(path, new GameState
                    {
                        HexGrid = new HexGridState(result.Grid.Definition),
                        Landmass = terrain
                    });
                    var restored = GameSaveStore.Load(path).Landmass!;
                    Check(restored.TectonicPreviewOnly &&
                        restored.PlateByCellId!.SequenceEqual(plates),
                        "Tectonic preview survives save/load");
                }
                finally { if (File.Exists(path)) File.Delete(path); }
            }
            Console.WriteLine($"PLATES {size}: " +
                $"{plates.Distinct().Count()} plates; min {sizes.Min()}, max {sizes.Max()}; " +
                $"{count} hexagons");
        }
        Console.WriteLine("PASS: independent tectonic plate pipeline, connected regions, persistence");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
