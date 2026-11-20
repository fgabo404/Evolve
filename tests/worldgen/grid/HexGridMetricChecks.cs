// Completes V03/V05 metric and invalid-input checks without changing accepted Grid mathematics.
// Supplements exhaustive topology and independent BFS with deterministic triples across every supported cell.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.WorldGeneration.Grid.Checks;

internal static class HexGridMetricChecks
{
    public static void Run()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size);
            int[] anchors = [0, grid.Columns - 1, grid.CellCount - grid.Columns, grid.CellCount - 1,
                grid.GetCellId(new(0, grid.Rows / 2)), grid.GetCellId(new(grid.Columns - 1, grid.Rows / 2)),
                grid.GetCellId(new(grid.Columns / 2, grid.Rows / 2))];
            long triangles = 0;
            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                // Anchors include both poles and seam edges; the mixed index provides repeatable interior coverage.
                int middle = (int)(((long)cell * 48271 + 17) % grid.CellCount);
                foreach (int end in anchors)
                {
                    int direct = grid.GetDistance(cell, end);
                    Require(direct <= grid.GetDistance(cell, middle) + grid.GetDistance(middle, end), "Triangle inequality");
                    Require((direct == 0) == (cell == end), "Identity of indiscernibles");
                    triangles++;
                }
                var offset = grid.GetOffset(cell);
                var center = grid.GetGeographicCenter(cell);
                Require(Math.Abs(center.Longitude - (-180d + 360d * (offset.Column + 0.5d) / grid.Columns)) < 1e-12 &&
                    Math.Abs(center.Latitude - (90d - 180d * (offset.Row + (offset.Column % 2) * 0.5d + 0.5d) / (grid.Rows + 0.5d))) < 1e-12,
                    "Independent normalized center formula");
                if (offset.Row + 1 < grid.Rows)
                {
                    var current = FlatHexGeometry.Center(offset);
                    var below = FlatHexGeometry.Center(new(offset.Column, offset.Row + 1));
                    Require(Math.Abs(below.Y - current.Y - Math.Sqrt(3d)) < 0.0001, "Regular vertical spacing");
                }
            }
            Expect<OverflowException>(() => FlatHexGeometry.ToAxial(new(2, int.MinValue)));
            Expect<OverflowException>(() => FlatHexGeometry.ToOffset(new(2, int.MaxValue)));
            Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.GeographicCenter(new(0, grid.Rows), grid.Columns, grid.Rows));
            Expect<ArgumentOutOfRangeException>(() => FlatHexGeometry.WrappedDistance(new(0, 0), new(-1, 0), grid.Columns, grid.Rows));
            Console.WriteLine($"PASS WG-00-D {size}: {triangles} deterministic triangles; independent geographic/spacing and invalid-input checks");
        }
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
