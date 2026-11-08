using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

// Owned intermediate calculation, never a persisted world or an editable public profile.
internal sealed record PangaeaSurfaceWork(HexTopology Topology, LandmassConfiguration Configuration,
    int Attempt, byte[] Surface, double[] InitialUplift)
{
    public PangaeaBaseFields? Fields { get; init; }
}
