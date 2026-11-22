// Compares provisional WG-01-C numeric policies against real, immutable production R partitions.
// This test-only calibration tool never publishes motion data or changes the accepted Engine or Laboratory.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MotionProfileComparison
{
    // No candidate is an approved profile, production default or frozen compatibility revision.
    private sealed record Profile(string Id, double MinimumSpeed, double MaximumSpeed, float Activity, float MixedRatio);
    private static readonly Profile[] Profiles =
    [
        new("H", 1, 8, .15f, .5f),
        new("Q", 1, 8, .50f, .5f),
        new("M", 1, 8, .15f, .75f),
        new("Z", 0, 8, .15f, .5f)
    ];
    private sealed record CalCase(string Profile, string Size, string Seed);
    private readonly record struct Motion(double Direction, double Speed, double X, double Y);
    private readonly record struct Edge(int First, int Second, double X, double Y, float Normal, float Tangent, int Kind);
    private sealed record Batch(double Milliseconds, long AllocatedBytes);
    private sealed record Result(string Profile, string Size, string Seed, string OwnershipDigest, string CandidateDigest,
        int BoundaryCount, int SeamCount, int[] Classes, double MinimumSpeed, double MaximumSpeed, Motion[] Motions, Batch[] Batches);

    // Compare production values against pre-approval evidence without reusing the experiment's generator/projector.
    internal static void VerifyApprovedM()
    {
        var cases = JsonSerializer.Deserialize<Result[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "experiments", "motion-cal01", "cases.json")))!.Where(row => row.Profile == "M").ToArray();
        Require(cases.Length == 75, "Missing M calibration evidence.");
        foreach (var row in cases)
        {
            var (grid, words) = GenerateComplete(Enum.Parse<MapSize>(row.Size), long.Parse(row.Seed, CultureInfo.InvariantCulture));
            var data = TectonicData.FromLayerData(words, grid);
            var motions = Enumerable.Range(0, 14).Select(id => data.GetMotion((byte)id)).Select(value =>
                new Motion(value.DirectionDegrees, value.SpeedCmPerYear, value.Velocity.X, value.Velocity.Y)).ToArray();
            var edges = Enumerable.Range(0, data.BoundaryCount).Select(data.GetBoundary).Select(value =>
                new Edge(value.FirstCellId, value.SecondCellId, value.Normal.X, value.Normal.Y, value.SignedNormal, value.SignedTangent, (int)value.Kind)).ToArray();
            Require(Digest(motions, edges) == row.CandidateDigest && TectonicCalibrationChecks.Digest(OwnershipOnly(words)) == row.OwnershipDigest,
                $"Approved M comparison mismatch: {row.Size}/{row.Seed}.");
        }
        Console.WriteLine("Approved M: all 75 full candidate fingerprints and unchanged R Ownership fingerprints matched.");
    }

    internal static void Run(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Use an empty output directory; preserve previous calibration evidence.");
        NumericChecks();
        var samples = JsonSerializer.Deserialize<CalCase[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "experiments", "cal01", "cases.json")))!.Where(row => row.Profile == "R").ToArray();
        Require(samples.Length == 75 && samples.Select(row => (row.Size, row.Seed)).Distinct().Count() == 75,
            "The full CAL01 R sample is required.");
        Console.WriteLine("Motion CAL01: PROVISIONAL numeric candidates; no production profile selected.");
        Console.WriteLine("Numeric checks passed: thresholds, ties, signs, zero, malformed values and seam projections.");
        var results = new List<Result>();
        foreach (var size in Enum.GetValues<MapSize>())
        {
            // Warm every candidate at each size; partition/view validation are outside measured intervals.
            var (warmGrid, warmData) = Generate(size);
            var warmView = TectonicData.FromLayerData(warmData, warmGrid);
            foreach (var profile in Profiles) _ = Build(warmGrid, warmView, GenerateMotions(0, profile), profile);
            foreach (var sample in samples.Where(row => row.Size == size.ToString()))
            {
                long seed = long.Parse(sample.Seed, CultureInfo.InvariantCulture);
                var (grid, words) = Generate(size, seed);
                var view = TectonicData.FromLayerData(words, grid);
                string originalDigest = TectonicCalibrationChecks.Digest(words);
                foreach (var profile in Profiles)
                {
                    var motions = GenerateMotions(seed, profile);
                    var edges = Build(grid, view, motions, profile);
                    VerifyEdges(grid, view, motions, edges, profile);
                    string digest = Digest(motions, edges);
                    var batches = new Batch[3];
                    for (int repeat = 0; repeat < batches.Length; repeat++)
                    {
                        // Measure motion + full Grid scan + sorting + projection, excluding hashing, assertions and output.
                        long allocated = GC.GetAllocatedBytesForCurrentThread();
                        long start = Stopwatch.GetTimestamp();
                        var repeatedMotions = GenerateMotions(seed, profile);
                        var repeatedEdges = Build(grid, view, repeatedMotions, profile);
                        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        batches[repeat] = new(elapsed, GC.GetAllocatedBytesForCurrentThread() - allocated);
                        Require(Digest(repeatedMotions, repeatedEdges) == digest, "Full candidate replay mismatch.");
                        GC.KeepAlive(repeatedEdges);
                    }
                    int[] classes = Enumerable.Range(0, 5).Select(kind => edges.Count(edge => edge.Kind == kind)).ToArray();
                    results.Add(new(profile.Id, size.ToString(), sample.Seed, originalDigest, digest, edges.Length,
                        edges.Count(edge => Math.Abs(grid.GetOffset(edge.First).Column - grid.GetOffset(edge.Second).Column) > 1),
                        classes, motions.Min(motion => motion.Speed), motions.Max(motion => motion.Speed), motions, batches));
                }
                Require(TectonicCalibrationChecks.Digest(words) == originalDigest, "Experiment modified published ownership.");
            }
            Console.WriteLine($"{size}: 25 R worlds, 4 candidates, 3 complete replays per candidate passed.");
        }
        // Motions must depend on seed alone, never on size or partition consumption.
        foreach (var group in results.GroupBy(row => (row.Seed, row.Profile)).Where(group => group.Count() > 1))
            Require(group.All(row => row.Motions.SequenceEqual(group.First().Motions)), "Size changed the motion stream.");
        Directory.CreateDirectory(directory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        WriteJson("profiles.json", Profiles);
        WriteJson("cases.json", results);
        WriteJson("environment.json", new
        {
            Runtime = RuntimeInformation.FrameworkDescription, OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), LogicalProcessors = Environment.ProcessorCount,
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "runtime default",
            Warmup = "One complete motion/build per size/profile; production partition is generated before measurement.",
            Measurement = "Three serial batches per world/profile; 25 worlds per size; full motion + scan + sort + projection.",
            Exclusions = "Partition, view validation, independent oracle, digest, JSON, console output and GC collections.",
            Limitations = "Fixed H/Q/M/Z order, no CPU reservation/affinity/frequency lock; allocations are not peak or retained heap."
        });
        var csv = new StringBuilder("Profile,Size,Worlds,Boundaries,Quiet,Convergent,Divergent,Transform,Mixed,MedianMs,MinMs,MaxMs,MinAllocatedBytes,MaxAllocatedBytes\n");
        foreach (var group in results.GroupBy(row => (row.Profile, row.Size)))
        {
            double[] elapsed = group.SelectMany(row => row.Batches).Select(batch => batch.Milliseconds).Order().ToArray();
            long[] bytes = group.SelectMany(row => row.Batches).Select(batch => batch.AllocatedBytes).ToArray();
            csv.Append(CultureInfo.InvariantCulture, $"{group.Key.Profile},{group.Key.Size},{group.Count()},{group.Sum(row => row.BoundaryCount)},");
            csv.Append(string.Join(',', Enumerable.Range(0, 5).Select(kind => group.Sum(row => row.Classes[kind]))));
            csv.Append(CultureInfo.InvariantCulture, $",{elapsed[elapsed.Length / 2]:F6},{elapsed.Min():F6},{elapsed.Max():F6},{bytes.Min()},{bytes.Max()}\n");
        }
        File.WriteAllText(Path.Combine(directory, "summary.csv"), csv.ToString(), new UTF8Encoding(false));
        Console.WriteLine(csv);
        Console.WriteLine("PASS: 300 candidate/world cases, 900 full candidate replays; no production motion publication or GUI validation claimed.");
        void WriteJson<T>(string name, T value) => File.WriteAllText(Path.Combine(directory, name), JsonSerializer.Serialize(value, json), new UTF8Encoding(false));
    }

    // Exactly the third reserved stage draw, followed by two upper-53-bit draws per ascending PlateId.
    private static Motion[] GenerateMotions(long seed, Profile profile)
    {
        var stage = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
        _ = stage.NextUInt64(); _ = stage.NextUInt64();
        var random = new DeterministicStageRandom(stage.NextUInt64());
        var motions = new Motion[14];
        for (int plate = 0; plate < motions.Length; plate++)
        {
            double direction = TectonicPartitionGenerator.NextUnit(random) * 360d;
            double speed = profile.MinimumSpeed + (profile.MaximumSpeed - profile.MinimumSpeed) * TectonicPartitionGenerator.NextUnit(random);
            motions[plate] = ToMotion(direction, speed);
        }
        return motions;
    }

    private static Motion ToMotion(double direction, double speed)
    {
        if (!double.IsFinite(direction) || direction < 0 || direction >= 360 || !double.IsFinite(speed) || speed < 0)
            throw new ArgumentException("Motion requires a finite canonical direction and nonnegative finite speed.");
        if (speed == 0) return new(0, 0, 0, 0);
        double radians = direction * (Math.PI / 180d);
        return new(direction == 0 ? 0 : direction, speed, Zero(speed * Math.Cos(radians)), Zero(speed * Math.Sin(radians)));
    }

    private static Edge[] Build(HexGridData grid, TectonicData view, Motion[] motions, Profile profile)
    {
        var edges = new List<Edge>();
        for (int cell = 0; cell < grid.CellCount; cell++)
        for (int direction = 0; direction < 6; direction++)
            if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && cell < next && view.GetPlateId(cell) != view.GetPlateId(next))
            {
                var normal = Normal(grid, cell, next);
                var (n, t) = Project(motions[view.GetPlateId(cell)], motions[view.GetPlateId(next)], normal.X, normal.Y);
                edges.Add(new(cell, next, normal.X, normal.Y, n, t, Classify(n, t, profile.Activity, profile.MixedRatio)));
            }
        edges.Sort((a, b) => a.First != b.First ? a.First.CompareTo(b.First) : a.Second.CompareTo(b.Second));
        return edges.ToArray();
    }

    private static (double X, double Y) Normal(HexGridData grid, int first, int second)
    {
        var a = FlatHexGeometry.Center(grid.GetOffset(first));
        var b = FlatHexGeometry.Center(grid.GetOffset(second));
        double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y, period = FlatHexGeometry.PeriodWidth(grid.Columns);
        if (dx > period / 2) dx -= period;
        else if (dx < -period / 2) dx += period;
        double length = Math.Sqrt(dx * dx + dy * dy);
        return (Zero(dx / length), Zero(dy / length));
    }

    private static (float Normal, float Tangent) Project(Motion first, Motion second, double nx, double ny) =>
        (Store((second.X - first.X) * nx + (second.Y - first.Y) * ny),
         Store(-(second.X - first.X) * ny + (second.Y - first.Y) * nx));

    private static int Classify(float normal, float tangent, float activity, float mixed)
    {
        if (!float.IsFinite(normal) || !float.IsFinite(tangent) || !float.IsFinite(activity) || activity < 0 ||
            !float.IsFinite(mixed) || mixed <= 0 || mixed > 1) throw new ArgumentException("Invalid classification values.");
        double axial = Math.Abs((double)normal), shear = Math.Abs((double)tangent);
        double maximum = Math.Max(axial, shear), minimum = Math.Min(axial, shear);
        if (maximum <= activity) return 0;
        // Promote stored floats before multiplication: one explicit classification precision on both sides of a tie.
        if (minimum >= activity && minimum >= (double)mixed * maximum) return 4;
        if (shear > axial) return 3;
        return normal < 0 ? 1 : 2;
    }

    private static float Store(double value)
    {
        float stored = (float)value;
        if (!double.IsFinite(value) || !float.IsFinite(stored)) throw new ArgumentException("Projection is not representable.");
        return stored == 0 ? 0 : stored;
    }
    private static double Zero(double value) => value == 0 ? 0 : value;

    // This oracle owns its Offset topology and analytic unit normals, independent from Build's accepted Grid queries.
    private static void VerifyEdges(HexGridData grid, TectonicData view, Motion[] motions, Edge[] edges, Profile profile)
    {
        var expected = new HashSet<(int, int)>();
        for (int cell = 0; cell < grid.CellCount; cell++)
            foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
                if (cell < next && view.GetPlateId(cell) != view.GetPlateId(next)) expected.Add((cell, next));
        Require(expected.SetEquals(edges.Select(edge => (edge.First, edge.Second))) && expected.Count == edges.Length, "Boundary coverage or duplicates.");
        (int First, int Second) previous = (-1, -1);
        foreach (var edge in edges)
        {
            Require(edge.First > previous.First || edge.First == previous.First && edge.Second > previous.Second, "Boundary ordering.");
            previous = (edge.First, edge.Second);
            var a = grid.GetOffset(edge.First); var b = grid.GetOffset(edge.Second);
            int dc = b.Column - a.Column;
            if (dc > grid.Columns / 2) dc -= grid.Columns;
            if (dc < -grid.Columns / 2) dc += grid.Columns;
            double dx = 1.5 * dc, dy = Math.Sqrt(3) * (b.Row - a.Row + .5 * ((b.Column & 1) - (a.Column & 1)));
            double length = Math.Sqrt(dx * dx + dy * dy);
            Require(Math.Abs(edge.X - dx / length) < 1e-4 && Math.Abs(edge.Y - dy / length) < 1e-4, "Independent analytic normal.");
            var reverse = Normal(grid, edge.Second, edge.First);
            var projected = Project(motions[view.GetPlateId(edge.Second)], motions[view.GetPlateId(edge.First)], reverse.X, reverse.Y);
            Require(projected.Normal == edge.Normal && projected.Tangent == edge.Tangent, "Endpoint reversal changed stored projections.");
            Require(Classify(projected.Normal, projected.Tangent, profile.Activity, profile.MixedRatio) == edge.Kind, "Endpoint reversal changed class.");
            // Orthogonal projection preserves relative speed, allowing only float storage error.
            var first = motions[view.GetPlateId(edge.First)]; var second = motions[view.GetPlateId(edge.Second)];
            double squaredSpeed = Math.Pow(second.X - first.X, 2) + Math.Pow(second.Y - first.Y, 2);
            double squaredProjection = (double)edge.Normal * edge.Normal + (double)edge.Tangent * edge.Tangent;
            Require(Math.Abs(squaredSpeed - squaredProjection) <= 3e-7 * Math.Max(1, squaredSpeed), "Projection magnitude.");
        }
    }

    private static void NumericChecks()
    {
        var east = ToMotion(0, 2); var south = ToMotion(90, 3); var stationary = ToMotion(123, -0d);
        Require(east.X == 2 && east.Y == 0 && Math.Abs(south.X) < 1e-14 && south.Y == 3, "Direction convention.");
        Require(stationary == new Motion(0, 0, 0, 0) && BitConverter.DoubleToUInt64Bits(stationary.Speed) == 0, "Canonical stationary motion.");
        Require(BitConverter.SingleToUInt32Bits(Store(-0d)) == 0 && BitConverter.SingleToUInt32Bits(Store(-double.Epsilon)) == 0, "Canonical float zero.");
        Expect<ArgumentException>(() => ToMotion(360, 1));
        Expect<ArgumentException>(() => ToMotion(double.NaN, 1));
        Expect<ArgumentException>(() => ToMotion(0, -1));
        Expect<ArgumentException>(() => ToMotion(0, double.PositiveInfinity));
        Expect<ArgumentException>(() => Store(double.MaxValue));
        Expect<ArgumentException>(() => Classify(0, 0, 0, 0));
        Expect<ArgumentException>(() => Classify(float.NaN, 0, .15f, .5f));
        Require(Project(stationary, east, 1, 0) == (2f, 0f), "Opening projection.");
        Require(Project(east, stationary, 1, 0) == (-2f, 0f), "Closing projection.");
        Require(Project(stationary, south, 1, 0).Tangent == 3, "Shear projection.");
        Require(Project(east, east, 1, 0) == (0f, 0f), "Identical velocity.");
        foreach (var profile in Profiles)
        {
            float a = profile.Activity, m = profile.MixedRatio;
            Require(Classify(a, -a, a, m) == 0, "Quiet inclusive threshold.");
            Require(Classify(MathF.BitIncrement(a), 0, a, m) == 2, "First active float.");
            Require(Classify(-2, 0, a, m) == 1 && Classify(2, 0, a, m) == 2 && Classify(0, 2, a, m) == 3, "Pure interactions.");
            Require(Classify(2, 2 * m, a, m) == 4, "Mixed inclusive ratio tie.");
            Require(Classify(2, MathF.BitDecrement(2 * m), a, m) == 2, "Below mixed ratio.");
            Require(Classify(2, 2, a, m) == 4, "Axial/shear equality precedence.");
            Require(Classify(0, 0, a, m) == 0, "Stationary quiet.");
            Require(GenerateMotions(0, profile).SequenceEqual(GenerateMotions(0, profile)), "Motion replay.");
        }
        var grid = new HexGridData(MapSize.Small);
        int seamFirst = grid.GetCellId(new(0, 50)), seamSecond = grid.GetCellId(new(grid.Columns - 1, 50));
        var seam = Normal(grid, seamFirst, seamSecond);
        Require(seam.X < 0 && seam.Y > 0 && Math.Abs(seam.X + Math.Sqrt(3) / 2) < 1e-4, "Nearest-image seam normal.");
    }

    // Fingerprints are provisional evidence, never the approved production compatibility fixtures.
    private static string Digest(Motion[] motions, Edge[] edges)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            foreach (var motion in motions) { writer.Write(motion.Direction); writer.Write(motion.Speed); writer.Write(motion.X); writer.Write(motion.Y); }
            foreach (var edge in edges) { writer.Write(edge.First); writer.Write(edge.Second); writer.Write(edge.X); writer.Write(edge.Y); writer.Write(edge.Normal); writer.Write(edge.Tangent); writer.Write(edge.Kind); }
        }
        return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, checked((int)stream.Length))));
    }
}
