// Supplies eight test-only layer implementations for registry validation.
// They expose metadata only and never execute generation or simulate a functional context.

using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class FakeWorldGenerationLayers
{
    // Build fresh registrations so each scenario can vary its own input membership.
    public static IWorldGenerationLayer[] Create() =>
    [
        new FakeGridLayer(), new FakeTectonicsLayer(), new FakeLandmassesLayer(),
        new FakeElevationLayer(), new FakeLandformsLayer(), new FakeHydrologyLayer(),
        new FakeClimateLayer(), new FakeBiomesLayer()
    ];
}

// The override allows malformed registration cases without adding production fake types.
internal sealed class FakeGridLayer(GenerationStage stage = GenerationStage.Grid) : IWorldGenerationLayer
{
    public GenerationStage Stage { get; } = stage;
}

// Represents the Tectonics registration without any tectonic algorithm.
internal sealed class FakeTectonicsLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Tectonics;
}

// Represents the Landmasses registration without geographic outputs.
internal sealed class FakeLandmassesLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Landmasses;
}

// Represents the Elevation registration without heights or terrain.
internal sealed class FakeElevationLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Elevation;
}

// Represents the Landforms registration without feature classification.
internal sealed class FakeLandformsLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Landforms;
}

// Represents the Hydrology registration without drainage or water data.
internal sealed class FakeHydrologyLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Hydrology;
}

// Represents the Climate registration without environmental calculations.
internal sealed class FakeClimateLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Climate;
}

// Represents the Biomes registration without biome generation or finalization.
internal sealed class FakeBiomesLayer : IWorldGenerationLayer
{
    public GenerationStage Stage => GenerationStage.Biomes;
}
