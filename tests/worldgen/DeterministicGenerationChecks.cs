// Verifies CORE-04 random compatibility, suffix rebuilding and real cross-thread exclusivity.
// Invokes production orchestration with the existing eight test-only layer executors.

using Evolve.Engine.WorldGeneration;
using Evolve.Definitions.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class DeterministicGenerationChecks
{
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("fixed SplitMix64 vectors", Vectors),
            ("stage and seed isolation", Streams),
            ("seed/settings and execution modes reproduce outputs", Modes),
            ("every rebuilt suffix preserves only its prefix and replays", Rebuilds),
            ("failed and pending rebuild states are explicit", PartialRebuild),
            ("retry restarts the isolated stage stream", Retry),
            ("cross-instance operations reject while another thread executes", Concurrent),
            ("invalid rebuild requests release ownership without mutation", InvalidRebuild)
        ];
        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try { check(); Console.WriteLine($"PASS CORE-04: {name}"); }
            catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL CORE-04: {name}: {exception}"); }
        }
        Console.WriteLine($"CORE-04: {checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    // Neutral samples depend on settings, prior validated outputs and the supplied stage stream.
    private static LayerExecutionResult Sample(GenerationLayerInputs inputs)
    {
        ulong value = inputs.Random.NextUInt64() ^ (ulong)inputs.Request.Settings.WorldType ^ ((ulong)inputs.Request.Settings.MapSize << 8);
        for (int index = 0; index < (int)inputs.Stage; index++)
            value ^= ((LayerData<ulong>)inputs.GetLayerData((GenerationStage)index))[0];
        return LayerExecutionResult.Success(new LayerData<ulong>(inputs.Stage, [value, inputs.Random.NextUInt64()]));
    }

    private static WorldGenerationOrchestrator Create(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) =>
        new(new WorldGenerationLayerRegistry(FakeWorldGenerationLayers.Create(execute ?? Sample)));

    private static GenerationRequest Request(long seed = 0, WorldType type = WorldType.Pangea, MapSize size = MapSize.Small) =>
        new(seed, new WorldGenerationSettings(type, size));

    private static ulong[] Values(GenerationContext context) =>
        Enum.GetValues<GenerationStage>().SelectMany(stage => (LayerData<ulong>)context.GetLayerData(stage)!).ToArray();

    private static void Vectors()
    {
        var stream = new DeterministicStageRandom(0);
        ulong[] expected = [0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL];
        foreach (ulong value in expected) Require(stream.NextUInt64() == value, "PRNG compatibility vector changed.");
        Require(DeterministicStageRandom.DeriveSeed(0, GenerationStage.Grid) == expected[0], "Derivation vector changed.");
        Require(DeterministicStageRandom.AlgorithmVersion == "splitmix64-stage-v1", "Unexpected algorithm version.");
        Reject<ArgumentOutOfRangeException>(() => DeterministicStageRandom.DeriveSeed(0, (GenerationStage)8));
    }

    private static void Streams()
    {
        foreach (long seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        {
            var seeds = Enum.GetValues<GenerationStage>().Select(stage => DeterministicStageRandom.DeriveSeed(seed, stage)).ToArray();
            Require(seeds.Distinct().Count() == 8, "Stage seeds overlap.");
            var first = new DeterministicStageRandom(seeds[1]);
            var second = new DeterministicStageRandom(seeds[1]);
            var unrelated = new DeterministicStageRandom(seeds[0]);
            for (int index = 0; index < 100; index++) unrelated.NextUInt64();
            Require(first.NextUInt64() == second.NextUInt64(), "Consumption affected another stream.");
        }
        Require(DeterministicStageRandom.DeriveSeed(0, GenerationStage.Grid) !=
            DeterministicStageRandom.DeriveSeed(1, GenerationStage.Grid), "Distinct seeds overlap.");
    }

    private static void Modes()
    {
        foreach (var type in Enum.GetValues<WorldType>())
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -42L, long.MinValue, long.MaxValue })
        {
            var orchestrator = Create();
            var contexts = Enumerable.Range(0, 3).Select(_ => orchestrator.CreateGeneration(Request(seed, type, size))).ToArray();
            orchestrator.RunAll(contexts[0]);
            for (int index = 0; index < 8; index++) orchestrator.RunNext(contexts[1]);
            orchestrator.RunUntil(contexts[2], GenerationStage.Elevation);
            orchestrator.RunAll(contexts[2]);
            Require(Values(contexts[0]).SequenceEqual(Values(contexts[1])) && Values(contexts[0]).SequenceEqual(Values(contexts[2])), "Mode outputs differ.");
        }
    }

    private static void Rebuilds()
    {
        foreach (var target in Enum.GetValues<GenerationStage>())
        {
            var order = new List<GenerationStage>();
            var orchestrator = Create(inputs => { order.Add(inputs.Stage); return Sample(inputs); });
            var context = orchestrator.CreateGeneration(Request(-42));
            orchestrator.RunAll(context);
            var expected = Values(context);
            var buffers = Enum.GetValues<GenerationStage>().Select(context.GetLayerData).ToArray();
            var historical = context.GetProgress();
            var progress = orchestrator.RebuildFrom(context, target);
            Require(progress.NextStage == target, "Rebuild eligibility is incorrect.");
            foreach (var stage in Enum.GetValues<GenerationStage>())
                Require(stage < target
                    ? ReferenceEquals(buffers[(int)stage], context.GetLayerData(stage))
                    : context.GetLayerData(stage) is null && progress.GetStatus(stage) == GenerationStageStatus.Invalidated, "Rebuild retained stale data or changed prefix.");
            Require(historical.LastCompletedStage == GenerationStage.Biomes, "Historical snapshot mutated.");
            order.Clear();
            orchestrator.RunAll(context);
            Require(order.SequenceEqual(Enum.GetValues<GenerationStage>().Skip((int)target)) && expected.SequenceEqual(Values(context)), "Rebuild order or replay differs.");
        }
    }

    private static void PartialRebuild()
    {
        var orchestrator = Create(inputs => inputs.Stage == GenerationStage.Elevation ? LayerExecutionResult.Failed("Sample failure.") : Sample(inputs));
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        var progress = orchestrator.RebuildFrom(context, GenerationStage.Landmasses);
        Require(progress.GetStatus(GenerationStage.Landmasses) == GenerationStageStatus.Invalidated &&
            progress.GetStatus(GenerationStage.Elevation) == GenerationStageStatus.Pending &&
            progress.GetFailure(GenerationStage.Elevation) is null &&
            progress.GetStatus(GenerationStage.Biomes) == GenerationStageStatus.Pending, "Partial rebuild states differ.");
    }

    private static void Retry()
    {
        ulong? first = null;
        var orchestrator = Create(inputs =>
        {
            ulong value = inputs.Random.NextUInt64();
            if (first is null) { first = value; return LayerExecutionResult.Failed("Retry."); }
            Require(value == first, "Retry consumed a continued stream.");
            return LayerExecutionResult.Success(new LayerData<ulong>(inputs.Stage, [value]));
        });
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunNext(context);
        Require(orchestrator.RunNext(context).LastCompletedStage == GenerationStage.Grid, "Retry failed.");
    }

    // Event barriers establish overlap without relying on sleeps or scheduling luck.
    private static void Concurrent()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var active = Create(inputs =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test release timed out.");
            return Sample(inputs);
        });
        var other = Create();
        var context = active.CreateGeneration(Request());
        var worker = Task.Run(() => active.RunAll(context));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Worker did not enter layer.");
            foreach (var orchestrator in new[] { active, other })
            {
                Reject<InvalidOperationException>(() => orchestrator.RunNext(context));
                Reject<InvalidOperationException>(() => orchestrator.RunAll(context));
                Reject<InvalidOperationException>(() => orchestrator.RunUntil(context, GenerationStage.Biomes));
                Reject<InvalidOperationException>(() => orchestrator.RebuildFrom(context, GenerationStage.Grid));
            }
            for (int index = 0; index < 100; index++)
                Require(context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Running &&
                    context.GetLayerData(GenerationStage.Grid) is null, "Observation exposed partial data.");
            var independent = other.CreateGeneration(Request());
            var independentWorker = Task.Run(() => other.RunAll(independent));
            Require(independentWorker.Wait(TimeSpan.FromSeconds(5)) &&
                independentWorker.Result.LastCompletedStage == GenerationStage.Biomes, "Independent context blocked.");
        }
        finally { release.Set(); Require(worker.Wait(TimeSpan.FromSeconds(10)), "Worker did not finish."); }
        Require(worker.Result.LastCompletedStage == GenerationStage.Biomes, "Concurrent rejection corrupted active execution.");
        other.RebuildFrom(context, GenerationStage.Grid);
        Require(other.RunAll(context).LastCompletedStage == GenerationStage.Biomes, "Lease was not released.");
    }

    private static void InvalidRebuild()
    {
        var orchestrator = Create();
        var context = orchestrator.CreateGeneration(Request());
        Reject<ArgumentOutOfRangeException>(() => orchestrator.RebuildFrom(context, (GenerationStage)8));
        Reject<InvalidOperationException>(() => orchestrator.RebuildFrom(context, GenerationStage.Tectonics));
        Reject<ArgumentNullException>(() => orchestrator.RebuildFrom(null!, GenerationStage.Grid));
        Require(orchestrator.RunAll(context).LastCompletedStage == GenerationStage.Biomes, "Rejected rebuild leaked ownership.");
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
