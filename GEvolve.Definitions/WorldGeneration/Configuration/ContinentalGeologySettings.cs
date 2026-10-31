namespace GEvolve.Definitions.WorldGeneration.Configuration;

/// <summary>Game-model calibration, independent of the immutable plate motion settings.</summary>
public static class ContinentalGeologySettings
{
    public const double ActiveNormalCmPerYear = .5;
    public const double MinimumNormalToShearRatio = .5;
    public const double OrogenicUpliftLevels = 48;
    public const double ArcUpliftLevels = 35;
    public const double RiftSubsidenceLevels = 20;
    public const double FaultSubsidenceLevels = 8;
    public const double ReferenceSpeedCmPerYear = 8;
    public const double VolcanicSpacingMapFraction = .04;
}
