// Measures candidate deformation profiles outside normal generation and records reproducible raw evidence.
// Separates measured phases from warmup, field analysis, hashing and serialization overhead.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceCalibration
{
    private readonly record struct Candidate(string Name, float Minimum, float Fraction, float Cutoff);
    private readonly record struct Timing(double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private readonly record struct PhaseRow(string Size, long Seed, string Candidate, string Phase, int Batch, bool Warmup,
        float Radius, float Cutoff, int Reach, int ActiveSources, long VisitedCells, int AttenuationEvaluations,
        long ChannelProducts, double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private readonly record struct FieldRow(string Size, long Seed, string Candidate, int Batch, string Channel, string Hash,
        float Maximum, double Mean, float P50, float P90, float P99, double NonzeroPercent, double OnePercentCoverage,
        double TenPercentCoverage, double OverlapPercent, double SeamMean, double PolarMean, double InteriorMean);
    private static readonly Candidate[] Candidates =
    [
        new("NARROW", 1, 0.01f, 3), new("HISTORICAL", 1, 0.02f, 3), new("WIDE", 1, 0.04f, 3),
        new("SHORT-CUTOFF", 1, 0.02f, 2), new("LONG-CUTOFF", 1, 0.02f, 4)
    ];

    internal static void Run(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
            throw new ArgumentException("Use an empty output directory; raw calibration evidence is never overwritten.");
        Directory.CreateDirectory(outputDirectory);
        var phases = new List<PhaseRow>();
        var fields = new List<FieldRow>();
        var environment = new
        {
            Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture, RuntimeInformation.ProcessArchitecture, Environment.ProcessorCount,
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), Stopwatch.Frequency,
            TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
            WarmupsPerCase = 1, MeasuredBatchesPerCase = 3, IterationsPerBatch = 1,
            Seeds = new[] { 0L, -1L, long.MaxValue }, Candidates,
            Notes = "Single process/thread; explicit full-GC outside timers. R/M generation and decoding excluded. Retained GC deltas are noisy, signed managed-heap observations, not peak working set."
        };
        File.WriteAllText(Path.Combine(outputDirectory, "environment.json"), JsonSerializer.Serialize(environment,
            new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
        // Record a cold empty-operation diagnostic, including first-call JIT; do not subtract it from warm timings.
        object token = new();
        var control = Measure(() => token).Timing;
        File.WriteAllText(Path.Combine(outputDirectory, "control.json"), JsonSerializer.Serialize(control));

        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var data = TectonicData.FromLayerData(packed, grid);
            foreach (var candidate in Candidates)
            {
                var parameters = TectonicInfluenceParameters.Resolve(grid, candidate.Minimum, candidate.Fraction, candidate.Cutoff);
                string? expectedHash = null;
                for (int batch = 0; batch <= 3; batch++)
                {
                    var sourceRun = Measure(() => TectonicInfluenceSources.Extract(grid, data));
                    var distanceRun = Measure(() => TectonicBoundaryDistances.Build(sourceRun.Value));
                    var fieldRun = Measure(() => TectonicInfluenceFields.Build(sourceRun.Value, parameters));
                    var validationRun = Measure(() => { fieldRun.Value.Validate(sourceRun.Value, distanceRun.Value); return fieldRun.Value; });
                    string hash = Hash(fieldRun.Value, distanceRun.Value);
                    if (expectedHash is not null && hash != expectedHash) throw new InvalidOperationException("Calibration replay mismatch.");
                    expectedHash = hash;
                    Add("Sources", sourceRun.Timing);
                    Add("BoundaryDistance", distanceRun.Timing);
                    Add("Gaussian", fieldRun.Timing);
                    Add("StructuralValidation", validationRun.Timing);
                    if (batch > 0) Analyze(fields, size, seed, candidate.Name, batch, sourceRun.Value, fieldRun.Value, distanceRun.Value, hash);

                    void Add(string phase, Timing timing) => phases.Add(new(size.ToString(), seed, candidate.Name, phase, batch, batch == 0,
                        parameters.Radius, parameters.Cutoff, parameters.GetReach(grid), sourceRun.Value.ActiveCount,
                        fieldRun.Value.VisitedCells, fieldRun.Value.AttenuationEvaluations, checked(3 * fieldRun.Value.VisitedCells),
                        timing.Milliseconds, timing.AllocatedBytes, timing.RetainedGcDeltaBytes));
                }
                var measured = phases.Where(row => row.Size == size.ToString() && row.Seed == seed && row.Candidate == candidate.Name && !row.Warmup && row.Phase == "Gaussian").ToArray();
                Console.WriteLine(FormattableString.Invariant($"CAL01 {size}/{seed}/{candidate.Name}: radius={parameters.Radius:R}, reach={parameters.GetReach(grid)}, Gaussian={measured.Average(row => row.Milliseconds):F3} ms, visits={measured[0].VisitedCells}, allocated={measured[0].AllocatedBytes} B"));
            }
        }
        WriteCsv(Path.Combine(outputDirectory, "timings.csv"), phases);
        WriteCsv(Path.Combine(outputDirectory, "fields.csv"), fields);
        var summary = phases.Where(row => !row.Warmup).GroupBy(row => (row.Size, row.Candidate, row.Phase)).Select(group => new
        {
            group.Key.Size, group.Key.Candidate, group.Key.Phase, Samples = group.Count(),
            MinimumMs = group.Min(row => row.Milliseconds), MedianMs = Median(group.Select(row => row.Milliseconds)), MaximumMs = group.Max(row => row.Milliseconds),
            MeanAllocatedBytes = group.Average(row => row.AllocatedBytes), MinimumRetainedGcDeltaBytes = group.Min(row => row.RetainedGcDeltaBytes),
            MaximumRetainedGcDeltaBytes = group.Max(row => row.RetainedGcDeltaBytes)
        }).ToArray();
        WriteCsv(Path.Combine(outputDirectory, "summary.csv"), summary);
        Console.WriteLine($"Calibration complete: {phases.Count} raw phase rows (warmup separated), {fields.Count} distribution rows. No profile selected or frozen.");
    }

    private static (T Value, Timing Timing) Measure<T>(Func<T> operation) where T : class
    {
        long before = GC.GetTotalMemory(true);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        T value = operation();
        long end = Stopwatch.GetTimestamp();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long after = GC.GetTotalMemory(true);
        GC.KeepAlive(value);
        return (value, new((end - start) * 1000d / Stopwatch.Frequency, bytes, after - before));
    }

    private static void Analyze(List<FieldRow> output, MapSize size, long seed, string candidate, int batch,
        TectonicInfluenceSources sources, TectonicInfluenceFields fields, TectonicBoundaryDistances distances, string hash)
    {
        var grid = sources.Grid;
        double[] maxima = [sources.Maximum.Compression, sources.Maximum.Extension, sources.Maximum.Shear];
        int overlap = Enumerable.Range(0, fields.Count).Count(cell => Enumerable.Range(0, 3)
            .Count(channel => maxima[channel] > 0 && Channel(fields[cell], channel) >= maxima[channel] * 0.01) >= 2);
        for (int channel = 0; channel < 3; channel++)
        {
            float[] values = Enumerable.Range(0, fields.Count).Select(cell => Channel(fields[cell], channel)).ToArray();
            var ordered = values.Order().ToArray();
            var seam = Enumerable.Range(0, fields.Count).Where(cell => cell % grid.Columns is 0 || cell % grid.Columns == grid.Columns - 1).ToArray();
            var polar = Enumerable.Range(0, fields.Count).Where(cell => cell < grid.Columns || cell >= fields.Count - grid.Columns).ToArray();
            var interior = Enumerable.Range(0, fields.Count).Where(cell => distances[cell] >= 2 * fields.Parameters.Radius).ToArray();
            double Coverage(double fraction) => maxima[channel] == 0 ? 0 : 100d * values.Count(value => value >= maxima[channel] * fraction) / fields.Count;
            double Mean(int[] cells) => cells.Length == 0 ? 0 : cells.Average(cell => (double)values[cell]);
            output.Add(new(size.ToString(), seed, candidate, batch, new[] { "Compression", "Extension", "Shear" }[channel], hash,
                values.Max(), values.Average(value => (double)value), ordered[(int)(0.5 * (ordered.Length - 1))],
                ordered[(int)(0.9 * (ordered.Length - 1))], ordered[(int)(0.99 * (ordered.Length - 1))],
                100d * values.Count(value => value > 0) / fields.Count, Coverage(0.01), Coverage(0.1), 100d * overlap / fields.Count,
                Mean(seam), Mean(polar), Mean(interior)));
        }
    }

    private static float Channel(TectonicInfluenceChannels value, int channel) => channel switch
        { 0 => value.Compression, 1 => value.Extension, _ => value.Shear };

    private static string Hash(TectonicInfluenceFields fields, TectonicBoundaryDistances distances)
    {
        byte[] bytes = new byte[checked(fields.Count * 16)];
        for (int cell = 0; cell < fields.Count; cell++)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell * 16), distances[cell]);
            for (int channel = 0; channel < 3; channel++)
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell * 16 + 4 + channel * 4), BitConverter.SingleToUInt32Bits(Channel(fields[cell], channel)));
        }
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static double Median(IEnumerable<double> samples)
    {
        var sorted = samples.Order().ToArray();
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
