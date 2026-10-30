using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Tectonics;

public static class PlateMotionGenerator
{
    public static PlateMotion[] Generate(int seed, TectonicMotionSettings? settings = null)
    {
        settings ??= new();
        settings.Validate();
        // Separate random stream; never consumes the plate or landmass generators' RNG.
        var random = new DeterministicRandom(seed, 0x4D4F5431);
        var result = new PlateMotion[TectonicPlateGenerator.NumberOfPlates];
        for (byte id = 0; id < result.Length; id++)
            result[id] = new(id, random.NextDouble() * 360,
                settings.MinimumSpeedCmPerYear + random.NextDouble() *
                (settings.MaximumSpeedCmPerYear - settings.MinimumSpeedCmPerYear));
        return result;
    }
}
