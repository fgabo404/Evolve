// Verifies production M kinematics, complete canonical interfaces and schema safety independently of rendering.
// Keeps full C compatibility buffers separate from the fifteen accepted B Ownership fixtures.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicMotionChecks
{
    internal static void Numeric()
    {
        Require(PlateMotionGenerator.Revision == 1 && PlateMotionGenerator.AlgorithmVersion == "tectonic-motion-m-v1" &&
            BitConverter.SingleToUInt32Bits(PlateMotionGenerator.Activity) == 0x3E19999A &&
            BitConverter.SingleToUInt32Bits(PlateMotionGenerator.MixedRatio) == 0x3F400000, "Frozen M policy.");
        var zero = new PlateMotion(0, 123, -0d);
        Require(zero.DirectionDegrees == 0 && BitConverter.DoubleToUInt64Bits(zero.SpeedCmPerYear) == 0 && zero.Velocity == new TectonicVector(0, 0), "Stationary canonical values.");
        Require(new PlateMotion(0, 0, 2).Velocity == new TectonicVector(2, 0), "East convention.");
        Require(Math.Abs(new PlateMotion(0, 90, 3).Velocity.X) < 1e-14 && new PlateMotion(0, 90, 3).Velocity.Y == 3, "South convention.");
        Expect<ArgumentException>(() => new PlateMotion(0, 360, 1));
        Expect<ArgumentException>(() => new PlateMotion(14, 0, 1));
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -1d })
        {
            Expect<ArgumentException>(() => new PlateMotion(0, bad, 1));
            Expect<ArgumentException>(() => new PlateMotion(0, 0, bad));
        }
        Require(BitConverter.SingleToUInt32Bits(PlateBoundaryBuilder.Store(-double.Epsilon)) == 0, "Float underflow canonicalization.");
        Expect<ArgumentException>(() => PlateBoundaryBuilder.Store(double.MaxValue));
        Expect<ArgumentException>(() => PlateBoundaryBuilder.Classify(float.NaN, 0));
        float a = PlateMotionGenerator.Activity;
        Require(PlateBoundaryBuilder.Classify(a, -a) == TectonicInteractionKind.Quiet, "Quiet tie.");
        Require(PlateBoundaryBuilder.Classify(MathF.BitIncrement(a), 0) == TectonicInteractionKind.Divergent, "Adjacent active float.");
        Require(PlateBoundaryBuilder.Classify(-2, 0) == TectonicInteractionKind.Convergent &&
            PlateBoundaryBuilder.Classify(2, 0) == TectonicInteractionKind.Divergent &&
            PlateBoundaryBuilder.Classify(0, 2) == TectonicInteractionKind.Transform, "Pure categories.");
        Require(PlateBoundaryBuilder.Classify(2, 1.5f) == TectonicInteractionKind.Mixed &&
            PlateBoundaryBuilder.Classify(2, MathF.BitDecrement(1.5f)) == TectonicInteractionKind.Divergent &&
            PlateBoundaryBuilder.Classify(2, 2) == TectonicInteractionKind.Mixed, "Mixed/axis ties.");
        var grid = new HexGridData(MapSize.Small);
        int first = grid.GetCellId(new(40, 40)), south = grid.GetCellId(new(40, 41));
        var closing = PlateBoundaryBuilder.Project(grid, first, south, new(0, 90, 2), new(1, 0, 0));
        Require(closing.SignedNormal == -2 && closing.Convergence == 2 && closing.Divergence == 0 && closing.Kind == TectonicInteractionKind.Convergent, "Hand-worked closing projection.");
        var identical = PlateBoundaryBuilder.Project(grid, first, south, new(0, 0, 2), new(1, 0, 2));
        Require(identical.SignedNormal == 0 && identical.SignedTangent == 0 && identical.Kind == TectonicInteractionKind.Quiet, "Identical vectors.");
    }

    internal static void Boundaries()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, words) = GenerateComplete(size, seed);
            foreach (var type in Enum.GetValues<WorldType>())
                Require(words.SequenceEqual(GenerateComplete(size, seed, type).Data), "Complete M output depends on WorldType.");
            var data = TectonicData.FromLayerData(words, grid);
            var expected = new HashSet<(int, int)>();
            for (int cell = 0; cell < grid.CellCount; cell++)
                foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
                    if (cell < next && data.GetPlateId(cell) != data.GetPlateId(next)) expected.Add((cell, next));
            Require(data.BoundaryCount == expected.Count && words.Count == (576 + grid.CellCount) / 4 + 5 * expected.Count, "Complete compact boundary coverage.");
            (int First, int Second) previous = (-1, -1);
            for (int index = 0; index < data.BoundaryCount; index++)
            {
                var edge = data.GetBoundary(index);
                Require(expected.Remove((edge.FirstCellId, edge.SecondCellId)) && edge.FirstCellId < edge.SecondCellId, "Duplicate/missing/non-neighbor boundary.");
                Require(edge.FirstCellId > previous.First || edge.FirstCellId == previous.First && edge.SecondCellId > previous.Second, "Canonical order.");
                previous = (edge.FirstCellId, edge.SecondCellId);
                var from = grid.GetOffset(edge.FirstCellId); var to = grid.GetOffset(edge.SecondCellId);
                int dc = to.Column - from.Column;
                if (dc > grid.Columns / 2) dc -= grid.Columns;
                if (dc < -grid.Columns / 2) dc += grid.Columns;
                double dx = 1.5 * dc, dy = Math.Sqrt(3) * (to.Row - from.Row + .5 * ((to.Column & 1) - (from.Column & 1)));
                double length = Math.Sqrt(dx * dx + dy * dy);
                Require(Math.Abs(edge.Normal.X - dx / length) < 1e-4 && Math.Abs(edge.Normal.Y - dy / length) < 1e-4, "Independent normal, including seam/poles.");
                var v1 = data.GetMotion(edge.FirstPlateId).Velocity; var v2 = data.GetMotion(edge.SecondPlateId).Velocity;
                double n = (v2.X - v1.X) * dx / length + (v2.Y - v1.Y) * dy / length;
                double t = -(v2.X - v1.X) * dy / length + (v2.Y - v1.Y) * dx / length;
                Require(Math.Abs(edge.SignedNormal - n) < .002 && Math.Abs(edge.SignedTangent - t) < .002, "Independent analytic projection.");
                var reverse = PlateBoundaryBuilder.Project(grid, edge.SecondCellId, edge.FirstCellId, data.GetMotion(edge.SecondPlateId), data.GetMotion(edge.FirstPlateId));
                Require(edge.SignedNormal == reverse.SignedNormal && edge.SignedTangent == reverse.SignedTangent && edge.Kind == reverse.Kind, "Reversal invariance.");
            }
            Require(expected.Count == 0, "Omitted boundary edges.");
            for (byte plate = 0; plate < 14; plate++)
            {
                var motion = data.GetMotion(plate);
                Require(motion.PlateId == plate && motion.DirectionDegrees >= 0 && motion.DirectionDegrees < 360 && motion.SpeedCmPerYear >= 1 && motion.SpeedCmPerYear < 8, "M range.");
            }
        }
    }

    internal static void Schema()
    {
        var (grid, packed) = GenerateComplete(MapSize.Small);
        var view = TectonicData.FromLayerData(packed, grid);
        Require(packed[2] == 7 && view.HasMotion && view.HasBoundaries && !view.HasInfluence && view.MotionRevision == 1 && view.InfluenceRevision == 0, "C flags/revisions.");
        uint[] source = packed.ToArray();
        var copy = new LayerData<uint>(GenerationStage.Tectonics, source);
        Array.Fill(source, uint.MaxValue);
        Require(copy.SequenceEqual(packed) && TectonicData.FromLayerData(copy, grid).GetMotion(0) == view.GetMotion(0), "Defensive C copy.");
        foreach (int field in Enumerable.Range(0, 32)) Reject(field, packed[field] ^ uint.MaxValue);
        for (int plate = 0; plate < 14; plate++)
        {
            int start = 32 + plate * 8;
            Reject(start + 4, 0x7FF80000); Reject(start + 6, 0x7FF00000); Reject(start + 6, 0xBFF00000);
            Reject(start + 7, 1);
            RejectDouble(start + 3, 360);
            RejectDouble(start + 3, -0d);
            RejectDouble(start + 5, -0d);
        }
        Reject(2, 15);
        int edgeOffset = checked((int)packed[10]);
        for (int field = 0; field < 5; field++) Reject(edgeOffset + field, uint.MaxValue);
        Reject(edgeOffset + 2, packed[edgeOffset + 2] ^ 1);
        uint[] reordered = packed.ToArray();
        Array.Copy(reordered, edgeOffset, reordered, edgeOffset + 5, 5);
        Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, reordered), grid));
        uint[] missing = packed.Take(packed.Count - 5).ToArray(); missing[11]--; missing[15] -= 5;
        Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, missing), grid));
        Expect<ArgumentOutOfRangeException>(() => view.GetBoundary(-1));
        Expect<ArgumentOutOfRangeException>(() => view.GetBoundary(view.BoundaryCount));
        Expect<ArgumentOutOfRangeException>(() => view.GetMotion(14));
        Expect<InvalidOperationException>(() => TectonicData.FromLayerData(OwnershipOnly(packed), grid).GetMotion(0));
        foreach (var type in new[] { typeof(PlateMotion), typeof(TectonicVector), typeof(TectonicBoundary) })
            Require(type.GetProperties().All(property => property.SetMethod is null), "Mutable kinematic value contract.");
        var runner = Orchestrator(); var context = runner.CreateGeneration(Request(MapSize.Small));
        runner.RunUntil(context, GenerationStage.Tectonics);
        var old = Published(context); var oldView = TectonicData.FromLayerData(old, grid);
        var oldMotion = oldView.GetMotion(0); var oldEdge = oldView.GetBoundary(0);
        runner.RebuildFrom(context, GenerationStage.Tectonics); runner.RunUntil(context, GenerationStage.Tectonics);
        Require(old.SequenceEqual(Published(context)) && oldView.GetMotion(0) == oldMotion && oldView.GetBoundary(0) == oldEdge, "Historical C values changed after rebuild.");
        int attempts = 0;
        var failing = Orchestrator(new TestLayer(GenerationStage.Tectonics, inputs =>
        {
            if (attempts++ > 0) return new TectonicPlatesLayer().Execute(inputs);
            var draft = CompleteDraft(grid, inputs.Request.Seed);
            var first = draft.Boundaries![0];
            draft.Boundaries[0] = new(first.FirstCellId, first.SecondCellId, first.FirstPlateId, first.SecondPlateId,
                first.Normal, float.NaN, first.SignedTangent, first.Kind);
            return LayerExecutionResult.Success(TectonicData.CreateCandidate(grid, draft, TectonicGenerationProfile.Approved));
        }));
        var failed = failing.CreateGeneration(Request(MapSize.Small));
        failing.RunUntil(failed, GenerationStage.Tectonics);
        Require(failed.GetProgress().GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Failed &&
            failed.GetLayerData(GenerationStage.Tectonics) is null && failed.GetLayerData(GenerationStage.Grid) is not null, "Invalid C codec candidate was partially published.");
        failing.RunNext(failed);
        Require(Published(failed).SequenceEqual(packed), "C retry changed the reserved motion stream.");
        void Reject(int index, uint value)
        {
            var bad = packed.ToArray(); bad[index] = value;
            Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, bad), grid));
        }
        void RejectDouble(int index, double value)
        {
            var bad = packed.ToArray(); ulong bits = BitConverter.DoubleToUInt64Bits(value);
            bad[index] = (uint)bits; bad[index + 1] = (uint)(bits >> 32);
            Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, bad), grid));
        }
    }

    private sealed record Vector(string Size, string Seed, string File, string Digest);
    private sealed record Manifest(string Algorithm, uint MotionRevision, uint Flags, Vector[] Vectors);

    internal static void Export(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any()) throw new InvalidOperationException("Never overwrite compatibility evidence.");
        Directory.CreateDirectory(directory);
        var vectors = new List<Vector>();
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            var (_, words) = GenerateComplete(size, seed);
            string file = $"{size}-{seed}.words";
            using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, file)))) foreach (uint word in words) writer.Write(word);
            vectors.Add(new(size.ToString(), seed.ToString(System.Globalization.CultureInfo.InvariantCulture), file, TectonicCalibrationChecks.Digest(words)));
        }
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(new Manifest(PlateMotionGenerator.AlgorithmVersion, 1, 7, vectors.ToArray()), new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Exported 15 complete production M buffers separately from accepted B vectors.");
    }

    internal static void Frozen()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "motion-vectors");
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")))!;
        Require(manifest.Algorithm == PlateMotionGenerator.AlgorithmVersion && manifest.MotionRevision == 1 && manifest.Flags == 7 && manifest.Vectors.Length == 15, "Frozen M identity.");
        foreach (var row in manifest.Vectors)
        {
            var (_, actual) = GenerateComplete(Enum.Parse<MapSize>(row.Size), long.Parse(row.Seed, System.Globalization.CultureInfo.InvariantCulture));
            using var stream = File.OpenRead(Path.Combine(directory, row.File)); using var reader = new BinaryReader(stream);
            Require(stream.Length == actual.Count * 4L, "M vector length.");
            for (int word = 0; word < actual.Count; word++) Require(reader.ReadUInt32() == actual[word], "Full M vector mismatch.");
            Require(TectonicCalibrationChecks.Digest(actual) == row.Digest, "M digest mismatch.");
        }
        Console.WriteLine("FROZEN M: 15 complete flags-7 production buffers matched word for word.");
    }

    internal static void Performance()
    {
        Console.WriteLine($"ENV {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}; CPUs={Environment.ProcessorCount}; {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}");
        Console.WriteLine($"METHOD M-v1 + frozen R-v2; Release; tiering={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation")}; one warmup/size; 3 batches/seed; sequential; times exclude output, independent checks and GC; per-thread allocations, not peak heap.");
        Console.WriteLine("Size,Seed,Batch,PartitionMs,MotionBoundaryMs,CodecMs,ViewMs,BuildAllocated,CodecAllocated,ViewAllocated,PayloadBytes,RetainedDelta,BoundaryCount");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size);
            var warm = CompleteDraft(grid, 0); _ = TectonicData.FromLayerData(TectonicData.CreateCandidate(grid, warm, TectonicGenerationProfile.Approved), grid);
            foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
            {
                var baseline = GenerateComplete(size, seed).Data;
                for (int batch = 1; batch <= 3; batch++)
                {
                    long beforeHeap = GC.GetTotalMemory(true), allocated = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    var draft = CompleteDraft(grid, seed);
                    double buildMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long buildBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    allocated = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var packed = TectonicData.CreateCandidate(grid, draft, TectonicGenerationProfile.Approved);
                    double codecMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long codecBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    allocated = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var view = TectonicData.FromLayerData(packed, grid);
                    double viewMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long viewBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    long retained = GC.GetTotalMemory(true) - beforeHeap;
                    Require(baseline.SequenceEqual(packed), "Measured build differs from full real orchestrator output.");
                    double partition = draft.Metrics.SeedMilliseconds + draft.Metrics.NoiseMilliseconds + draft.Metrics.GrowthMilliseconds;
                    Console.WriteLine(FormattableString.Invariant($"{size},{seed},{batch},{partition:F6},{buildMs - partition:F6},{codecMs:F6},{viewMs:F6},{buildBytes},{codecBytes},{viewBytes},{packed.Count * 4},{retained},{view.BoundaryCount}"));
                    GC.KeepAlive(draft); GC.KeepAlive(packed); GC.KeepAlive(view);
                }
            }
        }
    }

    private static TectonicDraft CompleteDraft(HexGridData grid, long seed)
    {
        var random = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
        ulong partition = random.NextUInt64(), noise = random.NextUInt64(), motion = random.NextUInt64();
        var draft = TectonicPartitionGenerator.Generate(grid, new(partition), new(noise), TectonicGenerationProfile.Approved);
        var motions = PlateMotionGenerator.Generate(new(motion));
        return draft with { Motions = motions, Boundaries = PlateBoundaryBuilder.Build(grid, draft.Owners, motions) };
    }
}
