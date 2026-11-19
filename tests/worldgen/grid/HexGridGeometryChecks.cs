// Runs headless WG-00-A geometry regressions and WG-00-B data checks against the production engine.
// Preserves seam, polar, signed-coordinate and BFS coverage while verifying the calculated grid API.
using System.Numerics;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.WorldGeneration.Grid.Checks;

internal static class HexGridGeometryChecks
{
    private static readonly (int Columns, int Rows, int Edges)[] Sizes =
        [(200, 100, 59600), (300, 150, 134400), (400, 200, 239200)];

    private static void Main()
    {
        ValidateInputs();
        ValidateSignedCoordinates();
        ValidateExtremeDistances();
        ValidateExtentOverflow();
        Console.WriteLine("PASS WG-00-A extreme distances and finite extent regressions");
        foreach (var (columns, rows, edges) in Sizes)
        {
            VerifyCompleteTopology(columns, rows, edges);
            VerifyDistancesAgainstBfs(columns, rows);
            Console.WriteLine($"PASS WG-00-A {columns}x{rows}: geometry, seam, poles and BFS");
        }
        Console.WriteLine("PASS WG-00-A all geometry checks");
        HexGridDataChecks.Run();
    }

    private static void VerifyCompleteTopology(int columns, int rows, int expectedEdges)
    {
        long directed = 0;
        int three = 0, five = 0, six = 0;
        float period = (float)FlatHexGeometry.PeriodWidth(columns);
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            HexOffset cell = new(column, row);
            HexAxial axial = FlatHexGeometry.ToAxial(cell);
            Require(FlatHexGeometry.ToOffset(axial) == cell, "Offset/Axial round trip");
            HexGeographicPosition geographic = FlatHexGeometry.GeographicCenter(cell, columns, rows);
            Require(geographic.Longitude >= -180 && geographic.Longitude < 180, "Longitude limits");
            Require(geographic.Latitude > -90 && geographic.Latitude < 90, "Latitude limits");
            Vector2 center = FlatHexGeometry.Center(cell);
            Require(float.IsFinite(center.X) && float.IsFinite(center.Y), "Finite center");

            var seen = new HashSet<HexOffset>();
            for (int direction = 0; direction < 6; direction++)
            {
                var heading = (HexDirection)direction;
                if (!FlatHexGeometry.TryNeighbor(cell, heading, columns, rows, out HexOffset neighbor))
                    continue;
                Require(neighbor != cell && seen.Add(neighbor), "No duplicate or self neighbor");
                Require(neighbor.Column >= 0 && neighbor.Column < columns &&
                    neighbor.Row >= 0 && neighbor.Row < rows, "Bounded neighbors");
                Require(FlatHexGeometry.TryNeighbor(neighbor,
                    (HexDirection)((direction + 3) % 6), columns, rows, out HexOffset reversed)
                    && reversed == cell, "Reciprocal neighbor");
                Require(FlatHexGeometry.WrappedDistance(cell, neighbor, columns, rows) == 1,
                    "Adjacent distance one");
                if (Math.Abs(neighbor.Column - column) == columns - 1)
                {
                    Vector2 next = FlatHexGeometry.Center(neighbor);
                    float wrappedX = next.X + (neighbor.Column == 0 ? period : -period);
                    double dx = wrappedX - center.X;
                    double dy = next.Y - center.Y;
                    Require(Math.Abs(Math.Sqrt(dx * dx + dy * dy) - Math.Sqrt(3)) < 0.0003,
                        "Seam geometry");
                }
            }
            Require(seen.SetEquals(FlatHexGeometry.Neighbors(cell, columns, rows)),
                "Neighbor enumeration is consistent");
            directed += seen.Count;
            switch (seen.Count)
            {
                case 3: three++; break;
                case 5: five++; break;
                case 6: six++; break;
                default: throw new InvalidOperationException($"Invalid degree {seen.Count} at {cell}");
            }
        }
        Require(three == columns && five == columns && six == columns * (rows - 2),
            "Exact 3/5/6 neighbor distribution");
        Require(directed % 2 == 0 && directed / 2 == expectedEdges, "Undirected edge count");
        Require(FlatHexGeometry.WrappedDistance(new(0, rows / 2),
            new(columns - 1, rows / 2), columns, rows) == 1, "Seam shortest route");
    }

    private static void VerifyDistancesAgainstBfs(int columns, int rows)
    {
        var grid = new HexGridData(columns switch
        {
            200 => MapSize.Small,
            300 => MapSize.Medium,
            400 => MapSize.Large,
            _ => throw new InvalidOperationException("Unexpected BFS preset")
        });
        HexOffset[] starts =
        [
            new(0, 0), new(columns - 1, 0),
            new(0, rows - 1), new(columns - 1, rows - 1),
            new(0, rows / 2), new(columns - 1, rows / 2),
            new(columns / 2, rows / 2), new(columns / 2 + 1, rows / 2 + 1)
        ];

        int count = checked(columns * rows);
        var distances = new int[count];
        var queue = new int[count];
        foreach (HexOffset start in starts)
        {
            Array.Fill(distances, -1);
            int startId = start.Row * columns + start.Column;
            queue[0] = startId;
            distances[startId] = 0;
            int head = 0, tail = 1;
            while (head < tail)
            {
                int id = queue[head++];
                var cell = new HexOffset(id % columns, id / columns);
                foreach (HexOffset neighbor in FlatHexGeometry.Neighbors(cell, columns, rows))
                {
                    int next = neighbor.Row * columns + neighbor.Column;
                    if (distances[next] >= 0) continue;
                    distances[next] = distances[id] + 1;
                    queue[tail++] = next;
                }
            }
            Require(tail == count, "Every grid cell must be connected");
            for (int id = 0; id < count; id++)
            {
                var target = new HexOffset(id % columns, id / columns);
                Require(FlatHexGeometry.WrappedDistance(start, target, columns, rows) ==
                    distances[id], $"BFS distance mismatch: {start} to {target}");
                Require(grid.GetDistance(startId, id) == distances[id],
                    $"Grid API BFS distance mismatch: {start} to {target}");
            }
        }
    }

    private static void ValidateInputs()
    {
        Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.ValidateDimensions(5, 10));
        Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.ValidateDimensions(2, 10));
        Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.ValidateDimensions(8, 1));
        Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.Center(new(0, 0), float.NaN));
        Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.Center(new(0, 0), -1));
        Expect<ArgumentOutOfRangeException>(() =>
            FlatHexGeometry.TryNeighbor(new(-1, 0), HexDirection.North, 8, 4, out _));
        Expect<ArgumentOutOfRangeException>(() =>
            FlatHexGeometry.TryNeighbor(new(0, 0), (HexDirection)6, 8, 4, out _));
        Expect<ArgumentOutOfRangeException>(() =>
            FlatHexGeometry.WrappedDistance(new(0, 0), new(8, 0), 8, 4));
        Require(FlatHexGeometry.PeriodWidth(200) == 300, "Horizontal period");
    }

    private static void ValidateSignedCoordinates()
    {
        foreach (int column in new[] { -401, -400, -3, -2, -1, 0, 1, 2, 3, 400, 401 })
        foreach (int row in new[] { -9, 0, 9 })
        {
            HexOffset offset = new(column, row);
            HexAxial axial = FlatHexGeometry.ToAxial(offset);
            Require(FlatHexGeometry.ToOffset(axial) == offset, "Signed Offset/Axial round trip");
            HexAxial wrapped = FlatHexGeometry.Wrap(axial, 400);
            HexOffset canonical = FlatHexGeometry.ToOffset(wrapped);
            Require(canonical.Column >= 0 && canonical.Column < 400 &&
                canonical.Row == row, "Signed canonical wrapping keeps row");
        }
        Require(new HexAxial(1, 0).DistanceTo(new HexAxial(0, 0)) == 1, "Axial metric");
    }

    // Exercise legal int-sized addresses without allocating a grid; image overflow must not reject a valid minimum.
    private static void ValidateExtremeDistances()
    {
        int columns = int.MaxValue - 1;
        int rows = int.MaxValue;
        (HexOffset First, HexOffset Second, int Expected)[] cases =
        [
            (new(0, 0), new(0, 0), 0),
            (new(columns - 1, rows - 1), new(columns - 1, rows - 1), 0),
            (new(0, 0), new(columns - 1, 0), 1),
            (new(0, rows - 1), new(columns - 1, rows - 1), 1),
            (new(0, 0), new(0, rows - 1), rows - 1),
            (new(0, 0), new(2, rows - 1), int.MaxValue)
        ];
        foreach (var (first, second, expected) in cases)
        {
            Require(FlatHexGeometry.WrappedDistance(first, second, columns, rows) == expected,
                "Extreme distance must remain representable");
            Require(FlatHexGeometry.WrappedDistance(second, first, columns, rows) == expected,
                "Extreme distance symmetry");
        }
        // The true minimum, not merely an intermediate periodic image, exceeds int.MaxValue here.
        HexOffset far = new(columns / 2, rows - 1);
        Expect<OverflowException>(() => FlatHexGeometry.WrappedDistance(new(0, 0), far, columns, rows));
        Expect<OverflowException>(() => FlatHexGeometry.WrappedDistance(far, new(0, 0), columns, rows));
    }

    // Finite input radii can still produce infinite extents; invalid inputs retain their argument-error policy.
    private static void ValidateExtentOverflow()
    {
        Expect<OverflowException>(() => FlatHexGeometry.PeriodWidth(4, double.MaxValue));
        Expect<OverflowException>(() => FlatHexGeometry.MapHeight(2, double.MaxValue));
        Expect<OverflowException>(() => FlatHexGeometry.PeriodWidth(int.MaxValue - 1, double.MaxValue / 8));
        Expect<OverflowException>(() => FlatHexGeometry.MapHeight(int.MaxValue, double.MaxValue / 8));
        Require(double.IsFinite(FlatHexGeometry.PeriodWidth(4, double.MaxValue / 8)), "Large finite width");
        Require(double.IsFinite(FlatHexGeometry.MapHeight(2, double.MaxValue / 8)), "Large finite height");
        Require(FlatHexGeometry.MapHeight(2) == Math.Sqrt(3d) * 2.5d, "Normal map height");
        foreach (double radius in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0d, -1d })
        {
            Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.PeriodWidth(4, radius));
            Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.MapHeight(2, radius));
        }
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
