namespace GEvolve.Game.Graphics;

public readonly record struct PangaeaHeightLabel(int CellId, int Column, int Row, byte Height);

/// <summary>Numeric annotation policy, independent of GL. No sampling or generation of elevation.</summary>
public static class PangaeaHeightLabels
{
    public const float MinimumScale = 13;
    public const int MaximumLabels = 1000;
    public static IReadOnlyList<PangaeaHeightLabel> Build(PangaeaMapData data, MapViewMode view,
        VisibleHexRange visible, float scale, bool enabled)
    {
        if (!enabled || scale < MinimumScale || !float.IsFinite(scale) || visible.Count > MaximumLabels)
            return [];
        var grid = data.Snapshot.Grid!;
        int columns = grid.Definition.Columns;
        var labels = new List<PangaeaHeightLabel>();
        for (int row = Math.Max(0, visible.FirstRow); row <= Math.Min(grid.Definition.Rows - 1, visible.LastRow); row++)
        for (int column = visible.FirstColumn; column <= visible.LastColumn; column++)
        {
            int id = row * columns + ((column % columns) + columns) % columns;
            if (data.Height(id, view) is { } height) labels.Add(new(id, column, row, height));
        }
        return labels;
    }
}
