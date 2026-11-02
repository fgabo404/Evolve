using System.Text.Json.Serialization;

namespace GEvolve.State.Game;

public readonly record struct TectonicVector(double X, double Y);

/// <summary>Map coordinates: 0° east, 90° south, clockwise. Speeds in cm/year.</summary>
public sealed record PlateMotion(byte PlateId, double DirectionDegrees, double SpeedCmPerYear)
{
    [JsonIgnore] public TectonicVector UnitVector => new(
        Math.Cos(DirectionDegrees * Math.PI / 180), Math.Sin(DirectionDegrees * Math.PI / 180));
    [JsonIgnore] public TectonicVector Velocity => new(UnitVector.X * SpeedCmPerYear, UnitVector.Y * SpeedCmPerYear);
}

public enum PlateInteractionKind { Quiet, Convergent, Divergent, Transform, Mixed }

public readonly record struct PlateInteraction(int FirstCellId, int SecondCellId,
    byte FirstPlateId, byte SecondPlateId, TectonicVector Normal,
    double Convergence, double Divergence, double Shear, PlateInteractionKind Kind);

/// <summary>Independent diagnostic kinematics, not physical stress in pascals.</summary>
public sealed record TectonicMotionState(string Version, PlateMotion[] Motions,
    PlateInteraction[] Interactions, float[] CompressionByCellId, float[] ExtensionByCellId,
    float[] ShearByCellId, ushort[] DistanceToBoundaryByCellId, int[] ArrowCellByPlateId,
    double InfluenceRadiusHexes);
