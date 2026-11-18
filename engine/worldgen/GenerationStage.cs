// Defines stable identities for the approved eight-stage generation pipeline.
// Numeric values express canonical registration order, not execution eligibility or progress.

namespace Evolve.Engine.WorldGeneration;

public enum GenerationStage
{
    Grid = 0,
    Tectonics = 1,
    Landmasses = 2,
    Elevation = 3,
    Landforms = 4,
    Hydrology = 5,
    Climate = 6,
    Biomes = 7
}
