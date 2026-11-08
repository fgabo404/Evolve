using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.State.Game;

public static class PangaeaUpliftTests
{
    private sealed record Reference(string Size, int Seed, int Attempt, string Surface, string Plates,
        string Motion, string Base, string Geometry, string Influence, string AdditiveFinal);

    public static void Run()
    {
        var references = new Dictionary<string, Reference>();
        {
            using var stream = typeof(PangaeaUpliftTests).Assembly.GetManifestResourceStream("PangaeaUpliftReference.txt")
                ?? throw new Exception("Missing immutable P03-A.4 comparison reference");
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
                var item = JsonSerializer.Deserialize<Reference>(line)!;
                references.Add($"{item.Size}/{item.Seed}", item);
            }
        }
        var cases = Enum.GetValues<GridSize>().SelectMany(size => new[] { 2026, 42, -731 }.Select(seed => (size, seed)))
            .Concat(new[] { -714216669, -2024691550, -174327693, -1979952984, 2076374193,
                -234020843, -690013300 }.Select(seed => (GridSize.Large, seed)))
            .Append((GridSize.Enormous, 1697481406)).ToArray();
        Check(cases.Length == 20 && cases.Distinct().Count() == 20, "Required comparison cases");
        Check(references.Count == cases.Length, "Complete immutable reference coverage");
        foreach (var (size, seed) in cases)
        {
            var timer = Stopwatch.StartNew();
            var world = new WorldGenerationService().GenerateGeology(size, LandmassType.Pangaea, seed);
            double generationMs = timer.Elapsed.TotalMilliseconds;
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            var fields = world.PangaeaElevations!;
            byte[] initial = fields.BaseByCellId.ToArray(), final = fields.FinalByCellId.ToArray();
            byte[] previous = fields.AdditiveFinalByCellId.ToArray();
            var ranges = terrain.PangaeaMountainRanges!;
            var reference = new Reference(size.ToString(), seed, terrain.DeterministicAttempt,
                Hash(terrain.SurfaceByCellId), Hash(terrain.PlateByCellId!), Hash(terrain.TectonicMotion),
                Hash(initial), Hash(ranges.Ranges.Select(range => range with { AffectedCells = 0 }).ToArray()),
                Hash(ranges.RangeByCellId), Hash(previous));
            Console.WriteLine("UPLIFT_REFERENCE " + JsonSerializer.Serialize(reference));
            Check(references.TryGetValue($"{size}/{seed}", out var expected) && expected == reference,
                $"P03-A.4 BASE, geography, H04, geometry, influence or additive reference changed: {size}/{seed}");
            var affected = new int[ranges.Ranges.Length + 1];
            for (int id = 0; id < topology.Count; id++)
            {
                Check(terrain.IsLand(id) ? final[id] >= previous[id] && final[id] <= 100 : final[id] == 0,
                    "Existing relief and ocean retained");
                Check(ranges.RangeByCellId[id] != 0 || final[id] == initial[id], "No elevation outside existing corridors");
                Check(initial[id] <= 94 || final[id] == initial[id], "Inherited summits above composition limit retained");
                Check(initial[id] + fields.ContributionByCellId.Span[id] == final[id], "Diagnostic contribution is actual gain");
                if (final[id] > initial[id]) affected[ranges.RangeByCellId[id]]++;
            }
            Check(affected[0] == 0 && ranges.Ranges.All(range => range.AffectedCells == affected[range.Id]),
                "Affected-cell metadata follows actual final heights and unchanged owners");
            var repeated = new PangaeaMountainRangeGenerator().Apply(topology,
                terrain with { ElevationByCellId = initial, PangaeaMountainRanges = null });
            Check(Hash(repeated) == Hash(terrain), "Mountain phase deterministic without diagnostic callbacks");
            var restored = JsonSerializer.Deserialize<LandmassState>(JsonSerializer.Serialize(terrain))!;
            Check(Hash(restored) == Hash(terrain), "Stored final and all mountain metadata round-trip unchanged");
            Console.WriteLine($"UPLIFT_COUNTS {size}/{seed}: systems={ranges.Ranges.Length}; " +
                $"axisMean={ranges.Ranges.SelectMany(r => r.AxisCellIds).Select(id => (double)previous[id]).DefaultIfEmpty().Average():F3}" +
                $"->{ranges.Ranges.SelectMany(r => r.AxisCellIds).Select(id => (double)final[id]).DefaultIfEmpty().Average():F3}; " +
                $"white={previous.Count(h => h >= 85)}->{final.Count(h => h >= 85)}; generationMs={generationMs:F3}");
            Console.WriteLine("UPLIFT_CASE " + JsonSerializer.Serialize(new
            {
                Size = size.ToString(), Seed = seed, GenerationMs = generationMs, Invariants = "PASS",
                Composition = PangaeaMountainRangeGenerator.ElevationCompositionVersion,
                Systems = ranges.Ranges.Length, AxisCells = ranges.Ranges.Sum(r => r.AxisCellIds.Length),
                InfluenceCells = ranges.RangeByCellId.Count(id => id > 0),
                Before = Measure(topology, terrain, initial, previous, "P03-A.4"),
                After = Measure(topology, terrain, initial, final, "P03-A.6")
            }));
        }
        Console.WriteLine("UPLIFT_PASS: twenty cases; immutable P03-A.4 geography/BASE/geometry/influence and additive heights verified; deterministic composition and metadata round-trip");
    }

    private static object Measure(HexTopology topology, LandmassState terrain, byte[] initial, byte[] heights, string version)
    {
        var ranges = terrain.PangaeaMountainRanges!;
        var slopes = TerrainSlope.Calculate(topology, terrain with { ElevationByCellId = heights })!;
        int count = ranges.Ranges.Length + 1;
        var outsideSum = new double[count]; var outsideLinks = new int[count];
        var raised = new int[count]; var gainSum = new double[count];
        var slopeSum = new double[count]; var influence = new int[count];
        int[] bands = new int[4], corridorBands = new int[4];
        int changed = 0, exactly94 = 0, new94 = 0, maximumStep = 0;
        var regionLand = new int[9]; var regionRaised = new int[9]; var regionHigh = new int[9];
        var regionSum = new double[9]; var regionBase = new double[9];
        var regionAxes = Enumerable.Range(0, 9).Select(_ => new HashSet<int>()).ToArray();
        int Region(int id) => id / topology.Columns * 3 / topology.Rows * 3 + id % topology.Columns * 3 / topology.Columns;
        foreach (var range in ranges.Ranges)
            foreach (int id in range.AxisCellIds) regionAxes[Region(id)].Add(range.Id);
        for (int id = 0; id < topology.Count; id++)
        {
            if (!terrain.IsLand(id)) continue;
            int owner = ranges.RangeByCellId[id], delta = heights[id] - initial[id], r = Region(id);
            regionLand[r]++; regionSum[r] += heights[id]; regionBase[r] += initial[id];
            if (delta > 0) { changed++; raised[owner]++; regionRaised[r]++; }
            if (heights[id] >= 85) regionHigh[r]++;
            if (heights[id] == 94) { exactly94++; if (initial[id] < 94) new94++; }
            int band = heights[id] < 61 ? -1 : heights[id] <= 75 ? 0 : heights[id] <= 84 ? 1 : heights[id] <= 94 ? 2 : 3;
            if (band >= 0) { bands[band]++; if (owner != 0) corridorBands[band]++; }
            if (owner != 0)
            {
                influence[owner]++; slopeSum[owner] += slopes[id]; gainSum[owner] += delta;
                foreach (int next in topology.Neighbors(id))
                    if (terrain.IsLand(next) && ranges.RangeByCellId[next] != owner)
                    { outsideSum[owner] += heights[next]; outsideLinks[owner]++; }
            }
            foreach (int next in topology.Neighbors(id))
                if (terrain.IsLand(next)) maximumStep = Math.Max(maximumStep, Math.Abs(heights[id] - heights[next]));
        }
        var systems = ranges.Ranges.Select(range =>
        {
            int[] axis = range.AxisCellIds.Select(id => (int)heights[id]).Order().ToArray();
            int Quantile(double q) => axis[(int)Math.Round(q * (axis.Length - 1))];
            double local = range.AxisCellIds.Average(id =>
            {
                int[] neighbors = topology.Neighbors(id).ToArray().Where(terrain.IsLand).ToArray();
                return heights[id] - (neighbors.Length == 0 ? heights[id] : neighbors.Average(next => heights[next]));
            });
            return new
            {
                range.Id, Length = axis.Length, range.MeanConvergence, range.PeakConvergence,
                Min = axis[0], Mean = axis.Average(), Max = axis[^1], P10 = Quantile(.10), P50 = Quantile(.50), P90 = Quantile(.90),
                AxisGain = range.AxisCellIds.Average(id => heights[id] - initial[id]), LocalProminence = local,
                OutsideProminence = outsideLinks[range.Id] == 0 ? (double?)null : axis.Average() - outsideSum[range.Id] / outsideLinks[range.Id],
                Influence = influence[range.Id], Raised = raised[range.Id], TotalGain = gainSum[range.Id],
                MeanSlope = influence[range.Id] == 0 ? 0 : slopeSum[range.Id] / influence[range.Id]
            };
        }).ToArray();
        int[] allAxis = ranges.Ranges.SelectMany(r => r.AxisCellIds).ToArray();
        var high = new byte[topology.Count];
        for (int id = 0; id < topology.Count; id++) if (terrain.IsLand(id) && heights[id] >= 85) high[id] = 1;
        var components = LandmassAnalysis.Analyze(topology, high);
        return new
        {
            Version = version, Bands61_75_76_84_85_94_95_100 = bands, CorridorBands = corridorBands,
            Raised = changed, Exactly94 = exactly94, New94 = new94, MaximumLandNeighborStep = maximumStep,
            HighComponents = components.ComponentCount, LargestHighComponent = components.DominantMassSize,
            MeanAxis = allAxis.Length == 0 ? (double?)null : allAxis.Average(id => heights[id]),
            MeanAxisGain = allAxis.Length == 0 ? (double?)null : allAxis.Average(id => heights[id] - initial[id]),
            MeanLocalProminence = allAxis.Length == 0 ? (double?)null : systems.Sum(s => s.LocalProminence * s.Length) / allAxis.Length,
            MeanCorridorSlope = influence.Sum() == 0 ? (double?)null : slopeSum.Sum() / influence.Sum(), Systems = systems,
            Regions = Enumerable.Range(0, 9).Select(r => new
            {
                Region = r, Land = regionLand[r], Axes = regionAxes[r].Count, Raised = regionRaised[r], High85 = regionHigh[r],
                RaisedPercentLand = regionLand[r] == 0 ? (double?)null : regionRaised[r] * 100.0 / regionLand[r],
                MeanBase = regionLand[r] == 0 ? (double?)null : regionBase[r] / regionLand[r],
                MeanFinal = regionLand[r] == 0 ? (double?)null : regionSum[r] / regionLand[r]
            }).ToArray()
        };
    }
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
