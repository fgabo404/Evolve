using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Game;

/// <summary>Una marca por ID de la Capa 01: 0 océano, 1 tierra.</summary>
public sealed record LandmassState(LandmassConfiguration Configuration,
    byte[] SurfaceByCellId, int DeterministicAttempt)
{
    public byte[]? ElevationByCellId { get; init; }
    public byte[]? SlopeByCellId { get; init; }
    public byte[]? PlateByCellId { get; init; }
    public BiomeState? Biomes { get; init; }
    public ClimateState? Climate { get; init; }
    public bool IsLand(int cellId) => SurfaceByCellId[cellId] == 1;
    /// <summary>Condición consultable desde el estado persistido, sin duplicar alturas.</summary>
    public bool IsImpassableSummit(int cellId) => IsLand(cellId) &&
        ElevationByCellId is { } heights &&
        heights[cellId] >= TerrainRules.ImpassableSummitMinimumElevation;
}
