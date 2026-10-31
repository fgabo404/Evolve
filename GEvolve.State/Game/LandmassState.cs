using GEvolve.Definitions.WorldGeneration.Configuration;
using System.Text.Json.Serialization;

namespace GEvolve.State.Game;

/// <summary>Terreno clasificado: 0 océano, 1 tierra. Un arreglo vacío representa superficie pendiente.</summary>
public sealed record LandmassState(LandmassConfiguration Configuration,
    byte[] SurfaceByCellId, int DeterministicAttempt)
{
    public byte[]? ElevationByCellId { get; init; }
    public byte[]? SlopeByCellId { get; init; }
    public byte[]? PlateByCellId { get; init; }
    public TectonicMotionState? TectonicMotion { get; init; }
    public ContinentalGeologyState? ContinentalGeology { get; init; }
    /// <summary>Continentes conserva sus placas y movimiento; la superficie aún no existe.</summary>
    public bool SurfaceGenerationPending { get; init; }
    /// <summary>
    /// Stage 01 only: no land/ocean, relief, climate or biomes have been classified.
    /// SurfaceByCellId is empty for pending Continents; older plate previews retain
    /// their unclassified compatibility placeholder.
    /// </summary>
    public bool TectonicPreviewOnly { get; init; }
    [JsonIgnore]
    public bool HasSurface => !TectonicPreviewOnly && !SurfaceGenerationPending && SurfaceByCellId.Length > 0;
    public BiomeState? Biomes { get; init; }
    public ClimateState? Climate { get; init; }
    public bool IsLand(int cellId) => HasSurface ? SurfaceByCellId[cellId] == 1 :
        throw new InvalidOperationException("Land and ocean have not been classified.");
    /// <summary>Condición consultable desde el estado persistido, sin duplicar alturas.</summary>
    public bool IsImpassableSummit(int cellId) => HasSurface && IsLand(cellId) &&
        ElevationByCellId is { } heights &&
        heights[cellId] >= TerrainRules.ImpassableSummitMinimumElevation;
}
