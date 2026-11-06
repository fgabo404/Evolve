namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Transient generation evidence. Never part of LandmassState or a saved game.</summary>
public sealed class PangaeaElevationDiagnostics
{
    public ReadOnlyMemory<byte> BaseByCellId { get; }
    public ReadOnlyMemory<byte> ContributionByCellId { get; }
    public ReadOnlyMemory<byte> FinalByCellId { get; }
    public ReadOnlyMemory<bool> CompositionLimitedByCellId { get; }
    public PangaeaElevationSources? Sources { get; internal set; }

    internal PangaeaElevationDiagnostics(byte[] initial, byte[] final, double[] requested)
    {
        var contribution = new byte[initial.Length];
        var limited = new bool[initial.Length];
        for (int id = 0; id < initial.Length; id++)
        {
            contribution[id] = checked((byte)(final[id] - initial[id]));
            limited[id] = requested[id] > 0 &&
                final[id] < Math.Round(initial[id] + requested[id]);
        }
        BaseByCellId = initial;
        FinalByCellId = final;
        ContributionByCellId = contribution;
        CompositionLimitedByCellId = limited;
    }
}

/// <summary>The approximation here predates H04 and uses Pangaea's own legacy vectors.</summary>
public sealed record PangaeaElevationSources(double[] Quantiles, double[] PotentialAnchors,
    double[] ElevationAnchors, double ApproximationCorrelation, double NoiseCorrelation,
    double MeanApproximationHigh, double MeanNoiseHigh,
    double MeanApproximationOther, double MeanNoiseOther)
{
    internal static PangaeaElevationSources Measure(byte[] surface, byte[] elevation,
        double[] potential, double[] uplift, double[] quantiles, double[] anchors, double[] levels)
    {
        int count = 0, high = 0;
        double sx = 0, sy = 0, sh = 0, sxx = 0, syy = 0, shh = 0, sxh = 0, syh = 0;
        double highX = 0, highY = 0;
        for (int id = 0; id < surface.Length; id++)
        {
            if (surface[id] != 1) continue;
            double x = 2.5 * uplift[id], y = potential[id] - x, h = elevation[id];
            count++; sx += x; sy += y; sh += h;
            sxx += x * x; syy += y * y; shh += h * h; sxh += x * h; syh += y * h;
            if (h >= 85) { high++; highX += x; highY += y; }
        }
        double Correlation(double sum, double squared, double product)
        {
            double denominator = Math.Sqrt(Math.Max(0, count * squared - sum * sum) *
                Math.Max(0, count * shh - sh * sh));
            return denominator > 0 ? (count * product - sum * sh) / denominator : 0;
        }
        return new((double[])quantiles.Clone(), (double[])anchors.Clone(), (double[])levels.Clone(),
            Correlation(sx, sxx, sxh), Correlation(sy, syy, syh),
            high == 0 ? 0 : highX / high, high == 0 ? 0 : highY / high,
            count == high ? 0 : (sx - highX) / (count - high),
            count == high ? 0 : (sy - highY) / (count - high));
    }
}
