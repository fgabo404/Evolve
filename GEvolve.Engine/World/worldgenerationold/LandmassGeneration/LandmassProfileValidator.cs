using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Evaluates geographic landmass profiles without orchestrating world generation.</summary>
public static class LandmassProfileValidator
{
    public static bool MeetsProfile(LandmassType type, LandmassStatistics result)
        => type switch
        {
            LandmassType.Pangaea => result.LandFraction is >= .30 and <= .55 &&
                result.ComponentCount == 1 && result.DominantLandShare == 1,
            LandmassType.SmallContinents => result.LandFraction is >= .23 and <= .38 &&
                result.ComponentCount >= 8 && result.DominantLandShare < .23,
            LandmassType.Archipelagos => result.LandFraction is >= .14 and <= .27 &&
                result.ComponentCount >= 20 && result.DominantLandShare < .12 &&
                result.SmallComponents * 2 >= result.ComponentCount,
            _ => false
        };
}
