namespace GEvolve.Definitions.WorldGeneration.Configuration;

public sealed record TectonicMotionSettings
{
    public double MinimumSpeedCmPerYear { get; init; } = 1;
    public double MaximumSpeedCmPerYear { get; init; } = 8;
    public double ActivityThresholdCmPerYear { get; init; } = .15;
    public double MixedComponentRatio { get; init; } = .5;
    // Fraction of the shorter map dimension, preserving relative width across sizes.
    public double InfluenceRadiusFraction { get; init; } = .02;
    public double CutoffRadii { get; init; } = 3;

    public void Validate()
    {
        double[] values = [MinimumSpeedCmPerYear, MaximumSpeedCmPerYear,
            ActivityThresholdCmPerYear, MixedComponentRatio, InfluenceRadiusFraction, CutoffRadii];
        if (values.Any(value => !double.IsFinite(value)) || MinimumSpeedCmPerYear <= 0 ||
            MaximumSpeedCmPerYear < MinimumSpeedCmPerYear || MaximumSpeedCmPerYear > 100 ||
            ActivityThresholdCmPerYear < 0 || MixedComponentRatio is <= 0 or > 1 ||
            InfluenceRadiusFraction is <= 0 or > .1 || CutoffRadii is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(TectonicMotionSettings));
    }
}
