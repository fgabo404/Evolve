using System.Numerics;
using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class GridTests
{
    public static void Run()
    {
        foreach (var (size, columns, rows) in new[]
        {
            (GridSize.Small, 200, 133),
            (GridSize.Medium, 250, 167),
            (GridSize.Large, 300, 200),
            (GridSize.Enormous, 400, 267)
        })
        {
            var definition = HexGridDefinitions.Get(size);
            Check(definition.Columns == columns && definition.Rows == rows &&
                definition.Orientation == GridOrientation.FlatTop &&
                definition.WrapHorizontal && !definition.WrapVertical, "Official configuration");
            Check(Math.Abs(definition.Columns / (double)definition.Rows - 1.5) < .01,
                "All four world grids are approximately 3:2");
            var grid = HexGrid.Generate(definition);
            var topology = new HexTopology(grid);
            Check(grid.Cells.Count == columns * rows, "Cell count");
            Check(grid.Cells.Select(cell => cell.Id).Distinct().Count() == grid.Cells.Count, "Unique IDs");
            Check(grid.Cells.SequenceEqual(HexGrid.Generate(definition).Cells), "Determinism");

            int edges = 0;
            for (int id = 0; id < topology.Count; id++)
            {
                var cell = grid.Cells[id];
                Check(cell.Id == id && cell.Address.Row == id / columns &&
                    cell.Address.Column == id % columns && topology.Id(cell.Address.Column, cell.Address.Row) == id,
                    $"Consecutive row-major ID: {size}/{id}");
                Check(grid.TryGet(cell.Address, out var retrieved) && retrieved == cell,
                    $"Grid address retrieves the same cell: {size}/{id}");
                Check(FlatHexGeometry.ToAxial(cell.Address) == cell.Axial &&
                    FlatHexGeometry.ToOffset(cell.Axial) == cell.Address, "Axial/offset conversion");
                Check(FlatHexGeometry.CellAt(cell.Center, columns, rows) == cell.Address,
                    "Picking at cell center");
                var compact = topology.Neighbors(id);
                Check(compact.Length <= 6, $"At most six neighbors: {size}/{id}");
                int index = 0;
                foreach (var neighbor in grid.Neighbors(cell))
                {
                    Check(index < compact.Length && compact[index] == neighbor.Id,
                        $"Grid/topology neighbor order agrees: {size}/{id}");
                    Check(neighbor.Id != id && (uint)neighbor.Id < (uint)topology.Count,
                        $"Valid distinct neighbor: {size}/{id}->{neighbor.Id}");
                    for (int previous = 0; previous < index; previous++)
                        Check(compact[previous] != neighbor.Id,
                            $"No duplicate neighbors: {size}/{id}->{neighbor.Id}");
                    Check(topology.Neighbors(neighbor.Id).Contains(id),
                        $"Symmetric neighbors: {size}/{id}<->{neighbor.Id}");
                    Check(cell.Address.Row != 0 || neighbor.Address.Row != rows - 1,
                        $"No north/south wrap: {size}/{id}->{neighbor.Id}");
                    Check(cell.Address.Row != rows - 1 || neighbor.Address.Row != 0,
                        $"No south/north wrap: {size}/{id}->{neighbor.Id}");
                    Check(grid.Distance(cell, neighbor) == 1, "Wrapped neighbor distance");
                    index++;
                }
                Check(index == compact.Length, $"Grid/topology neighbor count agrees: {size}/{id}");
                edges += index;
            }
            VerifyTopologyContracts(grid, topology);
            VerifyDistances(grid, topology);

            float period = FlatHexGeometry.PeriodWidth(columns);
            var west = grid.Cells.Where(cell => cell.Address.Column == 0).ToArray();
            var east = grid.Cells.Where(cell => cell.Address.Column == columns - 1).ToArray();
            Check(west.Any(cell => grid.Neighbors(cell).Any(n => n.Address.Column == columns - 1)),
                "West connects to east");
            Check(east.Any(cell => grid.Neighbors(cell).Any(n => n.Address.Column == 0)),
                "East connects to west");
            for (int row = 0; row < rows; row++)
            {
                int westId = topology.Id(0, row), eastId = topology.Id(columns - 1, row);
                Check(topology.Neighbors(westId).Contains(eastId) &&
                    topology.Neighbors(eastId).Contains(westId),
                    $"Every row crosses the horizontal seam: {size}/{row}");
            }
            Check(grid.Cells.Where(cell => cell.Address.Row == 0).All(cell =>
                grid.Neighbors(cell).All(n => n.Address.Row != rows - 1)), "Open north edge");
            Check(grid.Cells.Where(cell => cell.Address.Row == rows - 1).All(cell =>
                grid.Neighbors(cell).All(n => n.Address.Row != 0)), "Open south edge");

            foreach (var cell in west.Concat(east))
            foreach (var neighbor in grid.Neighbors(cell).Where(n =>
                Math.Abs(n.Address.Column - cell.Address.Column) == columns - 1))
            {
                float distance = Enumerable.Range(-1, 3).Min(shift => Vector2.Distance(
                    cell.Center, neighbor.Center + new Vector2(shift * period, 0)));
                Check(Math.Abs(distance - MathF.Sqrt(3)) < .0002f,
                    "Seam centers have exact hex neighbor spacing");
                var repeated = new Vector2(neighbor.Center.X +
                    (neighbor.Address.Column == 0 ? period : -period), neighbor.Center.Y);
                Check(FlatHexGeometry.CellAt(repeated, columns, rows) == neighbor.Address,
                    "Picking a repeated cell resolves its original ID");
            }

            Check(grid.Neighbors(grid.Cells[0]).Count() < 6, "Northern corner is bounded");
            Check(grid.Neighbors(grid.Cells[^1]).Count() < 6, "Southern corner is bounded");
            Check(grid.Distance(grid.Cells[0], grid.Cells[columns - 1]) <= 2,
                "Distance crosses horizontal seam");
            Console.WriteLine($"PASS: {size} flat-top grid ({grid.Cells.Count} cells, {edges} directed neighbor links)");
        }

        var small = HexGridDefinitions.Get(GridSize.Small);
        Check(FlatHexGeometry.Center(new GridAddress(1, 0)).Y >
            FlatHexGeometry.Center(new GridAddress(0, 0)).Y,
            "Odd columns are shifted downward");
        Check(FlatHexGeometry.CellAt(new Vector2(0, -20), small.Columns, small.Rows) is null,
            "North is not wrapped");
        Check(FlatHexGeometry.CellAt(new Vector2(0, FlatHexGeometry.MapHeight(small.Rows) + 20),
            small.Columns, small.Rows) is null, "South is not wrapped");

        string path = Path.Combine(Path.GetTempPath(), $"evolve-grid-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path, new GameState { HexGrid = new HexGridState(small) });
            var loaded = GameSaveStore.Load(path);
            Check(loaded.HexGrid?.Definition == small, "Effective grid configuration survives save/load");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        MeasureEnormous();
    }

    private static void VerifyTopologyContracts(HexGrid grid, HexTopology topology)
    {
        int row = topology.Rows / 2;
        Check(topology.Id(-1, row) == topology.Id(topology.Columns - 1, row) &&
            topology.Id(topology.Columns, row) == topology.Id(0, row),
            $"Horizontal column wrapping remains available: {grid.Definition.Size}");
        ExpectOutOfRange(() => topology.Id(0, -1), "row");
        ExpectOutOfRange(() => topology.Id(0, topology.Rows), "row");
        ExpectOutOfRange(() => { _ = topology.Neighbors(-1).Length; }, "id");
        ExpectOutOfRange(() => { _ = topology.Neighbors(topology.Count).Length; }, "id");
        ExpectOutOfRange(() => topology.Distance(-1, 0), "first");
        ExpectOutOfRange(() => topology.Distance(topology.Count, 0), "first");
        ExpectOutOfRange(() => topology.Distance(0, -1), "second");
        ExpectOutOfRange(() => topology.Distance(0, topology.Count), "second");
        Check(!grid.TryGet(new GridAddress(-1, row), out _) &&
            !grid.TryGet(new GridAddress(0, -1), out _),
            $"TryGet keeps its non-wrapping lookup contract: {grid.Definition.Size}");
    }

    private static void ExpectOutOfRange(Action action, string parameter)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException error) when (error.ParamName == parameter) { return; }
        throw new InvalidOperationException($"Expected ArgumentOutOfRangeException for {parameter}");
    }

    private static void VerifyDistances(HexGrid grid, HexTopology topology)
    {
        int columns = topology.Columns, rows = topology.Rows;
        GridAddress[] sources = [new(0, 0), new(columns - 1, 0),
            new(0, rows - 1), new(columns - 1, rows - 1),
            new(0, rows / 2), new(columns - 1, rows / 2),
            new(1, rows / 2), new(2, rows / 2 + 1),
            new(columns / 2, rows / 2), new(columns / 2 + 1, rows / 2 + 1)];
        GridAddress[] targets = [new(0, 0), new(columns - 1, 0),
            new(0, rows - 1), new(columns - 1, rows - 1),
            new(1, 1), new(2, 2), new(17, 17),
            new(columns / 4, rows / 4), new(columns / 2, rows / 2),
            new(columns / 2 + 1, rows / 2 + 1),
            new(3 * columns / 4, 3 * rows / 4),
            new(columns - 3, rows / 2 - 1), new(columns - 2, rows / 2),
            new(columns - 1, rows / 2), new(0, rows / 2)];
        var distances = new int[topology.Count];
        var queue = new int[topology.Count];
        int pairs = 0;
        foreach (var source in sources)
        {
            Array.Fill(distances, -1);
            int sourceId = topology.Id(source.Column, source.Row);
            distances[sourceId] = 0;
            int head = 0, tail = 0;
            queue[tail++] = sourceId;
            while (head < tail)
            {
                int current = queue[head++];
                foreach (int neighbor in topology.Neighbors(current))
                    if (distances[neighbor] < 0)
                    {
                        distances[neighbor] = distances[current] + 1;
                        queue[tail++] = neighbor;
                    }
            }
            Check(tail == topology.Count, $"BFS reaches every cell: {grid.Definition.Size}/{source}");
            foreach (var target in targets)
            {
                int targetId = topology.Id(target.Column, target.Row);
                int gridDistance = grid.Distance(grid.Cells[sourceId], grid.Cells[targetId]);
                int topologyDistance = topology.Distance(sourceId, targetId);
                int bfsDistance = distances[targetId];
                Check(gridDistance == bfsDistance && topologyDistance == bfsDistance,
                    $"Distance mismatch {grid.Definition.Size}: {source} id={sourceId} -> " +
                    $"{target} id={targetId}; Grid={gridDistance}, Topology={topologyDistance}, BFS={bfsDistance}");
                pairs++;
            }
        }
        Console.WriteLine($"PASS: {grid.Definition.Size} BFS distance comparison ({sources.Length} full traversals, {pairs} pairs)");
    }

    private static void MeasureEnormous()
    {
        var definition = HexGridDefinitions.Get(GridSize.Enormous);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long retainedBefore = GC.GetTotalMemory(true);
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        int[] collectionsBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        var timer = Stopwatch.StartNew();
        var grid = HexGrid.Generate(definition);
        timer.Stop();
        double gridMilliseconds = timer.Elapsed.TotalMilliseconds;
        long gridBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        timer.Restart();
        var topology = new HexTopology(grid);
        timer.Stop();
        double topologyMilliseconds = timer.Elapsed.TotalMilliseconds;
        long topologyBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        int[] collectionsAfter = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        long retainedAfter = GC.GetTotalMemory(true);
        Console.WriteLine($"ENORMOUS MEASURE: Grid {gridMilliseconds:F2} ms, {gridBytes} allocated bytes; " +
            $"Topology {topologyMilliseconds:F2} ms, {topologyBytes} allocated bytes; " +
            $"retained delta after forced GC {retainedAfter - retainedBefore} bytes; " +
            $"collections during construction gen0/1/2 " +
            $"{collectionsAfter[0] - collectionsBefore[0]}/" +
            $"{collectionsAfter[1] - collectionsBefore[1]}/" +
            $"{collectionsAfter[2] - collectionsBefore[2]}");
        GC.KeepAlive(topology);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
