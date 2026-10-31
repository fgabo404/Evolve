using System.Text.Json.Serialization;

namespace GEvolve.State.Game;

public enum ContinentalReliefKind : byte
{
    Ocean, CoastalPlain, InlandPlain, Hills, Plateau, Mountain, HighRange, Rift, Fault
}

/// <summary>Relief from classified plate contacts; ocean depth is separate from terrestrial elevation.</summary>
public sealed record ContinentalElevationState(string Version, GeologicalContact[] Contacts,
    byte[] LandformByCellId, byte[] OceanDepthByCellId)
{
    public TectonicReliefSystem[] Systems { get; init; } = [];
    public byte[] UpliftByCellId { get; init; } = [];
    public byte[] SubsidenceByCellId { get; init; } = [];
}

/// <summary>A connected, classified margin. Axis cells are actual boundary hexes.</summary>
public sealed record TectonicReliefSystem(int Id, GeologicalContactKind Kind,
    byte FirstPlateId, byte SecondPlateId, byte OverridingPlateId,
    int[] InteractionIndices, int[] AxisCellIds, double Intensity,
    double WidthHexes)
{
    [JsonIgnore] public int LengthHexEdges => InteractionIndices.Length;
}
