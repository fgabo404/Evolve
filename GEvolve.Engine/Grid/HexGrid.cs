using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Engine.Grid;

public readonly record struct GridCell(int Id, GridAddress Address, HexCoord Axial, Vector2 Center);

/// <summary>Capa espacial sin atributos de terreno ni contenido jugable.</summary>
public sealed class HexGrid
{
    private readonly GridCell[] _cells;

    public HexGridDefinition Definition { get; }
    public IReadOnlyList<GridCell> Cells => _cells;

    private HexGrid(HexGridDefinition definition)
    {
        Definition = definition;
        _cells = new GridCell[checked(definition.Columns * definition.Rows)];
        for (int row = 0; row < definition.Rows; row++)
        for (int column = 0; column < definition.Columns; column++)
        {
            var address = new GridAddress(column, row);
            int id = row * definition.Columns + column;
            _cells[id] = new GridCell(id, address, FlatHexGeometry.ToAxial(address),
                FlatHexGeometry.Center(address));
        }
    }

    public static HexGrid Generate(HexGridDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition != HexGridDefinitions.Get(definition.Size))
            throw new ArgumentException("Grid configuration differs from its official size.", nameof(definition));
        return new HexGrid(definition);
    }

    public bool TryGet(GridAddress address, out GridCell cell)
    {
        if (address.Column < 0 || address.Column >= Definition.Columns ||
            address.Row < 0 || address.Row >= Definition.Rows)
        {
            cell = default;
            return false;
        }
        cell = _cells[address.Row * Definition.Columns + address.Column];
        return true;
    }

    public IEnumerable<GridCell> Neighbors(GridCell cell)
    {
        for (int direction = 0; direction < 6; direction++)
        {
            HexCoord wrapped = FlatHexGeometry.Wrap(cell.Axial.Vecino(direction), Definition.Columns);
            if (TryGet(FlatHexGeometry.ToOffset(wrapped), out GridCell neighbor))
                yield return neighbor;
        }
    }

    public int Distance(GridCell from, GridCell to)
        => FlatHexGeometry.WrappedDistance(from.Axial, to.Axial, Definition.Columns);
}
