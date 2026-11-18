// Captures a completed generation as immutable metadata and eight stage-owned buffer references.
// Remains independent of later context rebuilds without copying world data or updating active game state.

namespace Evolve.Engine.WorldGeneration;

public sealed class WorldGenerationResult
{
    private readonly LayerData[] layers;
    public GenerationRequest Request { get; }
    public string RandomAlgorithmVersion { get; } = DeterministicStageRandom.AlgorithmVersion;

    // Own the small reference table; only the orchestrator constructs results from a leased context.
    internal WorldGenerationResult(GenerationRequest request, IEnumerable<LayerData> layers)
    {
        Request = request;
        this.layers = layers.ToArray();
        var stages = Enum.GetValues<GenerationStage>();
        if (this.layers.Length != stages.Length ||
            stages.Any(stage => this.layers[(int)stage]?.Stage != stage))
            throw new InvalidOperationException("Finalization requires eight correctly owned layer results.");
    }

    // Return the immutable historical buffer, never a live context lookup or the private reference table.
    public LayerData GetLayerData(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        return layers[(int)stage];
    }
}
