// Implements WG-00 regular flat-top geometry on an east/west-periodic hexagonal strip.
// Adapts the historical FlatHexGeometry and HexCoord mathematics without legacy grid or UI dependencies.
using System.Numerics;

namespace Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

public static class FlatHexGeometry
{
    public const int DirectionCount = 6;
    private static readonly double RootThree = Math.Sqrt(3d);

    // Odd columns are shifted downward by half a row. Floor must work for negative Q too.
    public static HexAxial ToAxial(HexOffset offset) =>
        new(offset.Column, checked(offset.Row - FloorHalf(offset.Column)));

    public static HexOffset ToOffset(HexAxial axial) =>
        new(axial.Q, checked(axial.R + FloorHalf(axial.Q)));

    // A hex's radius equals its side length. Centers are shifted so the first polygon fits x >= 0.
    public static Vector2 Center(HexOffset offset, float radius = 1f)
    {
        ValidateRadius(radius);
        double x = radius * (1d + 1.5d * offset.Column);
        double y = RootThree * radius * (offset.Row + 0.5d * (offset.Column & 1) + 0.5d);
        if (Math.Abs(x) > float.MaxValue || Math.Abs(y) > float.MaxValue)
            throw new OverflowException("Hex center is not representable as Vector2.");
        return new Vector2((float)x, (float)y);
    }

    public static double PeriodWidth(int columns, double radius = 1d)
    {
        ValidateColumns(columns);
        ValidateRadius(radius);
        return 1.5d * columns * radius;
    }

    public static double MapHeight(int rows, double radius = 1d)
    {
        ValidateRows(rows);
        ValidateRadius(radius);
        return RootThree * (rows + 0.5d) * radius;
    }

    // Canonicalize an axial image while retaining its physical Offset row at the horizontal seam.
    public static HexAxial Wrap(HexAxial axial, int columns)
    {
        ValidateColumns(columns);
        int column = PositiveMod(axial.Q, columns);
        long periods = ((long)axial.Q - column) / columns;
        int row = checked((int)((long)axial.R + periods * (columns / 2)));
        return new HexAxial(column, row);
    }

    // Out-of-range row means a polar boundary; horizontal wrap is always applied.
    public static bool TryNeighbor(
        HexOffset cell, HexDirection direction, int columns, int rows, out HexOffset neighbor)
    {
        ValidateDimensions(columns, rows);
        ValidateCell(cell, columns, rows);
        if ((uint)direction >= DirectionCount)
            throw new ArgumentOutOfRangeException(nameof(direction));

        HexAxial wrapped = Wrap(ToAxial(cell).Move(direction), columns);
        HexOffset candidate = ToOffset(wrapped);
        if ((uint)candidate.Row >= (uint)rows)
        {
            neighbor = default;
            return false;
        }

        neighbor = candidate;
        return true;
    }

    // No persistent adjacency array is needed; queries are computed from six fixed directions.
    public static IEnumerable<HexOffset> Neighbors(HexOffset cell, int columns, int rows)
    {
        ValidateDimensions(columns, rows);
        ValidateCell(cell, columns, rows);
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            if (TryNeighbor(cell, (HexDirection)direction, columns, rows, out HexOffset neighbor))
                yield return neighbor;
        }
    }

    // The three nearest periodic images suffice for canonical addresses in a supported-width strip.
    // Correctness for bounded polar rows must also be checked independently against graph BFS.
    public static int WrappedDistance(HexOffset first, HexOffset second, int columns, int rows)
    {
        ValidateDimensions(columns, rows);
        ValidateCell(first, columns, rows);
        ValidateCell(second, columns, rows);
        HexAxial from = ToAxial(first);
        HexAxial to = ToAxial(second);
        int best = int.MaxValue;
        for (int k = -1; k <= 1; k++)
        {
            var image = new HexAxial(
                checked(to.Q + k * columns),
                checked(to.R - k * (columns / 2)));
            best = Math.Min(best, from.DistanceTo(image));
        }
        return best;
    }

    // Geographic labels are normalized logical coordinates, not sphere-based distances.
    public static HexGeographicPosition GeographicCenter(HexOffset cell, int columns, int rows)
    {
        ValidateDimensions(columns, rows);
        ValidateCell(cell, columns, rows);
        double longitude = -180d + 360d * (cell.Column + 0.5d) / columns;
        double latitude = 90d - 180d *
            (cell.Row + 0.5d * (cell.Column & 1) + 0.5d) / (rows + 0.5d);
        return new HexGeographicPosition(longitude, latitude);
    }

    public static void ValidateDimensions(int columns, int rows)
    {
        ValidateColumns(columns);
        ValidateRows(rows);
    }

    private static void ValidateCell(HexOffset cell, int columns, int rows)
    {
        if ((uint)cell.Column >= (uint)columns || (uint)cell.Row >= (uint)rows)
            throw new ArgumentOutOfRangeException(nameof(cell));
    }

    private static void ValidateColumns(int columns)
    {
        if (columns < 4 || (columns & 1) != 0)
            throw new ArgumentOutOfRangeException(nameof(columns),
                "A periodic flat-top grid requires an even number of columns, at least four.");
    }

    private static void ValidateRows(int rows)
    {
        if (rows < 2)
            throw new ArgumentOutOfRangeException(nameof(rows));
    }

    private static int PositiveMod(int number, int modulus)
    {
        int remainder = number % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }

    private static int FloorHalf(int number) =>
        number / 2 - (number < 0 && (number & 1) != 0 ? 1 : 0);

    private static void ValidateRadius(double radius)
    {
        if (!double.IsFinite(radius) || radius <= 0d)
            throw new ArgumentOutOfRangeException(nameof(radius));
    }
}
