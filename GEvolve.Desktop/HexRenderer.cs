using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Desktop;

/// <summary>
/// Renderizador OpenGL 3.3 de una celda hexagonal tridimensional.
/// Su geometría tiene tapa superior y seis paredes: la altura visible es real.
/// No controla cámara, entrada, lógica del mundo ni simulación.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao, _vbo, _programa, _matrizUniform;
    private int _triangulos, _lineas;

    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aColor;
        uniform mat4 uVistaProyeccion;
        out vec3 color;
        void main()
        {
            color = aColor;
            gl_Position = uVistaProyeccion * vec4(aPos, 1.0);
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in vec3 color;
        out vec4 FragColor;
        void main() { FragColor = vec4(color, 1.0); }
        """;

    /// <summary>Reserva buffers y compila shaders con un contexto OpenGL activo.</summary>
    public void Inicializar()
    {
        if (_programa != 0) throw new InvalidOperationException("Renderizador ya inicializado.");

        int vs = Compilar(ShaderType.VertexShader, VertexShader);
        int fs = Compilar(ShaderType.FragmentShader, FragmentShader);
        _programa = GL.CreateProgram();
        GL.AttachShader(_programa, vs);
        GL.AttachShader(_programa, fs);
        GL.LinkProgram(_programa);
        GL.GetProgram(_programa, GetProgramParameterName.LinkStatus, out int ok);
        GL.DetachShader(_programa, vs);
        GL.DetachShader(_programa, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        if (ok == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(_programa));
        _matrizUniform = GL.GetUniformLocation(_programa, "uVistaProyeccion");

        // Sistema 3D: X/Z son la superficie del terreno; Y es la elevación.
        // Vértices pointy-top según la convención axial del mundo.
        const float radio = 1f, arriba = 0.35f, abajo = -0.35f;
        var vertices = new List<float>();
        var esquinas = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.PI / 3f * i;
            esquinas[i] = new Vector3(radio * MathF.Sin(a), arriba, radio * MathF.Cos(a));
        }

        static void Vertice(List<float> destino, Vector3 p, Vector3 c)
        {
            destino.AddRange(new[] { p.X, p.Y, p.Z, c.X, c.Y, c.Z });
        }
        static void Triangulo(List<float> destino, Vector3 a, Vector3 b, Vector3 c, Vector3 color)
        {
            Vertice(destino, a, color);
            Vertice(destino, b, color);
            Vertice(destino, c, color);
        }

        var verde = new Vector3(0.58f, 0.70f, 0.50f);
        for (int i = 0; i < 6; i++)
            Triangulo(vertices, new Vector3(0, arriba, 0), esquinas[i], esquinas[(i + 1) % 6], verde);

        // Las paredes cambian ligeramente de tonalidad para mostrar volumen.
        for (int i = 0; i < 6; i++)
        {
            Vector3 a = esquinas[i], b = esquinas[(i + 1) % 6];
            Vector3 c = new(b.X, abajo, b.Z), d = new(a.X, abajo, a.Z);
            float luz = 0.55f + i % 3 * 0.10f;
            Vector3 pared = new(0.42f * luz, 0.51f * luz, 0.38f * luz);
            Triangulo(vertices, a, d, c, pared);
            Triangulo(vertices, a, c, b, pared);
        }
        _triangulos = vertices.Count / 6;

        // Contornos superiores y aristas verticales con dibujo explícito.
        Vector3 oscuro = new(0.08f, 0.17f, 0.17f);
        for (int i = 0; i < 6; i++)
        {
            Vector3 a = esquinas[i], b = esquinas[(i + 1) % 6];
            Vertice(vertices, a, oscuro);
            Vertice(vertices, b, oscuro);
            Vertice(vertices, a, oscuro);
            Vertice(vertices, new Vector3(a.X, abajo, a.Z), oscuro);
        }
        _lineas = vertices.Count / 6 - _triangulos;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Dibuja la malla usando matrices construidas por la cámara orbital.
    /// OpenTK almacena matrices por filas; se transponen al cargarlas a GLSL.
    /// </summary>
    public void Dibujar(Matrix4 vistaProyeccion)
    {
        GL.UseProgram(_programa);
        GL.UniformMatrix4(_matrizUniform, true, ref vistaProyeccion);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _triangulos);
        GL.LineWidth(2f);
        GL.DrawArrays(PrimitiveType.Lines, _triangulos, _lineas);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    /// <summary>Libera recursos en GPU antes de destruir el contexto.</summary>
    public void Dispose()
    {
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_programa != 0) GL.DeleteProgram(_programa);
        _vbo = _vao = _programa = 0;
    }

    private static int Compilar(ShaderType tipo, string fuente)
    {
        int shader = GL.CreateShader(tipo);
        GL.ShaderSource(shader, fuente);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int correcto);
        if (correcto != 0) return shader;
        string error = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException("Error de GLSL: " + error);
    }
}
