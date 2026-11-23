// Measures the selected internal profile, complete production recomputation and independent full-map oracle.
// Keeps warmup, analysis and serialization outside recorded operations and retains raw per-case batches.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceFinalPerformance
{
    private readonly record struct Timing(double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Row(string Size, int CellCount, long Seed, int Batch, bool Warmup, string Phase,
        string Profile, uint Revision, int ActiveSources, float Radius, float Cutoff, int Reach, long Visits,
        int AttenuationTableEvaluations, double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);

    internal static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any()) throw new ArgumentException("Use a new empty evidence directory.");
        Directory.CreateDirectory(directory);
        var rows = new List<Row>();
        var environment = new
        {
            Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(), ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), Environment.ProcessorCount, Stopwatch.Frequency,
            TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
            WarmupBatchesPerCase = 1, MeasuredBatchesPerCase = 3, OperationsPerPhasePerBatch = 1,
            Seeds = new[] { 0L, -1L, long.MaxValue }, Profile = TectonicInfluenceProfile.Identifier, Revision = TectonicInfluenceProfile.Revision,
            Notes = "Single-threaded Release; GC before/after outside timers; reference M decoding and independent source extraction outside measured phases; no dedicated-machine guarantee; retained deltas are signed GC observations, not peak heap."
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(directory, "environment.json"), JsonSerializer.Serialize(environment, options));
        object token = new();
        _ = Measure(() => token);
        File.WriteAllText(Path.Combine(directory, "control.json"), JsonSerializer.Serialize(Measure(() => token).Timing, options));
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var data = TectonicData.FromLayerData(packed, grid);
            var reference = TectonicInfluenceOracle.Extract(grid, packed);
            var parameters = TectonicInfluenceProfile.Resolve(grid);
            string? expectedHash = null;
            for (int batch = 0; batch <= 3; batch++)
            {
                var sources = Measure(() => TectonicInfluenceSources.Extract(grid, data));
                var distances = Measure(() => TectonicBoundaryDistances.Build(sources.Value));
                var fields = Measure(() => TectonicInfluenceFields.Build(sources.Value, parameters));
                var structural = Measure(() => { fields.Value.Validate(sources.Value, distances.Value); return fields.Value; });
                var complete = Measure(() => { fields.Value.ValidateComplete(sources.Value, distances.Value); return fields.Value; });
                var oracle = Measure(() =>
                {
                    var result = TectonicInfluenceOracle.Compute(reference, parameters);
                    TectonicInfluenceOracle.Compare(result, fields.Value, distances.Value);
                    return result;
                });
                TectonicInfluenceOracle.CheckSources(reference, sources.Value);
                string hash = TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(fields.Value, distances.Value));
                if (expectedHash is not null && hash != expectedHash) throw new InvalidOperationException("Measured full-map replay differs.");
                expectedHash = hash;
                Add("Sources", sources.Timing, grid.CellCount);
                Add("BoundaryDistance", distances.Timing, grid.CellCount);
                Add("Gaussian", fields.Timing, fields.Value.VisitedCells);
                Add("StructuralValidation", structural.Timing, grid.CellCount);
                Add("RecomputedValidation", complete.Timing, fields.Value.VisitedCells);
                Add("IndependentOracleValidation", oracle.Timing, oracle.Value.TargetVisits);

                void Add(string phase, Timing timing, long visits) => rows.Add(new(size.ToString(), grid.CellCount, seed, batch, batch == 0,
                    phase, TectonicInfluenceProfile.Identifier, TectonicInfluenceProfile.Revision, sources.Value.ActiveCount,
                    parameters.Radius, parameters.Cutoff, parameters.GetReach(grid), visits,
                    phase is "Gaussian" or "RecomputedValidation" ? fields.Value.AttenuationEvaluations : 0,
                    timing.Milliseconds, timing.AllocatedBytes, timing.RetainedGcDeltaBytes));
            }
            Console.WriteLine($"PERF-01 {size}/{seed}: warmup + 3 batches; all complete oracle/replay comparisons passed.");
        }
        WriteCsv(Path.Combine(directory, "timings.csv"), rows);
        var summary = rows.Where(row => !row.Warmup).GroupBy(row => (row.Size, row.Seed, row.Phase)).Select(group => new
        {
            group.Key.Size, group.Key.Seed, group.Key.Phase, Samples = group.Count(),
            MinimumMs = group.Min(row => row.Milliseconds), MedianMs = Median(group.Select(row => row.Milliseconds)), MaximumMs = group.Max(row => row.Milliseconds),
            MeanAllocatedBytes = group.Average(row => row.AllocatedBytes), MinimumRetainedGcDeltaBytes = group.Min(row => row.RetainedGcDeltaBytes),
            MaximumRetainedGcDeltaBytes = group.Max(row => row.RetainedGcDeltaBytes)
        });
        WriteCsv(Path.Combine(directory, "summary.csv"), summary);
        Console.WriteLine($"PERF-01 complete: {rows.Count} raw rows, 9 size/seed cases; no performance budget or profile acceptance inferred.");
    }

    private static (T Value, Timing Timing) Measure<T>(Func<T> operation) where T : class
    {
        long before = GC.GetTotalMemory(true), allocated = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        T result = operation();
        long end = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long after = GC.GetTotalMemory(true);
        GC.KeepAlive(result);
        return (result, new((end - start) * 1000d / Stopwatch.Frequency, bytes, after - before));
    }

    private static double Median(IEnumerable<double> values)
    {
        double[] sorted = values.Order().ToArray();
        return sorted.Length % 2 == 0 ? sorted[sorted.Length / 2 - 1] / 2 + sorted[sorted.Length / 2] / 2 : sorted[sorted.Length / 2];
    }

    private static void WriteCsv<T>(string path, IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties();
        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(",", properties.Select(property => property.Name)));
        foreach (var row in rows)
            writer.WriteLine(string.Join(",", properties.Select(property => "\"" + Convert.ToString(property.GetValue(row), CultureInfo.InvariantCulture)!.Replace("\"", "\"\"") + "\"")));
    }
}
