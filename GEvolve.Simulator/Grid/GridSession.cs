using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.State.Game;

namespace GEvolve.Simulator.Grid;

public sealed record GridDto(string Size, int Columns, int Rows, string Orientation,
    string Layout, bool WrapHorizontal, bool WrapVertical, int[] Cells);
public sealed record GridCellDto(int Id, int Column, int Row, int Q, int R, int[] Neighbors);

/// <summary>Única capa 01 activa; conserva su configuración efectiva en GameState.</summary>
public sealed class GridSession
{
    private readonly object _gate = new();
    private readonly GameState _state = new();
    private HexGrid _grid;
    private GridDto _dto;

    public GridSession()
    {
        var definition = HexGridDefinitions.Get(GridSize.Small);
        _grid = HexGrid.Generate(definition);
        _state.HexGrid = new HexGridState(definition);
        _dto = Map(_grid);
    }

    public GridDto Get()
    {
        lock (_gate) return _dto;
    }

    public HexGridState GetState()
    {
        lock (_gate) return _state.HexGrid!;
    }

    public GridDto Select(GridSize size)
    {
        var definition = HexGridDefinitions.Get(size);
        var next = HexGrid.Generate(definition);
        var dto = Map(next);
        lock (_gate)
        {
            _grid = next;
            _state.HexGrid = new HexGridState(definition);
            _dto = dto;
            return dto;
        }
    }

    public GridCellDto? GetCell(int column, int row)
    {
        lock (_gate)
        {
            if (!_grid.TryGet(new GridAddress(column, row), out GridCell cell)) return null;
            return new GridCellDto(cell.Id, column, row, cell.Axial.Q, cell.Axial.R,
                _grid.Neighbors(cell).Select(neighbor => neighbor.Id).ToArray());
        }
    }

    private static GridDto Map(HexGrid grid)
    {
        var cells = new int[grid.Cells.Count];
        foreach (GridCell cell in grid.Cells)
            cells[cell.Id] = cell.Id;
        return new GridDto(grid.Definition.Size.ToString().ToLowerInvariant(),
            grid.Definition.Columns, grid.Definition.Rows, "flat-top", "odd-q",
            grid.Definition.WrapHorizontal, grid.Definition.WrapVertical, cells);
    }
}
