namespace GEvolve.State.Game;

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
