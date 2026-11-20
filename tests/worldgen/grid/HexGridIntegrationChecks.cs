// Verifies the real Grid executor inside the unchanged production generation pipeline.
// Uses test-only downstream executors and fault injection without implementing geographic algorithms or a second runner.
using System.Reflection;
using System.Runtime.CompilerServices;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.WorldGeneration.Grid.Checks;

internal static class HexGridIntegrationChecks
{
    public static void Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("real Grid registration and first publication for all sizes", FirstGrid),
            ("descriptor and published values are immutable", ImmutableDescriptor),
            ("all forward modes preserve prerequisite order", ForwardModes),
            ("Grid rebuild invalidates the full suffix and replays", Rebuild),
            ("seeds and world types do not alter size-specific geometry", EquivalentSettings),
            ("explicit failures, exceptions and invalid candidates are atomic and retryable", Failures),
            ("finalization retains historical Grid data after rebuild", Finalization),
            ("concurrent and reentrant operations remain rejected", Concurrent),
            ("registry and invalid argument guards remain enforced", InvalidInputs),
            ("engine remains independent of graphics", Headless)
        ];
        foreach (var (name, check) in checks)
        {
            check();
            Console.WriteLine($"PASS WG-00-C {name}");
        }
        Console.WriteLine($"PASS WG-00-C {checks.Length}/{checks.Length} integration checks");
    }

    private static GenerationRequest Request(MapSize size = MapSize.Small, long seed = 0, WorldType type = WorldType.Pangea) =>
        new(seed, new WorldGenerationSettings(type, size));

    // This is registration only: every test uses the actual production orchestrator for execution.
    private static WorldGenerationLayerRegistry Registry(
        Func<GenerationLayerInputs, LayerExecutionResult>? execute = null, IWorldGenerationLayer? grid = null) =>
        new(new[] { grid ?? new HexGridLayer() }.Concat(Enum.GetValues<GenerationStage>().Skip(1)
            .Select(stage => (IWorldGenerationLayer)new DownstreamLayer(stage, execute))));

    private static LayerData<HexGridDescriptor> GridData(GenerationContext context) =>
        context.GetLayerData(GenerationStage.Grid) as LayerData<HexGridDescriptor>
            ?? throw new InvalidOperationException("Published Grid descriptor is unavailable.");

    private static void FirstGrid()
    {
        foreach (var (size, columns, rows) in new[]
        {
            (MapSize.Small, 200, 100), (MapSize.Medium, 300, 150), (MapSize.Large, 400, 200)
        })
        {
            var registry = Registry(_ => throw new InvalidOperationException("RunNext executed downstream."));
            Require(registry.Layers.Count == 8 && registry.GetLayer(GenerationStage.Grid) is HexGridLayer, "Real Grid registration");
            var orchestrator = new WorldGenerationOrchestrator(registry);
            var context = orchestrator.CreateGeneration(Request(size));
            Require(context.GetProgress().NextStage == GenerationStage.Grid && context.GetLayerData(GenerationStage.Grid) is null, "Initial Grid eligibility");
            var progress = orchestrator.RunNext(context);
            var data = GridData(context);
            Require(progress.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Completed &&
                progress.NextStage == GenerationStage.Tectonics && data.Count == 1 &&
                data.Stage == GenerationStage.Grid && data[0].Size == context.Request.Settings.MapSize, "First publication and request agreement");
            var grid = data[0].ToGridData();
            Require(grid.Columns == columns && grid.Rows == rows && grid.CellCount == columns * rows, "Reconstructed dimensions");
        }
    }

    private static void ImmutableDescriptor()
    {
        Require(!RuntimeHelpers.IsReferenceOrContainsReferences<HexGridDescriptor>() &&
            typeof(HexGridDescriptor).GetProperties().All(property => property.SetMethod is null), "Unmanaged readonly descriptor");
        HexGridDescriptor[] source = [new(MapSize.Small)];
        var snapshot = new LayerData<HexGridDescriptor>(GenerationStage.Grid, source);
        source[0] = new(MapSize.Large);
        Require(snapshot[0].Size == MapSize.Small && snapshot.Values is not IList<HexGridDescriptor> &&
            snapshot.Values is not HexGridDescriptor[], "Core defensive copy and immutable access");
        var orchestrator = new WorldGenerationOrchestrator(Registry());
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunNext(context);
        var original = GridData(context);
        var copy = original[0];
        copy = new(MapSize.Large);
        Require(copy.Size == MapSize.Large && original[0].Size == MapSize.Small, "Descriptor is returned by value");
        Require(original[0].ToGridData().GetOffset(19999) == new HexOffset(199, 99), "Immutable reconstruction");
    }

    private static void ForwardModes()
    {
        foreach (int mode in new[] { 0, 1, 2 })
        {
            var order = new List<GenerationStage>();
            GenerationContext context = null!;
            var observed = new GridProbe(inputs =>
            {
                order.Add(inputs.Stage);
                Require(context.GetProgress().GetStatus(inputs.Stage) == GenerationStageStatus.Running &&
                    context.GetLayerData(inputs.Stage) is null, "Candidate must not be visible while running");
                var result = new HexGridLayer().Execute(inputs);
                var random = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(inputs.Request.Seed, inputs.Stage));
                Require(inputs.Random.NextUInt64() == random.NextUInt64(), "Grid consumed randomness");
                Require(context.GetLayerData(inputs.Stage) is null && result.Candidate is LayerData<HexGridDescriptor>, "Grid returned a candidate only");
                return result;
            });
            var orchestrator = new WorldGenerationOrchestrator(Registry(inputs =>
            {
                order.Add(inputs.Stage);
                for (int index = 0; index < (int)inputs.Stage; index++)
                    Require(context.GetProgress().GetStatus((GenerationStage)index) == GenerationStageStatus.Completed &&
                        inputs.GetLayerData((GenerationStage)index).Stage == (GenerationStage)index, "Prerequisites skipped");
                Require(((LayerData<HexGridDescriptor>)inputs.GetLayerData(GenerationStage.Grid))[0].Size == MapSize.Small, "Read-only Grid input");
                return DownstreamLayer.Sample(inputs);
            }, observed));
            context = orchestrator.CreateGeneration(Request());
            if (mode == 0) for (int index = 0; index < 8; index++) orchestrator.RunNext(context);
            else if (mode == 1) orchestrator.RunAll(context);
            else
            {
                orchestrator.RunUntil(context, GenerationStage.Elevation);
                Require(order.SequenceEqual(Enum.GetValues<GenerationStage>().Take(4)), "RunUntil target");
                orchestrator.RunUntil(context, GenerationStage.Biomes);
            }
            Require(order.SequenceEqual(Enum.GetValues<GenerationStage>()), "Canonical execution order");
            var published = GridData(context);
            orchestrator.RunAll(context);
            orchestrator.RunNext(context);
            orchestrator.RunUntil(context, GenerationStage.Grid);
            Require(order.Count == 8 && ReferenceEquals(published, GridData(context)), "Completed stages executed again");
        }
    }

    private static void Rebuild()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var orchestrator = new WorldGenerationOrchestrator(Registry());
            var context = orchestrator.CreateGeneration(Request(size));
            orchestrator.RunAll(context);
            var prior = GridData(context);
            orchestrator.RebuildFrom(context, GenerationStage.Grid);
            foreach (var stage in Enum.GetValues<GenerationStage>())
                Require(context.GetLayerData(stage) is null &&
                    context.GetProgress().GetStatus(stage) == GenerationStageStatus.Invalidated, "Stale downstream output");
            Require(context.GetProgress().NextStage == GenerationStage.Grid, "Rebuild eligibility");
            orchestrator.RunAll(context);
            Require(!ReferenceEquals(prior, GridData(context)) && prior[0] == GridData(context)[0], "Rebuild value equivalence and historical data");
        }
    }

    private static void EquivalentSettings()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        foreach (var type in Enum.GetValues<WorldType>())
        {
            var orchestrator = new WorldGenerationOrchestrator(Registry());
            var context = orchestrator.CreateGeneration(Request(size, seed, type));
            orchestrator.RunNext(context);
            var descriptor = GridData(context)[0];
            var grid = descriptor.ToGridData();
            var expected = new HexGridData(size);
            Require(descriptor == new HexGridDescriptor(size) && descriptor.Size == context.Request.Settings.MapSize &&
                grid.Columns == expected.Columns && grid.Rows == expected.Rows, "Configuration-independent geometry");
            for (int id = 0; id < grid.CellCount; id++)
                Require(grid.GetOffset(id) == expected.GetOffset(id) &&
                    grid.GetGeographicCenter(id) == expected.GetGeographicCenter(id), "Seed/world type changed coordinates");
        }
    }

    // Faults are confined to test executors; completed real Grid output must never be lost or overwritten.
    private static void Failures()
    {
        foreach (var failedStage in Enum.GetValues<GenerationStage>())
        for (int kind = 0; kind < 4; kind++)
        {
            bool fail = true;
            LayerExecutionResult Execute(GenerationLayerInputs inputs)
            {
                if (fail && inputs.Stage == failedStage)
                    return kind switch
                    {
                        0 => LayerExecutionResult.Failed("Injected failure."),
                        1 => throw new InvalidOperationException("Injected exception."),
                        2 => LayerExecutionResult.Success(new LayerData<int>((GenerationStage)(((int)inputs.Stage + 1) % 8), [999])),
                        _ => null!
                    };
                return inputs.Stage == GenerationStage.Grid ? new HexGridLayer().Execute(inputs) : DownstreamLayer.Sample(inputs);
            }
            var orchestrator = new WorldGenerationOrchestrator(Registry(Execute,
                failedStage == GenerationStage.Grid ? new GridProbe(Execute) : null));
            var context = orchestrator.CreateGeneration(Request());
            if (failedStage != GenerationStage.Grid) orchestrator.RunUntil(context, failedStage - 1);
            var before = Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData).ToArray();
            var failure = orchestrator.RunAll(context);
            Require(failure.GetStatus(failedStage) == GenerationStageStatus.Failed && failure.NextStage == failedStage &&
                !string.IsNullOrWhiteSpace(failure.GetFailure(failedStage)) && context.GetLayerData(failedStage) is null, "Failed candidate leaked");
            Require(before.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData)), "Prior results changed");
            foreach (var stage in Enum.GetValues<GenerationStage>().Skip((int)failedStage + 1))
                Require(failure.GetStatus(stage) == GenerationStageStatus.Pending && context.GetLayerData(stage) is null, "Failure unlocked later stages");
            fail = false;
            orchestrator.RunNext(context);
            Require(context.GetProgress().GetStatus(failedStage) == GenerationStageStatus.Completed &&
                context.GetProgress().GetFailure(failedStage) is null &&
                before.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData)), "Retry corrupted prerequisites");
            orchestrator.RunAll(context);
            Require(context.GetProgress().LastCompletedStage == GenerationStage.Biomes, "Retry did not resume");
        }
    }

    private static void Finalization()
    {
        var orchestrator = new WorldGenerationOrchestrator(Registry());
        var context = orchestrator.CreateGeneration(Request());
        Expect<InvalidOperationException>(() => orchestrator.FinalizeWorld(context));
        orchestrator.RunAll(context);
        var first = orchestrator.FinalizeWorld(context);
        Require(ReferenceEquals(first.GetLayerData(GenerationStage.Grid), GridData(context)) &&
            ReferenceEquals(orchestrator.FinalizeWorld(context).GetLayerData(GenerationStage.Grid), GridData(context)), "Repeated finalization changed Grid");
        orchestrator.RebuildFrom(context, GenerationStage.Grid);
        Require(first.GetLayerData(GenerationStage.Grid) is LayerData<HexGridDescriptor> historical && historical[0].Size == MapSize.Small, "Historical Grid snapshot changed");
        Expect<InvalidOperationException>(() => orchestrator.FinalizeWorld(context));
    }

    private static void Concurrent()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var other = new WorldGenerationOrchestrator(Registry());
        GenerationContext context = null!;
        var orchestrator = new WorldGenerationOrchestrator(Registry(grid: new GridProbe(inputs =>
        {
            Expect<InvalidOperationException>(() => other.RunNext(context));
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test release timed out.");
            return new HexGridLayer().Execute(inputs);
        })));
        context = orchestrator.CreateGeneration(Request());
        var worker = Task.Run(() => orchestrator.RunNext(context));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Worker did not enter Grid");
            Expect<InvalidOperationException>(() => other.RunAll(context));
            Expect<InvalidOperationException>(() => other.RunUntil(context, GenerationStage.Biomes));
            Expect<InvalidOperationException>(() => other.RebuildFrom(context, GenerationStage.Grid));
            Expect<InvalidOperationException>(() => other.FinalizeWorld(context));
            Require(context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Running &&
                context.GetLayerData(GenerationStage.Grid) is null, "Running Grid exposed partial data");
            var independent = other.CreateGeneration(Request());
            var separate = Task.Run(() => other.RunAll(independent));
            Require(separate.Wait(TimeSpan.FromSeconds(5)) && separate.Result.LastCompletedStage == GenerationStage.Biomes, "Independent context blocked");
        }
        finally { release.Set(); Require(worker.Wait(TimeSpan.FromSeconds(10)), "Grid worker did not complete"); }
        Require(worker.Result.LastCompletedStage == GenerationStage.Grid, "Concurrent rejection damaged Grid");
    }

    private static void InvalidInputs()
    {
        Expect<ArgumentNullException>(() => new HexGridLayer().Execute(null!));
        foreach (int size in new[] { -1, 3, int.MaxValue })
            Expect<ArgumentOutOfRangeException>(() => new HexGridDescriptor((MapSize)size));
        var layers = Registry().Layers.ToArray();
        Expect<ArgumentException>(() => new WorldGenerationLayerRegistry(layers.Skip(1)));
        layers[1] = new HexGridLayer();
        Expect<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        layers = Registry().Layers.Reverse().ToArray();
        Expect<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        var orchestrator = new WorldGenerationOrchestrator(Registry(inputs =>
        {
            Expect<ArgumentException>(() => new HexGridLayer().Execute(inputs));
            return DownstreamLayer.Sample(inputs);
        }));
        var context = orchestrator.CreateGeneration(Request());
        Expect<ArgumentOutOfRangeException>(() => orchestrator.RunUntil(context, (GenerationStage)8));
        orchestrator.RunAll(context);
        Require(context.GetProgress().LastCompletedStage == GenerationStage.Biomes, "Invalid requests corrupted context");
    }

    private static void Headless()
    {
        Require(typeof(HexGridLayer).Assembly.GetReferencedAssemblies().All(reference =>
            reference.Name?.StartsWith("System", StringComparison.Ordinal) == true ||
            reference.Name == typeof(MapSize).Assembly.GetName().Name), "Graphics or unrelated engine dependency");
        Require(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            assembly.GetName().Name?.StartsWith("OpenTK", StringComparison.Ordinal) == true), "OpenTK loaded");
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); } catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // A configurable test double represents any of the seven unimplemented downstream stages.
    private sealed class DownstreamLayer(GenerationStage stage,
        Func<GenerationLayerInputs, LayerExecutionResult>? execute) : IWorldGenerationLayer
    {
        public GenerationStage Stage => stage;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) => (execute ?? Sample)(inputs);
        public static LayerExecutionResult Sample(GenerationLayerInputs inputs) =>
            LayerExecutionResult.Success(new LayerData<int>(inputs.Stage, [(int)inputs.Stage]));
    }

    // Wrap the real Grid only to inspect inputs or inject faults; never publish data or simulate orchestration.
    private sealed class GridProbe(Func<GenerationLayerInputs, LayerExecutionResult> execute) : IWorldGenerationLayer
    {
        public GenerationStage Stage => GenerationStage.Grid;
        public LayerExecutionResult Execute(GenerationLayerInputs inputs) => execute(inputs);
    }
}
