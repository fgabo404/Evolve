using GEvolve.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Desktop;

/// <summary>
/// Construye una única malla de prismas hexagonales sin contornos.
/// La tapa usa un gris más oscuro que los laterales.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao, _vbo, _programa, _uniformeMatriz;
    private int _verticesTriangulos;

    private const int DatosPorVertice = 6; // posición(3), color(3)

    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec3 posicion;
        layout(location = 1) in vec3 colorEntrada;
        uniform mat4 matrizVistaProyeccion;
        out vec3 color;
        void main()
        {
            gl_Position = matrizVistaProyeccion * vec4(posicion, 1.0);
            color = colorEntrada;
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in vec3 color;
        out vec4 fragColor;
        void main() { fragColor = vec4(color, 1.0); }
        """;

    /// <summary>Crea un lote de geometría de tapas y seis paredes por celda.</summary>
    public void Inicializar(IReadOnlyList<HexTile> celdas)
    {
        if (_programa != 0)
            throw new InvalidOperationException("El renderer ya está inicializado.");

        int vert = Compilar(ShaderType.VertexShader, VertexShader);
        int frag = Compilar(ShaderType.FragmentShader, FragmentShader);
        _programa = GL.CreateProgram();
        GL.AttachShader(_programa, vert);
        GL.AttachShader(_programa, frag);
        GL.LinkProgram(_programa);
        GL.GetProgram(_programa, GetProgramParameterName.LinkStatus, out int exito);
        GL.DetachShader(_programa, vert);
        GL.DetachShader(_programa, frag);
        GL.DeleteShader(vert);
        GL.DeleteShader(frag);
        if (exito == 0)
            throw new InvalidOperationException(GL.GetProgramInfoLog(_programa));

        _uniformeMatriz = GL.GetUniformLocation(_programa, "matrizVistaProyeccion");

        var vertices = new List<float>(celdas.Count * 54 * DatosPorVertice);
        foreach (HexTile celda in celdas)
        {
            System.Numerics.Vector2 centro = HexGeometry.APosicion(celda.Coordenada);
            float nivel = celda.Elevacion;
            Vector3 arriba = new(centro.X, nivel, centro.Y);

            float gris = 0.32f + 0.48f
                * Math.Clamp((nivel - 0.15f) / 1.65f, 0f, 1f);
            // La tapa superior es más oscura que las paredes, sin bordes.
            Vector3 colorSuperior = new(gris * 0.72f, gris * 0.72f, gris * 0.72f);
            Vector3 colorLateral = new(gris, gris, gris);

            var esquinas = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angulo = MathF.PI * i / 3f;
                esquinas[i] = new Vector3(
                    centro.X + MathF.Sin(angulo), nivel,
                    centro.Y + MathF.Cos(angulo));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3 a = esquinas[i];
                Vector3 b = esquinas[(i + 1) % 6];

                // La tapa tiene un gris uniforme más oscuro.
                Vertice(vertices, arriba, colorSuperior);
                Vertice(vertices, a, colorSuperior);
                Vertice(vertices, b, colorSuperior);

                // Dos triángulos por pared con gris uniforme más claro.
                Vector3 aBase = new(a.X, 0f, a.Z);
                Vector3 bBase = new(b.X, 0f, b.Z);
                Vertice(vertices, a, colorLateral);
                Vertice(vertices, aBase, colorLateral);
                Vertice(vertices, bBase, colorLateral);

                Vertice(vertices, a, colorLateral);
                Vertice(vertices, bBase, colorLateral);
                Vertice(vertices, b, colorLateral);
            }
        }

        _verticesTriangulos = vertices.Count / DatosPorVertice;
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float),
            vertices.ToArray(), BufferUsageHint.StaticDraw);

        int stride = DatosPorVertice * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    /// <summary>Agrega un vértice con posición y color, sin atributos de borde.</summary>
    private static void Vertice(List<float> salida, Vector3 posicion, Vector3 color)
    {
        salida.AddRange(new[] { posicion.X, posicion.Y, posicion.Z, color.X, color.Y, color.Z });
    }

    /// <summary>Dibuja el lote 3D con tapas oscuras y paredes claras.</summary>
    public void Dibujar(Matrix4 vistaProyeccion)
    {
        GL.UseProgram(_programa);
        // Convención previamente validada para matrices OpenTK -> GLSL.
        GL.UniformMatrix4(_uniformeMatriz, false, ref vistaProyeccion);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _verticesTriangulos);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    /// <summary>Libera los recursos GPU cuando se cierra la aplicación.</summary>
    public void Dispose()
    {
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_programa != 0) GL.DeleteProgram(_programa);
        _vao = _vbo = _programa = 0;
    }

    /// <summary>Compila GLSL y describe el error si el controlador lo rechaza.</summary>
    private static int Compilar(ShaderType tipo, string fuente)
    {
        int shader = GL.CreateShader(tipo);
        GL.ShaderSource(shader, fuente);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok != 0) return shader;
        string mensaje = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException("Error GLSL: " + mensaje);
    }
}
