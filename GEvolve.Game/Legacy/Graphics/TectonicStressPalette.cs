using System.Numerics;

namespace GEvolve.Game.Graphics;

public static class TectonicStressPalette
{
    // Fixed diagnostic scale shared across worlds, cm/year-equivalent kinematic intensity.
    public const float FullScale = 16;
    public static Vector3 ForValue(float value, MapViewMode mode)
    {
        Vector3 hot = mode switch
        {
            MapViewMode.Compression => new(1, .28f, .12f),
            MapViewMode.Extension => new(.12f, .75f, 1),
            _ => new(.9f, .64f, 1)
        };
        return Vector3.Lerp(new(.06f, .09f, .14f), hot, MathF.Sqrt(Math.Clamp(value / FullScale, 0, 1)));
    }
}
