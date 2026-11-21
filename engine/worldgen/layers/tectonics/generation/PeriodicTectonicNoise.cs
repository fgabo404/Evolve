// Supplies deterministic X-periodic, open-Y multiscale value noise for tectonic partition fields.
// Uses a specified 64-bit lattice hash and never reads ambient randomness or changes Grid geometry.
namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal static class PeriodicTectonicNoise
{
    internal static double Sample(double column, double row, int columns, int rows, int frequency, ulong seed)
    {
        if (!double.IsFinite(column) || !double.IsFinite(row) || columns <= 0 || rows <= 0 ||
            frequency <= 0 || frequency > int.MaxValue / 4)
            throw new ArgumentOutOfRangeException(nameof(frequency), "Noise coordinates and dimensions must be finite and valid.");
        double sum = 0, amplitude = 1, total = 0;
        for (int octave = 0; octave < 3; octave++)
        {
            int period = frequency << octave;
            double x = column * period / columns, y = row * period / rows;
            sum += amplitude * Lattice(x, y, period, unchecked(seed + (ulong)octave * 0x9E3779B97F4A7C15UL));
            total += amplitude;
            amplitude *= 0.5;
        }
        return sum / total;
    }

    private static double Lattice(double x, double y, int period, ulong seed)
    {
        // Guard the integer lattice conversion explicitly; warped approved-grid coordinates remain far inside it.
        double floorX = Math.Floor(x), floorY = Math.Floor(y);
        if (!double.IsFinite(x) || !double.IsFinite(y) || floorX < int.MinValue || floorX >= int.MaxValue ||
            floorY < int.MinValue || floorY >= int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(x), "Noise lattice exceeds its supported coordinate range.");
        int ix = (int)floorX, iy = (int)floorY;
        double fx = x - floorX, fy = y - floorY;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        double a = Hash(Mod(ix, period), iy, seed), b = Hash(Mod(ix + 1, period), iy, seed);
        double c = Hash(Mod(ix, period), iy + 1, seed), d = Hash(Mod(ix + 1, period), iy + 1, seed);
        return Lerp(Lerp(a, b, fx), Lerp(c, d, fx), fy);
    }

    private static int Mod(int value, int period) => (int)(((long)value % period + period) % period);
    private static double Lerp(double a, double b, double amount) => a + (b - a) * amount;

    internal static double Hash(int x, int y, ulong seed)
    {
        unchecked
        {
            ulong value = seed ^ (ulong)(long)x * 0x9E3779B97F4A7C15UL ^ (ulong)(long)y * 0xD1B54A32D192ED03UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return 2d * (value >> 11) * (1d / 9007199254740992d) - 1d;
        }
    }
}
