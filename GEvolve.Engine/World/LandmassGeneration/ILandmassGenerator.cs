using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

public interface ILandmassGenerator
{
    LandmassType Type { get; }
    byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt);
}
