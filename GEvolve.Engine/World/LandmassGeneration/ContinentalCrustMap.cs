namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Geological input for a future relief stage after surface and crust generation.
/// This record does not generate or classify any land or ocean cells.
/// </summary>
public sealed record ContinentalCrustMap(byte[] CrustByCellId, float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells, byte[] ProvinceByCellId);
