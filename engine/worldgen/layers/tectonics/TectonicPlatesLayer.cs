// Executes the real ownership-only Tectonics stage after validating the completed Grid descriptor.
// Returns one immutable candidate and leaves publication, progress, retries and invalidation to WG-CORE.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

public sealed class TectonicPlatesLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Tectonics;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Stage != Stage) throw new ArgumentException("Tectonics execution requires Tectonics inputs.", nameof(inputs));
        if (inputs.GetLayerData(GenerationStage.Grid) is not LayerData<HexGridDescriptor> gridData ||
            gridData.Count != 1 || gridData[0].Size != inputs.Request.Settings.MapSize)
            return LayerExecutionResult.Failed("Tectonics requires one Grid descriptor matching the request.");
        var grid = gridData[0].ToGridData();
        // Reserve motion's stream now without generating motion or changing later partition consumption.
        ulong partitionSeed = inputs.Random.NextUInt64(), noiseSeed = inputs.Random.NextUInt64();
        _ = inputs.Random.NextUInt64();
        var profile = TectonicGenerationProfile.Approved;
        var draft = TectonicPartitionGenerator.Generate(grid, new(partitionSeed), new(noiseSeed), profile);
        try { return LayerExecutionResult.Success(TectonicData.CreateCandidate(grid, draft, profile)); }
        catch (ArgumentException exception) { return LayerExecutionResult.Failed($"Invalid tectonic partition: {exception.Message}"); }
    }
}
