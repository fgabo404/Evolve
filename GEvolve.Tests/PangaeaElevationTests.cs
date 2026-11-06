using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game.Graphics;
using GEvolve.Game;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaElevationTests
{
    private sealed record FieldStatistics(int Count, int Min, int Max, double Mean, double Median,
        int[] Percentiles, int[] Bands, int Zeros, int Above75, int Above85, int Above95,
        int Exactly100, int[] HighComponents, int InheritedComponents, int CreatedComponents,
        int MixedComponents, string LargestComponentDetails);

    public static void Run()
    {
        VerifyStatistics();
        using var baselineStream = typeof(PangaeaElevationTests).Assembly
            .GetManifestResourceStream("ApprovedNonContinentalBaseline.txt")!;
        using var baselineReader = new StreamReader(baselineStream);
        var baselines = new Dictionary<(GridSize, int), string[]>();
        while (baselineReader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            if (parts[1] == "Pangaea") baselines.Add((Enum.Parse<GridSize>(parts[0]), int.Parse(parts[2])), parts);
        }
        var cases = Enum.GetValues<GridSize>().SelectMany(size => new[] { 2026, 42, -731 }
            .Select(seed => (Size: size, Seed: seed))).Concat(new[]
        {
            (GridSize.Enormous, 1324918567), (GridSize.Enormous, 418579367),
            (GridSize.Enormous, 1697481406), (GridSize.Enormous, 1242668747),
            (GridSize.Large, -234020843), (GridSize.Large, -690013300)
        });
        foreach (var (size, seed) in cases)
        {
            var timer = Stopwatch.StartNew();
            var world = new WorldGenerationService().Generate(size, LandmassType.Pangaea, seed);
            timer.Stop();
            var fields = world.PangaeaElevations ?? throw new Exception("Missing elevation diagnostics");
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            byte[] initial = fields.BaseByCellId.ToArray(), delta = fields.ContributionByCellId.ToArray();
            byte[] final = fields.FinalByCellId.ToArray();
            if (baselines.TryGetValue((size, seed), out var baseline))
            {
                string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
                Check(terrain.DeterministicAttempt == int.Parse(baseline[3]) &&
                    Hash(terrain.SurfaceByCellId) == baseline[4] && Hash(initial) == baseline[5] &&
                    Hash(terrain.PlateByCellId!) == baseline[6],
                    "Official pre-P03-A surface, base elevation, plates and attempt are unchanged");
            }
            Check(final.SequenceEqual(terrain.ElevationByCellId!) && initial.Length == topology.Count &&
                delta.Length == topology.Count, "Diagnostic fields align with the generated world");
            for (int id = 0; id < topology.Count; id++)
                Check(terrain.IsLand(id)
                    ? initial[id] is >= 1 and <= 100 && final[id] is >= 1 and <= 100 &&
                      initial[id] + delta[id] == final[id]
                    : initial[id] == 0 && delta[id] == 0 && final[id] == 0,
                    "Base plus contribution equals final; ocean remains zero");

            // Reapply only the mountain phase to the captured base: no second world
            // or reconstruction of the original surface is needed for attribution.
            var baseTerrain = terrain with { ElevationByCellId = initial, PangaeaMountainRanges = null };
            var repeated = new PangaeaMountainRangeGenerator().Apply(topology, baseTerrain);
            Check(ReferenceEquals(repeated.SurfaceByCellId, terrain.SurfaceByCellId) &&
                ReferenceEquals(repeated.PlateByCellId, terrain.PlateByCellId) &&
                ReferenceEquals(repeated.TectonicMotion, terrain.TectonicMotion),
                "Diagnostic capture preserves surface, plates and official H04 state");
            Check(repeated.ElevationByCellId!.SequenceEqual(final) &&
                repeated.PangaeaMountainRanges!.RangeByCellId.SequenceEqual(terrain.PangaeaMountainRanges!.RangeByCellId) &&
                repeated.PangaeaMountainRanges.Ranges.Zip(terrain.PangaeaMountainRanges.Ranges).All(pair =>
                    pair.First.Id == pair.Second.Id && pair.First.AxisCellIds.SequenceEqual(pair.Second.AxisCellIds) &&
                    pair.First.WidthByAxisCell.SequenceEqual(pair.Second.WidthByAxisCell)),
                "Diagnostic capture preserves heights, range identity, axes and widths");

            Console.WriteLine($"P03-A.2 {size}/{seed}: generation={timer.Elapsed.TotalMilliseconds:F3} ms");
            Print("BASE", Measure(topology, terrain.SurfaceByCellId, initial));
            Print("APORTE", Measure(topology, terrain.SurfaceByCellId, delta));
            Print("FINAL", Measure(topology, terrain.SurfaceByCellId, final, initial));
            int raised = delta.Count(value => value > 0), total = delta.Sum(value => (int)value);
            int land = terrain.SurfaceByCellId.Count(value => value == 1);
            int limited = fields.CompositionLimitedByCellId.ToArray().Count(value => value);
            bool[] limitedCells = fields.CompositionLimitedByCellId.ToArray();
            int reached94 = Enumerable.Range(0, topology.Count)
                .Count(id => limitedCells[id] && initial[id] < 94 && final[id] == 94);
            int inheritedHigh = Enumerable.Range(0, topology.Count).Count(id => initial[id] >= 85);
            int createdHigh = Enumerable.Range(0, topology.Count).Count(id => initial[id] < 85 && final[id] >= 85);
            Console.WriteLine($"  raised={raised}/{land} ({raised * 100.0 / land:F3}%), " +
                $"gain affected mean={(raised == 0 ? 0 : (double)total / raised):F3}, max={delta.Max()}, " +
                $"composition limited={limited}, reached94 by clipping={reached94}, " +
                $"extreme inherited={inheritedHigh}, created={createdHigh}");
            var sources = fields.Sources ?? throw new Exception("Missing base-source diagnostics");
            Console.WriteLine($"  BASE potential anchors={string.Join(",", sources.PotentialAnchors.Select(v => v.ToString("F5")))}, " +
                $"quantiles={string.Join(",", sources.Quantiles)}, levels={string.Join(",", sources.ElevationAnchors)}");
            Console.WriteLine($"  BASE correlations: approximate tectonics={sources.ApproximationCorrelation:F4}, " +
                $"regional noise={sources.NoiseCorrelation:F4}; >=85 means approx/noise=" +
                $"{sources.MeanApproximationHigh:F4}/{sources.MeanNoiseHigh:F4}; " +
                $"other={sources.MeanApproximationOther:F4}/{sources.MeanNoiseOther:F4}");
            Console.WriteLine($"  SHA256 base={Convert.ToHexString(SHA256.HashData(initial))}, " +
                $"final={Convert.ToHexString(SHA256.HashData(final))}");
            VerifyViews(terrain, fields);
            if (size == GridSize.Small && seed == 2026)
            {
                VerifySave(world);
                VerifySession(world);
            }
        }
        foreach (var type in new[] { LandmassType.Archipelagos, LandmassType.SmallContinents, LandmassType.Continents })
            Check(new WorldGenerationService().Generate(GridSize.Small, type, 2026).PangaeaElevations is null,
                "Pangaea diagnostics are absent for other world types");
    }

    private static FieldStatistics Measure(HexTopology topology, byte[] surface, byte[] values,
        byte[]? original = null)
    {
        var histogram = new int[101];
        int count = 0, total = 0;
        for (int id = 0; id < topology.Count; id++)
            if (surface[id] == 1) { histogram[values[id]]++; count++; total += values[id]; }
        int AtRank(int rank)
        {
            int cumulative = 0;
            for (int level = 0; level <= 100; level++)
            { cumulative += histogram[level]; if (cumulative > rank) return level; }
            throw new InvalidOperationException("An elevation field must contain land");
        }
        int Quantile(double probability) => AtRank((int)Math.Round(probability * (count - 1)));
        var bands = new int[5];
        for (int level = 1; level <= 100; level++) bands[(level - 1) / 20] += histogram[level];
        var seen = new bool[topology.Count];
        var queue = new Queue<int>();
        var sizes = new List<int>();
        int inherited = 0, created = 0, mixed = 0;
        int largest = 0;
        string largestDetails = "none";
        for (int id = 0; id < topology.Count; id++)
        {
            if (seen[id] || surface[id] == 0 || values[id] < 85) continue;
            int size = 0, priorHigh = 0, sum = 0, minimum = 100, maximum = 0;
            seen[id] = true; queue.Enqueue(id);
            while (queue.TryDequeue(out int current))
            {
                size++;
                sum += values[current];
                minimum = Math.Min(minimum, values[current]);
                maximum = Math.Max(maximum, values[current]);
                if (original is not null && original[current] >= 85) priorHigh++;
                foreach (int next in topology.Neighbors(current))
                    if (!seen[next] && surface[next] == 1 && values[next] >= 85)
                    { seen[next] = true; queue.Enqueue(next); }
            }
            sizes.Add(size);
            if (size > largest)
            {
                largest = size;
                largestDetails = $"size={size}, min/max/mean={minimum}/{maximum}/{(double)sum / size:F3}" +
                    (original is null ? "" : $", inherited={priorHigh}, created={size - priorHigh}");
            }
            if (original is not null)
            {
                if (priorHigh == size) inherited++;
                else if (priorHigh == 0) created++;
                else mixed++;
            }
        }
        sizes.Sort((a, b) => b.CompareTo(a));
        return new(count, Quantile(0), Quantile(1), (double)total / count,
            (AtRank((count - 1) / 2) + AtRank(count / 2)) / 2.0,
            new[] { .10, .25, .50, .75, .90, .95, .99 }.Select(Quantile).ToArray(),
            bands, histogram[0], histogram.Skip(75).Sum(), histogram.Skip(85).Sum(),
            histogram.Skip(95).Sum(), histogram[100], sizes.ToArray(), inherited, created, mixed, largestDetails);
    }

    private static void Print(string name, FieldStatistics stats)
    {
        Console.WriteLine($"  {name}: min={stats.Min}, max={stats.Max}, mean={stats.Mean:F3}, " +
            $"median={stats.Median:F3}, P10/25/50/75/90/95/99={string.Join("/", stats.Percentiles)}");
        Console.WriteLine($"    land bands 1-20/21-40/41-60/61-80/81-100=" +
            string.Join("/", stats.Bands.Select(n => $"{n * 100.0 / stats.Count:F3}%")) +
            $", zero={stats.Zeros}, >=75/85/95={stats.Above75}/{stats.Above85}/{stats.Above95}, 100={stats.Exactly100}");
        if (name != "APORTE")
            Console.WriteLine($"    traditional white (>=85)={stats.Above85}; white below100=" +
                $"{stats.Above85 - stats.Exactly100}; largest white component: {stats.LargestComponentDetails}");
        Console.WriteLine($"    >=85 components={stats.HighComponents.Length}, " +
            $"sizes=[{string.Join(",", stats.HighComponents)}], " +
            $"inherited/created/mixed={stats.InheritedComponents}/{stats.CreatedComponents}/{stats.MixedComponents}");
    }

    private static void VerifyStatistics()
    {
        var topology = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        var surface = new byte[topology.Count];
        var values = new byte[topology.Count];
        int a = topology.Id(0, 66);
        int b = topology.Neighbors(a).ToArray().First(id => id % topology.Columns == topology.Columns - 1);
        int c = topology.Id(100, 66);
        int[] ids = [a, b, c, topology.Id(50, 50), topology.Id(50, 51)];
        byte[] levels = [85, 95, 100, 10, 20];
        for (int i = 0; i < ids.Length; i++) { surface[ids[i]] = 1; values[ids[i]] = levels[i]; }
        var stats = Measure(topology, surface, values);
        Check(stats.Min == 10 && stats.Max == 100 && stats.Mean == 62 && stats.Median == 85 && stats.Percentiles[2] == 85 &&
            stats.HighComponents.SequenceEqual(new[] { 2, 1 }) && stats.Above85 == 3 && stats.Exactly100 == 1,
            "Histogram and components use known levels and the east/west seam");
        int north = topology.Id(20, 0), south = topology.Id(20, topology.Rows - 1);
        Check(!topology.Neighbors(north).Contains(south), "Extreme components cannot wrap north/south");
    }

    private static void VerifyViews(LandmassState terrain, PangaeaElevationDiagnostics fields)
    {
        var before = (byte[])terrain.ElevationByCellId!.Clone();
        var mode = PangaeaElevationViews.Next(MapViewMode.Biomes, true);
        Check(mode == MapViewMode.ElevationBase && PangaeaElevationViews.Next(mode, true) == MapViewMode.ElevationContribution &&
            PangaeaElevationViews.Next(MapViewMode.ElevationContribution, true) == MapViewMode.ElevationFinal,
            "The same height control cycles through base, contribution and final");
        int land = Array.FindIndex(terrain.SurfaceByCellId, value => value == 1);
        Check(MapViewColors.ForCell(terrain, land, MapViewMode.ElevationBase, fields) ==
            new Vector3(fields.BaseByCellId.Span[land] / 100f), "Base view uses the actual pre-range height");
        for (int id = 0; id < terrain.SurfaceByCellId.Length; id++)
        {
            var color = MapViewColors.ForCell(terrain, id, MapViewMode.ElevationFinal, fields);
            if (terrain.IsLand(id)) Check(color == new Vector3(before[id] / 100f),
                "Final diagnostic retains numeric differences within the summit range");
        }
        Check(before.SequenceEqual(terrain.ElevationByCellId!), "View queries never mutate geography");
    }

    private static void VerifySave(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-elevations-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path, new GameState { HexGrid = new HexGridState(world.Grid.Definition), Landmass = world.Landmass });
            Check(!File.ReadAllText(path).Contains("PangaeaElevations", StringComparison.Ordinal) &&
                GameSaveStore.Load(path).Landmass!.ElevationByCellId!.SequenceEqual(world.Landmass.ElevationByCellId!),
                "Diagnostic arrays are transient and saved final heights remain compatible");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void VerifySession(GeneratedWorld world)
    {
        var session = new GameSession();
        session.OpenCreateWorld();
        session.ChooseSize(world.Grid.Definition.Size);
        session.ChooseType(LandmassType.Pangaea);
        session.SetSeedText(world.Landmass.Configuration.Seed.ToString());
        session.BeginGeneration();
        session.CompleteGeneration(world);
        var selected = session.SelectAt(FlatHexGeometry.Center(new GridAddress(20, 20)));
        Check(selected is not null && ReferenceEquals(session.PangaeaElevations, world.PangaeaElevations),
            "A session consumes diagnostics from the same generated world");
        foreach (var mode in new[] { MapViewMode.ElevationBase, MapViewMode.ElevationContribution, MapViewMode.ElevationFinal })
            _ = MapViewColors.ForCell(world.Landmass, selected!.Id, mode, session.PangaeaElevations);
        Check(session.SelectedCell == selected && ReferenceEquals(session.State.Landmass, world.Landmass),
            "Diagnostic views preserve selection and world state");
        session.ReturnToMenu();
        Check(session.PangaeaElevations is null, "Diagnostic arrays are released with the session world");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
