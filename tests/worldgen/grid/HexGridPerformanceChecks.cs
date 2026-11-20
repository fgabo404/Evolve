// Measures accepted Grid operations independently of correctness assertions and GPU components.
// Reports repeated raw batches, harness controls and retained construction memory without acceptance thresholds.
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.WorldGeneration.Grid.Checks;

internal static class HexGridPerformanceChecks
{
    private const int Iterations = 200000;
    private const int Warmup = 20000;
    private const int Batches = 5;
    private static long consumed;

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine($"UTC={DateTimeOffset.UtcNow:O}");
        Console.WriteLine($"Runtime={RuntimeInformation.FrameworkDescription}; OS={RuntimeInformation.OSDescription}; Architecture={RuntimeInformation.ProcessArchitecture}; CPUs={Environment.ProcessorCount}");
        Console.WriteLine($"Processor={Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "not reported"}; ServerGC={GCSettings.IsServerGC}; StopwatchFrequency={Stopwatch.Frequency}");
        Console.WriteLine($"TieredCompilation={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "runtime default"}; Warmup={Warmup}; Iterations={Iterations}; Batches={Batches}; InputCount=4096");
#if DEBUG
        Console.WriteLine("Configuration=Debug; use Release for comparable evidence");
#else
        Console.WriteLine("Configuration=Release");
#endif
        Console.WriteLine("BATCH,size,operation,batch,iterations,elapsed_ms,ns_per_op,allocated_bytes,bytes_per_op,retained_delta_bytes,checksum");
        foreach (var size in Enum.GetValues<MapSize>()) MeasureSize(size);
        Console.WriteLine($"ConsumedChecksum={Volatile.Read(ref consumed)}");
    }

    private static void MeasureSize(MapSize size)
    {
        var grid = new HexGridData(size);
        var descriptor = PublishedDescriptor(size);
        // All input/container/delegate allocation happens outside measured intervals.
        int[] ids = new int[4096];
        HexOffset[] offsets = new HexOffset[ids.Length];
        int[] anchors = [0, grid.Columns - 1, grid.CellCount - grid.Columns, grid.CellCount - 1,
            grid.CellCount / 2, grid.GetCellId(new(grid.Columns / 2, grid.Rows / 2)), grid.CellCount / 3];
        for (int index = 0; index < ids.Length; index++)
        {
            ids[index] = index < anchors.Length ? anchors[index] : (int)(((long)index * 48271 + 17) % grid.CellCount);
            offsets[index] = grid.GetOffset(ids[index]);
        }
        // Escaping references prevent construction elision. Their container is preallocated and reused.
        var retained = new HexGridData[Iterations];
        (string Name, Func<int, long> Operation, bool Retains)[] operations =
        [
            ("HarnessControl", i => ids[i & 4095], false),
            ("Construction", i => { var value = new HexGridData(size); retained[i] = value; return value.CellCount; }, true),
            ("CellIdToOffset", i => { var value = grid.GetOffset(ids[i & 4095]); return value.Column + value.Row; }, false),
            ("OffsetToCellId", i => grid.GetCellId(offsets[i & 4095]), false),
            ("AxialLookup", i => { var value = grid.GetAxial(ids[i & 4095]); return value.Q + value.R; }, false),
            ("GeographicCenter", i => { var value = grid.GetGeographicCenter(ids[i & 4095]); return BitConverter.DoubleToInt64Bits(value.Longitude) ^ BitConverter.DoubleToInt64Bits(value.Latitude); }, false),
            ("SingleNeighbor", i => grid.TryGetNeighbor(ids[i & 4095], (HexDirection)(i % 6), out int next) ? next : -1, false),
            ("AllNeighbors", i => { long sum = 0; foreach (int neighbor in grid.GetNeighbors(ids[i & 4095])) sum += neighbor; return sum; }, false),
            ("CylindricalDistance", i => grid.GetDistance(ids[i & 4095], ids[(i * 17 + 31) & 4095]), false),
            ("DescriptorReconstruction", i => { var value = descriptor.ToGridData(); retained[i] = value; return value.CellCount; }, true)
        ];
        foreach (var (name, operation, retains) in operations)
        {
            Consume(operation, Warmup);
            double[] times = new double[Batches];
            double[] bytes = new double[Batches];
            for (int batch = 0; batch < Batches; batch++)
            {
                Array.Clear(retained);
                long beforeRetained = GC.GetTotalMemory(true);
                long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();
                long checksum = Consume(operation, Iterations);
                long end = Stopwatch.GetTimestamp();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
                // Full collection and output are deliberately excluded from elapsed time and allocated bytes.
                long afterRetained = retains ? GC.GetTotalMemory(true) : 0;
                GC.KeepAlive(retained);
                double elapsedMs = (end - start) * 1000d / Stopwatch.Frequency;
                times[batch] = elapsedMs * 1000000d / Iterations;
                bytes[batch] = (double)allocated / Iterations;
                string retainedDelta = retains ? (afterRetained - beforeRetained).ToString(CultureInfo.InvariantCulture) : "NA";
                Console.WriteLine($"BATCH,{size},{name},{batch + 1},{Iterations},{elapsedMs:F6},{times[batch]:F3},{allocated},{bytes[batch]:F3},{retainedDelta},{checksum}");
            }
            Array.Sort(times);
            Console.WriteLine($"SUMMARY,{size},{name},min_ns={times[0]:F3},median_ns={times[Batches / 2]:F3},max_ns={times[^1]:F3},min_B={bytes.Min():F3},max_B={bytes.Max():F3}");
        }
    }

    // Both control and real measurements include delegate dispatch, indexing, looping and checksum accumulation.
    // Consume every result; report raw timings rather than subtracting a noisy control measurement.
    private static long Consume(Func<int, long> operation, int count)
    {
        long checksum = 0;
        for (int index = 0; index < count; index++) checksum = unchecked(checksum + operation(index));
        Volatile.Write(ref consumed, checksum);
        return checksum;
    }

    // Retrieve a genuinely published descriptor outside measurement; reconstruction does not include orchestration.
    private static HexGridDescriptor PublishedDescriptor(MapSize size)
    {
        IWorldGenerationLayer[] layers = Enum.GetValues<GenerationStage>().Select(stage =>
            stage == GenerationStage.Grid ? (IWorldGenerationLayer)new HexGridLayer() : new UnavailableLayer(stage)).ToArray();
        var orchestrator = new WorldGenerationOrchestrator(new WorldGenerationLayerRegistry(layers));
        var context = orchestrator.CreateGeneration(new GenerationRequest(0, new WorldGenerationSettings(WorldType.Pangea, size)));
        orchestrator.RunNext(context);
        return ((LayerData<HexGridDescriptor>)context.GetLayerData(GenerationStage.Grid)!)[0];
    }

    // Registry slots only: no downstream stage is invoked or represented as generated geography.
    private sealed class UnavailableLayer(GenerationStage stage) : IWorldGenerationLayer
    {
        public GenerationStage Stage => stage;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) => throw new NotSupportedException("Not part of Grid performance measurement.");
    }
}
