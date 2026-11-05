using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Frozen first experimental battery. Not WorldSettings or an official world profile.</summary>
public static class C1Profile
{
    public const string Version = "multiscale-cortex-c1-1";
    public const int Origins = 8;
    public const int CandidatesPerK = 3;
    public const double PrimaryBudgetFraction = .80;
    public const double SolverTolerance = 1e-8;
    public const int SolverIterations = 1024;
    public const double MainAreaFraction = .03;
    public const double AmbiguousAreaFraction = .01;
    public const double MainDominance = .60;
}

public enum C1OriginRole { Principal, Secondary }
public enum C1ProvinceRole { CratonicInterior, Margin, ContactMargin, ExtensionalMargin, Terrane }
public enum C1GeographicStatus { Accepted, ReviewRequired, Rejected }
public sealed record C1Origin(int Id, C1OriginRole Role, int ProspectiveDomainId,
    int AnchorCellId, int ParentInteractionIndex, int Budget, double Opportunity);
public sealed record C1Domain(int Id, int PrincipalOriginId, int[] OriginIds);
public sealed record C1FormationTrace(int[] OriginByCellId, int[] ParentByCellId,
    int[] RoundByCellId, int[] InteractionByCellId);
public readonly record struct C1SolverReport(int OriginId, bool Converged, int Iterations,
    double RelativeResidual, double Minimum, double Maximum, string Reason)
{
    public string FieldHash { get; init; } = "";
}
public sealed record C1FormationCandidate(int Ordinal, int PlannedDomains, int CorticalBudget,
    C1Origin[] Origins, C1Domain[] Domains, C1SolverReport[] Solvers, int[] Deficits,
    SharedCorticalModel? Model, int[] OriginByFragmentId, C1ProvinceRole[] ProvinceRoles, C1FormationTrace? Trace, string Failure);
public sealed record C1GeographicReport(C1GeographicStatus Status, string[] Reasons,
    int[] MainAreas, int[] AmbiguousAreas, int[] SecondaryAreas, double MainDominance,
    int LandCells, int ContinentalCells, int SubmergedContinentalCells,
    int[] CorticalFragmentAreas, int[] ProvinceAreas, int OceanComponents, double LargestOceanShare,
    int NarrowMainComponents, int ShelfSeparatedMainPairs, int UnsupportedMainComposites, int DirectAccretions,
    int RegionalAccretions, int RiftRelations, int EmergedAccretionContacts, CoastMetrics Coast);
public sealed record C1CandidateSummary(int Ordinal, int PlannedDomains, int CorticalBudget,
    C1SolverReport[] Solvers, int[] Deficits, C1GeographicReport? Geography, string Failure);
public sealed record C1EvaluatedCandidate(C1FormationCandidate Formation,
    ContinentsCorticalAssemblyResult Assembly, LandmassState Terrain, C1GeographicReport Geography);
public sealed record C1ExperimentResult(C1CandidateSummary[] Candidates, C1EvaluatedCandidate? Selected, int ReferenceMainCount);
