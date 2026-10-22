using GEvolve.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Desktop;

/// <summary>
/// Convierte celdas de terreno en prismas hexagonales tridimensionales.
/// Solo representa datos recibidos desde GEvolve.Core: no genera el mundo.
/// Usa una sola malla y subida a GPU para todas las celdas del mapa radial.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao, _vbo, _programa, _uniformeMatriz;
    private int _verticesTriangulos;
    // Banda negra de 0.18 unidades del mundo: geometría real, no GL.LineWidth.
    private const float GrosorCuadricula = 0.18f;

    private const string VertexShader = """
        #version 330 core
        layout(location=0) in vec3 posicion;
        layout(location=1) in vec3 colorEntrada;
        uniform mat4 matrizVistaProyeccion;
        out vec3 color;
        void main() {
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

    /// <summary>Inicializa shaders y construye la malla con un contexto OpenGL activo.</summary>
    public void Inicializar(IReadOnlyList<HexTile> celdas)
    {
        if (_programa != 0) throw new InvalidOperationException("El renderer ya está inicializado.");

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
        if (exito == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(_programa));
        _uniformeMatriz = GL.GetUniformLocation(_programa, "matrizVistaProyeccion");

        var triangulos = new List<float>();

        // El mapa vive en X/Z; la altura física se expresa sobre Y.
        // HexGeometry asegura que todos los centros compartan exactamente
        // la convención axial pointy-top del núcleo matemático.
        foreach (HexTile celda in celdas)
        {
            System.Numerics.Vector2 centro = HexGeometry.APosicion(celda.Coordenada);
            float nivel = celda.Elevacion;
            Vector3 superior = new(centro.X, nivel, centro.Y);
            var esquinas = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angulo = MathF.PI * i / 3f;
                esquinas[i] = new Vector3(
                    centro.X + MathF.Sin(angulo), nivel,
                    centro.Y + MathF.Cos(angulo));
            }

            // Escala de grises: elevaciones bajas oscuras, altas claras.
            // La luminosidad es visual; NO determina categorías ecológicas.
            float gris = 0.32f + 0.48f * Math.Clamp((nivel - 0.15f) / 1.25f, 0f, 1f);
            var tapa = new Vector3(gris, gris, gris);
            // Negro puro para marcar la silueta y la cuadrícula tridimensional.
            var contorno = Vector3.Zero;
            // Se dibuja un hexágono negro completo y una tapa gris más
            // pequeña encima. El borde resultante es un anillo hexagonal 3D
            // de grosor estable en unidades del mundo.
            Vector3 centroBorde = new(centro.X, nivel + 0.003f, centro.Y);
            Vector3 centroInterior = new(centro.X, nivel + 0.006f, centro.Y);

            for (int i = 0; i < 6; i++)
            {
                int siguiente = (i + 1) % 6;
                Vector3 a = esquinas[i], b = esquinas[siguiente];
                // Tapa negra de tamaño completo: garantiza que la cuadrícula
                // sea visible incluso si el driver ignora GL.LineWidth.
                Vector3 aNegra = new(a.X, nivel + 0.003f, a.Z);
                Vector3 bNegra = new(b.X, nivel + 0.003f, b.Z);
                Triangulo(triangulos, centroBorde, aNegra, bNegra, contorno);

                // Tapa gris inscrita (radio 1 - GrosorCuadricula).
                // Pequeña separación vertical evita z-fighting entre ambas.
                float escala = 1f - GrosorCuadricula;
                Vector3 aInterior = centroInterior + (a - superior) * escala;
                Vector3 bInterior = centroInterior + (b - superior) * escala;
                Triangulo(triangulos, centroInterior, aInterior, bInterior, tapa);

                Vector3 aBase = new(a.X, 0f, a.Z);
                Vector3 bBase = new(b.X, 0f, b.Z);
                // Sin sombras: paredes del mismo gris que la tapa.
                Triangulo(triangulos, a, aBase, bBase, tapa);
                Triangulo(triangulos, a, bBase, b, tapa);
            }
        }

        _verticesTriangulos = triangulos.Count / 6;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, triangulos.Count * sizeof(float),
            triangulos.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    private static void Vertice(List<float> salida, Vector3 posicion, Vector3 color)
    {
        salida.AddRange(new[] { posicion.X, posicion.Y, posicion.Z, color.X, color.Y, color.Z });
    }

    private static void Triangulo(List<float> salida, Vector3 a, Vector3 b, Vector3 c, Vector3 color)
    {
        Vertice(salida, a, color);
        Vertice(salida, b, color);
        Vertice(salida, c, color);
    }

    /// <summary>Dibuja tapas, paredes y bordes usando la cámara proporcionada.</summary>
    public void Dibujar(Matrix4 vistaProyeccion)
    {
        GL.UseProgram(_programa);
        // OpenTK compone matrices en orden fila (vista * proyección).
        // false carga los datos para que GLSL opere correctamente por columnas.
        GL.UniformMatrix4(_uniformeMatriz, false, ref vistaProyeccion);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _verticesTriangulos);
         GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    /// <summary>Libera los recursos gráficos antes de destruir el contexto OpenGL.</summary>
    public void Dispose()
    {
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_programa != 0) GL.DeleteProgram(_programa);
        _vao = _vbo = _programa = 0;
    }

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
