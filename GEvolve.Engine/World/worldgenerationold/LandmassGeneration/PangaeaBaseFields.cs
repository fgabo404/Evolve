namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Opt-in, detached intermediate fields for numerical BASE audits; never persisted.</summary>
public sealed record PangaeaBaseFields
{
    public ReadOnlyMemory<double> CenterX { get; init; }
    public ReadOnlyMemory<double> CenterY { get; init; }
    public ReadOnlyMemory<double> VelocityX { get; init; }
    public ReadOnlyMemory<double> VelocityY { get; init; }
    public ReadOnlyMemory<double> Compression { get; init; }
    public ReadOnlyMemory<double> Extension { get; init; }
    public ReadOnlyMemory<double> InitialPressure { get; init; }
    public ReadOnlyMemory<double> SmoothedPressure { get; init; }
    public ReadOnlyMemory<double> RegionalNoise { get; init; }
    public ReadOnlyMemory<double> LocalNoise { get; init; }
    public ReadOnlyMemory<double> Potential { get; init; }
    internal PangaeaBaseFields Copy() => this with
    {
        CenterX = CenterX.ToArray(), CenterY = CenterY.ToArray(), VelocityX = VelocityX.ToArray(), VelocityY = VelocityY.ToArray(),
        Compression = Compression.ToArray(), Extension = Extension.ToArray(), InitialPressure = InitialPressure.ToArray(),
        SmoothedPressure = SmoothedPressure.ToArray(), RegionalNoise = RegionalNoise.ToArray(), LocalNoise = LocalNoise.ToArray(), Potential = Potential.ToArray()
    };
}
