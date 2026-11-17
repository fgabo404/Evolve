using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Evolve.Game;

// Batches menu rectangles and local bitmap glyphs without a general UI framework.
internal sealed class PixelMenuRenderer : IDisposable
{
    private const int AtlasWidth = 128;
    private const int AtlasHeight = 96;
    private const int GlyphWidth = 8;
    private const int GlyphHeight = 16;
    private const int VertexSize = 8;
    private readonly List<float> vertices = new();
    private int program;
    private int vertexArray;
    private int vertexBuffer;
    private int atlasTexture;
    private int viewportUniform;

    private const string VertexShaderSource = """
        #version 330 core
        layout(location = 0) in vec2 position;
        layout(location = 1) in vec2 texturePosition;
        layout(location = 2) in vec4 vertexColor;
        uniform vec2 viewportSize;
        out vec2 atlasPosition;
        out vec4 tint;
        void main()
        {
            vec2 normalized = position / viewportSize;
            gl_Position = vec4(normalized.x * 2.0 - 1.0, 1.0 - normalized.y * 2.0, 0.0, 1.0);
            atlasPosition = texturePosition;
            tint = vertexColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec2 atlasPosition;
        in vec4 tint;
        uniform sampler2D fontAtlas;
        out vec4 outputColor;
        void main()
        {
            float coverage = texture(fontAtlas, atlasPosition).r;
            outputColor = vec4(tint.rgb, tint.a * coverage);
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
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexSize * sizeof(float), 2 * sizeof(float));
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, VertexSize * sizeof(float), 4 * sizeof(float));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            using var stream = typeof(PixelMenuRenderer).Assembly
                .GetManifestResourceStream("Evolve.Game.MonocraftAtlas")
                ?? throw new InvalidOperationException("The embedded Monocraft atlas is missing.");
            var pixels = new byte[AtlasWidth * AtlasHeight];
            stream.ReadExactly(pixels);
            atlasTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, atlasTexture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, AtlasWidth, AtlasHeight,
                0, PixelFormat.Red, PixelType.UnsignedByte, pixels);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.UseProgram(program);
            GL.Uniform1(GL.GetUniformLocation(program, "fontAtlas"), 0);
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

    // Sample the atlas's dedicated white texel to share one batch with the text.
    public void Rectangle(int x, int y, int width, int height, Color4 color)
    {
        float u = (AtlasWidth - 0.5f) / AtlasWidth;
        float v = (AtlasHeight - 0.5f) / AtlasHeight;
        Quad(x, y, width, height, u, v, u, v, color);
    }

    // Each cell advances by eight pixels; whole-number scaling avoids blurred strokes.
    public void CenteredText(string text, int center, int y, int scale, Color4 color)
    {
        int x = center - text.Length * GlyphWidth * scale / 2;
        foreach (char character in text)
        {
            int index = (character is >= ' ' and <= '~' ? character : '?') - ' ';
            float u = index % 16 * GlyphWidth / (float)AtlasWidth;
            float v = index / 16 * GlyphHeight / (float)AtlasHeight;
            Quad(x, y, GlyphWidth * scale, GlyphHeight * scale, u, v,
                u + GlyphWidth / (float)AtlasWidth, v + GlyphHeight / (float)AtlasHeight, color);
            x += GlyphWidth * scale;
        }
    }

    // Upload the composed menu once and preserve submission order for bevels and shadows.
    public void End()
    {
        float[] data = vertices.ToArray();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, atlasTexture);
        GL.BindVertexArray(vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / VertexSize);
    }

    // Expand one top-left rectangle into two triangles using the same winding.
    private void Quad(int x, int y, int width, int height, float u0, float v0, float u1, float v1, Color4 color)
    {
        Vertex(x, y, u0, v0, color);
        Vertex(x + width, y, u1, v0, color);
        Vertex(x + width, y + height, u1, v1, color);
        Vertex(x, y, u0, v0, color);
        Vertex(x + width, y + height, u1, v1, color);
        Vertex(x, y + height, u0, v1, color);
    }

    // Match the position, texture and color attribute layout declared by the shader.
    private void Vertex(float x, float y, float u, float v, Color4 color)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(u);
        vertices.Add(v);
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
        if (atlasTexture != 0) GL.DeleteTexture(atlasTexture);
        if (vertexBuffer != 0) GL.DeleteBuffer(vertexBuffer);
        if (vertexArray != 0) GL.DeleteVertexArray(vertexArray);
        if (program != 0) GL.DeleteProgram(program);
        atlasTexture = vertexBuffer = vertexArray = program = 0;
    }
}
