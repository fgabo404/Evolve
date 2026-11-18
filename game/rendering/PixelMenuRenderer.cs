// Renders colored rectangles and bitmap text in one OpenGL batch.
// Uses LegacyBitmapFont glyphs and owns the GPU resources managed by EvolveWindow.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Evolve.Game;

internal sealed class PixelMenuRenderer : IDisposable
{
    private const int VertexSize = 6;
    private readonly List<float> vertices = new();
    private int program;
    private int vertexArray;
    private int vertexBuffer;
    private int viewportUniform;

    private const string VertexShaderSource = """
        #version 330 core
        layout(location = 0) in vec2 position;
        layout(location = 1) in vec4 vertexColor;
        uniform vec2 viewportSize;
        out vec4 tint;
        void main()
        {
            vec2 normalized = position / viewportSize;
            gl_Position = vec4(normalized.x * 2.0 - 1.0, 1.0 - normalized.y * 2.0, 0.0, 1.0);
            tint = vertexColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec4 tint;
        out vec4 outputColor;
        void main()
        {
            outputColor = tint;
        }
        """;

    // Create GPU resources only while the owning window's context is current.
    public PixelMenuRenderer()
    {
        try
        {
            program = CreateProgram();
            viewportUniform = GL.GetUniformLocation(program, "viewportSize");
            vertexArray = GL.GenVertexArray();
            vertexBuffer = GL.GenBuffer();
            GL.BindVertexArray(vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VertexSize * sizeof(float), 0);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, VertexSize * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Multisample);
            GL.Disable(EnableCap.Dither);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    // Use framebuffer dimensions rather than window units, including on high-DPI displays.
    public void Begin(int width, int height)
    {
        vertices.Clear();
        GL.Viewport(0, 0, width, height);
        GL.UseProgram(program);
        GL.Uniform2(viewportUniform, (float)width, (float)height);
    }

    // Glyph pixels and menu surfaces share the existing colored triangle batch.
    public void Rectangle(float x, float y, float width, float height, Color4 color)
    {
        Vertex(x, y, color);
        Vertex(x + width, y, color);
        Vertex(x + width, y + height, color);
        Vertex(x, y, color);
        Vertex(x + width, y + height, color);
        Vertex(x, y + height, color);
    }

    // Preserve UiRenderer.Text's five lit columns, six-pixel advance and centering.
    public void CenteredText(string text, float center, float y, float pixel, Color4 color)
    {
        float left = center - (text.Length * LegacyBitmapFont.Advance - 1) * pixel / 2;
        for (int index = 0; index < text.Length; index++)
        {
            ReadOnlySpan<byte> rows = LegacyBitmapFont.Rows(text[index]);
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < LegacyBitmapFont.Width; column++)
                {
                    if ((rows[row] & (1 << (LegacyBitmapFont.Width - 1 - column))) != 0)
                    {
                        Rectangle(left + (index * LegacyBitmapFont.Advance + column) * pixel,
                            y + row * pixel, pixel, pixel, color);
                    }
                }
            }
        }
    }

    // Use the legacy thin border without highlights or beveled inner surfaces.
    public void Border(float x, float y, float width, float height, float thickness, Color4 color)
    {
        Rectangle(x, y, width, thickness, color);
        Rectangle(x, y + height - thickness, width, thickness, color);
        Rectangle(x, y, thickness, height, color);
        Rectangle(x + width - thickness, y, thickness, height, color);
    }

    // Upload the composed menu once and preserve submission order for borders and shadows.
    public void End()
    {
        float[] data = vertices.ToArray();
        GL.BindVertexArray(vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / VertexSize);
    }

    // Match the position and color attribute layout declared by the shader.
    private void Vertex(float x, float y, Color4 color)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(color.R);
        vertices.Add(color.G);
        vertices.Add(color.B);
        vertices.Add(color.A);
    }

    // Retain compiler diagnostics and release incomplete shaders on failure.
    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
        {
            string diagnostic = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Menu shader compilation failed: {diagnostic}");
        }

        return shader;
    }

    // Shader objects are temporary; only the linked program survives initialization.
    private static int CreateProgram()
    {
        int vertex = 0;
        int fragment = 0;
        int linkedProgram = 0;
        try
        {
            vertex = CompileShader(ShaderType.VertexShader, VertexShaderSource);
            fragment = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);
            linkedProgram = GL.CreateProgram();
            GL.AttachShader(linkedProgram, vertex);
            GL.AttachShader(linkedProgram, fragment);
            GL.LinkProgram(linkedProgram);
            GL.GetProgram(linkedProgram, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                throw new InvalidOperationException($"Menu shader linking failed: {GL.GetProgramInfoLog(linkedProgram)}");
            }

            return linkedProgram;
        }
        catch
        {
            if (linkedProgram != 0) GL.DeleteProgram(linkedProgram);
            throw;
        }
        finally
        {
            if (vertex != 0) GL.DeleteShader(vertex);
            if (fragment != 0) GL.DeleteShader(fragment);
        }
    }

    // Release every GPU handle before OpenTK destroys the owning context.
    public void Dispose()
    {
        if (vertexBuffer != 0) GL.DeleteBuffer(vertexBuffer);
        if (vertexArray != 0) GL.DeleteVertexArray(vertexArray);
        if (program != 0) GL.DeleteProgram(program);
        vertexBuffer = vertexArray = program = 0;
    }
}
