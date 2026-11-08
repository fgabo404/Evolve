namespace GEvolve.Game.Graphics;

public readonly record struct PangaeaHeightLabel(int CellId, int Column, int Row, byte Height);

/// <summary>Numeric annotation policy, independent of GL. No sampling or generation of elevation.</summary>
public static class PangaeaHeightLabels
{
    public const float MinimumScale = 13;
    public const int MaximumSimplifiedLabels = 600;
    public static IReadOnlyList<PangaeaHeightLabel> Build(PangaeaMapData data, MapViewMode view,
        VisibleHexRange visible, float scale)
    {
        if (scale <= 0 || !float.IsFinite(scale)) return [];
        int stride = scale >= MinimumScale ? 1 : Math.Max(1, (int)Math.Ceiling(MinimumScale / scale));
        while (scale < MinimumScale &&
            Math.Ceiling(Math.Max(0, visible.LastColumn - visible.FirstColumn + 1) / (double)stride) *
            Math.Ceiling(Math.Max(0, visible.LastRow - visible.FirstRow + 1) / (double)stride) > MaximumSimplifiedLabels) stride++;
        var grid = data.Snapshot.Grid!;
        int columns = grid.Definition.Columns;
        var labels = new List<PangaeaHeightLabel>();
        int firstRow = (int)Math.Ceiling(Math.Max(0, visible.FirstRow) / (double)stride) * stride;
        int firstColumn = (int)Math.Ceiling(visible.FirstColumn / (double)stride) * stride;
        for (int row = firstRow; row <= Math.Min(grid.Definition.Rows - 1, visible.LastRow); row += stride)
        for (int column = firstColumn; column <= visible.LastColumn; column += stride)
        {
            int id = row * columns + ((column % columns) + columns) % columns;
            if (data.Height(id, view) is { } height) labels.Add(new(id, column, row, height));
        }
        return labels;
    }
}
