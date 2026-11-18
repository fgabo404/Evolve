// Provides fixed request metadata, read-only prerequisite results and an isolated random stream to one layer.
// Captures only earlier buffer references, never the mutable generation context or a full world copy.

namespace Evolve.Engine.WorldGeneration;

public sealed class GenerationLayerInputs
{
    private readonly LayerData[] previousResults;
    public GenerationRequest Request { get; }
    public GenerationStage Stage { get; }
    public DeterministicStageRandom Random { get; }

    // The orchestrator validates prerequisites before constructing this restricted view.
    internal GenerationLayerInputs(GenerationContext context, GenerationStage stage)
    {
        Request = context.Request;
        Stage = stage;
        Random = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(Request.Seed, stage));
        previousResults = new LayerData[(int)stage];
        for (int index = 0; index < previousResults.Length; index++)
            previousResults[index] = context.GetLayerData((GenerationStage)index)
                ?? throw new InvalidOperationException("A prerequisite result is unavailable.");
    }

    // Own-stage and future-stage data are unavailable even if the input view is retained after execution.
    public LayerData GetLayerData(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        if (stage >= Stage)
            throw new InvalidOperationException("A layer may read only completed earlier stages.");
        return previousResults[(int)stage];
    }
}
