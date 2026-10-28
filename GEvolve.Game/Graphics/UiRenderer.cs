using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game.Graphics;

/// <summary>Interfaz 2D propia: rectángulos y fuente bitmap en un solo lote OpenGL.</summary>
public sealed class UiRenderer : IDisposable
{
    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec2 aPosition;
        layout(location = 1) in vec4 aColor;
        uniform vec2 uViewport;
        out vec4 vColor;
        void main() {
            gl_Position = vec4(2.0 * aPosition.x / uViewport.x - 1.0,
                1.0 - 2.0 * aPosition.y / uViewport.y, 0.0, 1.0);
            vColor = aColor;
        }
        """;
    private const string FragmentShader = """
        #version 330 core
        in vec4 vColor;
        out vec4 color;
        void main() { color = vColor; }
        """;

    private readonly ShaderProgram _shader = new(VertexShader, FragmentShader);
    private readonly int _vao;
    private readonly int _vbo;
    private readonly List<float> _vertices = [];

    // Five-column glyphs, seven rows. This keeps all menu text local to the game.
    private static readonly Dictionary<char, byte[]> Glyphs = new()
    {
        ['A'] = [14,17,17,31,17,17,17], ['B'] = [30,17,17,30,17,17,30],
        ['Á'] = [4,14,17,31,17,17,17],
        ['C'] = [14,17,16,16,16,17,14], ['D'] = [30,17,17,17,17,17,30],
        ['E'] = [31,16,16,30,16,16,31], ['F'] = [31,16,16,30,16,16,16],
        ['É'] = [4,31,16,30,16,16,31],
        ['G'] = [14,17,16,23,17,17,14], ['H'] = [17,17,17,31,17,17,17],
        ['I'] = [31,4,4,4,4,4,31], ['J'] = [7,2,2,2,18,18,12],
        ['Í'] = [4,31,4,4,4,4,31],
        ['K'] = [17,18,20,24,20,18,17], ['L'] = [16,16,16,16,16,16,31],
        ['M'] = [17,27,21,21,17,17,17], ['N'] = [17,25,21,19,17,17,17],
        ['Ñ'] = [10,21,25,21,19,17,17], ['O'] = [14,17,17,17,17,17,14],
        ['Ó'] = [4,14,17,17,17,17,14],
        ['P'] = [30,17,17,30,16,16,16], ['Q'] = [14,17,17,17,21,18,13],
        ['R'] = [30,17,17,30,20,18,17], ['S'] = [15,16,16,14,1,1,30],
        ['T'] = [31,4,4,4,4,4,4], ['U'] = [17,17,17,17,17,17,14],
        ['Ú'] = [4,17,17,17,17,17,14],
        ['V'] = [17,17,17,17,17,10,4], ['W'] = [17,17,17,21,21,21,10],
        ['X'] = [17,17,10,4,10,17,17], ['Y'] = [17,17,10,4,4,4,4],
        ['Z'] = [31,1,2,4,8,16,31],
        ['0'] = [14,17,19,21,25,17,14], ['1'] = [4,12,4,4,4,4,14],
        ['2'] = [14,17,1,2,4,8,31], ['3'] = [30,1,1,14,1,1,30],
        ['4'] = [2,6,10,18,31,2,2], ['5'] = [31,16,16,30,1,1,30],
        ['6'] = [14,16,16,30,17,17,14], ['7'] = [31,1,2,4,8,8,8],
        ['8'] = [14,17,17,14,17,17,14], ['9'] = [14,17,17,15,1,1,14],
        [':'] = [0,4,4,0,4,4,0], ['-'] = [0,0,0,31,0,0,0],
        ['.'] = [0,0,0,0,0,12,12], [','] = [0,0,0,0,4,4,8],
        ['('] = [2,4,8,8,8,4,2], [')'] = [8,4,2,2,2,4,8],
        [' '] = [0,0,0,0,0,0,0]
    };

    public UiRenderer()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), 2 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public void Clear() => _vertices.Clear();

    public void Rect(float x, float y, float width, float height, Vector4 color)
    {
        Triangle(x, y, x + width, y, x + width, y + height, color);
        Triangle(x, y, x + width, y + height, x, y + height, color);
    }

    public void Border(float x, float y, float width, float height, float thickness, Vector4 color)
    {
        Rect(x, y, width, thickness, color);
        Rect(x, y + height - thickness, width, thickness, color);
        Rect(x, y, thickness, height, color);
        Rect(x + width - thickness, y, thickness, height, color);
    }

    public void Hex(float x, float y, float radius, Vector4 fill, Vector4 edge)
    {
        Span<Vector2> points = stackalloc Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = i * MathF.PI / 3f;
            points[i] = new Vector2(x + radius * MathF.Cos(angle),
                y + radius * MathF.Sin(angle));
        }
        for (int i = 0; i < 6; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % 6];
            Triangle(x, y, a.X, a.Y, b.X, b.Y, fill);
            Vector2 direction = Vector2.Normalize(b - a);
            Vector2 normal = new(-direction.Y, direction.X);
            float dx = normal.X * .7f, dy = normal.Y * .7f;
            Triangle(a.X - dx, a.Y - dy, b.X - dx, b.Y - dy,
                b.X + dx, b.Y + dy, edge);
            Triangle(a.X - dx, a.Y - dy, b.X + dx, b.Y + dy,
                a.X + dx, a.Y + dy, edge);
        }
    }

    public void Text(string value, float x, float y, float pixel, Vector4 color, bool centered = true)
    {
        string upper = value.ToUpperInvariant();
        float left = centered ? x - (upper.Length * 6 - 1) * pixel / 2 : x;
        for (int i = 0; i < upper.Length; i++)
        {
            if (!Glyphs.TryGetValue(upper[i], out byte[]? rows)) continue;
            for (int row = 0; row < 7; row++)
            for (int column = 0; column < 5; column++)
                if ((rows[row] & (1 << (4 - column))) != 0)
                    Rect(left + (i * 6 + column) * pixel, y + row * pixel,
                        pixel, pixel, color);
        }
    }

    public void Flush(int width, int height)
    {
        if (_vertices.Count == 0) return;
        _shader.Use();
        GL.Uniform2(_shader.Uniform("uViewport"), (float)width, (float)height);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        float[] data = _vertices.ToArray();
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data,
            BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / 6);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
    }

    private void Triangle(float x1, float y1, float x2, float y2,
        float x3, float y3, Vector4 color)
    {
        Vertex(x1, y1, color);
        Vertex(x2, y2, color);
        Vertex(x3, y3, color);
    }

    private void Vertex(float x, float y, Vector4 color)
    {
        _vertices.Add(x); _vertices.Add(y);
        _vertices.Add(color.X); _vertices.Add(color.Y);
        _vertices.Add(color.Z); _vertices.Add(color.W);
    }
}
