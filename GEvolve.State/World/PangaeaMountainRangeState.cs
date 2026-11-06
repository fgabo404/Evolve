namespace GEvolve.State.Game;

/// <summary>A land-only axis favored by observed H04 convergence; not a crustal collision claim.</summary>
public sealed record PangaeaMountainRange(int Id, int[] AxisCellIds,
    byte[] WidthByAxisCell, float PeakConvergence);

/// <summary>Zero means no range; otherwise the value is the one-based index in Ranges.</summary>
public sealed record PangaeaMountainRangeState(string Version,
    PangaeaMountainRange[] Ranges, ushort[] RangeByCellId);
