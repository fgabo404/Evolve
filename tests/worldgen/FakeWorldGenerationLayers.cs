// Supplies eight test-only executors for registration and orchestrator checks.
// Produces neutral immutable sample buffers or scenario-specific callbacks, never geographic algorithms.

using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class FakeWorldGenerationLayers
{
    // Fresh instances keep membership and execution behavior isolated between checks.
    public static IWorldGenerationLayer[] Create(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) =>
    [
        new FakeGridLayer(execute: execute),
        new FakeTectonicsLayer(execute),
        new FakeLandmassesLayer(execute),
        new FakeElevationLayer(execute),
        new FakeLandformsLayer(execute),
        new FakeHydrologyLayer(execute),
        new FakeClimateLayer(execute),
        new FakeBiomesLayer(execute)
    ];

    // Values identify the executed stage only; they model no geographical data.
    public static LayerExecutionResult Sample(GenerationLayerInputs inputs) =>
        LayerExecutionResult.Success(new LayerData<int>(inputs.Stage, [(int)inputs.Stage]));
}

// Represents Grid in headless checks without implementing its real generator.
internal sealed class FakeGridLayer(GenerationStage stage = GenerationStage.Grid, Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage { get; } = stage;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Tectonics in headless checks without implementing its real generator.
internal sealed class FakeTectonicsLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Tectonics;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Landmasses in headless checks without implementing its real generator.
internal sealed class FakeLandmassesLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Landmasses;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Elevation in headless checks without implementing its real generator.
internal sealed class FakeElevationLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Elevation;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Landforms in headless checks without implementing its real generator.
internal sealed class FakeLandformsLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Landforms;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Hydrology in headless checks without implementing its real generator.
internal sealed class FakeHydrologyLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Hydrology;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Climate in headless checks without implementing its real generator.
internal sealed class FakeClimateLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Climate;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}

// Represents Biomes in headless checks without implementing its real generator.
internal sealed class FakeBiomesLayer(Func<GenerationLayerInputs, LayerExecutionResult>? execute = null) : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Biomes;

    public LayerExecutionResult Execute(GenerationLayerInputs inputs) =>
        (execute ?? FakeWorldGenerationLayers.Sample)(inputs);
}
