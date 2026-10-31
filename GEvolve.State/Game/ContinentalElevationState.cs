namespace GEvolve.State.Game;

public enum ContinentalReliefKind : byte
{
    Ocean, CoastalPlain, InlandPlain, Hills, Plateau, Mountain, HighRange, Rift, Fault
}

/// <summary>Relief from classified plate contacts; ocean depth is separate from terrestrial elevation.</summary>
public sealed record ContinentalElevationState(string Version, GeologicalContact[] Contacts,
    byte[] LandformByCellId, byte[] OceanDepthByCellId);
