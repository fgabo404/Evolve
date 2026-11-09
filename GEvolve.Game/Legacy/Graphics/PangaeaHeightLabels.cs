using System.Globalization;
using System.Numerics;
using GEvolve.Engine.Grid;

namespace GEvolve.Game.Graphics;

public readonly record struct PangaeaHeightLabel(int CellId, int Column, int Row, byte Height,
    string Text, Vector2 Center, Vector2 TextOrigin, BitmapTextBounds Bounds, float Pixel, Vector4 TextColor);

/// <summary>Individual readable labels inside visible hexes. Never samples or generates elevations.</summary>
public static class PangaeaHeightLabels
{
    public const float MinimumPixel = 1.25f;
    public const float PreferredPixel = 1.5f;
    public const float Padding = 2;
    private const float Root3 = 1.7320508075688772f;
    private static readonly string[] Texts = Enumerable.Range(0, 256).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();
    private static readonly BitmapTextBounds[] Metrics = Texts.Select(text => UiRenderer.MeasureTextInk(text)).ToArray();

    public static IReadOnlyList<PangaeaHeightLabel> Build(PangaeaMapData data, MapViewMode view, MapCamera2D camera)
    {
        var grid = data.Snapshot.Grid!;
        if (camera.Columns != grid.Definition.Columns || camera.Rows != grid.Definition.Rows)
            throw new ArgumentException("Label camera and grid dimensions differ.", nameof(camera));
        float radius = camera.Scale;
        // Even the narrowest height cannot fit: skip all candidates, not a spaced subset.
        if (!TryPixel(Metrics[1], radius, out _)) return [];
        float left = camera.ViewportX, top = camera.ViewportY;
        float right = left + camera.ViewWidth, bottom = top + camera.ViewHeight;
        var visible = camera.VisibleRange();
        int columns = grid.Definition.Columns;
        var labels = new List<PangaeaHeightLabel>();
        for (int row = Math.Max(0, visible.FirstRow); row <= Math.Min(grid.Definition.Rows - 1, visible.LastRow); row++)
        for (int column = visible.FirstColumn; column <= visible.LastColumn; column++)
        {
            var center = camera.WorldToScreen(FlatHexGeometry.Center(new(column, row)));
            if (center.X < left || center.X > right || center.Y < top || center.Y > bottom) continue;
            int id = row * columns + ((column % columns) + columns) % columns;
            if (data.Height(id, view) is not { } height || !TryPixel(Metrics[height], radius, out float pixel)) continue;
            var metric = Metrics[height];
            pixel = MathF.Min(pixel, MathF.Min(
                2 * (MathF.Min(center.X - left, right - center.X) - Padding) / metric.Width,
                2 * (MathF.Min(center.Y - top, bottom - center.Y) - Padding) / metric.Height));
            if (pixel < MinimumPixel) continue;
            float width = metric.Width * pixel, textHeight = metric.Height * pixel;
            var bounds = new BitmapTextBounds(center.X - width / 2, center.Y - textHeight / 2, width, textHeight);
            // A clipped number is not readable; do not create off-viewport text.
            if (bounds.X - Padding < left || bounds.Y - Padding < top ||
                bounds.X + bounds.Width + Padding > right || bounds.Y + bounds.Height + Padding > bottom) continue;
            var origin = center - metric.Center * pixel;
            labels.Add(new(id, column, row, height, Texts[height], center, origin, bounds, pixel, Contrast(data.Color(id, view))));
        }
        return labels;
    }
    private static bool TryPixel(BitmapTextBounds metric, float radius, out float pixel)
    {
        // For a centered rectangle in a flat-top regular hex:
        // halfHeight <= sqrt(3)*radius/2; sqrt(3)*halfWidth + halfHeight <= sqrt(3)*radius.
        float vertical = (Root3 * radius - 2 * Padding) / metric.Height;
        float diagonal = (Root3 * radius - (Root3 + 1) * Padding) / ((Root3 * metric.Width + metric.Height) / 2);
        pixel = MathF.Min(PreferredPixel, MathF.Min(vertical, diagonal));
        return float.IsFinite(pixel) && pixel >= MinimumPixel;
    }
    public static Vector4 Contrast(Vector3 background)
    {
        static float Linear(float v) { v = Math.Clamp(v, 0, 1); return v <= .04045f ? v / 12.92f : MathF.Pow((v + .055f) / 1.055f, 2.4f); }
        float light = .2126f * Linear(background.X) + .7152f * Linear(background.Y) + .0722f * Linear(background.Z);
        return (light + .05f) / .05f >= 1.05f / (light + .05f) ? new(0, 0, 0, 1) : Vector4.One;
    }
}
