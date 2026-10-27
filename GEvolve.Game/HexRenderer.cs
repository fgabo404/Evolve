using GEvolve.Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Game;

/// <summary>
/// Construye una sola malla de prismas hexagonales con tapas claras
/// y laterales oscuros. Solo el mar muestra contornos blancos superiores.
/// </summary>
internal sealed class HexRenderer : IDisposable
{
    private int _vao, _vbo, _programa, _uniformeMatriz, _uniformeDesplazamiento;
    private int _verticesTriangulos;
    private int _vaoAgua, _vboAgua, _verticesAgua;

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
        var bordesAgua = new List<float>();
        foreach (CeldaMundo celda in celdas)
        {
            // El tamaño de todas las celdas se define en un único lugar.
            System.Numerics.Vector2 centro = HexGeometry.APosicion(celda.Coordenada, HexGeometry.RadioEstandar);
            float nivel = celda.Elevacion;
            Vector3 arriba = new(centro.X, nivel, centro.Y);

            bool agua = celda.Superficie == TipoSuperficie.Oceano;
            Vector3 colorSuperior = ColorBioma(celda.Bioma);
            Vector3 colorLateral = colorSuperior * .68f;
            float brillo = .2126f * colorSuperior.X + .7152f * colorSuperior.Y + .0722f * colorSuperior.Z;
            Vector3 tinta = brillo < .43f ? Vector3.One : new Vector3(.07f,.09f,.10f);

            var esquinas = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angulo = MathF.PI * i / 3f;
                esquinas[i] = new Vector3(
                    centro.X + HexGeometry.RadioEstandar * MathF.Sin(angulo), nivel,
                    centro.Y + HexGeometry.RadioEstandar * MathF.Cos(angulo));
            }

            // Líneas blancas solo en las tapas marinas: pequeño sesgo evita z-fighting.
            if (agua)
            {
                Vector3 blanco = Vector3.One;
                for (int i = 0; i < 6; i++)
                {
                    Vector3 aBorde = esquinas[i];
                    Vector3 bBorde = esquinas[(i + 1) % 6];
                    aBorde.Y += 0.003f;
                    bBorde.Y += 0.003f;
                    Vertice(bordesAgua, aBorde, blanco);
                    Vertice(bordesAgua, bBorde, blanco);
                }
            }

            // Dígitos vectoriales sobre cada tapa, conservando el nivel lógico.
            if (celda.NivelNumerico is byte nivelNumerico)
                EtiquetaNivel(bordesAgua, centro.X, centro.Y, nivel + 0.012f,
                    nivelNumerico, agua ? Vector3.One : tinta);

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

        _verticesAgua = bordesAgua.Count / DatosPorVertice;
        _vaoAgua = GL.GenVertexArray();
        _vboAgua = GL.GenBuffer();
        GL.BindVertexArray(_vaoAgua);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vboAgua);
        GL.BufferData(BufferTarget.ArrayBuffer, bordesAgua.Count * sizeof(float),
            bordesAgua.ToArray(), BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    private static Vector3 ColorBioma(Bioma bioma) => bioma switch
    {
        Bioma.OceanoAbierto => new(.10f,.31f,.65f),
        Bioma.Arrecife => new(.13f,.70f,.78f),
        Bioma.EstuarioLitoral => new(.20f,.54f,.74f),
        Bioma.Manglar => new(.16f,.37f,.30f),
        Bioma.BosqueTropical => new(.08f,.43f,.18f),
        Bioma.BosqueSubtropical => new(.24f,.57f,.27f),
        Bioma.BosqueTemplado => new(.27f,.50f,.33f),
        Bioma.BosqueBoreal => new(.20f,.33f,.28f),
        Bioma.DesiertoArenoso => new(.86f,.72f,.38f),
        Bioma.DesiertoRocoso => new(.63f,.40f,.29f),
        Bioma.Salar => new(.87f,.85f,.80f),
        Bioma.Sabana => new(.72f,.69f,.30f),
        Bioma.Pradera => new(.50f,.72f,.38f),
        Bioma.Estepa => new(.67f,.61f,.43f),
        Bioma.Humedal => new(.32f,.58f,.49f),
        Bioma.PantanoEverglades => new(.22f,.36f,.22f),
        Bioma.TundraArtica => new(.70f,.80f,.85f),
        Bioma.TundraAntartica => new(.90f,.93f,.97f),
        Bioma.Alpino => new(.57f,.59f,.65f),
        Bioma.Cumbres => new(.96f,.97f,.99f),
        _ => new(.5f,.5f,.5f)
    };

    // Segmentos: arriba, superior derecha, inferior derecha, abajo,
    // inferior izquierda, superior izquierda y centro.
    private static readonly int[] SegmentosNumero =
        [0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F];

    /// <summary>Dibuja la elevación 0–100 con trazos sobre la tapa del hexágono.</summary>
    private static void EtiquetaNivel(List<float> salida, float x, float z,
        float y, byte numero, Vector3 tinta)
    {
        string texto = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
        const float ancho = 0.20f;
        const float alto = 0.36f;
        const float separacion = 0.08f;
        float inicio = x - (texto.Length * ancho + (texto.Length - 1) * separacion) / 2f;
        for (int i = 0; i < texto.Length; i++)
        {
            int segmentos = SegmentosNumero[texto[i] - '0'];
            float izq = inicio + i * (ancho + separacion);
            float der = izq + ancho;
            float superior = z - alto / 2f;
            float medio = z;
            float inferior = z + alto / 2f;
            void Trazo(int bit, float x1, float z1, float x2, float z2)
            {
                if ((segmentos & (1 << bit)) == 0) return;
                Vertice(salida, new Vector3(x1, y, z1), tinta);
                Vertice(salida, new Vector3(x2, y, z2), tinta);
            }
            Trazo(0, izq, superior, der, superior);
            Trazo(1, der, superior, der, medio);
            Trazo(2, der, medio, der, inferior);
            Trazo(3, izq, inferior, der, inferior);
            Trazo(4, izq, medio, izq, inferior);
            Trazo(5, izq, superior, izq, medio);
            Trazo(6, izq, medio, der, medio);
        }
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

    /// <summary>Dibuja terreno y líneas del mar usando la misma continuidad este-oeste.</summary>
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
        GL.BindVertexArray(_vaoAgua);
        GL.LineWidth(1f);
        if (anchoMundo > 0f)
        {
            GL.Uniform1(_uniformeDesplazamiento, -anchoMundo);
            GL.DrawArrays(PrimitiveType.Lines, 0, _verticesAgua);
        }
        GL.Uniform1(_uniformeDesplazamiento, 0f);
        GL.DrawArrays(PrimitiveType.Lines, 0, _verticesAgua);
        if (anchoMundo > 0f)
        {
            GL.Uniform1(_uniformeDesplazamiento, anchoMundo);
            GL.DrawArrays(PrimitiveType.Lines, 0, _verticesAgua);
        }
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    /// <summary>Libera los recursos GPU cuando se cierra la aplicación.</summary>
    public void Dispose()
    {
        if (_vboAgua != 0) GL.DeleteBuffer(_vboAgua);
        if (_vaoAgua != 0) GL.DeleteVertexArray(_vaoAgua);
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_programa != 0) GL.DeleteProgram(_programa);
        _vao = _vbo = _programa = _vaoAgua = _vboAgua = 0;
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
