namespace GEvolve.State.Game;

/// <summary>Static landform diagnostic derived from plate-motion fields, not geological time.</summary>
public enum GeologicFeature : byte
{
    None = 0,
    MountainRange = 1,
    RiftValley = 2,
    FaultValley = 3,
    OceanTrench = 4,
    MidOceanRidge = 5,
    OceanTransformFault = 6
}
