using System.Numerics;
using GEvolve.Engine.Grid;
using GEvolve.Game.Graphics;

/// <summary>Pure bitmap/convex-geometry contracts; no window, renderer instance or GL context.</summary>
public static class PangaeaHeightLabelTests
{
    public static MapCamera2D Camera(PangaeaMapData map, bool close = true)
    {
        var grid = map.Snapshot.Grid!; var camera = new MapCamera2D();
        camera.FitViewport(grid.Definition.Columns, grid.Definition.Rows, 280, 0, 720, 480);
        camera.Zoom(close ? 100 : -100, new(640, 240));
        return camera;
    }
    public static void Run(PangaeaMapData map)
    {
        Check(UiRenderer.MeasureTextInk("0") == new BitmapTextBounds(0, 0, 5, 7), "Zero glyph ink dimensions");
        Check(UiRenderer.MeasureTextInk("1") == new BitmapTextBounds(1, 0, 3, 7), "Narrow one glyph has real bearing and width");
        Check(UiRenderer.MeasureTextInk("10") == new BitmapTextBounds(1, 0, 10, 7) &&
            UiRenderer.MeasureTextInk("100") == new BitmapTextBounds(1, 0, 16, 7), "Spacing and actual multi-digit bitmap bounds");
        Check(UiRenderer.MeasureTextInk("100", 1.25f) == new BitmapTextBounds(1.25f, 0, 20, 8.75f), "Glyph metrics scale exactly");
        Check(PangaeaHeightLabels.Contrast(Vector3.One) == new Vector4(0, 0, 0, 1) &&
            PangaeaHeightLabels.Contrast(Vector3.Zero) == Vector4.One, "Text contrasts with actual light/dark backgrounds");
        var far = Camera(map, close: false);
        foreach (var view in map.AvailableViews)
            Check(PangaeaHeightLabels.Build(map, view, far).Count == 0, "Far cells hide all numbers; no spaced samples");
        var camera = Camera(map);
        foreach (var view in map.AvailableViews)
        {
            var labels = PangaeaHeightLabels.Build(map, view, camera);
            Check(labels.Count > 0, "Readable cells display real individual heights");
            Verify(map, view, camera, labels);
            EveryEligibleCell(map, view, camera, labels);
        }
        var before = PangaeaHeightLabels.Build(map, MapViewMode.ElevationBase, camera).ToDictionary(l => (l.Column, l.Row));
        camera.Pan(17, 11);
        var moved = PangaeaHeightLabels.Build(map, MapViewMode.ElevationBase, camera);
        Verify(map, MapViewMode.ElevationBase, camera, moved);
        int common = 0;
        foreach (var label in moved)
            if (before.TryGetValue((label.Column, label.Row), out var old))
            { common++; Check(Vector2.Distance(label.Center - old.Center, new(17, 11)) < .005f, "Text follows exact camera displacement"); }
        Check(common > 0, "Pan retains common visible cells");
        camera.Zoom(-2, new(640, 240));
        Verify(map, MapViewMode.ElevationFinal, camera, PangaeaHeightLabels.Build(map, MapViewMode.ElevationFinal, camera));
        var grid = map.Snapshot.Grid!;
        float period = FlatHexGeometry.PeriodWidth(grid.Definition.Columns);
        camera.Pan((camera.X - (period - 2)) * camera.Scale, 0);
        var seam = PangaeaHeightLabels.Build(map, MapViewMode.Surface, camera);
        Check(seam.Any(l => l.Column >= grid.Definition.Columns), "Visible east/west copy has labels");
        Verify(map, MapViewMode.Surface, camera, seam);
        camera.Pan(0, (camera.Y + 1) * camera.Scale);
        var north = PangaeaHeightLabels.Build(map, MapViewMode.Surface, camera);
        Check(north.Any(l => l.Row == 0), "North edge cells remain selectable for labels"); Verify(map, MapViewMode.Surface, camera, north);
        float lastY = FlatHexGeometry.MapHeight(grid.Definition.Rows) - camera.ViewHeight / camera.Scale + 1;
        camera.Pan(0, (camera.Y - lastY) * camera.Scale);
        var south = PangaeaHeightLabels.Build(map, MapViewMode.Surface, camera);
        Check(south.Any(l => l.Row == grid.Definition.Rows - 1), "South edge has labels without vertical wrapping"); Verify(map, MapViewMode.Surface, camera, south);
        if (grid.Definition.Size == GEvolve.Definitions.WorldGeneration.Configuration.GridSize.Small) DifferentTextWidths(map);
        Console.WriteLine($"LAB_LABELS_PASS {grid.Definition.Size}: glyph metrics, geometry-based fit, exact ink centering, viewport culling, all eligible cells/no samples, contrast, non-overlap, pan/zoom, E/O seam and N/S bounds");
    }
    private static void Verify(PangaeaMapData map, MapViewMode view, MapCamera2D camera, IReadOnlyList<PangaeaHeightLabel> labels)
    {
        var grid = map.Snapshot.Grid!;
        foreach (var label in labels)
        {
            int column = ((label.Column % grid.Definition.Columns) + grid.Definition.Columns) % grid.Definition.Columns;
            Check(label.Row >= 0 && label.Row < grid.Definition.Rows && label.CellId == label.Row * grid.Definition.Columns + column, "Logical identity and open map bounds");
            byte expected = view == MapViewMode.ElevationBase ? map.Snapshot.Base![label.CellId] :
                view == MapViewMode.ElevationContribution ? map.Snapshot.Elevations!.ContributionByCellId.Span[label.CellId] : map.Snapshot.Elevations!.FinalByCellId.Span[label.CellId];
            Check(label.Height == expected && label.Text == expected.ToString(System.Globalization.CultureInfo.InvariantCulture), "Exact active scalar, not a sample or aggregate");
            Check(label.Pixel >= PangaeaHeightLabels.MinimumPixel && label.Pixel <= PangaeaHeightLabels.PreferredPixel, "Readable font range");
            var projected = camera.WorldToScreen(FlatHexGeometry.Center(new(label.Column, label.Row)));
            Check(Vector2.Distance(projected, label.Center) < .001f, "Hex and number use same camera projection");
            var actual = UiRenderer.MeasureTextInk(label.Text, label.Pixel);
            var drawn = new BitmapTextBounds(label.TextOrigin.X + actual.X, label.TextOrigin.Y + actual.Y, actual.Width, actual.Height);
            Check(Vector2.Distance(drawn.Center, label.Center) < .001f && Vector2.Distance(drawn.Center, label.Bounds.Center) < .001f,
                "Actual rendered bitmap is centered in both axes, including narrow glyph bearings");
            foreach (var point in Corners(label.Bounds, PangaeaHeightLabels.Padding))
            {
                Check(InHex(point - label.Center, camera.Scale), "Entire padded text rectangle fits convex hex");
                Check(point.X >= camera.ViewportX - .001f && point.X <= camera.ViewportX + camera.ViewWidth + .001f &&
                    point.Y >= camera.ViewportY - .001f && point.Y <= camera.ViewportY + camera.ViewHeight + .001f, "No generated label outside map viewport");
            }
            double luminance = Luminance(map.Color(label.CellId, view));
            double ratio = label.TextColor.X == 0 ? (luminance + .05) / .05 : 1.05 / (luminance + .05);
            Check(ratio >= 4.5, "Readable black/white contrast comes from layer background");
        }
        var ordered = labels.OrderBy(l => l.Bounds.X).ToArray();
        for (int i = 0; i < ordered.Length; i++)
        for (int j = i + 1; j < ordered.Length && ordered[j].Bounds.X < ordered[i].Bounds.X + ordered[i].Bounds.Width; j++)
        {
            var a = ordered[i].Bounds; var b = ordered[j].Bounds;
            Check(a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y, "Neighboring ink rectangles never overlap");
        }
    }
    private static void EveryEligibleCell(PangaeaMapData map, MapViewMode view, MapCamera2D camera, IReadOnlyList<PangaeaHeightLabel> labels)
    {
        var present = labels.Select(l => (l.Column, l.Row)).ToHashSet();
        var range = camera.VisibleRange(); var grid = map.Snapshot.Grid!;
        for (int row = Math.Max(0, range.FirstRow); row <= Math.Min(grid.Definition.Rows - 1, range.LastRow); row++)
        for (int column = range.FirstColumn; column <= range.LastColumn; column++)
        {
            int id = row * grid.Definition.Columns + ((column % grid.Definition.Columns) + grid.Definition.Columns) % grid.Definition.Columns;
            if (map.Height(id, view) is not { } value) continue;
            var center = camera.WorldToScreen(FlatHexGeometry.Center(new(column, row)));
            var ink = UiRenderer.MeasureTextInk(value.ToString(System.Globalization.CultureInfo.InvariantCulture), PangaeaHeightLabels.MinimumPixel);
            var rect = new BitmapTextBounds(center.X - ink.Width / 2, center.Y - ink.Height / 2, ink.Width, ink.Height);
            bool eligible = Corners(rect, PangaeaHeightLabels.Padding + .01f).All(p => InHex(p - center, camera.Scale) &&
                p.X >= camera.ViewportX && p.X <= camera.ViewportX + camera.ViewWidth && p.Y >= camera.ViewportY && p.Y <= camera.ViewportY + camera.ViewHeight);
            if (eligible) Check(present.Contains((column, row)), "Every geometrically readable visible cell is included without stride or count cap");
        }
    }
    private static void DifferentTextWidths(PangaeaMapData map)
    {
        var grid = map.Snapshot.Grid!;
        int count = grid.Cells.Count;
        foreach (byte value in new byte[] { 0, 1, 10, 90, 100 })
        {
            var fixture = new PangaeaMapData(map.Snapshot with { Base = Enumerable.Repeat(value, count).ToArray(), Elevations = null });
            foreach (float radius in new float[] { 4, 6, 8, 10, 12, 16, 20, 35 })
            {
                var camera = Camera(fixture);
                camera.Zoom(MathF.Log(radius / camera.Scale) / MathF.Log(1.16f), new(640, 240));
                var labels = PangaeaHeightLabels.Build(fixture, MapViewMode.ElevationBase, camera);
                if (radius <= 6) Check(labels.Count == 0, "Unreadable cells hide all labels, regardless of digit count");
                if (radius >= 20) Check(labels.Count > 0, "Readable multi-digit cells remain labelled");
                foreach (var label in labels)
                    Check(label.Height == value && Corners(label.Bounds, PangaeaHeightLabels.Padding).All(p => InHex(p - label.Center, camera.Scale)), "Text-dependent fit preserves individual zero and multi-digit values");
            }
        }
        var ones = new PangaeaMapData(map.Snapshot with { Base = Enumerable.Repeat((byte)1, count).ToArray(), Elevations = null });
        var hundreds = new PangaeaMapData(map.Snapshot with { Base = Enumerable.Repeat((byte)100, count).ToArray(), Elevations = null });
        var shared = Camera(ones); shared.Zoom(MathF.Log(10 / shared.Scale) / MathF.Log(1.16f), new(640, 240));
        Check(PangaeaHeightLabels.Build(ones, MapViewMode.ElevationBase, shared).Count > 0 &&
            PangaeaHeightLabels.Build(hundreds, MapViewMode.ElevationBase, shared).Count == 0, "Same hex size fits one digit but not three: dimensions, not an arbitrary zoom threshold");
    }
    private static IEnumerable<Vector2> Corners(BitmapTextBounds rect, float padding)
    {
        yield return new(rect.X - padding, rect.Y - padding); yield return new(rect.X + rect.Width + padding, rect.Y - padding);
        yield return new(rect.X + rect.Width + padding, rect.Y + rect.Height + padding); yield return new(rect.X - padding, rect.Y + rect.Height + padding);
    }
    private static bool InHex(Vector2 point, float radius)
    {
        // Independent polygon-edge containment, not the production rectangle inequalities.
        for (int i = 0; i < 6; i++)
        {
            double a = i * Math.PI / 3, b = (i + 1) * Math.PI / 3;
            var first = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
            var next = new Vector2((float)Math.Cos(b), (float)Math.Sin(b)) * radius;
            var edge = next - first; var relative = point - first;
            if ((edge.X * relative.Y - edge.Y * relative.X) / edge.Length() < -.001f) return false;
        }
        return true;
    }
    private static double Luminance(Vector3 color)
    {
        static double Linear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
        return .2126 * Linear(color.X) + .7152 * Linear(color.Y) + .0722 * Linear(color.Z);
    }
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
