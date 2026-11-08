using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;
using GEvolve.Engine.Grid;

public static class PangaeaBaseAuditTests
{
    private static readonly int[] Seeds = [-1387338765, -132790047];
    private static readonly int[] Thresholds = [60, 75, 85, 90, 95, 100];
    public static void Run()
    {
        MorphologyControls();
        CaptureReference(); // Post-instrumentation normal path: same protocol as the prior capture.
        foreach (var size in Enum.GetValues<GridSize>()) foreach (int seed in Seeds) Audit(size, seed);
        Console.WriteLine("BASE_AUDIT_PASS: eight worlds; detached opt-in A-H fields; unchanged automatic/manual geology+ecology; persistence; deterministic sources; morphology controls; no calibration or baseline updates");
    }
    private static void Audit(GridSize size, int seed)
    {
        var config = LandmassProfiles.Create(size, LandmassType.Pangaea, seed); var service = new WorldGenerationService();
        var control = service.GenerateGeology(config);
        string referencePath = Path.Combine("GEvolve.Tests", "ValidationReports", "P03-A74-reference_20261108_221618802Z_0fd6f9ac", "base-reference.log");
        const string prefix = "BASE_REFERENCE ";
        bool matchedReference = false;
        foreach (string line in File.ReadLines(referencePath).Where(line => line.StartsWith(prefix, StringComparison.Ordinal)))
        {
            using var reference = JsonDocument.Parse(line[prefix.Length..]);
            var expected = reference.RootElement;
            if (expected.GetProperty("Size").GetString() != size.ToString() || expected.GetProperty("Seed").GetInt32() != seed) continue;
            matchedReference = true;
            Check(expected.GetProperty("State").GetString() == Hash(control.Landmass) &&
                expected.GetProperty("Base").GetString() == Hash(control.PangaeaElevations!.BaseByCellId.ToArray()) &&
                expected.GetProperty("Final").GetString() == Hash(control.Landmass.ElevationByCellId!), "Normal path equals independently captured pre-instrumentation state/BASE/FINAL");
        }
        Check(matchedReference, "Required pre-instrumentation reference exists");
        Check(control.PangaeaElevations!.Sources!.Fields is null, "No detailed fields on normal path");
        var session = service.CreatePangaeaSession(config, captureBaseFields: true);
        foreach (var stage in PangaeaGenerationSession.GeologicalStages.Take(5)) session.Execute(stage);
        var beforeRanges = session.GetSnapshot();
        Check(beforeRanges.Base is not null && beforeRanges.Mountains is null && beforeRanges.Elevations is null,
            "BASE high structures exist before mountain selection");
        var sources = beforeRanges.BaseSources!; var fields = sources.Fields!;
        Check(fields.InitialPressure.Length == control.Grid.Cells.Count, "All requested fields captured only on opt-in");
        var topology = new HexTopology(control.Grid); int n = topology.Count;
        byte[] surface = control.Landmass.SurfaceByCellId, normalized = sources.PreviousBaseByCellId.ToArray(),
            shaped = sources.PriorCalibrationBaseByCellId.ToArray(), initial = beforeRanges.Base!;
        int[] land = Enumerable.Range(0, n).Where(i => surface[i] == 1).ToArray();
        var raw = fields.InitialPressure.ToArray(); var smooth = fields.SmoothedPressure.ToArray();
        var regional = fields.RegionalNoise.ToArray(); var local = fields.LocalNoise.ToArray(); var potential = fields.Potential.ToArray();
        for (int id = 0; id < n; id++)
        {
            Check(raw[id] == Math.Max(0, fields.Compression.Span[id] - fields.Extension.Span[id]), "A is the actual clipped legacy pressure");
            Check(potential[id] == 2.5 * smooth[id] + 1.3 * regional[id] + .45 * local[id], "E exactly composes captured B/C/D without new samples");
        }
        var smoothing = raw.ToArray();
        for (int pass = 0; pass < 4; pass++)
        {
            var next = new double[n];
            for (int id = 0; id < n; id++) { double sum = smoothing[id] * 2; int weight = 2;
                foreach (int nb in topology.Neighbors(id)) { sum += smoothing[nb]; weight++; } next[id] = sum / weight; }
            smoothing = next;
        }
        Check(smoothing.SequenceEqual(smooth), "B is exactly four self-weight-two passes on actual neighbors");
        Check(land.All(i => initial[i] >= shaped[i]) && land.Where(i => shaped[i] >= 85 || initial[i] >= 85).All(i => initial[i] == shaped[i]),
            "Shoulder recovery never lowers and leaves every >=85 cell/height unchanged");
        foreach (var stage in PangaeaGenerationSession.GeologicalStages.Skip(5)) session.Execute(stage);
        var world = session.GetCompletedWorld(); var terrain = world.Landmass; var final = terrain.ElevationByCellId!;
        Check(Hash(terrain) == Hash(control.Landmass) && Hash(initial) == Hash(control.PangaeaElevations.BaseByCellId.ToArray()), "Opt-in tracing cannot change complete geology/BASE/FINAL");
        Check(Hash(beforeRanges.Base!) == Hash(session.GetSnapshot().Base!), "Mountain selection cannot change pre-existing BASE");
        var repeat = service.CreatePangaeaSession(config, captureBaseFields: true);
        foreach (var stage in PangaeaGenerationSession.GeologicalStages.Take(5)) repeat.Execute(stage);
        Check(Hash(repeat.GetSnapshot().BaseSources!.Fields) == Hash(fields), "A-E field capture is deterministic");
        var detached = session.GetSnapshot();
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(detached.BaseSources!.Fields!.Potential, out var buffer);
        buffer.Array![buffer.Offset] = 999;
        Check(session.GetSnapshot().BaseSources!.Fields!.Potential.Span[0] != 999, "Read-only field exports have detached backing arrays");

        var motion = terrain.TectonicMotion!;
        var convergent = motion.Interactions.Where(e => (e.Kind is PlateInteractionKind.Convergent or PlateInteractionKind.Mixed) && e.Convergence >= 1).ToArray();
        var eligible = convergent.Where(e => e.Convergence >= e.Shear * .65 && surface[e.FirstCellId] == 1 && surface[e.SecondCellId] == 1).ToArray();
        int[] Edges(IEnumerable<PlateInteraction> edges) => edges.SelectMany(e => new[] { e.FirstCellId, e.SecondCellId }).Distinct().ToArray();
        var coastCells = land.Where(id => { foreach (int nb in topology.Neighbors(id)) if (surface[nb] == 0) return true; return false; }).ToArray();
        int[] coast = Distances(topology, coastCells), boundary = motion.DistanceToBoundaryByCellId.Select(x => (int)x).ToArray(),
            convergence = Distances(topology, Edges(convergent)), eligibility = Distances(topology, Edges(eligible)),
            axis = Distances(topology, terrain.PangaeaMountainRanges!.Ranges.SelectMany(r => r.AxisCellIds)),
            systems = Distances(topology, Enumerable.Range(0, n).Where(i => terrain.PangaeaMountainRanges.RangeByCellId[i] > 0));
        bool detailed = size == GridSize.Small && seed == Seeds[0] || size == GridSize.Enormous && seed == Seeds[1];
        Emit("BASE_WORLD", new { Size = size.ToString(), Seed = seed, Detailed = detailed, Columns = topology.Columns, Rows = topology.Rows,
            LandCells = land.Length, terrain.DeterministicAttempt, State = Hash(terrain), Base = Hash(initial), Final = Hash(final),
            Sources = new[] { Source("A_InitialPressure", raw), Source("B_FourPassPressure", smooth), Source("C_RegionalNoise5", regional), Source("D_LocalNoise15", local), Source("E_Potential", potential) },
            sources.Quantiles, sources.PotentialAnchors, sources.ElevationAnchors,
            PotentialThresholds = Thresholds.Select(level => new { Level = level, PotentialCutoff = Inverse(level),
                Cells = land.Count(i => potential[i] >= Inverse(level)), Note = "unrounded equivalent cutoff, not raw heights" }),
            Levels = new[] { Levels("F_Percentiles", normalized), Levels("G_ShapeUpperRelief", shaped), Levels("H_BASE", initial), Levels("FINAL", final) },
            Transforms = new[] { Change("F_to_G", normalized, shaped), Change("G_to_H", shaped, initial), Change("H_to_FINAL", initial, final) },
            HighInheritance = Thresholds.Select(t => new { Threshold = t, FinalHigh = land.Count(i => final[i] >= t),
                AlreadyBaseHigh = land.Count(i => final[i] >= t && initial[i] >= t), NewlyCrossedByFinal = land.Count(i => final[i] >= t && initial[i] < t),
                AlreadyHighAndRaised = land.Count(i => initial[i] >= t && final[i] > initial[i]), AxisOverlap = land.Count(i => initial[i] >= t && axis[i] == 0),
                WithinAxis3 = land.Count(i => initial[i] >= t && axis[i] is >= 0 and <= 3), InMountainFootprint = land.Count(i => initial[i] >= t && systems[i] == 0) }),
            H04 = Kinematics(topology, terrain, fields, land, raw, smooth, initial, convergence, eligibility),
            Peaks = Peaks(topology, surface, initial, final, raw, smooth, regional, local, potential, convergence, axis, systems, coast, detailed) });
        foreach (var (name, values, thresholds) in new[] { ("F", normalized, new[] { 85 }), ("G", shaped, new[] { 85 }),
            ("H", initial, Thresholds), ("FINAL", final, new[] { 85 }) })
        foreach (int threshold in thresholds)
            Emit("BASE_MORPH", new { Size = size.ToString(), Seed = seed, Stage = name,
                Morphology = PangaeaBaseMorphology.Measure(topology, surface, values, threshold, boundary, coast, axis, detailed) });
        foreach (var stage in new[] { PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes }) session.Execute(stage);
        var complete = session.GetCompletedWorld();
        Check(Hash(complete.Landmass) == Hash(service.Generate(config).Landmass), "Full automatic/manual ecological result unchanged with trace");
        Persist(complete);
        Console.WriteLine($"BASE_CASE_PASS {size}/{seed}: exact geology/BASE/FINAL; source determinism; detached snapshots; automatic/manual ecology; persistence");

        object Source(string name, double[] values) => new { Name = name, Distribution = Stats(land.Select(i => values[i])),
            CorrelationPotential = Correlation(land, values, potential), CorrelationF = Correlation(land, values, normalized.Select(x => (double)x).ToArray()),
            CorrelationG = Correlation(land, values, shaped.Select(x => (double)x).ToArray()), CorrelationH = Correlation(land, values, initial.Select(x => (double)x).ToArray()),
            MeanAtHighBase = land.Where(i => initial[i] >= 85).Select(i => values[i]).DefaultIfEmpty().Average() };
        object Levels(string name, byte[] values) => new { Name = name, Distribution = Stats(land.Select(i => (double)values[i])),
            Thresholds = Thresholds.Select(t => new { Threshold = t, Cells = land.Count(i => values[i] >= t), Percent = Pct(land.Count(i => values[i] >= t), land.Length) }),
            Exactly100 = land.Count(i => values[i] == 100) };
        object Change(string name, byte[] a, byte[] b) => new { Name = name, Increased = land.Count(i => b[i] > a[i]), Decreased = land.Count(i => b[i] < a[i]),
            MeanDelta = land.Average(i => (double)b[i] - a[i]), Thresholds = Thresholds.Select(t => new { Threshold = t,
                Inherited = land.Count(i => a[i] >= t && b[i] >= t), Entered = land.Count(i => a[i] < t && b[i] >= t),
                Exited = land.Count(i => a[i] >= t && b[i] < t), AlreadyHighRaised = land.Count(i => a[i] >= t && b[i] > a[i]),
                AlreadyHighLowered = land.Count(i => a[i] >= t && b[i] < a[i]) }) };
        double Inverse(int level) { int section = 0; while (section < sources.ElevationAnchors.Length - 2 && level > sources.ElevationAnchors[section + 1]) section++;
            return sources.PotentialAnchors[section] + (level - sources.ElevationAnchors[section]) /
                (sources.ElevationAnchors[section + 1] - sources.ElevationAnchors[section]) * (sources.PotentialAnchors[section + 1] - sources.PotentialAnchors[section]); }
    }

    private static object Kinematics(HexTopology t, LandmassState terrain, PangaeaBaseFields fields, int[] land,
        double[] raw, double[] smooth, byte[] initial, int[] convergence, int[] eligible)
    {
        var motion = terrain.TectonicMotion!; var pairs = new List<(double Legacy, double H04)>();
        foreach (var edge in motion.Interactions)
        {
            int a = edge.FirstPlateId, b = edge.SecondPlateId;
            double dx = fields.CenterX.Span[b] - fields.CenterX.Span[a]; dx -= t.Columns * Math.Round(dx / t.Columns);
            double dy = fields.CenterY.Span[b] - fields.CenterY.Span[a];
            double rate = ((fields.VelocityX.Span[a] - fields.VelocityX.Span[b]) * dx + (fields.VelocityY.Span[a] - fields.VelocityY.Span[b]) * dy) / Math.Sqrt(dx * dx + dy * dy + 1e-6);
            pairs.Add((rate, edge.Convergence - edge.Divergence));
        }
        var active = pairs.Where(p => Math.Abs(p.Legacy) > 1e-9 && Math.Abs(p.H04) > .15).ToArray();
        var c = motion.CompressionByCellId.Select(x => (double)x).ToArray();
        return new { ContactEdges = pairs.Count, ActiveNormalEdges = active.Length, SignAgreements = active.Count(p => Math.Sign(p.Legacy) == Math.Sign(p.H04)),
            SignAgreementPercent = Pct(active.Count(p => Math.Sign(p.Legacy) == Math.Sign(p.H04)), active.Length),
            EdgeRateCorrelation = Correlation(Enumerable.Range(0, pairs.Count).ToArray(), pairs.Select(p => p.Legacy).ToArray(), pairs.Select(p => p.H04).ToArray()),
            InitialPositiveCells = land.Count(i => raw[i] > 0), InitialPositiveFarther5 = land.Count(i => raw[i] > 0 && convergence[i] > 5),
            HighWithoutSmoothedPressure = land.Count(i => initial[i] >= 85 && smooth[i] == 0),
            InitialVsSpreadCompression = Correlation(land, raw, c), SmoothedVsSpreadCompression = Correlation(land, smooth, c),
            BaseVsSpreadCompression = Correlation(land, initial.Select(x => (double)x).ToArray(), c),
            HighFarther5 = land.Count(i => initial[i] >= 85 && convergence[i] > 5), HighFarther10 = land.Count(i => initial[i] >= 85 && convergence[i] > 10),
            HighFarther20 = land.Count(i => initial[i] >= 85 && convergence[i] > 20),
            HighConvergenceDistance = Stats(land.Where(i => initial[i] >= 85 && convergence[i] >= 0).Select(i => (double)convergence[i])),
            HighEligibleContactDistance = Stats(land.Where(i => initial[i] >= 85 && eligible[i] >= 0).Select(i => (double)eligible[i])),
            Velocities = motion.Motions.Select((p, i) => new { Plate = i, LegacyX = fields.VelocityX.Span[i], LegacyY = fields.VelocityY.Span[i],
                H04X = p.Velocity.X, H04Y = p.Velocity.Y, p.SpeedCmPerYear }) };
    }
    private static object Peaks(HexTopology t, byte[] surface, byte[] initial, byte[] final, double[] raw, double[] smooth,
        double[] regional, double[] local, double[] potential, int[] convergence, int[] axis, int[] systems, int[] coast, bool detailed)
    {
        var seen = new bool[t.Count]; var peaks = new List<(int Id, int Cells, int Convergence, int Axis, int System)>();
        for (int start = 0; start < t.Count; start++)
        {
            if (seen[start] || surface[start] != 1 || initial[start] < 85) continue;
            var q = new Queue<int>(); q.Enqueue(start); seen[start] = true; bool maximum = true; var plateau = new List<int>();
            while (q.TryDequeue(out int id)) { plateau.Add(id); foreach (int next in t.Neighbors(id)) {
                if (surface[next] != 1) continue;
                if (initial[next] > initial[start]) maximum = false;
                if (!seen[next] && initial[next] == initial[start]) { seen[next] = true; q.Enqueue(next); } } }
            if (maximum) peaks.Add((start, plateau.Count, plateau.Min(i => convergence[i]), plateau.Min(i => axis[i]), plateau.Min(i => systems[i])));
        }
        return new { PlateauMaxima85 = peaks.Count, Farther5 = peaks.Count(p => p.Convergence > 5), Farther10 = peaks.Count(p => p.Convergence > 10),
            Farther20 = peaks.Count(p => p.Convergence > 20), OnAxis = peaks.Count(p => p.Axis == 0), WithinAxis3 = peaks.Count(p => p.Axis is >= 0 and <= 3),
            InFootprint = peaks.Count(p => p.System == 0),
            Details = peaks.OrderByDescending(p => initial[p.Id]).ThenByDescending(p => p.Cells).ThenBy(p => p.Id).Take(detailed ? 50 : 15)
                .Select(p => new { CellId = p.Id, Column = p.Id % t.Columns, Row = p.Id / t.Columns, PlateauCells = p.Cells, Base = initial[p.Id], Final = final[p.Id],
                    H04Distance = convergence[p.Id], AxisDistance = axis[p.Id], FootprintDistance = systems[p.Id], CoastDistance = coast[p.Id],
                    PlateauMinimumH04Distance = p.Convergence, PlateauMinimumAxisDistance = p.Axis, PlateauMinimumFootprintDistance = p.System,
                    A = raw[p.Id], B = smooth[p.Id], C = regional[p.Id], D = local[p.Id], E = potential[p.Id] }) };
    }
    internal static int[] Distances(HexTopology t, IEnumerable<int> sources)
    {
        var result = Enumerable.Repeat(-1, t.Count).ToArray(); var q = new Queue<int>();
        foreach (int id in sources.Distinct()) { result[id] = 0; q.Enqueue(id); }
        while (q.TryDequeue(out int id)) foreach (int nb in t.Neighbors(id)) if (result[nb] < 0) { result[nb] = result[id] + 1; q.Enqueue(nb); }
        // -1 explicitly means no source exists, not an arbitrary long distance.
        return result;
    }
    private static object Stats(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray(); if (sorted.Length == 0) return new { Count = 0 };
        double mean = sorted.Average();
        return new { Count = sorted.Length, Min = sorted[0], P10 = sorted[(int)((sorted.Length - 1) * .1)], Median = sorted[(sorted.Length - 1) / 2],
            P90 = sorted[(int)((sorted.Length - 1) * .9)], P99 = sorted[(int)((sorted.Length - 1) * .99)], Max = sorted[^1], Mean = sorted.Average(),
            StdDev = Math.Sqrt(sorted.Average(v => Math.Pow(v - mean, 2))) };
    }
    private static double? Correlation(int[] ids, double[] a, double[] b)
    {
        if (ids.Length == 0) return null;
        double ma = ids.Average(i => a[i]), mb = ids.Average(i => b[i]), aa = 0, bb = 0, ab = 0;
        foreach (int id in ids) { double x = a[id] - ma, y = b[id] - mb; aa += x * x; bb += y * y; ab += x * y; }
        return aa > 0 && bb > 0 ? ab / Math.Sqrt(aa * bb) : null;
    }
    private static double Pct(int count, int total) => total == 0 ? 0 : 100.0 * count / total;
    private static void Persist(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), "evolve-base-audit-" + Guid.NewGuid().ToString("N") + ".json");
        try { GameSaveStore.Save(path, new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass });
            Check(Hash(GameSaveStore.Load(path).Landmass) == Hash(world.Landmass) && !File.ReadAllText(path).Contains("InitialPressure", StringComparison.Ordinal), "Persisted state unchanged and no audit fields saved"); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void MorphologyControls()
    {
        var t = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small))); int center = t.Id(0, t.Rows / 2);
        byte[] surface = Enumerable.Repeat((byte)1, t.Count).ToArray(), heights = Enumerable.Repeat((byte)40, t.Count).ToArray();
        var distances = new int[t.Count];
        for (int i = 0; i < t.Count; i++) if (t.Distance(center, i) == 3) heights[i] = 95;
        var ring = PangaeaBaseMorphology.Measure(t, surface, heights, 85, distances, distances, distances, true);
        Check(ring.Components == 1 && ring.RingCandidates == 1 && ring.Details.Single().CrossesSeam && !ring.Details.Single().WindsCylinder, "Numerical enclosed ring survives horizontal seam");
        for (int i = 0; i < t.Count; i++) if (t.Distance(center, i) <= 3) heights[i] = 95;
        var disk = PangaeaBaseMorphology.Measure(t, surface, heights, 85, distances, distances, distances, true);
        Check(disk.RingCandidates == 0 && disk.CompactCandidates == 1, "Compact region has graph cycles but is not an annular enclosure");
        Array.Fill(heights, (byte)40); for (int x = -5; x <= 5; x++) heights[t.Id(x, t.Rows / 2)] = 95;
        var line = PangaeaBaseMorphology.Measure(t, surface, heights, 85, distances, distances, distances, true);
        Check(line.Components == 1 && line.Details.Single().DiameterLowerBound == 10 && line.Details.Single().SkeletonEndpoints == 2 && line.RingCandidates == 0,
            "Thin crest crossing seam has correct connected length/endpoints, not screen-width artifact");
        Console.WriteLine("BASE_MORPH_CONTROLS_PASS: seam-crossing enclosed ring, compact disk rejection, thin crest length/endpoints; true hex neighbors, no images");
    }
    public static void CaptureReference()
    {
        _ = new WorldGenerationService().GenerateGeology(GridSize.Small, LandmassType.Pangaea, Seeds[0]);
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in Seeds)
        {
            var samples = new List<double>(); var allocated = new List<long>(); GeneratedWorld world = null!;
            for (int repeat = 0; repeat < 3; repeat++)
            {
                long bytes = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
                world = new WorldGenerationService().GenerateGeology(size, LandmassType.Pangaea, seed);
                samples.Add(timer.Elapsed.TotalMilliseconds); allocated.Add(GC.GetAllocatedBytesForCurrentThread() - bytes);
            }
            Emit("BASE_REFERENCE", new { Size = size.ToString(), Seed = seed, world.Landmass.DeterministicAttempt,
                State = Hash(world.Landmass), Base = Hash(world.PangaeaElevations!.BaseByCellId.ToArray()),
                Final = Hash(world.Landmass.ElevationByCellId!), SamplesMs = samples, AllocatedBytes = allocated });
        }
        Console.WriteLine("BASE_REFERENCE_PASS: eight mandatory worlds; three uninstrumented geology measurements each; no baseline file changed");
    }
    private static void Emit(string name, object value) => Console.WriteLine(name + " " + JsonSerializer.Serialize(value));
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
