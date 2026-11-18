// Coordinates the fixed world-generation pipeline through one synchronous stage runner.
// Enforces prerequisites and publishes validated candidates without exposing context mutation to layers.

namespace Evolve.Engine.WorldGeneration;

public sealed class WorldGenerationOrchestrator
{
    private readonly WorldGenerationLayerRegistry registry;

    public WorldGenerationOrchestrator(WorldGenerationLayerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.registry = registry;
    }

    // Creating a draft preserves immutable metadata and executes no layer.
    public GenerationContext CreateGeneration(GenerationRequest request) => new(request);

    // The Laboratory advances precisely one eligible stage, including a retry after failure.
    public GenerationProgress RunNext(GenerationContext context) => Advance(context, GenerationStage.Biomes, true);

    // All forward modes share validation, invocation and publication through Advance.
    public GenerationProgress RunAll(GenerationContext context) => Advance(context, GenerationStage.Biomes, false);

    // A target limits forward progress; it never selects a layer out of sequence.
    public GenerationProgress RunUntil(GenerationContext context, GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        return Advance(context, stage, false);
    }

    // Synchronous and reentrant Running guards are enforced here; full concurrency policy belongs to CORE-04.
    private GenerationProgress Advance(GenerationContext context, GenerationStage target, bool singleStage)
    {
        ArgumentNullException.ThrowIfNull(context);
        while (true)
        {
            var progress = context.GetProgress();
            ValidateProgress(context, progress);
            if (progress.NextStage is not GenerationStage stage || stage > target)
                return progress;

            var inputs = new GenerationLayerInputs(context, stage);
            context.MarkRunning(stage);
            try
            {
                var layer = registry.GetLayer(stage);
                if (layer.Stage != stage)
                    throw new InvalidOperationException("Registered layer identity changed.");
                var result = layer.Execute(inputs)
                    ?? throw new InvalidOperationException("Layer returned no execution result.");
                if (result.Failure is string diagnostic)
                    context.MarkFailed(stage, diagnostic);
                else
                {
                    var candidate = result.Candidate
                        ?? throw new InvalidOperationException("Layer returned no candidate data.");
                    if (candidate.Stage != stage)
                        throw new InvalidOperationException("Candidate belongs to a different generation stage.");
                    context.Publish(stage, candidate);
                }
            }
            catch (Exception exception)
            {
                // Failure replaces only the active slot; previous immutable buffers remain untouched.
                context.MarkFailed(stage, $"{exception.GetType().Name}: {exception.Message}");
            }

            progress = context.GetProgress();
            if (singleStage || progress.GetStatus(stage) == GenerationStageStatus.Failed)
                return progress;
        }
    }

    // Reject inconsistent drafts before mutation rather than skipping gaps or implicitly rebuilding them.
    private static void ValidateProgress(GenerationContext context, GenerationProgress progress)
    {
        bool unfinished = false;
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var status = progress.GetStatus(stage);
            if (status == GenerationStageStatus.Running)
                throw new InvalidOperationException("A generation stage is already running.");
            if (status == GenerationStageStatus.Invalidated)
                throw new InvalidOperationException("Rebuilding invalidated stages is not implemented.");
            if (status == GenerationStageStatus.Completed)
            {
                if (unfinished || context.GetLayerData(stage)?.Stage != stage)
                    throw new InvalidOperationException("Completed stages must form a valid prerequisite prefix.");
            }
            else
                unfinished = true;
        }
    }
}
