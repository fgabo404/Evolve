using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

// Preserve every valid propagation sample: the maximum envelope depends on BASE.
internal readonly record struct MountainUpliftSample(int Cell, double LegacyUplift, double Crest, double Blend);
internal sealed record PangaeaMountainPlan(PangaeaMountainRange[] Ranges, ushort[] Owner,
    double[] LegacyUplift, MountainUpliftSample[] Samples, PangaeaMountainRangeAudit? Audit);
