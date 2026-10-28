namespace GEvolve.Definitions.WorldGeneration.Configuration;

/// <summary>Annual mean climate calibration. Distances are hex steps, not kilometres.</summary>
public sealed record TemperatureSettings
{
    public double EquatorCelsius { get; init; } = 27;
    public double PoleCelsius { get; init; } = -23;
    public double OceanEquatorCelsius { get; init; } = 24;
    public double OceanPoleCelsius { get; init; } = -18;
    public double MetresPerElevationLevel { get; init; } = 40;
    public double LapseCelsiusPerKilometre { get; init; } = 6.5;
    public double CoastalStrength { get; init; } = .65;
    public double OceanDecayHexSteps { get; init; } = 6;
    public double RegionalAmplitudeCelsius { get; init; } = 1.5;
    public int RegionalFrequency { get; init; } = 2;

    public void Validate()
    {
        double[] values = [EquatorCelsius, PoleCelsius, OceanEquatorCelsius, OceanPoleCelsius,
            MetresPerElevationLevel, LapseCelsiusPerKilometre, CoastalStrength,
            OceanDecayHexSteps, RegionalAmplitudeCelsius];
        if (values.Any(value => !double.IsFinite(value)) || MetresPerElevationLevel < 0 ||
            LapseCelsiusPerKilometre < 0 || CoastalStrength is < 0 or > 1 ||
            OceanDecayHexSteps <= 0 || RegionalAmplitudeCelsius < 0 ||
            RegionalFrequency is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(TemperatureSettings));
    }
}
