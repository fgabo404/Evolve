// Measures the historical Mantle candidate by isolated phases, keeping full correctness oracles separate.
// Records allocations and signed retained GC deltas without claiming a simultaneous process memory peak.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleFinalPerformance
{
    private readonly record struct Timing(double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Row(string Size, long Seed, int Batch, bool Warmup, string Phase,
        double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);

    internal static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new ArgumentException("Use a new empty evidence directory.");
        Directory.CreateDirectory(directory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(directory, "environment.json"), JsonSerializer.Serialize(new
        {
            Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(), ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), Environment.ProcessorCount, Stopwatch.Frequency,
            Profile = MantleHistoricalProfile.Identity, Parameters = MantleHistoricalProfile.Parameters,
            WarmupBatchesPerCase = 1, MeasuredBatchesPerCase = 3, OperationsPerPhasePerBatch = 1,
            Seeds = new[] { 0L, -1L, long.MinValue, long.MaxValue },
            FlowTupleStride = System.Runtime.CompilerServices.Unsafe.SizeOf<(float, float, float)>(),
            CellValueStride = System.Runtime.CompilerServices.Unsafe.SizeOf<MantleCell>(),
            Notes = "Single-threaded Release. Input R/M/Influence preparation, delegates and forced GC outside timers. Independent oracle includes its own harness allocations. Signed retained deltas are not simultaneous peak/live-set measurements; MEM-01 remains OPEN. No dedicated-machine guarantee or hardware-independent thresholds."
        }, options));
        object token = new(); _ = Measure(() => token);
        File.WriteAllText(Path.Combine(directory, "control.json"), JsonSerializer.Serialize(Measure(() => token).Timing, options));
        var rows = new List<Row>();
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var influence = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var request = Request(size, seed); var p = MantleHistoricalProfile.Parameters;
            string? expectedHash = null;
            for (int batch = 0; batch <= 3; batch++)
            {
                var signals = Measure(() => MantleNoiseFields.Build(grid, request, p));
                var potential = Measure(() => MantlePotentialField.Combine(grid, influence, signals.Value, p));
                var edges = Measure(() => MantleConvectionField.Build(grid, potential.Value));
                var validation = Measure(() => { edges.Value.Validate(grid); return edges.Value; });
                var oracle = Measure(() => MantleFinalChecks.Verify(grid, request, influence, signals.Value, edges.Value));
                var serialization = Measure(() => MantleCandidateVectors.Serialize(edges.Value));
                var hash = Measure(() => MantleCandidateVectors.Hash(serialization.Value));
                if (expectedHash is not null) Require(expectedHash == hash.Value, "Measured complete Mantle replay differs.");
                expectedHash = hash.Value;
                Add("Signals", signals.Timing); Add("Potential", potential.Timing); Add("Edges", edges.Timing);
                Add("StructuralValidation", validation.Timing); Add("IndependentFullOracle", oracle.Timing);
                Add("Serialization", serialization.Timing); Add("Hash", hash.Timing);
                void Add(string phase, Timing value) => rows.Add(new(size.ToString(), seed, batch, batch == 0, phase,
                    value.Milliseconds, value.AllocatedBytes, value.RetainedGcDeltaBytes));
            }
            Console.WriteLine($"MANTLE FINAL PERF {size}/{seed}: warmup + 3 samples; complete independent oracle/replay passed.");
        }
        WriteCsv(Path.Combine(directory, "timings.csv"), rows);
        WriteCsv(Path.Combine(directory, "summary.csv"), rows.Where(row => !row.Warmup).GroupBy(row => (row.Size, row.Seed, row.Phase)).Select(group => new
        {
            group.Key.Size, group.Key.Seed, group.Key.Phase, Samples = group.Count(),
            MinimumMs = group.Min(row => row.Milliseconds), MedianMs = Median(group.Select(row => row.Milliseconds)), MaximumMs = group.Max(row => row.Milliseconds),
            MeanAllocatedBytes = group.Average(row => row.AllocatedBytes),
            MinimumRetainedGcDeltaBytes = group.Min(row => row.RetainedGcDeltaBytes), MaximumRetainedGcDeltaBytes = group.Max(row => row.RetainedGcDeltaBytes)
        }));
        Console.WriteLine($"MANTLE FINAL PERF: {rows.Count} raw rows; 12 cases; 7 phases. MEM-01 remains OPEN.");
    }

    // Timer and allocation counters cover only the supplied operation; forced GC and delegates are outside.
    private static (T Value, Timing Timing) Measure<T>(Func<T> operation) where T : class
    {
        long before = GC.GetTotalMemory(true), allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
        T result = operation();
        long end = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long after = GC.GetTotalMemory(true); GC.KeepAlive(result);
        return (result, new((end - start) * 1000d / Stopwatch.Frequency, bytes, after - before));
    }
    private static double Median(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        return sorted.Length % 2 == 0 ? sorted[sorted.Length / 2 - 1] / 2 + sorted[sorted.Length / 2] / 2 : sorted[sorted.Length / 2];
    }
    private static void WriteCsv<T>(string path, IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties(); using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(",", properties.Select(property => property.Name)));
        foreach (var row in rows) writer.WriteLine(string.Join(",", properties.Select(property => "\"" +
            Convert.ToString(property.GetValue(row), CultureInfo.InvariantCulture)!.Replace("\"", "\"\"") + "\"")));
    }
}
