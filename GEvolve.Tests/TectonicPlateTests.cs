using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class TectonicPlateTests
{
    private static readonly int[] Seeds = [2026, 42, -731];

    public static void Run()
    {
        var service = new WorldGenerationService();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        {
            var partitions = new Dictionary<int, byte[]>();
            foreach (int seed in Seeds)
            {
                var configuration = LandmassProfiles.Create(size, LandmassType.Pangaea, seed);
                var preview = service.GeneratePlatePreview(configuration);
                var terrain = preview.Landmass;
                byte[] plates = terrain.PlateByCellId
                    ?? throw new InvalidOperationException("Every world needs tectonic plates");
                var topology = new HexTopology(preview.Grid);
                int count = topology.Count;
                Check(terrain.TectonicPreviewOnly && terrain.ElevationByCellId is null &&
                    terrain.Biomes is null && terrain.Climate is null &&
                    terrain.SurfaceByCellId.Length == count &&
                    terrain.SurfaceByCellId.All(value => value == 0),
                    $"Plate preview has no classified surface or climate: {size}/{seed}");

                int[] sizes = VerifyConnectedPartition(topology, plates, size, seed);
                if (seed == 2026)
                    Check(sizes.Max() > sizes.Min() * 1.5,
                        $"Plates retain unequal areas: {size}/{seed}");
                Check(plates.SequenceEqual(TectonicPlateGenerator.Generate(topology, seed)) &&
                    plates.SequenceEqual(service.GeneratePlatePreview(configuration).Landmass.PlateByCellId!),
                    $"Identical configurations reproduce every plate ID: {size}/{seed}");
                if (seed == 2026)
                {
                    var alternate = service.GeneratePlatePreview(
                        LandmassProfiles.Create(size, LandmassType.Archipelagos, seed));
                    Check(alternate.Landmass.TectonicPreviewOnly &&
                        alternate.Landmass.PlateByCellId!.SequenceEqual(plates),
                        $"Archipelagos preview shares universal plates: {size}/{seed}");
                }
                var boundaries = PlateBoundaryMap.Analyze(topology, terrain)!;
                int seamBoundaries = VerifyBoundaries(topology, plates, boundaries, terrain, size, seed);

                foreach (LandmassType type in Enum.GetValues<LandmassType>())
                {
                    var world = service.Generate(LandmassProfiles.Create(size, type, seed));
                    Check(world.Landmass.PlateByCellId is { } worldPlates &&
                        worldPlates.SequenceEqual(plates),
                        $"Universal plates differ: {size}/{seed}/{type}");
                    if (type == LandmassType.Continents)
                        Check(world.Landmass.TectonicPreviewOnly &&
                            world.Landmass.SurfaceGenerationPending &&
                            world.Landmass.SurfaceByCellId.Length == 0 &&
                            world.Landmass.ElevationByCellId is null && world.Statistics is null,
                            $"Continents remains pending without surface: {size}/{seed}");
                }

                if (size == GridSize.Small && seed == 2026)
                {
                    foreach (int variation in new[] { 7, 12345 })
                    {
                        var variant = TectonicPlateGenerator.Generate(topology, variation);
                        Check(variant.Distinct().Count() == TectonicPlateGenerator.NumberOfPlates,
                            "Auxiliary seed variants preserve all 16 plates");
                        Check(!variant.SequenceEqual(plates),
                            "Auxiliary seeds alter the irregular tectonic mosaic");
                    }
                    VerifyPersistence(preview, plates);
                }
                partitions.Add(seed, plates);
                Console.WriteLine($"PLATES {size}/{seed}: 16 connected plates; " +
                    $"min {sizes.Min()}, max {sizes.Max()}; {count} cells; " +
                    $"{boundaries.Edges.Count} unique boundary edges, {seamBoundaries} across seam");
            }
            Check(!partitions[2026].SequenceEqual(partitions[42]) &&
                !partitions[2026].SequenceEqual(partitions[-731]) &&
                !partitions[42].SequenceEqual(partitions[-731]),
                $"Official seeds produce distinct full partitions: {size}");
        }
        Check(TectonicPlateGenerator.Version == "tectonic-plates-2" &&
            TectonicPlateGenerator.NumberOfPlates == 16 &&
            TectonicPlateGenerator.BorderIrregularity > 1,
            "Approved tectonic plate version, count and roughness remain unchanged");
        Console.WriteLine("PASS: universal connected tectonic partitions, complete boundaries, seeds and persistence");
    }

    private static int[] VerifyConnectedPartition(HexTopology topology, byte[] plates,
        GridSize size, int seed)
    {
        int count = topology.Count;
        Check(plates.Length == count &&
            plates.All(id => id < TectonicPlateGenerator.NumberOfPlates),
            $"Every cell has one valid plate ID 0..15: {size}/{seed}");
        var sizes = new int[TectonicPlateGenerator.NumberOfPlates];
        var starts = new int[sizes.Length];
        Array.Fill(starts, -1);
        for (int id = 0; id < count; id++)
        {
            int plate = plates[id];
            sizes[plate]++;
            if (starts[plate] < 0) starts[plate] = id;
        }
        Check(sizes.All(value => value > 0),
            $"Exactly the 16 plate IDs 0..15 are populated: {size}/{seed}");

        var visited = new bool[count];
        var queue = new int[count];
        for (int plate = 0; plate < sizes.Length; plate++)
        {
            int head = 0, tail = 0;
            queue[tail++] = starts[plate];
            visited[starts[plate]] = true;
            while (head < tail)
                foreach (int neighbor in topology.Neighbors(queue[head++]))
                    if (!visited[neighbor] && plates[neighbor] == plate)
                    {
                        visited[neighbor] = true;
                        queue[tail++] = neighbor;
                    }
            Check(tail == sizes[plate],
                $"Plate {plate} is one region under real hex neighbors: {size}/{seed}, " +
                $"reached {tail} of {sizes[plate]}");
        }
        Check(visited.All(value => value),
            $"Connected components cover every cell: {size}/{seed}");
        return sizes;
    }

    private static int VerifyBoundaries(HexTopology topology, byte[] plates,
        PlateBoundaryMap boundaries, LandmassState terrain, GridSize size, int seed)
    {
        var actual = new HashSet<PlateBoundaryEdge>(boundaries.Edges);
        Check(actual.Count == boundaries.Edges.Count,
            $"No duplicate boundary edges: {size}/{seed}");
        int expected = 0, seamBoundaries = 0;
        for (int id = 0; id < topology.Count; id++)
        foreach (int neighbor in topology.Neighbors(id))
        {
            if (id >= neighbor || plates[id] == plates[neighbor]) continue;
            var edge = new PlateBoundaryEdge(id, neighbor);
            Check(actual.Contains(edge),
                $"Missing boundary between real neighbors {id}/{neighbor}: {size}/{seed}");
            Check(Math.Abs(id / topology.Columns - neighbor / topology.Columns) < topology.Rows - 1,
                $"Artificial north/south boundary {id}/{neighbor}: {size}/{seed}");
            expected++;
            if (Math.Abs(id % topology.Columns - neighbor % topology.Columns) == topology.Columns - 1)
                seamBoundaries++;
        }
        Check(expected == boundaries.Edges.Count,
            $"Boundary map has only adjacent cells with differing plates: {size}/{seed}");
        for (int row = 0; row < topology.Rows; row++)
        {
            int west = topology.Id(0, row), east = topology.Id(topology.Columns - 1, row);
            Check(topology.Neighbors(west).Contains(east) &&
                topology.Neighbors(east).Contains(west),
                $"Horizontal seam remains connected: {size}/{seed}/{row}");
            if (plates[west] != plates[east])
                Check(actual.Contains(new PlateBoundaryEdge(west, east)),
                    $"Different plates across seam have a boundary: {size}/{seed}/{row}");
        }
        var repeated = PlateBoundaryMap.Analyze(topology, terrain)!;
        Check(boundaries.Edges.SequenceEqual(repeated.Edges) &&
            boundaries.BoundaryByCellId.SequenceEqual(repeated.BoundaryByCellId) &&
            boundaries.DistanceToBoundaryByCellId.SequenceEqual(repeated.DistanceToBoundaryByCellId),
            $"Complete boundary analysis is deterministic: {size}/{seed}");
        return seamBoundaries;
    }

    private static void VerifyPersistence(GEvolve.Engine.World.LandmassGeneration.GeneratedWorld preview,
        byte[] plates)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-tectonic-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path, new GameState
            {
                HexGrid = new HexGridState(preview.Grid.Definition),
                Landmass = preview.Landmass
            });
            var restored = GameSaveStore.Load(path).Landmass!;
            Check(restored.TectonicPreviewOnly && restored.PlateByCellId!.SequenceEqual(plates),
                "Tectonic preview survives save/load");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
