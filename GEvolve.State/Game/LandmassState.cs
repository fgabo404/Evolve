using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Game;

/// <summary>Una marca por ID de la Capa 01: 0 océano, 1 tierra.</summary>
public sealed record LandmassState(LandmassConfiguration Configuration,
    byte[] SurfaceByCellId, int DeterministicAttempt)
{
    public bool IsLand(int cellId) => SurfaceByCellId[cellId] == 1;
}
