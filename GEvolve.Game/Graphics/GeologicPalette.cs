using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

public static class GeologicPalette
{
    public static Vector3 ForFeature(GeologicFeature feature, bool land) => feature switch
    {
        GeologicFeature.MountainRange => new(.81f, .50f, .22f),
        GeologicFeature.RiftValley => new(.14f, .79f, .68f),
        GeologicFeature.FaultValley => new(.77f, .45f, .85f),
        GeologicFeature.OceanTrench => new(.18f, .25f, .74f),
        GeologicFeature.MidOceanRidge => new(.20f, .81f, .94f),
        GeologicFeature.OceanTransformFault => new(.62f, .40f, .82f),
        _ => land ? new(.23f, .32f, .26f) : new(.06f, .16f, .26f)
    };
}
