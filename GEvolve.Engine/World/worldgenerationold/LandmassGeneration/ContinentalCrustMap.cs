namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Geological input for relief when a future surface and crust model exists.
/// This record does not generate or classify any land or ocean cells.
/// </summary>
public sealed record ContinentalCrustMap(byte[] CrustByCellId, float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells, byte[] ProvinceByCellId);
