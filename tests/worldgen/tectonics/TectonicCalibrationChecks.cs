// Records the frozen owner-approved WG-01-B R profile's areas, boundary diagnostics and repeated phase/memory measurements.
// Uses actual partition/codec code through the restricted friend grant; real orchestrator output is compared in full.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicCalibrationChecks
{
    internal static void Run()
    {
        Console.WriteLine($"MEASUREMENTS profile=R partition_revision=2; profile_approval=owner; UTC={DateTimeOffset.UtcNow:O}");
        Console.WriteLine($"ENV runtime={RuntimeInformation.FrameworkDescription}; OS={RuntimeInformation.OSDescription}; architecture={RuntimeInformation.ProcessArchitecture}; CPUs={Environment.ProcessorCount}; processor={Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}");
        Console.WriteLine($"METHOD Release; 1 full warmup per size; 3 batches/seed; seeds=0,1,-1,long.MinValue,long.MaxValue; stopwatch_frequency={Stopwatch.Frequency}; tiered_compilation={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        Console.WriteLine("PROFILE costs=" + string.Join(",", Enumerable.Range(0, 14).Select(rank => TectonicGenerationProfile.Approved.GetCost(rank).ToString("R", System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine($"VECTOR hash(13,-7,0x123456789ABCDEF0)={BitConverter.DoubleToUInt64Bits(PeriodicTectonicNoise.Hash(13, -7, 0x123456789ABCDEF0)):X16}");
        Console.WriteLine($"VECTOR sample(12.125,15.75,200,100,9,0x123456789ABCDEF0)={BitConverter.DoubleToUInt64Bits(PeriodicTectonicNoise.Sample(12.125, 15.75, 200, 100, 9, 0x123456789ABCDEF0)):X16}");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size);
            var warmup = Draft(grid, 0);
            _ = TectonicData.FromLayerData(TectonicData.CreateCandidate(grid, warmup, TectonicGenerationProfile.Approved), grid);
            foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
            {
                LayerData<uint>? baseline = null;
                for (int batch = 1; batch <= 3; batch++)
                {
                    long beforeRetained = GC.GetTotalMemory(true), beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    var draft = Draft(grid, seed);
                    double partitionMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long partitionAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
                    long codecAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    start = Stopwatch.GetTimestamp();
                    var packed = TectonicData.CreateCandidate(grid, draft, TectonicGenerationProfile.Approved);
                    double candidateMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long codecAllocated = GC.GetAllocatedBytesForCurrentThread() - codecAllocatedBefore;
                    long viewAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    start = Stopwatch.GetTimestamp();
                    var view = TectonicData.FromLayerData(packed, grid);
                    double viewMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    long viewAllocated = GC.GetAllocatedBytesForCurrentThread() - viewAllocatedBefore;
                    long retainedDelta = GC.GetTotalMemory(true) - beforeRetained;
                    if (baseline is not null) Require(baseline.SequenceEqual(packed), "Repeated R measurement batch differs");
                    else baseline = packed;
                    var metrics = draft.Metrics;
                    Console.WriteLine(FormattableString.Invariant($"MEASURE {size} seed={seed} batch={batch} seed_ms={metrics.SeedMilliseconds:F4} noise_ms={metrics.NoiseMilliseconds:F4} growth_ms={metrics.GrowthMilliseconds:F4} partition_ms={partitionMs:F4} peak_queue={metrics.PeakFrontier} partition_allocated_B={partitionAllocated} instrumented_phase_allocated_B={metrics.AllocatedBytes} validate_pack_copy_ms={candidateMs:F4} codec_allocated_B={codecAllocated} view_validate_ms={viewMs:F4} view_allocated_B={viewAllocated} payload_B={packed.Count * 4} retained_delta_B={retainedDelta}"));
                    if (batch == 1) ReportPartition(grid, view, seed, packed);
                    GC.KeepAlive(draft); GC.KeepAlive(packed); GC.KeepAlive(view);
                }
                Require(baseline!.SequenceEqual(Generate(size, seed).Data), "Measured builder differs from real orchestrator");
            }
        }
    }

    private static TectonicDraft Draft(HexGridData grid, long seed)
    {
        var stream = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
        ulong partitionSeed = stream.NextUInt64(), noiseSeed = stream.NextUInt64();
        _ = stream.NextUInt64();
        return TectonicPartitionGenerator.Generate(grid, new(partitionSeed), new(noiseSeed), TectonicGenerationProfile.Approved);
    }

    private static void ReportPartition(HexGridData grid, TectonicData view, long seed, LayerData<uint> packed)
    {
        int crossEdges = 0, seamEdges = 0, polarEdges = 0, boundaryCells = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            bool boundary = false;
            foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
            {
                if (view.GetPlateId(cell) == view.GetPlateId(next)) continue;
                boundary = true;
                if (cell >= next) continue;
                crossEdges++;
                if (Math.Abs(cell % grid.Columns - next % grid.Columns) == grid.Columns - 1) seamEdges++;
                if (cell / grid.Columns == 0 || cell / grid.Columns == grid.Rows - 1 ||
                    next / grid.Columns == 0 || next / grid.Columns == grid.Rows - 1) polarEdges++;
            }
            if (boundary) boundaryCells++;
        }
        string fractions = string.Join(",", Enumerable.Range(0, 14).Select(id =>
            ((double)view.GetPlate((byte)id).CellCount / grid.CellCount).ToString("F6", System.Globalization.CultureInfo.InvariantCulture)));
        string counts = string.Join(",", Enumerable.Range(0, 14).Select(id => view.GetPlate((byte)id).CellCount));
        string origins = string.Join(",", Enumerable.Range(0, 14).Select(id => view.GetPlate((byte)id).SeedCellId));
        int minimumSeedDistance = int.MaxValue;
        for (byte first = 0; first < 14; first++)
        for (byte second = (byte)(first + 1); second < 14; second++)
            minimumSeedDistance = Math.Min(minimumSeedDistance, grid.GetDistance(view.GetPlate(first).SeedCellId, view.GetPlate(second).SeedCellId));
        Console.WriteLine($"AREAS {grid.Columns}x{grid.Rows} seed={seed} counts_by_plate={counts} fractions_by_plate={fractions}");
        Console.WriteLine($"SHAPE {grid.Columns}x{grid.Rows} seed={seed} interplate_edges={crossEdges} interplate_seam_edges={seamEdges} interplate_polar_edges={polarEdges} boundary_cells={boundaryCells} minimum_seed_distance={minimumSeedDistance}");
        Console.WriteLine($"VECTOR {grid.Columns}x{grid.Rows} seed={seed} origins={origins} canonical_text_sha256={Digest(packed)}");
    }

    // Hashes supplement, never replace, complete sequence comparisons. Text is an explicit host-endian-independent encoding.
    internal static string Digest(IEnumerable<uint> packed)
    {
        string canonical = string.Join(",", packed.Select(word => word.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)));
        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(canonical)));
    }
}
