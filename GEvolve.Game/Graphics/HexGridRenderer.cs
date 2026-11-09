using GEvolve.Engine.Grid;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game.Graphics;

/// <summary>Neutral instanced grid only. No geography, palettes or per-cell scalar fields.</summary>
public sealed class HexGridRenderer : IDisposable
{
    private const string Vertex = """
        #version 330 core
        layout(location=0) in vec2 aOffset;
        layout(location=1) in vec2 aCenter;
        uniform vec2 uViewport;
        uniform vec2 uOrigin;
        uniform vec2 uCamera;
        uniform float uScale;
        void main() {
            vec2 p=(aCenter+aOffset-uCamera)*uScale+uOrigin;
            gl_Position=vec4(2.0*p.x/uViewport.x-1.0,1.0-2.0*p.y/uViewport.y,0,1);
        }
        """;
    private const string Fragment = """
        #version 330 core
        uniform int uOutline;
        out vec4 color;
        void main() { color = uOutline != 0 ? vec4(.36,.43,.48,1) : vec4(.84,.88,.90,1); }
        """;
    private readonly HexGrid _grid;
    private readonly ShaderProgram _shader = new(Vertex, Fragment);
    private readonly int _vao, _vertices, _instances;
    private int _cameraVersion = -1, _count;
    public HexGridRenderer(HexGrid grid)
    {
        _grid = grid;
        _vao = GL.GenVertexArray(); _vertices = GL.GenBuffer(); _instances = GL.GenBuffer();
        GL.BindVertexArray(_vao); GL.BindBuffer(BufferTarget.ArrayBuffer, _vertices);
        float[] vertices = [0, 0, 1, 0, .5f, .8660254f, -.5f, .8660254f, -1, 0, -.5f, -.8660254f, .5f, -.8660254f, 1, 0];
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0); GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instances);
        GL.EnableVertexAttribArray(1); GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.VertexAttribDivisor(1, 1); GL.BindVertexArray(0);
    }
    public void Draw(MapCamera2D camera, int width, int height)
    {
        if (camera.Columns != _grid.Definition.Columns || camera.Rows != _grid.Definition.Rows)
            throw new InvalidOperationException("Grid and camera dimensions differ.");
        if (_cameraVersion != camera.Version)
        {
            var range = camera.VisibleRange(); var centers = new List<float>();
            for (int row = range.FirstRow; row <= range.LastRow; row++)
            for (int col = range.FirstColumn; col <= range.LastColumn; col++)
            {
                var center = FlatHexGeometry.Center(new(col, row)); centers.Add(center.X); centers.Add(center.Y);
            }
            var data = centers.ToArray(); _count = data.Length / 2;
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instances);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StreamDraw);
            _cameraVersion = camera.Version;
        }
        _shader.Use();
        GL.Uniform2(_shader.Uniform("uViewport"), (float)width, (float)height);
        GL.Uniform2(_shader.Uniform("uOrigin"), (float)camera.ViewportX, (float)camera.ViewportY);
        GL.Uniform2(_shader.Uniform("uCamera"), camera.X, camera.Y);
        GL.Uniform1(_shader.Uniform("uScale"), camera.Scale);
        GL.BindVertexArray(_vao);
        GL.Uniform1(_shader.Uniform("uOutline"), 0); GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, 8, _count);
        GL.Uniform1(_shader.Uniform("uOutline"), 1); GL.DrawArraysInstanced(PrimitiveType.LineLoop, 1, 6, _count);
        GL.BindVertexArray(0);
    }
    public void Dispose() { GL.DeleteBuffer(_instances); GL.DeleteBuffer(_vertices); GL.DeleteVertexArray(_vao); _shader.Dispose(); }
}
