using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

public enum PangaeaGenerationStage { Grid = 1, Plates, Motion, Surface, Base, MountainSystems, Final, Temperature, Precipitation, Biomes }
public enum PangaeaStageStatus { Pending, Running, Completed, Invalidated, Failed }

public sealed record PangaeaStageInfo(PangaeaGenerationStage Stage, PangaeaStageStatus Status,
    bool CanExecute, long Revision, string? Hash, TimeSpan? Duration, string? Error,
    IReadOnlyDictionary<PangaeaGenerationStage, long> InputRevisions,
    IReadOnlyDictionary<PangaeaGenerationStage, string> InputHashes);

public sealed record PangaeaSurfaceAttempt(int Attempt, bool Accepted, LandmassStatistics Statistics, string? Hash);

/// <summary>Geometry only: affected-cell counts do not exist until FINAL.</summary>
public sealed record PangaeaMountainAxis(int Id, int[] AxisCellIds, byte[] WidthByAxisCell,
    byte FirstPlateId, byte SecondPlateId, float PeakConvergence, float MeanConvergence,
    float Elongation, int ContactEndpoints, int ContactCycles, int BranchingCells);

public sealed record PangaeaMountainSelection(PangaeaMountainAxis[] Systems, ushort[] InfluenceByCellId,
    IReadOnlyDictionary<MountainContactDecision, int> ContactDecisions,
    IReadOnlyDictionary<MountainCandidateDecision, int> CandidateDecisions, int PropagationSamples);

/// <summary>Detached diagnostic data, not a save contract. Null means that layer is unavailable.</summary>
public sealed record PangaeaGenerationSnapshot(LandmassConfiguration Configuration, long ConfigurationRevision,
    IReadOnlyList<PangaeaStageInfo> Stages, HexGrid? Grid, byte[]? Plates, TectonicMotionState? Motion,
    int? SelectedAttempt, IReadOnlyList<PangaeaSurfaceAttempt> Attempts, byte[]? Surface,
    LandmassStatistics? SurfaceStatistics, byte[]? Base, PangaeaElevationSources? BaseSources,
    PangaeaMountainSelection? Mountains, PangaeaElevationDiagnostics? Elevations)
{
    public byte[]? Slopes { get; init; }
    public ClimateState? Climate { get; init; }
    public BiomeState? Biomes { get; init; }
}
