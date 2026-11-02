using GEvolve.Engine.Grid;

namespace GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>Vecinos reales de Engine en memoria compacta, incluidos los del cierre este-oeste.</summary>
public sealed class HexTopology
{
    private readonly int[] _neighbors;
    private readonly byte[] _counts;
    public HexGrid Grid { get; }
    public int Count => Grid.Cells.Count;
    public int Columns => Grid.Definition.Columns;
    public int Rows => Grid.Definition.Rows;

    public HexTopology(HexGrid grid)
    {
        Grid = grid;
        _neighbors = new int[checked(grid.Cells.Count * 6)];
        _counts = new byte[grid.Cells.Count];
        foreach (var cell in grid.Cells)
        foreach (var neighbor in grid.Neighbors(cell))
            _neighbors[cell.Id * 6 + _counts[cell.Id]++] = neighbor.Id;
    }

    public ReadOnlySpan<int> Neighbors(int id)
    {
        ValidateId(id);
        return _neighbors.AsSpan(id * 6, _counts[id]);
    }
    public int Distance(int first, int second)
    {
        if ((uint)first >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(first));
        if ((uint)second >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(second));
        return Grid.Distance(Grid.Cells[first], Grid.Cells[second]);
    }
    public int Id(int column, int row)
    {
        if ((uint)row >= (uint)Rows)
            throw new ArgumentOutOfRangeException(nameof(row));
        return row * Columns + ((column % Columns) + Columns) % Columns;
    }

    private void ValidateId(int id)
    {
        if ((uint)id >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(id));
    }
}
