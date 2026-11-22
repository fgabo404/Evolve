// Builds presentation-only unit-radius hex outlines around centers supplied by accepted engine geometry.
// Shares those outlines with picking; identities, neighbors and geographic coordinates remain engine queries.
using System.Numerics;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Game;

internal static class GridVisualGeometry
{
    // Six 60-degree unit vectors describe the approved regular flat-top outline, not a new grid topology.
    private static readonly Vector2[] Corners = Enumerable.Range(0, 6)
        .Select(index => new Vector2((float)Math.Cos(index * Math.PI / 3), (float)Math.Sin(index * Math.PI / 3))).ToArray();

    internal static Vector2 Corner(int index) => Corners[index];

    public static LaboratoryBounds Bounds(HexGridData grid)
    {
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        foreach (int column in new[] { 0, grid.Columns - 1 })
        foreach (int row in new[] { 0, grid.Rows - 1 })
        {
            var center = FlatHexGeometry.Center(new(column, row));
            foreach (var corner in Corners)
            {
                left = Math.Min(left, center.X + corner.X); right = Math.Max(right, center.X + corner.X);
                top = Math.Min(top, center.Y + corner.Y); bottom = Math.Max(bottom, center.Y + corner.Y);
            }
        }
        return new(left, top, right - left, bottom - top);
    }

    // Construct once per generated grid and upload as static line data, never rebuild per frame.
    public static float[] BuildLines(HexGridData grid, IEnumerable<int>? cells = null)
    {
        if (cells is null)
        {
            var all = new float[grid.CellCount * 24];
            for (int id = 0; id < grid.CellCount; id++) WriteCell(grid, id, all.AsSpan(id * 24, 24));
            return all;
        }
        int[] selected = cells.ToArray();
        var data = new float[selected.Length * 24];
        for (int index = 0; index < selected.Length; index++) WriteCell(grid, selected[index], data.AsSpan(index * 24, 24));
        return data;
    }

    private static void WriteCell(HexGridData grid, int id, Span<float> vertices)
    {
        var center = FlatHexGeometry.Center(grid.GetOffset(id));
        for (int edge = 0; edge < 6; edge++)
        {
            var start = center + Corners[edge];
            var end = center + Corners[(edge + 1) % 6];
            vertices[edge * 4] = start.X; vertices[edge * 4 + 1] = start.Y;
            vertices[edge * 4 + 2] = end.X; vertices[edge * 4 + 3] = end.Y;
        }
    }

    // Linear nearest-center picking happens only on click; no duplicated axial inverse or neighbor algorithm.
    public static int? Pick(HexGridData grid, double x, double y)
    {
        int closest = 0;
        double best = double.MaxValue;
        for (int id = 0; id < grid.CellCount; id++)
        {
            var center = FlatHexGeometry.Center(grid.GetOffset(id));
            double distance = (center.X - x) * (center.X - x) + (center.Y - y) * (center.Y - y);
            if (distance < best) { best = distance; closest = id; }
        }
        var origin = FlatHexGeometry.Center(grid.GetOffset(closest));
        double px = x - origin.X, py = y - origin.Y;
        for (int edge = 0; edge < 6; edge++)
        {
            var a = Corners[edge]; var b = Corners[(edge + 1) % 6];
            if ((b.X - a.X) * (py - a.Y) - (b.Y - a.Y) * (px - a.X) < -0.00001)
                return null;
        }
        return closest;
    }
}
