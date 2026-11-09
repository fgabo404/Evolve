namespace GEvolve.State.Game;

public enum CrustKind : byte { Oceanic, Continental }
public enum GeologicalContactKind : byte
{
    Quiet, ContinentalCollision, OceanContinentSubduction, OceanOceanSubduction,
    ContinentalRift, OceanRidge, Transform
}
public enum VolcanoKind : byte { ContinentalArc, IslandArc }
public enum ContinentalLandform : byte { None, MountainBelt, Rift, Fault, Trench, Ridge, VolcanicArc }

public readonly record struct GeologicalContact(int InteractionIndex, GeologicalContactKind Kind,
    byte SubductingPlateId = byte.MaxValue, byte OverridingPlateId = byte.MaxValue);
public sealed record Volcano(int CellId, VolcanoKind Kind, byte SubductingPlateId,
    byte OverridingPlateId, int InteractionIndex);

/// <summary>Continents-only geology. Ocean depth is a relative 0..100 index, not metres.</summary>
public sealed record ContinentalGeologyState(string Version, byte[] CrustByCellId,
    byte[] ProvinceByCellId, int[] CratonCellIds, ushort[] OceanicAgeMaByPlateId,
    GeologicalContact[] Contacts, Volcano[] Volcanoes, byte[] LandformByCellId,
    byte[] OceanDepthByCellId, float SeaLevel);
