using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Desktop;

/// <summary>
/// Renderiza un hexágono de prueba en OpenGL 3.3 Core.
/// Es una implementación gráfica aislada: las coordenadas y reglas
/// del mundo permanecen en GEvolve.Core.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao;
    private int _vbo;
    private int _programa;
    private int _colorUniform;
    private bool _inicializado;

    // GLSL: la geometría del prototipo se expresa en coordenadas normalizadas.
    private const string VertexShader = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        void main() {
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        out vec4 FragColor;
        uniform vec3 uColor;
        void main() {
            FragColor = vec4(uColor, 1.0);
        }
        """;

    /// <summary>
    /// Crea shaders y buffers. Debe invocarse después de que exista
    /// un contexto OpenGL activo, normalmente desde OnLoad.
    /// </summary>
    public void Inicializar()
    {
        if (_inicializado)
            throw new InvalidOperationException("El renderizador ya fue inicializado.");

        // Ocho vértices: centro + seis esquinas + primera esquina repetida.
        // TRIANGLE_FAN necesita cerrar explícitamente el sexto triángulo.
        const float radio = 0.43f;
        var vertices = new float[16];
        for (int i = 0; i < 6; i++)
        {
            float angulo = MathF.PI / 3f * i;
            vertices[(i + 1) * 2] = radio * MathF.Sin(angulo);
            vertices[(i + 1) * 2 + 1] = radio * MathF.Cos(angulo);
        }

        vertices[14] = vertices[2];
        vertices[15] = vertices[3];

        int vs = CompilarShader(ShaderType.VertexShader, VertexShader);
        int fs = CompilarShader(ShaderType.FragmentShader, FragmentShader);
        try
        {
            _programa = GL.CreateProgram();
            GL.AttachShader(_programa, vs);
            GL.AttachShader(_programa, fs);
            GL.LinkProgram(_programa);
            GL.GetProgram(_programa, GetProgramParameterName.LinkStatus, out int enlazado);
            if (enlazado == 0)
                throw new InvalidOperationException("Error enlazando shaders: " + GL.GetProgramInfoLog(_programa));
        }
        finally
        {
            GL.DetachShader(_programa, vs);
            GL.DetachShader(_programa, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        _colorUniform = GL.GetUniformLocation(_programa, "uColor");
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
        _inicializado = true;
    }

    /// <summary>
    /// Dibuja el relleno verde y el borde oscuro. En esta etapa solo
    /// verificamos un hexágono plano; todavía no es la cuadrícula 20x20.
    /// </summary>
    public void Dibujar()
    {
        if (!_inicializado)
            throw new InvalidOperationException("Inicializar primero el renderizador.");

        GL.UseProgram(_programa);
        GL.BindVertexArray(_vao);

        GL.Uniform3(_colorUniform, 0.55f, 0.67f, 0.49f);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 8);

        GL.Uniform3(_colorUniform, 0.09f, 0.16f, 0.19f);
        GL.LineWidth(2f);
        GL.DrawArrays(PrimitiveType.LineLoop, 1, 6);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    /// <summary>Libera los recursos GPU con el contexto aún activo.</summary>
    public void Dispose()
    {
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_programa != 0) GL.DeleteProgram(_programa);
        _vbo = _vao = _programa = 0;
        _inicializado = false;
    }

    private static int CompilarShader(ShaderType tipo, string codigo)
    {
        int shader = GL.CreateShader(tipo);
        GL.ShaderSource(shader, codigo);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int correcto);
        if (correcto != 0) return shader;

        string detalle = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException("Error compilando GLSL: " + detalle);
    }
}
