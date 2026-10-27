using GEvolve.Engine.Grid;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game.Graphics;

/// <summary>Un hexágono unitario y centros visibles en un VBO; una llamada instanciada.</summary>
public sealed class HexGridRenderer : IDisposable
{
    private const float Root3Half = .8660254f;
    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec2 aVertex;
        layout(location = 1) in vec2 aCenter;
        uniform vec2 uViewport;
        uniform vec2 uCamera;
        uniform float uScale;
        uniform float uTop;
        void main() {
            vec2 pixel = (aCenter + aVertex - uCamera) * uScale + vec2(0.0, uTop);
            gl_Position = vec4(2.0 * pixel.x / uViewport.x - 1.0,
                1.0 - 2.0 * pixel.y / uViewport.y, 0.0, 1.0);
        }
        """;
    private const string FragmentShader = """
        #version 330 core
        out vec4 color;
        void main() { color = vec4(0.67, 0.71, 0.75, 1.0); }
        """;

    private readonly HexGrid _grid;
    private readonly ShaderProgram _shader;
    private readonly int _vao;
    private readonly int _hexVbo;
    private readonly int _instanceVbo;
    private float[] _centers = [];
    private int _instanceCount;
    private int _cameraVersion = -1;

    public int VisibleInstanceCount => _instanceCount;

    public HexGridRenderer(HexGrid grid)
    {
        _grid = grid;
        _shader = new ShaderProgram(VertexShader, FragmentShader);
        _vao = GL.GenVertexArray();
        _hexVbo = GL.GenBuffer();
        _instanceVbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _hexVbo);
        float[] vertices = [1, 0, .5f, Root3Half, -.5f, Root3Half,
            -1, 0, -.5f, -Root3Half, .5f, -Root3Half];
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.VertexAttribDivisor(1, 1);
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
        GL.DrawArraysInstanced(PrimitiveType.LineLoop, 0, 6, _instanceCount);
        GL.BindVertexArray(0);
    }

    private void UpdateVisible(MapCamera2D camera)
    {
        VisibleHexRange range = camera.VisibleRange();
        int count = range.Count;
        if (_centers.Length < count * 2) _centers = new float[count * 2];
        int index = 0;
        for (int row = range.FirstRow; row <= range.LastRow; row++)
        for (int column = range.FirstColumn; column <= range.LastColumn; column++)
        {
            var center = FlatHexGeometry.Center(new GridAddress(column, row));
            _centers[index++] = center.X;
            _centers[index++] = center.Y;
        }
        _instanceCount = index / 2;
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, index * sizeof(float),
            _centers, BufferUsageHint.StreamDraw);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_instanceVbo);
        GL.DeleteBuffer(_hexVbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
    }
}
