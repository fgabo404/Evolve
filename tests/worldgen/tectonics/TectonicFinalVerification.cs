// Adds independent final numeric oracles and separated production phase/query measurements for WG-01-D.
// Uses the existing restricted test grant; no production instrumentation, profile changes or alternate execution path is introduced.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicFinalVerification
{
    private static long querySink;

    internal static void CheckTimingStatistics()
    {
        Require(GraphicsTimingStatistics.MedianOfSortedSamples([1, 3, 5]) == 3, "Odd sample count.");
        Require(GraphicsTimingStatistics.MedianOfSortedSamples([1, 3, 5, 7]) == 4, "Even sample count.");
        Require(GraphicsTimingStatistics.MedianOfSortedSamples(Enumerable.Range(1, 30).Select(value => (double)value).ToArray()) == 15.5,
            "Thirty samples must average indices 14 and 15, not select index 15.");
        Require(GraphicsTimingStatistics.MedianOfSortedSamples([2, 2, 2, 2]) == 2 &&
            GraphicsTimingStatistics.MedianOfSortedSamples([1, 2, 2, 7, 9]) == 2, "Repeated values.");
        Require(GraphicsTimingStatistics.MedianOfSortedSamples([double.MaxValue]) == double.MaxValue &&
            GraphicsTimingStatistics.MedianOfSortedSamples([double.MaxValue, double.MaxValue]) == double.MaxValue &&
            GraphicsTimingStatistics.MedianOfSortedSamples([-double.MaxValue, -double.MaxValue]) == -double.MaxValue,
            "Finite extremes must not overflow the median.");
        Require(GraphicsTimingStatistics.MedianOfSortedSamples([-double.MaxValue, double.MaxValue]) == 0 &&
            GraphicsTimingStatistics.MedianOfSortedSamples([double.Epsilon, 2 * double.Epsilon]) == 2 * double.Epsilon,
            "Opposite signs and subnormal halfway rounding.");
        Expect<ArgumentException>(() => GraphicsTimingStatistics.MedianOfSortedSamples([]));
        Expect<ArgumentException>(() => GraphicsTimingStatistics.MedianOfSortedSamples([2, 1]));
        Expect<ArgumentException>(() => GraphicsTimingStatistics.MedianOfSortedSamples([double.NaN]));
        Expect<ArgumentException>(() => GraphicsTimingStatistics.MedianOfSortedSamples([double.PositiveInfinity]));
        Console.WriteLine("TIMING MEDIAN: odd/even/30 samples, repeats, finite extremes, subnormals and invalid inputs passed.");
    }

    internal static void Check()
    {
        float a = BitConverter.UInt32BitsToSingle(0x3E19999A);
        float[] values = [0, -0f, a, -a, MathF.BitIncrement(a), MathF.BitDecrement(a), -MathF.BitIncrement(a),
            -MathF.BitDecrement(a), 1, -1, .75f, -.75f, MathF.BitIncrement(.75f), MathF.BitDecrement(.75f),
            float.Epsilon, -float.Epsilon, float.MaxValue, -float.MaxValue];
        foreach (float normal in values)
        foreach (float tangent in values)
            Require(PlateBoundaryBuilder.Classify(normal, tangent) == Oracle(normal, tangent), "Independent Boolean category oracle differs.");
        // Exact binary midpoints distinguish ties-to-even from truncation or decimal rounding.
        float odd = MathF.BitIncrement(1f), even = MathF.BitIncrement(odd);
        Require(PlateBoundaryBuilder.Store((1d + odd) / 2) == 1f &&
            PlateBoundaryBuilder.Store(((double)odd + even) / 2) == even, "Positive halfway rounding.");
        Require(PlateBoundaryBuilder.Store(-(1d + odd) / 2) == -1f &&
            PlateBoundaryBuilder.Store(-((double)odd + even) / 2) == -even, "Negative halfway rounding.");
        long boundaries = 0;
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, words) = GenerateComplete(size, seed);
            Require(words.GetType() == typeof(LayerData<uint>) && words[2] == 7 && words[5] == 14 &&
                words[16] == 2 && words[17] == 1 && words[18] == 0 && words[13] == 0 && words[14] == 0, "Single publication/features/revisions.");
            var data = TectonicData.FromLayerData(words, grid);
            int[] counts = new int[5];
            for (int edge = 0; edge < data.BoundaryCount; edge++)
            {
                var value = data.GetBoundary(edge);
                Require(value.Kind == Oracle(value.SignedNormal, value.SignedTangent), "Published category differs from independent predicates.");
                counts[(int)value.Kind]++;
            }
            boundaries += data.BoundaryCount;
            Console.WriteLine($"D_CLASSES {size} seed={seed} boundaries={data.BoundaryCount} quiet/convergent/divergent/transform/mixed={string.Join('/', counts)}");
        }
        Console.WriteLine($"D_ORACLE: {values.Length * values.Length} signed/extreme component pairs; exact midpoint rounding; {boundaries} published interfaces.");
    }

    // Independent mutually exclusive predicates use 4*minimum >= 3*maximum instead of the production M multiplication.
    private static TectonicInteractionKind Oracle(float normal, float tangent)
    {
        double n = normal, t = tangent, threshold = BitConverter.UInt32BitsToSingle(0x3E19999A);
        bool quiet = n >= -threshold && n <= threshold && t >= -threshold && t <= threshold;
        double axial = Math.Abs(n), shear = Math.Abs(t), low = Math.Min(axial, shear), high = Math.Max(axial, shear);
        bool mixed = !quiet && low >= threshold && 4d * low >= 3d * high;
        bool transform = !quiet && !mixed && shear > axial;
        bool convergent = !quiet && !mixed && !transform && n < 0;
        bool divergent = !quiet && !mixed && !transform && n >= 0;
        Require(new[] { quiet, mixed, transform, convergent, divergent }.Count(value => value) == 1, "Oracle predicates do not partition the domain.");
        return quiet ? TectonicInteractionKind.Quiet : mixed ? TectonicInteractionKind.Mixed : transform ?
            TectonicInteractionKind.Transform : convergent ? TectonicInteractionKind.Convergent : TectonicInteractionKind.Divergent;
    }

    internal static void Measure()
    {
        Console.WriteLine($"ENV {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}; CPUs={Environment.ProcessorCount}; {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}");
        Console.WriteLine($"METHOD Release; tiering={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "runtime default"}; 1 full warmup/size; 3 batches for seeds 0,1,-1,long.MinValue,long.MaxValue; serialized; no simultaneous builds/checks; elapsed includes actual calls, excludes assertions/output/forced GC.");
        Console.WriteLine("Size,Seed,Batch,Phase,Operations,Milliseconds,NanosecondsPerOperation,AllocatedBytes,AllocatedBytesPerOperation,RetainedDeltaBytes,PayloadBytes,BoundaryCount");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size);
            var warm = Build(grid, 0);
            var warmPacked = TectonicData.CreateCandidate(grid, warm, TectonicGenerationProfile.Approved);
            var warmView = TectonicData.FromLayerData(warmPacked, grid);
            _ = new LayerData<uint>(GenerationStage.Tectonics, warmPacked);
            Queries(warmView, 2000, false); Queries(warmView, 2000, true);
            foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
            {
                var baseline = GenerateComplete(size, seed).Data;
                for (int batch = 1; batch <= 3; batch++)
                {
                    long beforeHeap = GC.GetTotalMemory(true);
                    var stage = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
                    ulong partition = stage.NextUInt64(), noise = stage.NextUInt64(), motion = stage.NextUInt64();
                    var samples = new List<(string Phase, double Ms, long Bytes)>();
                    long allocation = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                    var draft = TectonicPartitionGenerator.Generate(grid, new(partition), new(noise), TectonicGenerationProfile.Approved);
                    samples.Add(End("Regions", start, allocation));
                    allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var motions = PlateMotionGenerator.Generate(new(motion));
                    samples.Add(End("Motions", start, allocation));
                    allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var edges = PlateBoundaryBuilder.Build(grid, draft.Owners, motions);
                    samples.Add(End("Boundaries", start, allocation));
                    var complete = draft with { Motions = motions, Boundaries = edges };
                    allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var packed = TectonicData.CreateCandidate(grid, complete, TectonicGenerationProfile.Approved);
                    samples.Add(End("ValidatePackCopy", start, allocation));
                    allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var view = TectonicData.FromLayerData(packed, grid);
                    samples.Add(End("ValidateView", start, allocation));
                    long retained = GC.GetTotalMemory(true) - beforeHeap;
                    // Match production's known-array constructor path; preparing this diagnostic source is outside timing/retained-core delta.
                    uint[] copySource = packed.ToArray();
                    allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                    var copied = new LayerData<uint>(GenerationStage.Tectonics, copySource);
                    samples.Add(End("DefensiveCopyStandalone", start, allocation));
                    Require(packed.SequenceEqual(baseline) && copied.SequenceEqual(baseline) && !ReferenceEquals(copied, packed), "Measured payload/copy mismatch.");
                    foreach (var sample in samples) Row(sample.Phase, 1, sample.Ms, sample.Bytes);
                    foreach (bool boundary in new[] { false, true })
                    {
                        const int operations = 200000;
                        allocation = GC.GetAllocatedBytesForCurrentThread(); start = Stopwatch.GetTimestamp();
                        Queries(view, operations, boundary);
                        var query = End(boundary ? "BoundaryQuery" : "OwnerQuery", start, allocation);
                        Row(query.Phase, operations, query.Ms, query.Bytes);
                    }
                    GC.KeepAlive(complete); GC.KeepAlive(packed); GC.KeepAlive(copied); GC.KeepAlive(view);

                    void Row(string phase, int operations, double ms, long bytes) => Console.WriteLine(FormattableString.Invariant(
                        $"{size},{seed},{batch},{phase},{operations},{ms:F6},{ms * 1_000_000 / operations:F6},{bytes},{(double)bytes / operations:F6},{retained},{packed.Count * 4},{view.BoundaryCount}"));
                }
            }
        }
        Console.WriteLine($"SINK {Volatile.Read(ref querySink)}");
    }

    private static (string Phase, double Ms, long Bytes) End(string phase, long start, long allocation)
    {
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        return (phase, elapsed, GC.GetAllocatedBytesForCurrentThread() - allocation);
    }

    // Fixed strides and explicit polar/seam/interior IDs avoid RNG and per-query harness allocation.
    private static void Queries(TectonicData view, int operations, bool boundaries)
    {
        double sum = 0;
        for (int operation = 0; operation < operations; operation++)
        {
            if (boundaries)
            {
                var edge = view.GetBoundary((int)((long)operation * 7919 % view.BoundaryCount));
                sum += edge.FirstCellId + edge.SecondCellId + edge.Normal.X + edge.SignedTangent + (int)edge.Kind;
            }
            else
            {
                int cell = (operation % 8) switch
                {
                    0 => 0, 1 => view.CellCount - 1, 2 => view.CellCount / 2,
                    _ => (int)((long)operation * 7919 % view.CellCount)
                };
                sum += view.GetPlateId(cell);
            }
        }
        Volatile.Write(ref querySink, BitConverter.DoubleToInt64Bits(sum));
    }

    private static TectonicDraft Build(HexGridData grid, long seed)
    {
        var stage = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
        ulong partition = stage.NextUInt64(), noise = stage.NextUInt64(), motion = stage.NextUInt64();
        var draft = TectonicPartitionGenerator.Generate(grid, new(partition), new(noise), TectonicGenerationProfile.Approved);
        var motions = PlateMotionGenerator.Generate(new(motion));
        return draft with { Motions = motions, Boundaries = PlateBoundaryBuilder.Build(grid, draft.Owners, motions) };
    }
}
