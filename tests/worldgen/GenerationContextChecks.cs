// Verifies CORE-02 context metadata, storage transitions and immutable observations.
// Calls restricted internal preparation methods directly without simulating an orchestrator or executing layers.

using System.Collections;
using System.Reflection;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class GenerationContextChecks
{
    // Keep context checks separate so the accepted CORE-01 suite remains unchanged.
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("CORE-02: lifecycle contains only the five approved states", LifecycleIdentities),
            ("CORE-02: all eight stages start Pending with no published data", InitialState),
            ("CORE-02: request metadata and public context surface are read-only", ImmutableMetadata),
            ("CORE-02: each stage retains only its own published data", StageOwnership),
            ("CORE-02: mismatched publication leaves both slots unchanged", MismatchedOwnership),
            ("CORE-02: incomplete and failed data are unavailable", UnavailableData),
            ("CORE-02: invalidated data are unavailable without an automatic cascade", InvalidatedData),
            ("CORE-02: mutating source arrays and lists cannot change published values", SourceIsolation),
            ("CORE-02: published collections expose no mutable container or array", ConsumerIsolation),
            ("CORE-02: heterogeneous buffers retain their exact value types", TypedStorage),
            ("CORE-02: progress observations remain unchanged after later transitions", ProgressSnapshots),
            ("CORE-02: progress derives its contiguous prefix from stage storage", ContiguousProgress),
            ("CORE-02: failure details are immutable and local to their stage", FailureDetails),
            ("CORE-02: invalid storage transitions preserve existing state and data", RejectedTransitions),
            ("CORE-02: invalid arguments cannot modify context state", InvalidArguments),
            ("CORE-02: independent contexts cannot share mutable metadata or slots", IndependentContexts),
            ("CORE-02: publication and inspection reuse immutable stage buffers", NoWorldCopies),
            ("CORE-02: internal access is granted only to the named headless checks", RestrictedTestAccess),
            ("CORE-02: execution stays outside context and future operations remain absent", DeferredExecution)
        ];

        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try
            {
                check();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
            }
        }
        Console.WriteLine($"CORE-02: {checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    // A context remains a temporary holder for the already accepted request contract.
    private static GenerationContext CreateContext() =>
        new(new GenerationRequest(-42, new WorldGenerationSettings(WorldType.Archipelagos, MapSize.Medium)));

    // Stable lifecycle values exclude Blocked, which is a future operation rejection rather than stored state.
    private static void LifecycleIdentities()
    {
        Require(Enum.GetNames<GenerationStageStatus>().SequenceEqual(["Pending", "Running", "Completed", "Failed", "Invalidated"]),
            "Unexpected lifecycle state.");
        Require(Enum.GetValues<GenerationStageStatus>().Select(status => (int)status).SequenceEqual(Enumerable.Range(0, 5)),
            "Lifecycle numeric identities changed.");
    }

    // Inspect every initial slot without asking a layer or orchestrator to run.
    private static void InitialState()
    {
        var context = CreateContext();
        var progress = context.GetProgress();
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            Require(progress.GetStatus(stage) == GenerationStageStatus.Pending, $"{stage} did not start Pending.");
            Require(progress.GetFailure(stage) is null && context.GetLayerData(stage) is null, "Initial data or diagnostics leaked.");
        }
        Require(progress.LastCompletedStage is null && progress.NextStage == GenerationStage.Grid, "Initial progress is incorrect.");
    }

    // Get-only properties and a narrow inspection surface keep arbitrary mutation out of public callers.
    private static void ImmutableMetadata()
    {
        var context = CreateContext();
        Require(context.Request.Seed == -42 && context.Request.Settings.WorldType == WorldType.Archipelagos &&
            context.Request.Settings.MapSize == MapSize.Medium, "Context changed request metadata.");
        foreach (var type in new[] { typeof(GenerationContext), typeof(GenerationRequest), typeof(WorldGenerationSettings), typeof(GenerationProgress) })
            Require(type.GetProperties().All(property => property.SetMethod is null), $"Public mutation on {type.Name}.");

        var methods = typeof(GenerationContext).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName).Select(method => method.Name).Order();
        Require(methods.SequenceEqual(["GetLayerData", "GetProgress"]), "Context exposes a public mutator or execution method.");
    }

    // Direct per-slot preparation demonstrates ownership, not prerequisite eligibility or generation execution.
    private static void StageOwnership()
    {
        var context = CreateContext();
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            context.MarkRunning(stage);
            context.Publish(stage, new LayerData<int>(stage, [(int)stage]));
        }
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var data = context.GetLayerData(stage) as LayerData<int>;
            Require(data?.Stage == stage && data.Count == 1 && data[0] == (int)stage, $"{stage} lost its exclusive data.");
        }
    }

    // A payload's immutable stage identity must match the slot selected by the engine caller.
    private static void MismatchedOwnership()
    {
        var context = CreateContext();
        context.MarkRunning(GenerationStage.Grid);
        var wrongOwner = new LayerData<int>(GenerationStage.Tectonics, [10]);
        Throws<ArgumentException>(() => context.Publish(GenerationStage.Grid, wrongOwner));
        Require(context.GetLayerData(GenerationStage.Grid) is null && context.GetLayerData(GenerationStage.Tectonics) is null,
            "Rejected ownership published data.");
        var progress = context.GetProgress();
        Require(progress.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Running &&
            progress.GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Pending, "Rejected ownership changed stage state.");
    }

    // Constructing a candidate is insufficient to make it available through the context.
    private static void UnavailableData()
    {
        var context = CreateContext();
        var candidate = new LayerData<int>(GenerationStage.Grid, [2, 3]);
        Require(context.GetLayerData(candidate.Stage) is null, "Unpublished candidate is visible.");
        context.MarkRunning(candidate.Stage);
        Require(context.GetLayerData(candidate.Stage) is null, "Running data is visible.");
        context.MarkFailed(candidate.Stage, "Sample validation failure.");
        Require(context.GetLayerData(candidate.Stage) is null, "Failed data is visible.");
    }

    // Single-slot discard is a storage primitive; operational downstream invalidation is still CORE-04.
    private static void InvalidatedData()
    {
        var context = CreateContext();
        context.MarkRunning(GenerationStage.Grid);
        context.Publish(GenerationStage.Grid, new LayerData<int>(GenerationStage.Grid, [1]));
        context.MarkRunning(GenerationStage.Tectonics);
        var other = new LayerData<int>(GenerationStage.Tectonics, [2]);
        context.Publish(GenerationStage.Tectonics, other);
        context.InvalidateStage(GenerationStage.Grid);
        Require(context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Invalidated &&
            context.GetLayerData(GenerationStage.Grid) is null, "Invalidated slot remained available.");
        Require(ReferenceEquals(context.GetLayerData(GenerationStage.Tectonics), other), "Storage discard performed an unauthorized cascade.");
        context.MarkRunning(GenerationStage.Grid);
        Require(context.GetLayerData(GenerationStage.Grid) is null, "Discarded data reappeared during preparation.");
    }

    // Copying one unmanaged buffer isolates both scalar arrays and structured value lists.
    private static void SourceIsolation()
    {
        var context = CreateContext();
        int[] source = [4, 8];
        var data = new LayerData<int>(GenerationStage.Grid, source);
        source[0] = 90;
        context.MarkRunning(GenerationStage.Grid);
        context.Publish(GenerationStage.Grid, data);
        source[1] = 91;
        Require(data.Values.SequenceEqual([4, 8]), "Source array changed captured values.");

        var values = new List<FakeLayerValue> { new(1, 3.5), new(2, 7.5) };
        var structured = new LayerData<FakeLayerValue>(GenerationStage.Climate, values);
        context.MarkRunning(GenerationStage.Climate);
        context.Publish(GenerationStage.Climate, structured);
        values[0] = new(99, 99);
        values.Clear();
        Require(structured.Count == 2 && structured[0] == new FakeLayerValue(1, 3.5), "Source list changed published values.");
    }

    // The value view deliberately avoids IList/ICollection wrappers that could expose a mutable backing container.
    private static void ConsumerIsolation()
    {
        var data = new LayerData<FakeLayerValue>(GenerationStage.Grid, [new(1, 2.5)]);
        Require(data.Values is not IList<FakeLayerValue> && data.Values is not ICollection && data.Values is not FakeLayerValue[],
            "Mutable collection or backing array is exposed.");
        FakeLayerValue copy = data.Values[0];
        copy = copy with { Sample = 99 };
        Require(data.Values[0].Sample == 2.5, "Mutating a retrieved value copy changed storage.");
        Require(((IEnumerable)data).Cast<FakeLayerValue>().Single().Sample == 2.5, "Non-generic iteration changed values.");
        Throws<ArgumentException>(() => typeof(LayerData<>).MakeGenericType(typeof(List<int>)));
        Require(typeof(LayerData).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .All(constructor => constructor.IsFamilyAndAssembly), "External mutable subclasses are not restricted.");
    }

    // Different stages may own different unmanaged value types without an object payload escape hatch.
    private static void TypedStorage()
    {
        var context = CreateContext();
        context.MarkRunning(GenerationStage.Grid);
        context.Publish(GenerationStage.Grid, new LayerData<int>(GenerationStage.Grid, [17]));
        context.MarkRunning(GenerationStage.Landforms);
        context.Publish(GenerationStage.Landforms, new LayerData<FakeLayerValue>(GenerationStage.Landforms, [new(3, 4.5)]));
        Require(context.GetLayerData(GenerationStage.Grid) is LayerData<int> integers && integers[0] == 17, "Scalar type was lost.");
        Require(context.GetLayerData(GenerationStage.Landforms) is LayerData<FakeLayerValue> values && values[0].Identifier == 3,
            "Structured value type was lost.");
    }

    // Observations are immutable snapshots; callers cannot write progress back into the context.
    private static void ProgressSnapshots()
    {
        var context = CreateContext();
        var before = context.GetProgress();
        context.MarkRunning(GenerationStage.Grid);
        var running = context.GetProgress();
        context.Publish(GenerationStage.Grid, new LayerData<int>(GenerationStage.Grid, [7]));
        var completed = context.GetProgress();
        Require(before.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Pending && before.NextStage == GenerationStage.Grid,
            "Initial observation was changed retroactively.");
        Require(running.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Running && running.NextStage is null,
            "Running observation was changed or offered another stage.");
        Require(completed.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Completed &&
            completed.LastCompletedStage == GenerationStage.Grid && completed.NextStage == GenerationStage.Tectonics,
            "Completed observation is inconsistent.");
        context.InvalidateStage(GenerationStage.Grid);
        Require(completed.GetStatus(GenerationStage.Grid) == GenerationStageStatus.Completed, "Historical observation was mutated.");
    }

    // Completed slots beyond a gap must not be counted as a valid completed prefix.
    private static void ContiguousProgress()
    {
        var context = CreateContext();
        context.MarkRunning(GenerationStage.Biomes);
        context.Publish(GenerationStage.Biomes, new LayerData<int>(GenerationStage.Biomes, [8]));
        var progress = context.GetProgress();
        Require(progress.LastCompletedStage is null && progress.NextStage == GenerationStage.Grid, "Progress skipped an incomplete prefix.");
        context.MarkRunning(GenerationStage.Grid);
        context.MarkFailed(GenerationStage.Grid, "Sample failure.");
        Require(context.GetProgress().NextStage == GenerationStage.Grid, "Failed prefix slot was skipped.");
    }

    // Failure strings are captured by value semantics and cleared only by a new preparation transition.
    private static void FailureDetails()
    {
        var context = CreateContext();
        context.MarkRunning(GenerationStage.Climate);
        context.MarkFailed(GenerationStage.Climate, "Sample diagnostic.");
        var failed = context.GetProgress();
        Require(failed.GetFailure(GenerationStage.Climate) == "Sample diagnostic." && failed.GetFailure(GenerationStage.Grid) is null,
            "Failure information escaped its owner.");
        context.MarkRunning(GenerationStage.Climate);
        Require(context.GetProgress().GetFailure(GenerationStage.Climate) is null &&
            failed.GetFailure(GenerationStage.Climate) == "Sample diagnostic.", "Failure snapshots do not remain isolated.");
    }

    // Only the narrow storage lifecycle can change a slot; rejected writes leave published data untouched.
    private static void RejectedTransitions()
    {
        var context = CreateContext();
        var data = new LayerData<int>(GenerationStage.Grid, [1]);
        Throws<InvalidOperationException>(() => context.Publish(GenerationStage.Grid, data));
        Throws<InvalidOperationException>(() => context.MarkFailed(GenerationStage.Grid, "Failure."));
        Throws<InvalidOperationException>(() => context.InvalidateStage(GenerationStage.Grid));
        context.MarkRunning(GenerationStage.Grid);
        Throws<InvalidOperationException>(() => context.MarkRunning(GenerationStage.Grid));
        Throws<InvalidOperationException>(() => context.InvalidateStage(GenerationStage.Grid));
        context.Publish(GenerationStage.Grid, data);
        Throws<InvalidOperationException>(() => context.Publish(GenerationStage.Grid, new LayerData<int>(GenerationStage.Grid, [2])));
        Throws<InvalidOperationException>(() => context.MarkRunning(GenerationStage.Grid));
        Throws<InvalidOperationException>(() => context.MarkFailed(GenerationStage.Grid, "Failure."));
        Require(ReferenceEquals(context.GetLayerData(GenerationStage.Grid), data) &&
            context.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Completed, "Rejected write corrupted published data.");
    }

    // Reject invalid enum casts and candidates before replacing authoritative state records.
    private static void InvalidArguments()
    {
        Throws<ArgumentNullException>(() => new GenerationContext(null!));
        Throws<ArgumentNullException>(() => new LayerData<int>(GenerationStage.Grid, null!));
        foreach (int value in new[] { -1, 8, int.MaxValue })
        {
            var stage = (GenerationStage)value;
            var context = CreateContext();
            Throws<ArgumentOutOfRangeException>(() => context.GetLayerData(stage));
            Throws<ArgumentOutOfRangeException>(() => context.GetProgress().GetStatus(stage));
            Throws<ArgumentOutOfRangeException>(() => context.GetProgress().GetFailure(stage));
            Throws<ArgumentOutOfRangeException>(() => context.MarkRunning(stage));
            Throws<ArgumentOutOfRangeException>(() => context.Publish(stage, new LayerData<int>(GenerationStage.Grid, [1])));
            Throws<ArgumentOutOfRangeException>(() => context.MarkFailed(stage, "Failure."));
            Throws<ArgumentOutOfRangeException>(() => context.InvalidateStage(stage));
            Throws<ArgumentOutOfRangeException>(() => new LayerData<int>(stage, [1]));
        }
        var running = CreateContext();
        running.MarkRunning(GenerationStage.Grid);
        Throws<ArgumentNullException>(() => running.Publish(GenerationStage.Grid, null!));
        Throws<ArgumentException>(() => running.MarkFailed(GenerationStage.Grid, " "));
        Require(running.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Running &&
            running.GetLayerData(GenerationStage.Grid) is null, "Rejected argument changed the slot.");
    }

    // Sharing immutable request metadata does not share state tables between generation sessions.
    private static void IndependentContexts()
    {
        var first = CreateContext();
        var second = new GenerationContext(first.Request);
        first.MarkRunning(GenerationStage.Grid);
        first.Publish(GenerationStage.Grid, new LayerData<int>(GenerationStage.Grid, [3]));
        Require(second.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Pending &&
            second.GetLayerData(GenerationStage.Grid) is null, "Contexts share mutable slot state.");
    }

    // Snapshotting progress and publishing another stage must not duplicate previously owned output buffers.
    private static void NoWorldCopies()
    {
        var context = CreateContext();
        var data = new LayerData<int>(GenerationStage.Grid, [1, 2]);
        context.MarkRunning(GenerationStage.Grid);
        context.Publish(GenerationStage.Grid, data);
        context.GetProgress();
        context.MarkRunning(GenerationStage.Biomes);
        context.Publish(GenerationStage.Biomes, new LayerData<byte>(GenerationStage.Biomes, []));
        Require(ReferenceEquals(context.GetLayerData(GenerationStage.Grid), data) &&
            ReferenceEquals(context.GetLayerData(GenerationStage.Grid), context.GetLayerData(GenerationStage.Grid)),
            "Inspection or another stage copied immutable output data.");
    }

    // The named friend assembly is the only explicit exception to normal internal visibility.
    private static void RestrictedTestAccess()
    {
        var friends = typeof(GenerationContext).Assembly
            .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName).ToArray();
        Require(friends.SequenceEqual([typeof(GenerationContextChecks).Assembly.GetName().Name!]), "Unexpected friend assembly grant.");
    }

    // Data preparation cannot accidentally become a second execution path or pull window dependencies into the core.
    private static void DeferredExecution()
    {
        var core = typeof(GenerationContext).Assembly;
        Require(!typeof(GenerationContext).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(method => method.Name == "FinalizeWorld"), "Finalization must remain outside context.");
        Require(core.GetReferencedAssemblies().All(reference => reference.Name?.StartsWith("System", StringComparison.Ordinal) == true ||
            reference.Name == typeof(WorldGenerationSettings).Assembly.GetName().Name), "Context introduced a graphics or UI dependency.");
    }

    // Distinguish expected validation failures from accidental exceptions.
    private static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    // Propagate each violated invariant into the executable's nonzero result.
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
