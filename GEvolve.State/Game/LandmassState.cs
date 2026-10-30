using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Game;

/// <summary>Terreno final: 0 océano, 1 tierra. Durante la vista de placas es un placeholder sin clasificación.</summary>
public sealed record LandmassState(LandmassConfiguration Configuration,
    byte[] SurfaceByCellId, int DeterministicAttempt)
{
    public byte[]? ElevationByCellId { get; init; }
    public byte[]? SlopeByCellId { get; init; }
    public byte[]? PlateByCellId { get; init; }
    public TectonicMotionState? TectonicMotion { get; init; }
    /// <summary>
    /// Stage 01 only: no land/ocean, relief, climate or biomes have been classified.
    /// SurfaceByCellId is an unclassified compatibility placeholder in this mode.
    /// </summary>
    public bool TectonicPreviewOnly { get; init; }
    public BiomeState? Biomes { get; init; }
    public ClimateState? Climate { get; init; }
    public bool IsLand(int cellId) => SurfaceByCellId[cellId] == 1;
    /// <summary>Condición consultable desde el estado persistido, sin duplicar alturas.</summary>
    public bool IsImpassableSummit(int cellId) => IsLand(cellId) &&
        ElevationByCellId is { } heights &&
        heights[cellId] >= TerrainRules.ImpassableSummitMinimumElevation;
}
