using System.Numerics;

namespace GEvolve.Game.Graphics;

/// <summary>Stable diagnostic colors for plate IDs; oceans use subdued tones.</summary>
public static class PlatePalette
{
    public static Vector3 ForPlate(byte plateId, bool ocean)
    {
        float hue = (plateId * .61803398875f + .07f) % 1f;
        float saturation = ocean ? .32f : .55f;
        float brightness = ocean ? .53f : .86f;
        float sector = hue * 6f;
        int part = (int)sector;
        float fraction = sector - part;
        float p = brightness * (1 - saturation);
        float q = brightness * (1 - saturation * fraction);
        float t = brightness * (1 - saturation * (1 - fraction));
        return part switch
        {
            0 => new(brightness, t, p),
            1 => new(q, brightness, p),
            2 => new(p, brightness, t),
            3 => new(p, q, brightness),
            4 => new(t, p, brightness),
            _ => new(brightness, p, q)
        };
    }
}
