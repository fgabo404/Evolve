using GEvolve.Engine.Grid;
using GEvolve.State.Game;
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
        uniform vec2 uViewport;
        uniform vec2 uCamera;
        uniform float uScale;
        uniform float uTop;
        out vec3 vColor;
        void main() {
            vec2 pixel = (aCenter + aVertex - uCamera) * uScale + vec2(0.0, uTop);
            gl_Position = vec4(2.0 * pixel.x / uViewport.x - 1.0,
                1.0 - 2.0 * pixel.y / uViewport.y, 0.0, 1.0);
            vColor = aColor;
        }
        """;
    private const string FragmentShader = """
        #version 330 core
        in vec3 vColor;
        uniform int uOutline;
        out vec4 color;
        void main() {
            if (uOutline != 0) color = vec4(0.32, 0.42, 0.48, 0.55);
            else color = vec4(vColor, 1.0);
        }
        """;

    private readonly HexGrid _grid;
    private readonly LandmassState _landmass;
    private readonly ShaderProgram _shader;
    private readonly int _vao;
    private readonly int _hexVbo;
    private readonly int _instanceVbo;
    private float[] _instances = [];
    private int _instanceCount;
    private int _cameraVersion = -1;

    public int VisibleInstanceCount => _instanceCount;

    public HexGridRenderer(HexGrid grid, LandmassState landmass)
    {
        if (landmass.SurfaceByCellId.Length != grid.Cells.Count ||
            landmass.Configuration.Size != grid.Definition.Size)
            throw new ArgumentException("Landmass state differs from the grid.", nameof(landmass));
        _grid = grid;
        _landmass = landmass;
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
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.VertexAttribDivisor(1, 1);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false,
            5 * sizeof(float), 2 * sizeof(float));
        GL.VertexAttribDivisor(2, 1);
        GL.BindVertexArray(0);
    }

    public void Draw(MapCamera2D camera, int width, int height)
    {
        if (camera.Columns != _grid.Definition.Columns || camera.Rows != _grid.Definition.Rows)
            throw new InvalidOperationException("Camera and grid dimensions differ.");
        if (_cameraVersion != camera.Version)
        {
            UpdateVisible(camera);
            _cameraVersion = camera.Version;
        }
        _shader.Use();
        GL.Uniform2(_shader.Uniform("uViewport"), (float)width, (float)height);
        GL.Uniform2(_shader.Uniform("uCamera"), camera.X, camera.Y);
        GL.Uniform1(_shader.Uniform("uScale"), camera.Scale);
        GL.Uniform1(_shader.Uniform("uTop"), (float)MapCamera2D.TopBarHeight);
        GL.BindVertexArray(_vao);
        GL.Uniform1(_shader.Uniform("uOutline"), 0);
        GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 8, _instanceCount);
        GL.Uniform1(_shader.Uniform("uOutline"), 1);
        GL.DrawArraysInstanced(PrimitiveType.LineLoop, 1, 6, _instanceCount);
        GL.BindVertexArray(0);
    }

    private void UpdateVisible(MapCamera2D camera)
    {
        VisibleHexRange range = camera.VisibleRange();
        int count = range.Count;
        if (_instances.Length < count * 5) _instances = new float[count * 5];
        int index = 0;
        int columns = _grid.Definition.Columns;
        for (int row = range.FirstRow; row <= range.LastRow; row++)
        for (int column = range.FirstColumn; column <= range.LastColumn; column++)
        {
            var center = FlatHexGeometry.Center(new GridAddress(column, row));
            int wrapped = ((column % columns) + columns) % columns;
            _instances[index++] = center.X;
            _instances[index++] = center.Y;
            var color = BiomePalette.ForCell(_landmass, row * columns + wrapped);
            _instances[index++] = color.X;
            _instances[index++] = color.Y;
            _instances[index++] = color.Z;
        }
        _instanceCount = index / 5;
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
