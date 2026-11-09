using GEvolve.Engine.Grid;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed record GeneratedWorld(HexGrid Grid, LandmassState Landmass,
    LandmassStatistics? Statistics)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public PangaeaElevationDiagnostics? PangaeaElevations { get; init; }
}
