using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Stage 02 surface generator which consumes the universal plates.</summary>
public interface IPlateAwareLandmassGenerator : ILandmassGenerator
{
    byte[] Generate(HexTopology topology, LandmassConfiguration configuration,
        int attempt, ReadOnlySpan<byte> plates);
}
