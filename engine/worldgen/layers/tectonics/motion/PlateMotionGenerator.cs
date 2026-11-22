// Generates fourteen immutable kinematic values from the reserved motion stream.
// Freezes owner-approved M independently of the unchanged R partition and schema revisions.
namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal static class PlateMotionGenerator
{
    internal const uint Revision = 1;
    internal const string AlgorithmVersion = "tectonic-motion-m-v1";
    internal static float Activity => BitConverter.UInt32BitsToSingle(0x3E19999A);
    internal static float MixedRatio => BitConverter.UInt32BitsToSingle(0x3F400000);

    internal static PlateMotion[] Generate(DeterministicStageRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        var motions = new PlateMotion[14];
        for (byte plate = 0; plate < motions.Length; plate++)
        {
            double direction = TectonicPartitionGenerator.NextUnit(random) * 360d;
            double speed = 1d + 7d * TectonicPartitionGenerator.NextUnit(random);
            motions[plate] = new(plate, direction, speed);
        }
        return motions;
    }
}

// Values use clockwise map degrees and diagnostic cm/year, not Grid displacement units.
public readonly record struct PlateMotion
{
    public byte PlateId { get; }
    public double DirectionDegrees { get; }
    public double SpeedCmPerYear { get; }
    public TectonicVector Velocity
    {
        get
        {
            double radians = DirectionDegrees * (Math.PI / 180d);
            return new(Zero(SpeedCmPerYear * Math.Cos(radians)), Zero(SpeedCmPerYear * Math.Sin(radians)));
        }
    }

    internal PlateMotion(byte plateId, double directionDegrees, double speedCmPerYear)
    {
        if (plateId >= 14 || !double.IsFinite(directionDegrees) || directionDegrees < 0 || directionDegrees >= 360 ||
            !double.IsFinite(speedCmPerYear) || speedCmPerYear < 0)
            throw new ArgumentException("Invalid tectonic motion.");
        PlateId = plateId;
        DirectionDegrees = speedCmPerYear == 0 ? 0 : Zero(directionDegrees);
        SpeedCmPerYear = Zero(speedCmPerYear);
    }

    private static double Zero(double value) => value == 0 ? 0 : value;
}

public readonly record struct TectonicVector
{
    public double X { get; }
    public double Y { get; }
    internal TectonicVector(double x, double y) => (X, Y) = (x, y);
}
