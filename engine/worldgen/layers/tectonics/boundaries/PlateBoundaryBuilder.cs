// Builds canonical real inter-plate Grid edges and their owner-approved M kinematics.
// Projection and validation share explicit numeric policy; no influence field or geological classification is generated.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal static class PlateBoundaryBuilder
{
    internal static TectonicBoundary[] Build(HexGridData grid, byte[] owners, PlateMotion[] motions)
    {
        var boundaries = new List<TectonicBoundary>();
        for (int cell = 0; cell < grid.CellCount; cell++)
        for (int direction = 0; direction < 6; direction++)
            if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && cell < next && owners[cell] != owners[next])
                boundaries.Add(Project(grid, cell, next, motions[owners[cell]], motions[owners[next]]));
        boundaries.Sort((a, b) => a.FirstCellId != b.FirstCellId
            ? a.FirstCellId.CompareTo(b.FirstCellId) : a.SecondCellId.CompareTo(b.SecondCellId));
        return boundaries.ToArray();
    }

    internal static TectonicVector Normal(HexGridData grid, int first, int second)
    {
        var a = FlatHexGeometry.Center(grid.GetOffset(first));
        var b = FlatHexGeometry.Center(grid.GetOffset(second));
        double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y, period = FlatHexGeometry.PeriodWidth(grid.Columns);
        if (dx > period / 2) dx -= period;
        else if (dx < -period / 2) dx += period;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!double.IsFinite(length) || length <= 0) throw new ArgumentException("Invalid boundary normal.");
        return new(Zero(dx / length), Zero(dy / length));
    }

    internal static TectonicBoundary Project(HexGridData grid, int first, int second, PlateMotion firstMotion, PlateMotion secondMotion)
    {
        var n = Normal(grid, first, second);
        var a = firstMotion.Velocity; var b = secondMotion.Velocity;
        float normal = Store((b.X - a.X) * n.X + (b.Y - a.Y) * n.Y);
        float tangent = Store(-(b.X - a.X) * n.Y + (b.Y - a.Y) * n.X);
        return new(first, second, firstMotion.PlateId, secondMotion.PlateId, n, normal, tangent, Classify(normal, tangent));
    }

    internal static TectonicInteractionKind Classify(float normal, float tangent)
    {
        if (!float.IsFinite(normal) || !float.IsFinite(tangent)) throw new ArgumentException("Nonfinite boundary components.");
        double axial = Math.Abs((double)normal), shear = Math.Abs((double)tangent);
        double maximum = Math.Max(axial, shear), minimum = Math.Min(axial, shear);
        if (maximum <= PlateMotionGenerator.Activity) return TectonicInteractionKind.Quiet;
        // All operands originate in stored floats; multiplication is explicitly promoted to double.
        if (minimum >= PlateMotionGenerator.Activity && minimum >= (double)PlateMotionGenerator.MixedRatio * maximum)
            return TectonicInteractionKind.Mixed;
        if (shear > axial) return TectonicInteractionKind.Transform;
        return normal < 0 ? TectonicInteractionKind.Convergent : TectonicInteractionKind.Divergent;
    }

    internal static float Store(double value)
    {
        float stored = (float)value;
        if (!double.IsFinite(value) || !float.IsFinite(stored)) throw new ArgumentException("Unrepresentable boundary component.");
        return stored == 0 ? 0 : stored;
    }
    private static double Zero(double value) => value == 0 ? 0 : value;
}

public enum TectonicInteractionKind { Quiet = 0, Convergent = 1, Divergent = 2, Transform = 3, Mixed = 4 }

public readonly record struct TectonicBoundary
{
    public int FirstCellId { get; }
    public int SecondCellId { get; }
    public byte FirstPlateId { get; }
    public byte SecondPlateId { get; }
    public TectonicVector Normal { get; }
    public float SignedNormal { get; }
    public float SignedTangent { get; }
    public TectonicInteractionKind Kind { get; }
    public float Convergence => Math.Max(0, -SignedNormal);
    public float Divergence => Math.Max(0, SignedNormal);
    public float Shear => Math.Abs(SignedTangent);
    internal TectonicBoundary(int first, int second, byte firstPlate, byte secondPlate, TectonicVector normal,
        float signedNormal, float signedTangent, TectonicInteractionKind kind) =>
        (FirstCellId, SecondCellId, FirstPlateId, SecondPlateId, Normal, SignedNormal, SignedTangent, Kind) =
        (first, second, firstPlate, secondPlate, normal, signedNormal, signedTangent, kind);
}
