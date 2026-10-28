namespace GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>Ruido de valores suavizado con retícula periódica en X y abierta en Y.</summary>
public static class PeriodicNoise
{
    public static double Sample(double column, double row, int columns, int rows,
        int frequency, int seed)
    {
        if (columns <= 0 || rows <= 0 || frequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        double value = 0, amplitude = 1, total = 0;
        for (int octave = 0; octave < 3; octave++)
        {
            int periods = frequency << octave;
            double x = column * periods / columns;
            double y = row * periods / rows;
            value += amplitude * Lattice(x, y, periods, seed + octave * 7919);
            total += amplitude;
            amplitude *= .5;
        }
        return value / total;
    }

    private static double Lattice(double x, double y, int period, int seed)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = Smooth(x - ix), fy = Smooth(y - iy);
        double a = Hash(Mod(ix, period), iy, seed);
        double b = Hash(Mod(ix + 1, period), iy, seed);
        double c = Hash(Mod(ix, period), iy + 1, seed);
        double d = Hash(Mod(ix + 1, period), iy + 1, seed);
        return Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fy);
    }

    private static double Hash(int x, int y, int seed)
    {
        uint h = unchecked((uint)seed) ^ (uint)x * 0x9E3779B9u ^
            (uint)y * 0x85EBCA6Bu;
        h ^= h >> 16; h *= 0x7FEB352Du;
        h ^= h >> 15; h *= 0x846CA68Bu;
        h ^= h >> 16;
        return 2.0 * h / uint.MaxValue - 1.0;
    }

    private static int Mod(int value, int period) => (value % period + period) % period;
    private static double Smooth(double x) => x * x * (3 - 2 * x);
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
