namespace GEvolve.State.Game;

/// <summary>Plate groups and crust used to derive the continental surface. 255 means no continent.</summary>
public sealed record ContinentalSurfaceState(string Version, byte[] GroupByPlateId,
    byte[] CrustByCellId, byte[] ContinentByCellId, float SeaLevel)
{
    public ContinentalSelectionState? Selection { get; init; }
}

public sealed record ContinentalSelectionAttempt(int Attempt, int PrincipalSystems,
    int SecondarySystems, string Outcome);
public sealed record ContinentalSelectionState(int EvaluatedGroupings, int SelectedAttempt,
    int AvailableConvergentMargins, ContinentalSelectionAttempt[] Attempts);
