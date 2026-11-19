// Verifies WG-00-B identities, immutable calculated queries and exhaustive topology against production geometry.
// Reports construction allocation, retained memory and elapsed time without imposing unapproved performance thresholds.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.WorldGeneration.Grid.Checks;

internal static class HexGridDataChecks
{
    public static void Run()
    {
        VerifyImmutableShape();
        foreach (var (size, columns, rows, edges) in new[]
        {
            (MapSize.Small, 200, 100, 59600),
            (MapSize.Medium, 300, 150, 134400),
            (MapSize.Large, 400, 200, 239200)
        })
        {
            var grid = new HexGridData(size);
            Require(grid.Columns == columns && grid.Rows == rows && grid.CellCount == columns * rows, "Preset dimensions");
            VerifyEveryCell(grid, new HexGridData(size), edges);
            VerifyInvalidInputs(grid);
            Console.WriteLine($"PASS WG-00-B {size}: {grid.CellCount} unique IDs; {edges} edges; geometry, neighbors, determinism and input checks");
        }
        MeasureConstruction();
        Console.WriteLine("PASS WG-00-B all compact grid checks");
    }

    // The production type contains only readonly scalar dimensions, not cell records or mutable caches.
    private static void VerifyImmutableShape()
    {
        var type = typeof(HexGridData);
        Require(type.IsSealed && type.GetProperties().All(property => property.SetMethod is null), "Read-only dimensions");
        Require(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .All(field => field.IsInitOnly && field.FieldType == typeof(int)), "Compact scalar storage");
        foreach (int value in new[] { -1, 3, int.MaxValue })
            Expect<ArgumentOutOfRangeException>(() => new HexGridData((MapSize)value));
    }

    private static void VerifyEveryCell(HexGridData grid, HexGridData equivalent, int expectedEdges)
    {
        var seen = new bool[grid.CellCount];
        long directed = 0;
        int three = 0, five = 0, six = 0;
        for (int row = 0; row < grid.Rows; row++)
        for (int column = 0; column < grid.Columns; column++)
        {
            var offset = new HexOffset(column, row);
            int id = grid.GetCellId(offset);
            Require(id == row * grid.Columns + column && !seen[id], "Unique row-major identity");
            seen[id] = true;
            Require(grid.GetOffset(id) == offset && equivalent.GetCellId(offset) == id, "Deterministic identity round trip");
            Require(grid.GetAxial(id) == FlatHexGeometry.ToAxial(offset) &&
                equivalent.GetAxial(id) == grid.GetAxial(id), "Derived axial coordinates");
            var geographic = grid.GetGeographicCenter(id);
            Require(geographic == FlatHexGeometry.GeographicCenter(offset, grid.Columns, grid.Rows) &&
                geographic == equivalent.GetGeographicCenter(id) &&
                geographic.Longitude >= -180 && geographic.Longitude < 180 &&
                geographic.Latitude > -90 && geographic.Latitude < 90, "Normalized geographic centers");

            var neighbors = grid.GetNeighbors(id).ToArray();
            Require(neighbors.Length == neighbors.Distinct().Count() && !neighbors.Contains(id), "Unique valid neighbors");
            Require(neighbors.SequenceEqual(equivalent.GetNeighbors(id)), "Deterministic neighbor order");
            var directional = new List<int>();
            for (int direction = 0; direction < FlatHexGeometry.DirectionCount; direction++)
            {
                var heading = (HexDirection)direction;
                bool actual = grid.TryGetNeighbor(id, heading, out int next);
                bool expected = FlatHexGeometry.TryNeighbor(offset, heading, grid.Columns, grid.Rows, out var expectedOffset);
                Require(actual == expected, "Neighbor boundary matches geometry");
                if (!actual) { Require(next == -1, "Missing neighbor sentinel"); continue; }
                Require(next == grid.GetCellId(expectedOffset) && next >= 0 && next < grid.CellCount, "Neighbor identity");
                directional.Add(next);
                Require(grid.TryGetNeighbor(next, (HexDirection)((direction + 3) % 6), out int reverse) && reverse == id, "Reciprocal edge");
                Require(grid.GetDistance(id, next) == 1, "Adjacent distance");
            }
            Require(neighbors.SequenceEqual(directional), "All neighbors match directional queries");
            int expectedDegree = row == 0 ? ((column & 1) == 0 ? 3 : 5)
                : row == grid.Rows - 1 ? ((column & 1) == 0 ? 5 : 3) : 6;
            Require(neighbors.Length == expectedDegree, "Exact polar/interior neighbor count");
            directed += neighbors.Length;
            if (neighbors.Length == 3) three++;
            else if (neighbors.Length == 5) five++;
            else if (neighbors.Length == 6) six++;
            Require(grid.GetDistance(id, id) == 0, "Self distance");
            int target = grid.CellCount - 1 - id;
            int distance = grid.GetDistance(id, target);
            Require(distance == grid.GetDistance(target, id) && distance == equivalent.GetDistance(id, target) &&
                distance == FlatHexGeometry.WrappedDistance(offset, grid.GetOffset(target), grid.Columns, grid.Rows),
                "Deterministic symmetric cylindrical distance");
            // Materializing a query creates caller-owned data, not writable grid state.
            if (neighbors.Length > 0) neighbors[0] = -999;
            Require(grid.GetNeighbors(id).SequenceEqual(directional), "Query array cannot mutate grid");
        }
        Require(seen.All(value => value) && grid.GetCellId(new(grid.Columns - 1, grid.Rows - 1)) == grid.CellCount - 1, "Consecutive complete ID range");
        Require(three == grid.Columns && five == grid.Columns && six == grid.Columns * (grid.Rows - 2), "Exact degree distribution");
        Require(directed == 2L * expectedEdges, "Undirected edge count");
        Require(grid.GetDistance(grid.GetCellId(new(0, grid.Rows / 2)), grid.GetCellId(new(grid.Columns - 1, grid.Rows / 2))) == 1, "Seam shortcut");
    }

    private static void VerifyInvalidInputs(HexGridData grid)
    {
        foreach (int id in new[] { -1, grid.CellCount, int.MinValue, int.MaxValue })
        {
            Expect<ArgumentOutOfRangeException>(() => grid.GetOffset(id));
            Expect<ArgumentOutOfRangeException>(() => grid.GetAxial(id));
            Expect<ArgumentOutOfRangeException>(() => grid.GetGeographicCenter(id));
            Expect<ArgumentOutOfRangeException>(() => grid.GetNeighbors(id));
            Expect<ArgumentOutOfRangeException>(() => grid.TryGetNeighbor(id, HexDirection.North, out _));
            Expect<ArgumentOutOfRangeException>(() => grid.GetDistance(id, 0));
            Expect<ArgumentOutOfRangeException>(() => grid.GetDistance(0, id));
        }
        foreach (var coordinate in new HexOffset[]
        {
            new(-1, 0), new(grid.Columns, 0), new(0, -1), new(0, grid.Rows),
            new(int.MinValue, int.MaxValue)
        })
            Expect<ArgumentOutOfRangeException>(() => grid.GetCellId(coordinate));
        foreach (int direction in new[] { -1, 6, int.MaxValue })
            Expect<ArgumentOutOfRangeException>(() => grid.TryGetNeighbor(0, (HexDirection)direction, out _));
    }

    // Warm up first; a preallocated retained batch prevents dead-code elimination and excludes container allocation.
    private static void MeasureConstruction()
    {
        const int count = 100000;
        foreach (var size in Enum.GetValues<MapSize>())
            for (int index = 0; index < 10000; index++) GC.KeepAlive(new HexGridData(size));
        Console.WriteLine($"MEASURE runtime={RuntimeInformation.FrameworkDescription}; OS={RuntimeInformation.OSDescription}; architecture={RuntimeInformation.ProcessArchitecture}; CPUs={Environment.ProcessorCount}");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var batch = new HexGridData[count];
            long beforeRetained = GC.GetTotalMemory(true);
            long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int index = 0; index < count; index++) batch[index] = new HexGridData(size);
            TimeSpan elapsed = Stopwatch.GetElapsedTime(start);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
            long retained = GC.GetTotalMemory(true) - beforeRetained;
            Console.WriteLine(FormattableString.Invariant($"MEASURE {size}: count={count}; allocated={allocated} B ({(double)allocated / count:F2} B/grid); retained_delta={retained} B ({(double)retained / count:F2} B/grid); elapsed={elapsed.TotalMilliseconds:F3} ms ({elapsed.TotalNanoseconds / count:F2} ns/grid)"));
            GC.KeepAlive(batch);
        }
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); } catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
}
