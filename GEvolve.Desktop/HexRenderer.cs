using GEvolve.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Desktop;

/// <summary>
/// Construye una sola malla de prismas hexagonales con tapas claras
/// y laterales oscuros, sin contornos ni geometría extra.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao, _vbo, _programa, _uniformeMatriz, _uniformeDesplazamiento;
    private int _verticesTriangulos;

    private const int DatosPorVertice = 6; // posición(3) y color(3)

    private const string VertexShader = """
        #version 330 core
        layout(location = 0) in vec3 posicion;
        layout(location = 1) in vec3 colorEntrada;
        uniform mat4 matrizVistaProyeccion;
        uniform float desplazamientoX;
        out vec3 color;
        void main()
        {
            gl_Position = matrizVistaProyeccion * vec4(posicion.x + desplazamientoX, posicion.yz, 1.0);
            color = colorEntrada;
        }
        """;

    private const string FragmentShader = """
        #version 330 core
        in vec3 color;
        out vec4 fragColor;
        void main() { fragColor = vec4(color, 1.0); }
        """;

    /// <summary>Compatibilidad con las antiguas celdas de demostración.</summary>
    public void Inicializar(IReadOnlyList<HexTile> celdas) => Inicializar(
        celdas.Select(x => new CeldaMundo(x.Coordenada, x.Elevacion, TipoSuperficie.Tierra)).ToArray());

    /// <summary>Crea un lote de geometría para tierra y océano.</summary>
    public void Inicializar(IReadOnlyList<CeldaMundo> celdas)
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
        _uniformeDesplazamiento = GL.GetUniformLocation(_programa, "desplazamientoX");

        var vertices = new List<float>(celdas.Count * 54 * DatosPorVertice);
        foreach (CeldaMundo celda in celdas)
        {
            // El tamaño de todas las celdas se define en un único lugar.
            System.Numerics.Vector2 centro = HexGeometry.APosicion(celda.Coordenada, HexGeometry.RadioEstandar);
            float nivel = celda.Elevacion;
            Vector3 arriba = new(centro.X, nivel, centro.Y);

            float gris = 0.32f + 0.48f
                * Math.Clamp((nivel - 0.15f) / 1.65f, 0f, 1f);
            // El mar permanece plano y azul. En tierra se mantiene
            // el esquema acordado: tapa clara y laterales oscuros.
            bool agua = celda.Superficie == TipoSuperficie.Oceano;
            float claro = Math.Clamp(gris * 1.15f, 0f, 1f);
            float oscuro = gris * 0.70f;
            Vector3 colorSuperior = agua ? new Vector3(0.12f, 0.34f, 0.65f)
                : new Vector3(0.25f * claro, 0.70f * claro, 0.36f * claro);
            Vector3 colorLateral = agua ? new Vector3(0.08f, 0.25f, 0.48f)
                : new Vector3(0.25f * oscuro, 0.65f * oscuro, 0.32f * oscuro);

            var esquinas = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angulo = MathF.PI * i / 3f;
                esquinas[i] = new Vector3(
                    centro.X + HexGeometry.RadioEstandar * MathF.Sin(angulo), nivel,
                    centro.Y + HexGeometry.RadioEstandar * MathF.Cos(angulo));
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3 a = esquinas[i];
                Vector3 b = esquinas[(i + 1) % 6];

                // Seis sectores forman la tapa uniforme, sin contornos dibujados.
                Vertice(vertices, arriba, colorSuperior);
                Vertice(vertices, a, colorSuperior);
                Vertice(vertices, b, colorSuperior);

                // Dos triángulos por pared con el mismo gris oscuro.
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

    /// <summary>Guarda solo posición XYZ y color RGB por vértice.</summary>
    private static void Vertice(List<float> salida, Vector3 posicion, Vector3 color)
    {
        salida.AddRange(new[]
        {
            posicion.X, posicion.Y, posicion.Z,
            color.X, color.Y, color.Z
        });
    }

    /// <summary>Dibuja tapas claras y laterales oscuros sin bordes.</summary>
    public void Dibujar(Matrix4 vistaProyeccion, float anchoMundo = 0f)
    {
        GL.UseProgram(_programa);
        // Convención previamente validada para matrices OpenTK -> GLSL.
        GL.UniformMatrix4(_uniformeMatriz, false, ref vistaProyeccion);
        GL.BindVertexArray(_vao);
        // La misma malla GPU se reutiliza a izquierda y derecha para que
        // el mundo sea visualmente continuo en la costura este-oeste.
        // No se duplican vértices ni se reconstruye el mapa al desplazarse.
        if (anchoMundo > 0f)
        {
            GL.Uniform1(_uniformeDesplazamiento, -anchoMundo);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _verticesTriangulos);
        }
        GL.Uniform1(_uniformeDesplazamiento, 0f);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _verticesTriangulos);
        if (anchoMundo > 0f)
        {
            GL.Uniform1(_uniformeDesplazamiento, anchoMundo);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _verticesTriangulos);
        }
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
        throw new InvalidOperationException($"Error compilando shader {tipo}: {mensaje}");
    }
}
