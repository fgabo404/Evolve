using System.Numerics;

namespace GEvolve.Game.Graphics;

/// <summary>Stylized annual temperature scale, not physical infrared radiation.</summary>
public static class ThermalPalette
{
    private static readonly (double Celsius, Vector3 Color)[] Stops =
    [
        (-40, Rgb(0x281A59)), (-20, Rgb(0x3457A4)), (0, Rgb(0x54C9E8)),
        (10, Rgb(0x63BA82)), (20, Rgb(0xE8D66D)), (30, Rgb(0xE87836)),
        (40, Rgb(0xC72D32))
    ];

    public static Vector3 ForCelsius(double temperature)
    {
        if (!double.IsFinite(temperature)) throw new ArgumentOutOfRangeException(nameof(temperature));
        if (temperature <= Stops[0].Celsius) return Stops[0].Color;
        for (int i = 1; i < Stops.Length; i++)
            if (temperature <= Stops[i].Celsius)
                return Vector3.Lerp(Stops[i - 1].Color, Stops[i].Color,
                    (float)((temperature - Stops[i - 1].Celsius) /
                        (Stops[i].Celsius - Stops[i - 1].Celsius)));
        return Stops[^1].Color;
    }

    private static Vector3 Rgb(int hex) => new((hex >> 16 & 255) / 255f,
        (hex >> 8 & 255) / 255f, (hex & 255) / 255f);
}
