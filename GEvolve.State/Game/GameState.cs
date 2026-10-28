using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Definitions.Versioning;

namespace GEvolve.State.Game;

/// <summary>Per-save mutable data; JSON catalog definitions remain external/read-only.</summary>
public sealed class GameState
{
    public int SaveFormatVersion { get; set; } = EvolveVersion.SaveFormat;
    public string DefinitionSetVersion { get; set; } = "prototype-unversioned";
    public long Turn { get; set; }
    public WorldSettings WorldSettings { get; set; } = new();
    public HexGridState? HexGrid { get; set; }
    public LandmassState? Landmass { get; set; }
    public Dictionary<string, CivilizationState> Civilizations { get; set; } = new();
    public Dictionary<string, RegionState> Regions { get; set; } = new();
}

public sealed class CivilizationState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, double> ResourcesByDefinitionId { get; set; } = new();
    public HashSet<string> DiscoveredTechnologyIds { get; set; } = new();
}

public sealed class RegionState
{
    public string Id { get; set; } = "";
    public string? OwnerCivilizationId { get; set; }
}
