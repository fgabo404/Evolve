// Produces the single immutable Grid descriptor candidate for the existing generation orchestrator.
// Leaves publication, progress and failure handling to WG-CORE and never consumes the supplied random stream.

namespace Evolve.Engine.WorldGeneration.Layers.Grid;

public sealed class HexGridLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Grid;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Stage != Stage)
            throw new ArgumentException("Grid execution requires Grid inputs.", nameof(inputs));
        var descriptor = new HexGridDescriptor(inputs.Request.Settings.MapSize);
        return LayerExecutionResult.Success(new LayerData<HexGridDescriptor>(Stage, [descriptor]));
    }
}
