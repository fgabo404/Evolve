// Represents an immutable approved grid using only dimensions and calculated row-major identities.
// Delegates all spatial mathematics to accepted WG-00-A geometry without per-cell storage or core integration.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Grid;

public sealed class HexGridData
{
    public int Columns { get; }
    public int Rows { get; }
    public int CellCount => Columns * Rows;

    // Presets belong to the grid; the shared MapSize definition remains the only size selector.
    public HexGridData(MapSize size)
    {
        (Columns, Rows) = size switch
        {
            MapSize.Small => (200, 100),
            MapSize.Medium => (300, 150),
            MapSize.Large => (400, 200),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported map size.")
        };
    }

    // Canonical addresses are bounded; only neighbor queries wrap across the seam.
    public int GetCellId(HexOffset coordinates)
    {
        if ((uint)coordinates.Column >= (uint)Columns || (uint)coordinates.Row >= (uint)Rows)
            throw new ArgumentOutOfRangeException(nameof(coordinates));
        return coordinates.Row * Columns + coordinates.Column;
    }

    public HexOffset GetOffset(int cellId)
    {
        if ((uint)cellId >= (uint)CellCount)
            throw new ArgumentOutOfRangeException(nameof(cellId));
        return new HexOffset(cellId % Columns, cellId / Columns);
    }

    public HexAxial GetAxial(int cellId) => FlatHexGeometry.ToAxial(GetOffset(cellId));

    public HexGeographicPosition GetGeographicCenter(int cellId) =>
        FlatHexGeometry.GeographicCenter(GetOffset(cellId), Columns, Rows);

    // Missing polar neighbors return false and -1, which cannot be confused with valid cell zero.
    public bool TryGetNeighbor(int cellId, HexDirection direction, out int neighborId)
    {
        if (FlatHexGeometry.TryNeighbor(GetOffset(cellId), direction, Columns, Rows, out var neighbor))
        {
            neighborId = GetCellId(neighbor);
            return true;
        }
        neighborId = -1;
        return false;
    }

    // Validate eagerly, then enumerate calculated IDs without retaining a neighbor array.
    public IEnumerable<int> GetNeighbors(int cellId)
    {
        var coordinates = GetOffset(cellId);
        return EnumerateNeighbors(coordinates);
    }

    public int GetDistance(int firstCellId, int secondCellId) =>
        FlatHexGeometry.WrappedDistance(GetOffset(firstCellId), GetOffset(secondCellId), Columns, Rows);

    private IEnumerable<int> EnumerateNeighbors(HexOffset coordinates)
    {
        foreach (var neighbor in FlatHexGeometry.Neighbors(coordinates, Columns, Rows))
            yield return GetCellId(neighbor);
    }
}
