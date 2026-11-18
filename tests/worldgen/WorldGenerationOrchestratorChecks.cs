// Exercises the actual CORE-03 orchestrator with test-only immutable sample outputs.
// Verifies sequencing, failure isolation and retry without geographic algorithms, UI or GPU access.

using System.Reflection;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class WorldGenerationOrchestratorChecks
{
    // Each named scenario reports independently and contributes to the process exit code.
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("creation executes no layers and Grid is first", InitialGrid),
            ("RunNext executes exactly one stage in canonical order", SingleSteps),
            ("RunUntil includes every prerequisite for every target", EveryTarget),
            ("all forward modes produce the same sample outputs", EquivalentModes),
            ("completed targets and completed worlds never execute twice", CompletedNoOp),
            ("inconsistent prerequisite storage is rejected before execution", PrerequisiteRejection),
            ("all failure kinds stop every mode without partial publication", FailureIsolation),
            ("every failed stage can retry without repeating predecessors", Retry),
            ("read-only inputs capture earlier buffers and immutable metadata", InputIsolation),
            ("source mutation cannot change candidates or previous results", SourceIsolation),
            ("Running rejects all operations without corrupting the active stage", RunningRejection),
            ("reentrant calls cannot start a second stage", ReentrantRejection),
            ("invalid arguments preserve progress and published data", InvalidArguments),
            ("explicit outcome factories enforce candidate or diagnostic", ResultContract),
            ("contexts remain independent through the same orchestrator", IndependentContexts),
            ("invalidated storage does not trigger an implicit rebuild", NoImplicitRebuild),
            ("PRNG, rebuild and finalization remain deferred", DeferredScope)
        ];

        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try { check(); Console.WriteLine($"PASS CORE-03: {name}"); }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL CORE-03: {name}: {exception.Message}");
            }
        }
        Console.WriteLine($"CORE-03: {checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    // Use approved dimensionless configuration values and a signed seed without any PRNG.
    private static GenerationRequest Request() => new(-42, new WorldGenerationSettings(WorldType.Continents, MapSize.Small));

    private static WorldGenerationOrchestrator Create(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) =>
        new(new WorldGenerationLayerRegistry(FakeWorldGenerationLayers.Create(execute)));

    private static void InitialGrid()
    {
        int calls = 0;
        var orchestrator = Create(inputs => { calls++; return FakeWorldGenerationLayers.Sample(inputs); });
        var request = Request();
        var context = orchestrator.CreateGeneration(request);
        Require(calls == 0 && ReferenceEquals(context.Request, request), "Creation executed a layer or replaced metadata.");
        Require(context.GetProgress().NextStage == GenerationStage.Grid, "Grid is not first eligible.");
        Require(Enum.GetValues<GenerationStage>().All(stage => context.GetProgress().GetStatus(stage) == GenerationStageStatus.Pending),
            "Creation changed stage states.");
    }

    private static void SingleSteps()
    {
        var calls = new List<GenerationStage>();
        GenerationContext context = null!;
        var orchestrator = Create(inputs =>
        {
            Require(context.GetProgress().GetStatus(inputs.Stage) == GenerationStageStatus.Running, "Layer did not enter Running.");
            Require(context.GetLayerData(inputs.Stage) is null, "Candidate visible before completion.");
            for (int index = 0; index < (int)inputs.Stage; index++)
                Require(context.GetProgress().GetStatus((GenerationStage)index) == GenerationStageStatus.Completed,
                    "A prerequisite was skipped.");
            calls.Add(inputs.Stage);
            return FakeWorldGenerationLayers.Sample(inputs);
        });
        context = orchestrator.CreateGeneration(Request());
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var progress = orchestrator.RunNext(context);
            Require(calls.Count == (int)stage + 1 && calls[^1] == stage, "RunNext skipped or ran multiple layers.");
            Require(progress.LastCompletedStage == stage, "Progress lost the completed stage.");
        }
    }

    private static void EveryTarget()
    {
        foreach (var target in Enum.GetValues<GenerationStage>())
        {
            var calls = new List<GenerationStage>();
            var orchestrator = Create(inputs => { calls.Add(inputs.Stage); return FakeWorldGenerationLayers.Sample(inputs); });
            var context = orchestrator.CreateGeneration(Request());
            var progress = orchestrator.RunUntil(context, target);
            Require(calls.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)target + 1)), "RunUntil omitted prerequisites.");
            Require(progress.LastCompletedStage == target && progress.NextStage == (target == GenerationStage.Biomes ? null : target + 1),
                "RunUntil crossed its target.");
        }
    }

    // Derived sample values make mode equivalence depend on actual prior published inputs.
    private static void EquivalentModes()
    {
        var orchestrator = Create(inputs =>
        {
            int value = (int)inputs.Stage + 1;
            for (int index = 0; index < (int)inputs.Stage; index++)
                value += ((LayerData<int>)inputs.GetLayerData((GenerationStage)index))[0];
            return LayerExecutionResult.Success(new LayerData<int>(inputs.Stage, [value]));
        });
        var all = orchestrator.CreateGeneration(Request());
        var next = orchestrator.CreateGeneration(Request());
        var until = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(all);
        foreach (var stage in Enum.GetValues<GenerationStage>()) orchestrator.RunNext(next);
        orchestrator.RunUntil(until, GenerationStage.Elevation);
        orchestrator.RunAll(until);
        foreach (var stage in Enum.GetValues<GenerationStage>())
            Require(((LayerData<int>)all.GetLayerData(stage)!)[0] == ((LayerData<int>)next.GetLayerData(stage)!)[0] &&
                ((LayerData<int>)all.GetLayerData(stage)!)[0] == ((LayerData<int>)until.GetLayerData(stage)!)[0], "Modes disagree.");
    }

    private static void CompletedNoOp()
    {
        int calls = 0;
        var orchestrator = Create(inputs => { calls++; return FakeWorldGenerationLayers.Sample(inputs); });
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunUntil(context, GenerationStage.Elevation);
        orchestrator.RunUntil(context, GenerationStage.Grid);
        orchestrator.RunUntil(context, GenerationStage.Elevation);
        Require(calls == 4, "Completed target executed again.");
        orchestrator.RunAll(context);
        var buffers = Enum.GetValues<GenerationStage>().Select(context.GetLayerData).ToArray();
        orchestrator.RunAll(context);
        orchestrator.RunNext(context);
        orchestrator.RunUntil(context, GenerationStage.Biomes);
        Require(calls == 8 && buffers.SequenceEqual(Enum.GetValues<GenerationStage>().Select(context.GetLayerData)), "Completed world executed again.");
    }

    // Only the restricted friend can prepare inconsistent storage; public execution must reject it safely.
    private static void PrerequisiteRejection()
    {
        int calls = 0;
        var orchestrator = Create(inputs => { calls++; return FakeWorldGenerationLayers.Sample(inputs); });
        var context = orchestrator.CreateGeneration(Request());
        context.MarkRunning(GenerationStage.Tectonics);
        context.Publish(GenerationStage.Tectonics, new LayerData<int>(GenerationStage.Tectonics, [1]));
        foreach (var operation in Operations(orchestrator, context)) Throws<InvalidOperationException>(operation);
        Require(calls == 0 && context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Pending,
            "Invalid prerequisite storage was mutated.");
    }

    // Exercise explicit failure, exception, wrong ownership and missing result at every stage in every mode.
    private static void FailureIsolation()
    {
        foreach (var failedStage in Enum.GetValues<GenerationStage>())
        for (int kind = 0; kind < 4; kind++)
        for (int mode = 0; mode < 3; mode++)
        {
            var calls = new List<GenerationStage>();
            LayerData<int>? discarded = null;
            var orchestrator = Create(inputs =>
            {
                calls.Add(inputs.Stage);
                if (inputs.Stage != failedStage) return FakeWorldGenerationLayers.Sample(inputs);
                discarded = new LayerData<int>(inputs.Stage, [999]);
                return kind switch
                {
                    0 => LayerExecutionResult.Failed("Sample layer rejected its output."),
                    1 => throw new InvalidOperationException("Sample failure after candidate allocation."),
                    2 => LayerExecutionResult.Success(new LayerData<int>((GenerationStage)(((int)inputs.Stage + 1) % 8), [999])),
                    _ => null!
                };
            });
            var context = orchestrator.CreateGeneration(Request());
            // Earlier buffers are captured before the failure to verify their identities afterward.
            if (failedStage != GenerationStage.Grid) orchestrator.RunUntil(context, failedStage - 1);
            var previous = Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData).ToArray();
            ExecuteMode(orchestrator, context, mode);
            var progress = context.GetProgress();
            Require(calls.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)failedStage + 1)), "Failure ran a later stage or repeated a prerequisite.");
            Require(discarded is not null && context.GetLayerData(failedStage) is null && progress.GetStatus(failedStage) == GenerationStageStatus.Failed &&
                !string.IsNullOrWhiteSpace(progress.GetFailure(failedStage)) && progress.NextStage == failedStage, "Failure published or lost diagnostic/retry eligibility.");
            Require(previous.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData)), "Failure changed previous buffers.");
            foreach (var later in Enum.GetValues<GenerationStage>().Skip((int)failedStage + 1))
                Require(progress.GetStatus(later) == GenerationStageStatus.Pending && context.GetLayerData(later) is null, "Failure unlocked later data.");
        }
    }

    private static void Retry()
    {
        foreach (var failedStage in Enum.GetValues<GenerationStage>())
        for (int mode = 0; mode < 3; mode++)
        {
            int[] counts = new int[8];
            var orchestrator = Create(inputs => ++counts[(int)inputs.Stage] == 1 && inputs.Stage == failedStage
                ? LayerExecutionResult.Failed("Retryable sample failure.") : FakeWorldGenerationLayers.Sample(inputs));
            var context = orchestrator.CreateGeneration(Request());
            var failed = orchestrator.RunAll(context);
            var previous = Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData).ToArray();
            ExecuteMode(orchestrator, context, mode);
            Require(context.GetProgress().GetStatus(failedStage) == GenerationStageStatus.Completed && context.GetProgress().GetFailure(failedStage) is null,
                "Retry did not clear failure and complete.");
            Require(counts[(int)failedStage] == 2 && counts.Take((int)failedStage).All(count => count == 1), "Retry repeated an earlier stage.");
            Require(previous.SequenceEqual(Enum.GetValues<GenerationStage>().Take((int)failedStage).Select(context.GetLayerData)), "Retry replaced previous buffers.");
            Require(failed.GetStatus(failedStage) == GenerationStageStatus.Failed, "Historical failure snapshot changed.");
        }
    }

    private static void InputIsolation()
    {
        var captured = new List<GenerationLayerInputs>();
        var orchestrator = Create(inputs =>
        {
            captured.Add(inputs);
            Throws<InvalidOperationException>(() => inputs.GetLayerData(inputs.Stage));
            Throws<InvalidOperationException>(() => inputs.GetLayerData(GenerationStage.Biomes));
            Throws<ArgumentOutOfRangeException>(() => inputs.GetLayerData((GenerationStage)(-1)));
            return FakeWorldGenerationLayers.Sample(inputs);
        });
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        foreach (var inputs in captured)
        {
            Require(ReferenceEquals(inputs.Request, context.Request), "Input metadata changed.");
            for (int index = 0; index < (int)inputs.Stage; index++)
                Require(ReferenceEquals(inputs.GetLayerData((GenerationStage)index), context.GetLayerData((GenerationStage)index)), "Inputs copied or replaced prior buffers.");
            Throws<InvalidOperationException>(() => inputs.GetLayerData(inputs.Stage));
        }
        Require(typeof(GenerationLayerInputs).GetConstructors().Length == 0 &&
            typeof(GenerationLayerInputs).GetProperties().All(property => property.SetMethod is null && property.PropertyType != typeof(GenerationContext)),
            "Inputs expose mutation or context access.");
    }

    private static void SourceIsolation()
    {
        var origins = new List<int[]>();
        var orchestrator = Create(inputs =>
        {
            foreach (var origin in origins) origin[0] = -999;
            int[] values = [(int)inputs.Stage];
            origins.Add(values);
            var candidate = new LayerData<int>(inputs.Stage, values);
            values[0] = -999;
            return LayerExecutionResult.Success(candidate);
        });
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        foreach (var stage in Enum.GetValues<GenerationStage>())
            Require(((LayerData<int>)context.GetLayerData(stage)!)[0] == (int)stage, "Source mutation escaped into publication.");
    }

    private static void RunningRejection()
    {
        int calls = 0;
        var orchestrator = Create(inputs => { calls++; return FakeWorldGenerationLayers.Sample(inputs); });
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var context = orchestrator.CreateGeneration(Request());
            context.MarkRunning(stage);
            foreach (var operation in Operations(orchestrator, context)) Throws<InvalidOperationException>(operation);
            Require(context.GetProgress().GetStatus(stage) == GenerationStageStatus.Running && context.GetLayerData(stage) is null,
                "Rejected Running operation changed context.");
        }
        Require(calls == 0, "Running guard invoked an executor.");
    }

    private static void ReentrantRejection()
    {
        GenerationContext context = null!;
        WorldGenerationOrchestrator orchestrator = null!;
        orchestrator = Create(inputs =>
        {
            foreach (var operation in Operations(orchestrator, context)) Throws<InvalidOperationException>(operation);
            Require(context.GetProgress().GetStatus(inputs.Stage) == GenerationStageStatus.Running, "Reentrant rejection damaged active stage.");
            return FakeWorldGenerationLayers.Sample(inputs);
        });
        context = orchestrator.CreateGeneration(Request());
        Require(orchestrator.RunAll(context).LastCompletedStage == GenerationStage.Biomes, "Outer execution did not complete.");
    }

    private static void InvalidArguments()
    {
        Throws<ArgumentNullException>(() => new WorldGenerationOrchestrator(null!));
        var orchestrator = Create();
        Throws<ArgumentNullException>(() => orchestrator.CreateGeneration(null!));
        Throws<ArgumentNullException>(() => orchestrator.RunNext(null!));
        Throws<ArgumentNullException>(() => orchestrator.RunAll(null!));
        Throws<ArgumentNullException>(() => orchestrator.RunUntil(null!, GenerationStage.Grid));
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunNext(context);
        var grid = context.GetLayerData(GenerationStage.Grid);
        foreach (int unknown in new[] { -1, 8, int.MaxValue })
            Throws<ArgumentOutOfRangeException>(() => orchestrator.RunUntil(context, (GenerationStage)unknown));
        Require(ReferenceEquals(grid, context.GetLayerData(GenerationStage.Grid)) && context.GetProgress().NextStage == GenerationStage.Tectonics,
            "Invalid target mutated context.");
    }

    private static void ResultContract()
    {
        Throws<ArgumentNullException>(() => LayerExecutionResult.Success(null!));
        foreach (var diagnostic in new[] { null, "", " " }) Throws<ArgumentException>(() => LayerExecutionResult.Failed(diagnostic!));
        var candidate = new LayerData<int>(GenerationStage.Grid, [1]);
        var success = LayerExecutionResult.Success(candidate);
        var failed = LayerExecutionResult.Failed("Sample failure.");
        Require(ReferenceEquals(success.Candidate, candidate) && success.Failure is null && failed.Candidate is null && failed.Failure == "Sample failure.",
            "Outcome allows ambiguous publication.");
    }

    private static void IndependentContexts()
    {
        var orchestrator = Create();
        var first = orchestrator.CreateGeneration(Request());
        var second = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(first);
        Require(second.GetProgress().NextStage == GenerationStage.Grid && second.GetLayerData(GenerationStage.Grid) is null, "Context execution leaked.");
        orchestrator.RunNext(second);
        Require(second.GetProgress().NextStage == GenerationStage.Tectonics && first.GetProgress().LastCompletedStage == GenerationStage.Biomes,
            "Context progress leaked.");
    }

    private static void NoImplicitRebuild()
    {
        var orchestrator = Create();
        var context = orchestrator.CreateGeneration(Request());
        orchestrator.RunAll(context);
        var biomes = context.GetLayerData(GenerationStage.Biomes);
        context.InvalidateStage(GenerationStage.Grid);
        foreach (var operation in Operations(orchestrator, context)) Throws<InvalidOperationException>(operation);
        Require(ReferenceEquals(biomes, context.GetLayerData(GenerationStage.Biomes)) && context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Invalidated,
            "Implicit reconstruction or invalidation occurred.");
    }

    private static void DeferredScope()
    {
        var types = typeof(WorldGenerationOrchestrator).Assembly.GetExportedTypes();
        Require(!types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Any(method => method.Name is "RebuildFrom" or "FinalizeWorld"), "CORE-04/05 operation introduced.");
        Require(!types.Any(type => type.Name.Contains("Random", StringComparison.OrdinalIgnoreCase) || type.Name.Contains("Prng", StringComparison.OrdinalIgnoreCase) ||
            type.Name == "WorldGenerationResult"), "Random streams or finalized world introduced.");
    }

    // Exercise each public forward mode, including a target before an active stage.
    private static Action[] Operations(WorldGenerationOrchestrator orchestrator, GenerationContext context) =>
    [
        () => orchestrator.RunNext(context), () => orchestrator.RunAll(context),
        () => orchestrator.RunUntil(context, GenerationStage.Biomes), () => orchestrator.RunUntil(context, GenerationStage.Grid)
    ];

    private static void ExecuteMode(WorldGenerationOrchestrator orchestrator, GenerationContext context, int mode)
    {
        if (mode == 0) orchestrator.RunNext(context);
        else if (mode == 1) orchestrator.RunAll(context);
        else orchestrator.RunUntil(context, GenerationStage.Biomes);
    }

    // Wrong exception kinds cannot satisfy a rejection assertion.
    private static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
