namespace GEvolve.Engine.World.LandmassGeneration;

public enum MountainContactDecision
{
    Eligible, KindNotConvergent, ConvergenceBelowOne, ShearDominates, NotBothLand
}

public enum MountainCandidateDecision
{
    Selected, ComponentBelowMinimum, AxisBelowMinimum, CompactShort, CompactRoundFewEndpoints
}

/// <summary>Transient observations of actual branches. These records never drive generation.</summary>
public readonly record struct MountainContactAudit(int InteractionIndex, MountainContactDecision Decision);

public sealed record MountainCandidateAudit(int CandidateId, byte FirstPlateId, byte SecondPlateId,
    int[] ContactCellIds, int[] ProposedAxisCellIds, int[] FinalAxisCellIds, byte[] WidthByAxisCell,
    MountainCandidateDecision Decision, float? TestedElongation, int? TestedEndpoints,
    int? TestedCycles, int? TestedBranchingCells, int SelectedRangeId = 0, int AffectedCells = 0);

public sealed record PangaeaMountainRangeAudit(MountainContactAudit[] Contacts,
    MountainCandidateAudit[] Candidates, int MinimumAxisCells);
