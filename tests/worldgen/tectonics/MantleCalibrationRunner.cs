// Measures provisional Model A candidates and preserves raw phase, distribution and conservation evidence.
// Runs separately from regressions; warmup, analysis, hashing and output are outside measured operations.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Buffers.Binary;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleCalibrationRunner
{
    private readonly record struct Timing(double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Phase(string Size, long Seed, string Candidate, int Batch, bool Warmup, string Operation,
        int Cells, int Edges, double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Distribution(string Size, long Seed, string Candidate, int Batch, string Channel,
        double Minimum, double Maximum, double Mean, double Rms, float P01, float P50, float P99);
    private sealed record CaseResult(string Size, long Seed, string Candidate, int Batch, string Hash, int Cells, int Edges,
        double DoubleBalance, double DoubleBudget, double FloatBalance, double FloatBudget, double RoundingUpper,
        double UpPercent, double DownPercent, int InteriorCells, double InteriorPotentialRms, double TectonicCorrelation);

    internal static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any()) throw new ArgumentException("Use an empty evidence directory.");
        Directory.CreateDirectory(directory);
        var phases = new List<Phase>(); var distributions = new List<Distribution>(); var results = new List<CaseResult>();
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(directory, "environment.json"), JsonSerializer.Serialize(new
        {
            Framework = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(), ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), Environment.ProcessorCount,
            Stopwatch.Frequency, TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
            WarmupBatches = 1, MeasuredBatches = 3, IterationsPerPhase = 1,
            Seeds = new[] { 0L, -1L, long.MinValue, long.MaxValue },
            SeedDomain = "ASCII WG01MNT1 little-endian 0x31544E4D31304757; fresh stage-derived stream, broad then regional",
            Candidates = MantleCalibrationChecks.Candidates,
            Notes = "Experimental Model A only; no profile selected. Single thread, GC outside timers. Earlier R/M/Influence construction excluded. Retained deltas are noisy signed GC observations, not peak memory."
        }, options));
        object token = new(); Func<object> control = () => token;
        _ = Measure(control);
        File.WriteAllText(Path.Combine(directory, "control.json"), JsonSerializer.Serialize(Measure(control).Timing, options));
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            uint[] original = packed.ToArray();
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var influence = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources);
            string originalInfluence = TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances));
            foreach (var candidate in MantleCalibrationChecks.Candidates)
            {
                string? expectedHash = null;
                for (int batch = 0; batch <= 3; batch++)
                {
                    var noise = Measure(() => MantleNoiseFields.Build(grid, Request(size, seed), candidate.Parameters));
                    var potential = Measure(() => MantlePotentialField.Combine(grid, influence, noise.Value, candidate.Parameters));
                    var flow = Measure(() => MantleConvectionField.Build(grid, potential.Value));
                    var validation = Measure(() => { flow.Value.Validate(grid); return flow.Value; });
                    string hash = Hash(flow.Value);
                    if (expectedHash is not null && hash != expectedHash) throw new InvalidOperationException("Mantle calibration replay differs.");
                    expectedHash = hash;
                    Add("Signals", noise.Timing); Add("Potential", potential.Timing); Add("Edges", flow.Timing); Add("Validation", validation.Timing);
                    if (batch > 0) Analyze(size.ToString(), seed, candidate.Name, batch, candidate.Parameters.ReferenceSpeed, flow.Value, influence, distances, hash, distributions, results);
                    void Add(string operation, Timing timing) => phases.Add(new(size.ToString(), seed, candidate.Name, batch, batch == 0,
                        operation, grid.CellCount, flow.Value.Balance.Edges, timing.Milliseconds, timing.AllocatedBytes, timing.RetainedGcDeltaBytes));
                }
                Console.WriteLine($"MANTLE CAL01 {size}/{seed}/{candidate.Name}: warmup + 3 batches; replay and derived balance checks passed.");
            }
            Require(original.SequenceEqual(packed) && originalInfluence == TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances)), "Earlier fields changed during measurement.");
        }
        WriteCsv(Path.Combine(directory, "timings.csv"), phases);
        WriteCsv(Path.Combine(directory, "distributions.csv"), distributions);
        WriteCsv(Path.Combine(directory, "results.csv"), results);
        WriteCsv(Path.Combine(directory, "summary.csv"), phases.Where(row => !row.Warmup).GroupBy(row => (row.Size, row.Candidate, row.Operation)).Select(group => new
        {
            group.Key.Size, group.Key.Candidate, group.Key.Operation, Samples = group.Count(), MinimumMs = group.Min(row => row.Milliseconds),
            MedianMs = Median(group.Select(row => row.Milliseconds)), MaximumMs = group.Max(row => row.Milliseconds),
            MeanAllocatedBytes = group.Average(row => row.AllocatedBytes), MinimumRetainedBytes = group.Min(row => row.RetainedGcDeltaBytes),
            MaximumRetainedBytes = group.Max(row => row.RetainedGcDeltaBytes)
        }));
        Console.WriteLine($"MANTLE CAL01 complete: {phases.Count} phase rows; {distributions.Count} distribution rows; {results.Count} conservation rows. No candidate selected.");
    }

    private static void Analyze(string size, long seed, string candidate, int batch, float referenceSpeed, MantleConvectionField field,
        TectonicInfluenceFields influence, TectonicBoundaryDistances distances, string hash, List<Distribution> distributions, List<CaseResult> results)
    {
        int up = 0, down = 0, interior = 0;
        int interiorReach = influence.Parameters.GetReach(new Evolve.Engine.WorldGeneration.Layers.Grid.HexGridData(Enum.Parse<MapSize>(size)));
        double interiorSquares = 0, sumP = 0, sumZ = 0, sumPP = 0, sumZZ = 0, sumPZ = 0;
        for (int cell = 0; cell < field.Count; cell++)
        {
            var value = field[cell]; var source = influence[cell];
            if (value.SignedVertical > 0) up++;
            if (value.SignedVertical < 0) down++;
            if (distances[cell] > interiorReach)
            { interior++; interiorSquares += (double)value.Potential * value.Potential; }
            double z = ((double)source.Extension - source.Compression) / (((double)referenceSpeed + source.Extension) + source.Compression);
            double p = value.Potential;
            sumP += p; sumZ += z; sumPP += p * p; sumZZ += z * z; sumPZ += p * z;
        }
        for (int channel = 0; channel < 4; channel++)
        {
            float[] values = new float[field.Count];
            double sum = 0, squares = 0;
            for (int cell = 0; cell < field.Count; cell++)
            {
                var value = field[cell];
                float sample = channel switch { 0 => value.Potential, 1 => value.FlowX, 2 => value.FlowY, _ => value.SignedVertical };
                values[cell] = sample; sum += sample; squares += (double)sample * sample;
            }
            Array.Sort(values);
            distributions.Add(new(size, seed, candidate, batch, new[] { "Potential", "FlowX", "FlowY", "SignedVertical" }[channel],
                values[0], values[^1], sum / field.Count, Math.Sqrt(squares / field.Count), values[(int)(.01 * (values.Length - 1))],
                values[(values.Length - 1) / 2], values[(int)(.99 * (values.Length - 1))]));
        }
        double denominator = Math.Sqrt(Math.Max(0, (field.Count * sumPP - sumP * sumP) * (field.Count * sumZZ - sumZ * sumZ)));
        var balance = field.Balance;
        results.Add(new(size, seed, candidate, batch, hash, field.Count, balance.Edges, balance.DoubleSum, balance.DoubleBudget,
            balance.FloatSum, balance.FloatBudget, balance.RoundingErrorUpper, up * 100d / field.Count, down * 100d / field.Count,
            interior, interior == 0 ? 0 : Math.Sqrt(interiorSquares / interior), denominator == 0 ? 0 : (field.Count * sumPZ - sumP * sumZ) / denominator));
    }

    private static (T Value, Timing Timing) Measure<T>(Func<T> operation) where T : class
    {
        long before = GC.GetTotalMemory(true), allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
        T value = operation();
        long end = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long after = GC.GetTotalMemory(true); GC.KeepAlive(value);
        return (value, new((end - start) * 1000d / Stopwatch.Frequency, bytes, after - before));
    }

    private static string Hash(MantleConvectionField field)
    {
        byte[] bytes = new byte[field.Count * 16];
        for (int cell = 0; cell < field.Count; cell++)
        {
            var value = field[cell];
            Write(0, value.Potential); Write(4, value.FlowX); Write(8, value.FlowY); Write(12, value.SignedVertical);
            void Write(int offset, float sample) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell * 16 + offset), BitConverter.SingleToUInt32Bits(sample));
        }
        return TectonicInfluenceVectors.Hash(bytes);
    }
    private static double Median(IEnumerable<double> samples)
    {
        double[] sorted = samples.Order().ToArray();
        return sorted.Length % 2 == 0 ? sorted[sorted.Length / 2 - 1] / 2 + sorted[sorted.Length / 2] / 2 : sorted[sorted.Length / 2];
    }
    private static void WriteCsv<T>(string path, IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties(); using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(",", properties.Select(property => property.Name)));
        foreach (var row in rows) writer.WriteLine(string.Join(",", properties.Select(property => "\"" + Convert.ToString(property.GetValue(row), CultureInfo.InvariantCulture)!.Replace("\"", "\"\"") + "\"")));
    }
}
