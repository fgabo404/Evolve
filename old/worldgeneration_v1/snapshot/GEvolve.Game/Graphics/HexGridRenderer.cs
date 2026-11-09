using GEvolve.Engine.Grid;
using GEvolve.State.Game;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.Engine.World.LandmassGeneration;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game.Graphics;

/// <summary>Un hexágono unitario y colores por ID; dos lotes instanciados por vista.</summary>
public sealed class HexGridRenderer : IDisposable
{
    private const float Root3Half = .8660254f;
    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec2 aVertex;
        layout(location = 1) in vec2 aCenter;
        layout(location = 2) in vec3 aColor;
        layout(location = 3) in float aHeight;
        layout(location = 4) in float aSummit;
        uniform vec2 uViewport;
        uniform vec2 uCamera;
        uniform float uScale;
        uniform float uTop;
        uniform float uLeft;
        out vec3 vColor;
        out float vHeight;
        out float vSummit;
        void main() {
            vec2 pixel = (aCenter + aVertex - uCamera) * uScale + vec2(uLeft, uTop);
            gl_Position = vec4(2.0 * pixel.x / uViewport.x - 1.0,
                1.0 - 2.0 * pixel.y / uViewport.y, 0.0, 1.0);
            vColor = aColor;
            vHeight = aHeight;
            vSummit = aSummit;
        }
        """;
    private const string FragmentShader = """
        #version 330 core
        in vec3 vColor;
        in float vHeight;
        in float vSummit;
        uniform int uOutline;
        uniform int uGrayscale;
        uniform int uInfrared;
        uniform vec3 uSummitColor;
        out vec4 color;
        void main() {
            if (uOutline != 0) color = vec4(0.32, 0.42, 0.48, 0.55);
            else color = vec4(vColor, 1.0);
            if (uGrayscale != 0 && vHeight >= 0.0) {
                color = uOutline != 0 ? vec4(vec3(0.4), 0.55)
                    : vec4(vec3(vHeight), 1.0);
            }
            if (uOutline == 0 && vSummit > 0.5 && uInfrared == 0) color = vec4(uSummitColor, 1.0);
        }
        """;

    private readonly HexGrid _grid;
    private readonly LandmassState? _landmass;
    private PangaeaMapData? _laboratory;
    private readonly PangaeaElevationDiagnostics? _elevationDiagnostics;
    private HexTopology? _plateTopology;
    public PlateBoundaryMap? PlateBoundaries { get; }
    private readonly ShaderProgram _shader;
    private readonly int _vao;
    private readonly int _hexVbo;
    private readonly int _instanceVbo;
    private float[] _instances = [];
    private int _instanceCount;
    private int _cameraVersion = -1;
    private MapViewMode _viewMode;

    public int VisibleInstanceCount => _instanceCount;

    public HexGridRenderer(HexGrid grid, LandmassState landmass) : this(grid, landmass, null) { }

    public HexGridRenderer(HexGrid grid, LandmassState landmass,
        PangaeaElevationDiagnostics? elevationDiagnostics)
        : this(grid, landmass, elevationDiagnostics, null) { }

    public HexGridRenderer(PangaeaMapData laboratory)
        : this(laboratory.Snapshot.Grid!, null, null, laboratory) { }

    private HexGridRenderer(HexGrid grid, LandmassState? landmass,
        PangaeaElevationDiagnostics? elevationDiagnostics, PangaeaMapData? laboratory)
    {
        if (landmass is not null && ((landmass.SurfaceGenerationPending ? landmass.SurfaceByCellId.Length != 0 :
                landmass.SurfaceByCellId.Length != grid.Cells.Count) ||
            landmass.Configuration.Size != grid.Definition.Size))
            throw new ArgumentException("Landmass state differs from the grid.", nameof(landmass));
        _grid = grid;
        _landmass = landmass;
        _laboratory = laboratory;
        _elevationDiagnostics = elevationDiagnostics;
        if (landmass?.PlateByCellId is not null)
        {
            _plateTopology = new HexTopology(grid);
            PlateBoundaries = PlateBoundaryMap.Analyze(_plateTopology, landmass);
        }
        _shader = new ShaderProgram(VertexShader, FragmentShader);
        _vao = GL.GenVertexArray();
        _hexVbo = GL.GenBuffer();
        _instanceVbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _hexVbo);
        float[] vertices = [0, 0, 1, 0, .5f, Root3Half, -.5f, Root3Half,
            -1, 0, -.5f, -Root3Half, .5f, -Root3Half, 1, 0];
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.VertexAttribDivisor(1, 1);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false,
            7 * sizeof(float), 2 * sizeof(float));
        GL.VertexAttribDivisor(2, 1);
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false,
            7 * sizeof(float), 5 * sizeof(float));
        GL.VertexAttribDivisor(3, 1);
        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false,
            7 * sizeof(float), 6 * sizeof(float));
        GL.VertexAttribDivisor(4, 1);
        GL.BindVertexArray(0);
    }

    public void UpdateLaboratory(PangaeaMapData laboratory)
    {
        if (_landmass is not null || laboratory.Snapshot.Grid?.Definition != _grid.Definition)
            throw new InvalidOperationException("A laboratory renderer must keep its grid configuration.");
        _laboratory = laboratory;
        _cameraVersion = -1;
    }

    public void Draw(MapCamera2D camera, int width, int height, MapViewMode viewMode = MapViewMode.Biomes)
    {
        if (camera.Columns != _grid.Definition.Columns || camera.Rows != _grid.Definition.Rows)
            throw new InvalidOperationException("Camera and grid dimensions differ.");
        if (_cameraVersion != camera.Version || _viewMode != viewMode)
        {
            _viewMode = viewMode;
            UpdateVisible(camera);
            _cameraVersion = camera.Version;
        }
        _shader.Use();
        GL.Uniform1(_shader.Uniform("uGrayscale"), viewMode == MapViewMode.Elevation ? 1 : 0);
        GL.Uniform1(_shader.Uniform("uInfrared"), viewMode is not (MapViewMode.Biomes or MapViewMode.Elevation) ? 1 : 0);
        var summitColor = BiomePalette.ImpassableSummitColor;
        GL.Uniform3(_shader.Uniform("uSummitColor"), summitColor.X, summitColor.Y, summitColor.Z);
        GL.Uniform2(_shader.Uniform("uViewport"), (float)width, (float)height);
        GL.Uniform2(_shader.Uniform("uCamera"), camera.X, camera.Y);
        GL.Uniform1(_shader.Uniform("uScale"), camera.Scale);
        GL.Uniform1(_shader.Uniform("uTop"), (float)camera.ViewportY);
        GL.Uniform1(_shader.Uniform("uLeft"), (float)camera.ViewportX);
        GL.BindVertexArray(_vao);
        GL.Uniform1(_shader.Uniform("uOutline"), 0);
        GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 8, _instanceCount);
        GL.Uniform1(_shader.Uniform("uOutline"), 1);
        GL.DrawArraysInstanced(PrimitiveType.LineLoop, 1, 6, _instanceCount);
        GL.BindVertexArray(0);
    }

    /// <summary>Draw real shared edges on the visible hex copies, including wrap.</summary>
    public void DrawPlateBoundaries(UiRenderer ui, MapCamera2D camera)
    {
        var plates = _landmass?.PlateByCellId ?? _laboratory?.Snapshot.Plates;
        if (plates is null) return;
        _plateTopology ??= new HexTopology(_grid);
        var range = camera.VisibleRange();
        int columns = _plateTopology.Columns;
        var tint = new Vector4(.99f, .84f, .28f, .95f);
        float thickness = Math.Clamp(camera.Scale * .16f, 1.2f, 4.2f);
        for (int row = range.FirstRow; row <= range.LastRow; row++)
        for (int column = range.FirstColumn; column <= range.LastColumn; column++)
        {
            int wrapped = ((column % columns) + columns) % columns;
            int id = row * columns + wrapped;
            if (PlateBoundaries is not null && !PlateBoundaries.BoundaryByCellId[id]) continue;
            Vector2 center = FlatHexGeometry.Center(new GridAddress(column, row));
            foreach (int neighbor in _plateTopology.Neighbors(id))
            {
                if (id >= neighbor || plates[id] == plates[neighbor]) continue;
                int deltaColumn = neighbor % columns - wrapped;
                if (deltaColumn > columns / 2) deltaColumn -= columns;
                else if (deltaColumn < -columns / 2) deltaColumn += columns;
                Vector2 other = FlatHexGeometry.Center(
                    new GridAddress(column + deltaColumn, neighbor / columns));
                Vector2 delta = other - center;
                float length = delta.Length();
                if (length < .01f) continue;
                Vector2 normal = new(-delta.Y / length, delta.X / length);
                Vector2 middle = (center + other) / 2;
                Vector2 a = camera.WorldToScreen(middle - normal * .5f);
                Vector2 b = camera.WorldToScreen(middle + normal * .5f);
                if (MathF.Max(a.X, b.X) < camera.ViewportX || MathF.Min(a.X, b.X) > camera.ViewportX + camera.ViewWidth ||
                    MathF.Max(a.Y, b.Y) < camera.ViewportY ||
                    MathF.Min(a.Y, b.Y) > camera.ViewHeight + camera.ViewportY)
                    continue;
                ui.Line(a, b, thickness, tint);
            }
        }
    }

    public void DrawPlateMotions(UiRenderer ui, MapCamera2D camera)
    {
        if ((_landmass?.TectonicMotion ?? _laboratory?.Snapshot.Motion) is not { } motion) return;
        int columns = _grid.Definition.Columns;
        var range = camera.VisibleRange();
        foreach (var plate in motion.Motions)
        {
            int id = motion.ArrowCellByPlateId[plate.PlateId];
            int canonical = id % columns, row = id / columns;
            int firstCopy = (int)Math.Ceiling((range.FirstColumn - canonical) / (double)columns);
            int lastCopy = (int)Math.Floor((range.LastColumn - canonical) / (double)columns);
            for (int copy = firstCopy; copy <= lastCopy; copy++)
            {
                var center = camera.WorldToScreen(FlatHexGeometry.Center(new(canonical + copy * columns, row)));
                if (center.Y < camera.ViewportY + 20 || center.Y > camera.ViewportY + camera.ViewHeight - 20) continue;
                var unit = plate.UnitVector;
                Vector2 direction = new((float)unit.X, (float)unit.Y);
                Vector2 tangent = new(-direction.Y, direction.X);
                float length = 22 + (float)plate.SpeedCmPerYear * 3;
                Vector2 start = center - direction * length * .5f, end = center + direction * length * .5f;
                // Dark outline keeps the arrow readable over every plate color.
                foreach (var (width, color) in new[] { (6f, new Vector4(.03f,.04f,.07f,1)), (2.5f, Vector4.One) })
                {
                    ui.Line(start, end, width, color);
                    ui.Line(end, end - direction * 10 + tangent * 6, width, color);
                    ui.Line(end, end - direction * 10 - tangent * 6, width, color);
                }
            }
        }
    }

    public void DrawContinentalGeology(UiRenderer ui, MapCamera2D camera)
    {
        if (_landmass?.ContinentalGeology is not { } geology || _landmass.TectonicMotion is not { } motion) return;
        int columns = _grid.Definition.Columns;
        var range = camera.VisibleRange();
        foreach (var contact in geology.Contacts)
        {
            if (contact.Kind is not (GeologicalContactKind.OceanContinentSubduction or GeologicalContactKind.OceanOceanSubduction)) continue;
            var edge = motion.Interactions[contact.InteractionIndex];
            int column = edge.FirstCellId % columns, row = edge.FirstCellId / columns;
            for (int copy = (int)Math.Floor((range.FirstColumn - column) / (double)columns);
                column + copy * columns <= range.LastColumn; copy++)
            {
                Vector2 center = FlatHexGeometry.Center(new(column + copy * columns, row));
                Vector2 normal = new((float)edge.Normal.X, (float)edge.Normal.Y);
                Vector2 tangent = new(-normal.Y, normal.X);
                Vector2 middle = center + normal * (.8660254f);
                Vector2 screen = camera.WorldToScreen(middle);
                if (screen.X < camera.ViewportX || screen.X > camera.ViewportX + camera.ViewWidth || screen.Y < camera.ViewportY || screen.Y > camera.ViewportY + camera.ViewHeight) continue;
                ui.Line(camera.WorldToScreen(middle - tangent * .5f), camera.WorldToScreen(middle + tangent * .5f),
                    2, new(1, .65f, .22f, 1));
            }
        }
        foreach (var volcano in geology.Volcanoes)
        {
            int column = volcano.CellId % columns, row = volcano.CellId / columns;
            for (int copy = (int)Math.Ceiling((range.FirstColumn - column) / (double)columns);
                column + copy * columns <= range.LastColumn; copy++)
            {
                Vector2 point = camera.WorldToScreen(FlatHexGeometry.Center(new(column + copy * columns, row)));
                if (point.Y < camera.ViewportY || point.Y > camera.ViewportY + camera.ViewHeight) continue;
                Vector2 top = point + new Vector2(0, -5), left = point + new Vector2(-4, 4), right = point + new Vector2(4, 4);
                foreach (var (width, tint) in new[] { (4f, new Vector4(.08f,.04f,.03f,1)), (1.5f, new Vector4(1,.85f,.32f,1)) })
                { ui.Line(top, left, width, tint); ui.Line(left, right, width, tint); ui.Line(right, top, width, tint); }
            }
        }
    }

    public void DrawPangaeaMountainRanges(UiRenderer ui, MapCamera2D camera)
    {
        if (_laboratory?.Snapshot.Mountains is { } selection)
        {
            foreach (var system in selection.Systems) DrawMountainAxis(ui, camera, system.AxisCellIds, system.WidthByAxisCell);
            return;
        }
        if (_landmass?.PangaeaMountainRanges is not { } state) return;
        foreach (var mountain in state.Ranges) DrawMountainAxis(ui, camera, mountain.AxisCellIds, null);
    }

    private void DrawMountainAxis(UiRenderer ui, MapCamera2D camera, int[] axis, byte[]? widths)
    {
        int columns = _grid.Definition.Columns;
        var visible = camera.VisibleRange();
        for (int i = 1; i < axis.Length; i++)
        {
            int previous = axis[i - 1], current = axis[i];
            int x = previous % columns, nextX = current % columns;
            int step = nextX - x;
            if (step > columns / 2) nextX -= columns;
            if (step < -columns / 2) nextX += columns;
            int row = previous / columns, nextRow = current / columns;
            for (int copy = (int)Math.Floor((visible.FirstColumn - Math.Max(x, nextX)) / (double)columns);
                 Math.Min(x, nextX) + copy * columns <= visible.LastColumn; copy++)
            {
                var a = camera.WorldToScreen(FlatHexGeometry.Center(new(x + copy * columns, row)));
                var b = camera.WorldToScreen(FlatHexGeometry.Center(new(nextX + copy * columns, nextRow)));
                if (Math.Max(a.Y, b.Y) < camera.ViewportY ||
                    Math.Min(a.Y, b.Y) > camera.ViewportY + camera.ViewHeight) continue;
                if (widths is not null)
                    ui.Line(a, b, Math.Max(2, widths[i] * 1.7320508f * camera.Scale), new(.7f, .48f, .22f, .16f));
                ui.Line(a, b, 2, new(1f, .86f, .38f, 1f));
            }
        }
    }

    private void UpdateVisible(MapCamera2D camera)
    {
        VisibleHexRange range = camera.VisibleRange();
        int count = range.Count;
        if (_instances.Length < count * 7) _instances = new float[count * 7];
        int index = 0;
        int columns = _grid.Definition.Columns;
        for (int row = range.FirstRow; row <= range.LastRow; row++)
        for (int column = range.FirstColumn; column <= range.LastColumn; column++)
        {
            var center = FlatHexGeometry.Center(new GridAddress(column, row));
            int wrapped = ((column % columns) + columns) % columns;
            _instances[index++] = center.X;
            _instances[index++] = center.Y;
            var color = _laboratory is { } lab ? lab.Color(row * columns + wrapped, _viewMode) :
                MapViewColors.ForCell(_landmass!, row * columns + wrapped, _viewMode, _elevationDiagnostics);
            _instances[index++] = color.X;
            _instances[index++] = color.Y;
            _instances[index++] = color.Z;
            _instances[index++] = _landmass?.ElevationByCellId is { } heights
                ? heights[row * columns + wrapped] / 100f : -1f;
            _instances[index++] = _landmass?.IsImpassableSummit(row * columns + wrapped) == true ? 1f : 0f;
        }
        _instanceCount = index / 7;
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, index * sizeof(float),
            _instances, BufferUsageHint.StreamDraw);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_instanceVbo);
        GL.DeleteBuffer(_hexVbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
    }
}
