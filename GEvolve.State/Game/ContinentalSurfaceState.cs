namespace GEvolve.State.Game;

/// <summary>Plate groups and crust used to derive the continental surface. 255 means no continent.</summary>
public sealed record ContinentalSurfaceState(string Version, byte[] GroupByPlateId,
    byte[] CrustByCellId, byte[] ContinentByCellId, float SeaLevel);
