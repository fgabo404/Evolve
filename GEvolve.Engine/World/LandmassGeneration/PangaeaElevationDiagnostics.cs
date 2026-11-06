namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Transient generation evidence. Never part of LandmassState or a saved game.</summary>
public sealed class PangaeaElevationDiagnostics
{
    public ReadOnlyMemory<byte> BaseByCellId { get; }
    public ReadOnlyMemory<byte> ContributionByCellId { get; }
    public ReadOnlyMemory<byte> FinalByCellId { get; }
    public ReadOnlyMemory<bool> CompositionLimitedByCellId { get; }
    public ReadOnlyMemory<byte> PreviousBaseByCellId { get; }
    public ReadOnlyMemory<byte> PreviousFinalByCellId { get; }
    public int PreviousCompositionLimitedCells { get; }
    public ReadOnlyMemory<byte> PriorCalibrationBaseByCellId { get; }
    public ReadOnlyMemory<byte> PriorCalibrationFinalByCellId { get; }
    public int PriorCalibrationCompositionLimitedCells { get; }
    public PangaeaElevationSources? Sources { get; internal set; }

    internal PangaeaElevationDiagnostics(byte[] initial, byte[] final, double[] requested,
        ReadOnlyMemory<byte> previousBase = default, ReadOnlyMemory<byte> priorCalibrationBase = default)
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
        if (!previousBase.IsEmpty)
        {
            if (previousBase.Length != initial.Length)
                throw new ArgumentException("Previous base dimensions differ from the current world.", nameof(previousBase));
            var comparison = ComposeComparison(previousBase, requested);
            PreviousBaseByCellId = previousBase;
            PreviousFinalByCellId = comparison.Final;
            PreviousCompositionLimitedCells = comparison.Clipped;
        }
        if (!priorCalibrationBase.IsEmpty)
        {
            if (priorCalibrationBase.Length != initial.Length)
                throw new ArgumentException("Prior calibration dimensions differ from the current world.", nameof(priorCalibrationBase));
            var comparison = ComposeComparison(priorCalibrationBase, requested);
            PriorCalibrationBaseByCellId = priorCalibrationBase;
            PriorCalibrationFinalByCellId = comparison.Final;
            PriorCalibrationCompositionLimitedCells = comparison.Clipped;
        }
    }

    private static (byte[] Final, int Clipped) ComposeComparison(ReadOnlyMemory<byte> initial, double[] requested)
    {
        var final = new byte[initial.Length];
        int clipped = 0;
        for (int id = 0; id < initial.Length; id++)
        {
            byte height = initial.Span[id];
            // Reuse the one generated uplift field; no surface or ranges are regenerated.
            final[id] = height == 0 ? (byte)0 : PangaeaMountainRangeGenerator.ComposeElevation(height, requested[id]);
            if (requested[id] > 0 && final[id] < Math.Round(height + requested[id])) clipped++;
        }
        return (final, clipped);
    }
}

/// <summary>The approximation here predates H04 and uses Pangaea's own legacy vectors.</summary>
public sealed record PangaeaElevationSources(double[] Quantiles, double[] PotentialAnchors,
    double[] ElevationAnchors, double ApproximationCorrelation, double NoiseCorrelation,
    double MeanApproximationHigh, double MeanNoiseHigh,
    double MeanApproximationOther, double MeanNoiseOther)
{
    public PangaeaElevationSources? Previous { get; init; }
    public ReadOnlyMemory<byte> PreviousBaseByCellId { get; init; }
    public PangaeaElevationSources? PriorCalibration { get; init; }
    public ReadOnlyMemory<byte> PriorCalibrationBaseByCellId { get; init; }
    public double CalibrationMilliseconds { get; init; }
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
