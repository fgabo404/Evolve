using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class GridTests
{
    public static void Run()
    {
        foreach (var (size, columns, rows) in new[]
        {
            (GridSize.Small, 150, 100),
            (GridSize.Medium, 250, 200),
            (GridSize.Large, 300, 250)
        })
        {
            var definition = HexGridDefinitions.Get(size);
            Check(definition.Columns == columns && definition.Rows == rows &&
                definition.Orientation == GridOrientation.FlatTop &&
                definition.WrapHorizontal && !definition.WrapVertical, "Official configuration");
            var grid = HexGrid.Generate(definition);
            Check(grid.Cells.Count == columns * rows, "Cell count");
            Check(grid.Cells.Select(cell => cell.Id).Distinct().Count() == grid.Cells.Count, "Unique IDs");
            Check(grid.Cells.SequenceEqual(HexGrid.Generate(definition).Cells), "Determinism");

            foreach (var cell in grid.Cells)
            {
                Check(cell.Id == cell.Address.Row * columns + cell.Address.Column, "Stable row-major ID");
                Check(FlatHexGeometry.ToAxial(cell.Address) == cell.Axial &&
                    FlatHexGeometry.ToOffset(cell.Axial) == cell.Address, "Axial/offset conversion");
                Check(FlatHexGeometry.CellAt(cell.Center, columns, rows) == cell.Address,
                    "Picking at cell center");
                foreach (var neighbor in grid.Neighbors(cell))
                {
                    Check(grid.Neighbors(neighbor).Any(back => back.Id == cell.Id), "Symmetric neighbors");
                    Check(grid.Distance(cell, neighbor) == 1, "Wrapped neighbor distance");
                }
            }

            float period = FlatHexGeometry.PeriodWidth(columns);
            var west = grid.Cells.Where(cell => cell.Address.Column == 0).ToArray();
            var east = grid.Cells.Where(cell => cell.Address.Column == columns - 1).ToArray();
            Check(west.Any(cell => grid.Neighbors(cell).Any(n => n.Address.Column == columns - 1)),
                "West connects to east");
            Check(east.Any(cell => grid.Neighbors(cell).Any(n => n.Address.Column == 0)),
                "East connects to west");
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
            Console.WriteLine($"PASS: {size} flat-top grid ({grid.Cells.Count} cells)");
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
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
