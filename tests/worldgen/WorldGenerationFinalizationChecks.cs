// Verifies CORE-05 finalization against production orchestration and immutable buffers.
// Exercises completion gates, historical snapshots and concurrency using test-only executors.

using System.Reflection;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class WorldGenerationFinalizationChecks
{
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("completed snapshots reuse buffers without changing context", Completed),
            ("all incomplete prefixes are rejected", Incomplete),
            ("every failed, invalidated and running stage is rejected", InvalidStates),
            ("incorrect or missing result ownership is rejected", InvalidOwnership),
            ("historical results survive every suffix rebuild and regeneration", Historical),
            ("result metadata and data expose no public mutation", Immutable),
            ("repeated finalization executes no layers", Repeated),
            ("finalization rejects concurrent and reentrant execution", Concurrent),
            ("all operations respect finalization's context lease", ExclusiveBoundary),
            ("invalid arguments and rejected finalization release the lease", InvalidArguments)
        ];
        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try { check(); Console.WriteLine($"PASS CORE-05: {name}"); }
            catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL CORE-05: {name}: {exception}"); }
        }
        Console.WriteLine($"CORE-05: {checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static GenerationRequest Request() => new(-42, new WorldGenerationSettings(WorldType.Archipelagos, MapSize.Medium));
    private static WorldGenerationOrchestrator Create(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) =>
        new(new WorldGenerationLayerRegistry(FakeWorldGenerationLayers.Create(execute)));

    // Compare both published reference identity and authoritative state/diagnostics before and after inspection.
    private static void Unchanged(GenerationContext context, Action action)
    {
        var before = context.GetProgress();
        var data = Enum.GetValues<GenerationStage>().Select(context.GetLayerData).ToArray();
        action();
        foreach (var stage in Enum.GetValues<GenerationStage>())
            Require(before.GetStatus(stage) == context.GetProgress().GetStatus(stage) &&
                before.GetFailure(stage) == context.GetProgress().GetFailure(stage) &&
                ReferenceEquals(data[(int)stage], context.GetLayerData(stage)), "Finalization mutated context.");
    }

    private static void Completed()
    {
        var orchestrator = Create();
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        Unchanged(context, () =>
        {
            var result = orchestrator.FinalizeWorld(context);
            Require(ReferenceEquals(result.Request, context.Request) &&
                result.RandomAlgorithmVersion == DeterministicStageRandom.AlgorithmVersion, "Metadata was lost.");
            foreach (var stage in Enum.GetValues<GenerationStage>())
                Require(ReferenceEquals(result.GetLayerData(stage), context.GetLayerData(stage)), "Finalization copied or lost layer buffers.");
        });
    }

    private static void Incomplete()
    {
        var orchestrator = Create();
        var context = orchestrator.CreateGeneration(Request());
        for (int count = 0; count < 8; count++)
        {
            Unchanged(context, () => Reject<InvalidOperationException>(() => orchestrator.FinalizeWorld(context)));
            orchestrator.RunNext(context);
        }
        orchestrator.FinalizeWorld(context);
    }

    private static void InvalidStates()
    {
        foreach (var target in Enum.GetValues<GenerationStage>())
        {
            var orchestrator = Create(inputs => inputs.Stage == target
                ? LayerExecutionResult.Failed("Sample failure.") : FakeWorldGenerationLayers.Sample(inputs));
            var context = orchestrator.CreateGeneration(Request());
            orchestrator.RunAll(context);
            Unchanged(context, () => Reject<InvalidOperationException>(() => orchestrator.FinalizeWorld(context)));
            var complete = Create();
            context = complete.CreateGeneration(Request());
            complete.RunAll(context);
            complete.RebuildFrom(context, target);
            Unchanged(context, () => Reject<InvalidOperationException>(() => complete.FinalizeWorld(context)));
            // Restricted storage preparation verifies the state gate independently of the active-operation gate.
            context = complete.CreateGeneration(Request());
            context.MarkRunning(target);
            Unchanged(context, () => Reject<InvalidOperationException>(() => complete.FinalizeWorld(context)));
        }
    }

    // These malformed engine-owned reference tables cannot be supplied through the public API.
    private static void InvalidOwnership()
    {
        var buffers = Enum.GetValues<GenerationStage>().Select(stage => (LayerData)new LayerData<int>(stage, [1])).ToArray();
        Reject<InvalidOperationException>(() => new WorldGenerationResult(Request(), buffers.Take(7)));
        buffers[3] = new LayerData<int>(GenerationStage.Grid, [1]);
        Reject<InvalidOperationException>(() => new WorldGenerationResult(Request(), buffers));
        buffers[3] = null!;
        Reject<InvalidOperationException>(() => new WorldGenerationResult(Request(), buffers));
    }

    private static void Historical()
    {
        foreach (var target in Enum.GetValues<GenerationStage>())
        {
            var orchestrator = Create();
            var context = orchestrator.CreateGeneration(Request());
            orchestrator.RunAll(context);
            var original = orchestrator.FinalizeWorld(context);
            var buffers = Enum.GetValues<GenerationStage>().Select(original.GetLayerData).ToArray();
            orchestrator.RebuildFrom(context, target);
            Reject<InvalidOperationException>(() => orchestrator.FinalizeWorld(context));
            foreach (var stage in Enum.GetValues<GenerationStage>())
                Require(ReferenceEquals(buffers[(int)stage], original.GetLayerData(stage)) &&
                    ((LayerData<int>)original.GetLayerData(stage))[0] == (int)stage, "Historical result changed.");
            orchestrator.RunAll(context);
            var rebuilt = orchestrator.FinalizeWorld(context);
            foreach (var stage in Enum.GetValues<GenerationStage>())
            {
                Require(((LayerData<int>)rebuilt.GetLayerData(stage))[0] == ((LayerData<int>)original.GetLayerData(stage))[0], "Replayed value changed.");
                Require(ReferenceEquals(buffers[(int)stage], original.GetLayerData(stage)), "Regeneration changed historical buffers.");
                Require(ReferenceEquals(original.GetLayerData(stage), rebuilt.GetLayerData(stage)) == (stage < target), "Rebuilt result retained a stale suffix.");
            }
        }
    }

    private static void Immutable()
    {
        int[] source = [7];
        var orchestrator = Create(inputs => LayerExecutionResult.Success(new LayerData<int>(inputs.Stage, source)));
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        var result = orchestrator.FinalizeWorld(context);
        source[0] = 99;
        Require(typeof(WorldGenerationResult).IsSealed && typeof(WorldGenerationResult).GetConstructors().Length == 0 &&
            typeof(WorldGenerationResult).GetProperties().All(property => property.SetMethod is null), "Result exposes mutation/construction.");
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var data = (LayerData<int>)result.GetLayerData(stage);
            Require(data[0] == 7 && data.Values is not IList<int> && data.Values is not int[], "Result exposes mutable buffers.");
            int copy = data[0];
            copy = 55;
            Require(data[0] == 7, "Value copy changed result.");
        }
        // Constructor captures its own small reference table, even against trusted internal caller mutation.
        var table = Enum.GetValues<GenerationStage>().Select(result.GetLayerData).ToArray();
        var isolated = new WorldGenerationResult(Request(), table);
        table[0] = new LayerData<int>(GenerationStage.Grid, [999]);
        Require(((LayerData<int>)isolated.GetLayerData(GenerationStage.Grid))[0] == 7, "Reference table escaped.");
    }

    private static void Repeated()
    {
        int calls = 0;
        var orchestrator = Create(inputs => { calls++; return FakeWorldGenerationLayers.Sample(inputs); });
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        var first = orchestrator.FinalizeWorld(context);
        for (int index = 0; index < 20; index++)
        {
            var next = orchestrator.FinalizeWorld(context);
            Require(ReferenceEquals(first.GetLayerData(GenerationStage.Biomes), next.GetLayerData(GenerationStage.Biomes)), "Repeated finalization changed buffers.");
        }
        Require(calls == 8, "Finalization executed layers.");
    }

    // Barriers prove overlap; different orchestrators share the same context-wide exclusion.
    private static void Concurrent()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var other = Create();
        GenerationContext context = null!;
        var active = Create(inputs =>
        {
            Reject<InvalidOperationException>(() => other.FinalizeWorld(context));
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test release timed out.");
            return FakeWorldGenerationLayers.Sample(inputs);
        });
        context = active.CreateGeneration(Request());
        var worker = Task.Run(() => active.RunAll(context));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Worker never entered execution.");
            Unchanged(context, () => Reject<InvalidOperationException>(() => other.FinalizeWorld(context)));
            var independent = other.CreateGeneration(Request());
            var separate = Task.Run(() => { other.RunAll(independent); return other.FinalizeWorld(independent); });
            Require(separate.Wait(TimeSpan.FromSeconds(5)), "Independent context blocked.");
        }
        finally { release.Set(); Require(worker.Wait(TimeSpan.FromSeconds(10)), "Worker did not finish."); }
        Require(worker.Result.LastCompletedStage == GenerationStage.Biomes, "Finalization rejection damaged execution.");
        other.FinalizeWorld(context);
    }

    // Restricted friend access holds precisely the lease used by finalization, without adding production test hooks.
    private static void ExclusiveBoundary()
    {
        var orchestrator = Create();
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        context.EnterOperation();
        try
        {
            var worker = Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => orchestrator.FinalizeWorld(context));
                Reject<InvalidOperationException>(() => orchestrator.RebuildFrom(context, GenerationStage.Grid));
                Reject<InvalidOperationException>(() => orchestrator.RunNext(context));
                Reject<InvalidOperationException>(() => orchestrator.RunAll(context));
                Reject<InvalidOperationException>(() => orchestrator.RunUntil(context, GenerationStage.Biomes));
            });
            Require(worker.Wait(TimeSpan.FromSeconds(5)), "Competing operation blocked instead of rejecting.");
        }
        finally { context.ExitOperation(); }
        orchestrator.FinalizeWorld(context);
    }

    private static void InvalidArguments()
    {
        var orchestrator = Create();
        Reject<ArgumentNullException>(() => orchestrator.FinalizeWorld(null!));
        var context = orchestrator.CreateGeneration(Request());
        Reject<InvalidOperationException>(() => orchestrator.FinalizeWorld(context));
        orchestrator.RunAll(context);
        var result = orchestrator.FinalizeWorld(context);
        foreach (int stage in new[] { -1, 8, int.MaxValue })
            Reject<ArgumentOutOfRangeException>(() => result.GetLayerData((GenerationStage)stage));
        orchestrator.RebuildFrom(context, GenerationStage.Grid);
        orchestrator.RunAll(context);
        orchestrator.FinalizeWorld(context);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
