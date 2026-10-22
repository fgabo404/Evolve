using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GEvolve.Desktop;

/// <summary>Interfaz 2D de estilo pixelado, sin texturas ni fuentes externas.</summary>
internal sealed class MenuRenderer : IDisposable
{
    private int _vao, _vbo, _shader, _matriz;
    private readonly List<float> _vertices = new();

    private const string Vertex = """
        #version 330 core
        layout(location=0) in vec2 position;
        layout(location=1) in vec3 colorIn;
        uniform mat4 projection;
        out vec3 color;
        void main() { color=colorIn; gl_Position=projection*vec4(position,0,1); }
        """;
    private const string Fragment = """
        #version 330 core
        in vec3 color;
        out vec4 fragColor;
        void main() { fragColor=vec4(color,1); }
        """;

    // Cada carácter ocupa cinco columnas por siete filas. '1' indica un píxel.
    private static readonly Dictionary<char,string[]> Glifos = new()
    {
        [' '] = ["00000","00000","00000","00000","00000","00000","00000"],
        ['A'] = ["01110","10001","10001","11111","10001","10001","10001"],
        ['B'] = ["11110","10001","10001","11110","10001","10001","11110"],
        ['C'] = ["01111","10000","10000","10000","10000","10000","01111"],
        ['D'] = ["11110","10001","10001","10001","10001","10001","11110"],
        ['E'] = ["11111","10000","10000","11110","10000","10000","11111"],
        ['F'] = ["11111","10000","10000","11110","10000","10000","10000"],
        ['G'] = ["01111","10000","10000","10111","10001","10001","01111"],
        ['H'] = ["10001","10001","10001","11111","10001","10001","10001"],
        ['I'] = ["11111","00100","00100","00100","00100","00100","11111"],
        ['J'] = ["00111","00010","00010","00010","10010","10010","01100"],
        ['K'] = ["10001","10010","10100","11000","10100","10010","10001"],
        ['L'] = ["10000","10000","10000","10000","10000","10000","11111"],
        ['M'] = ["10001","11011","10101","10101","10001","10001","10001"],
        ['N'] = ["10001","11001","10101","10011","10001","10001","10001"],
        ['O'] = ["01110","10001","10001","10001","10001","10001","01110"],
        ['P'] = ["11110","10001","10001","11110","10000","10000","10000"],
        ['Q'] = ["01110","10001","10001","10001","10101","10010","01101"],
        ['R'] = ["11110","10001","10001","11110","10100","10010","10001"],
        ['S'] = ["01111","10000","10000","01110","00001","00001","11110"],
        ['T'] = ["11111","00100","00100","00100","00100","00100","00100"],
        ['U'] = ["10001","10001","10001","10001","10001","10001","01110"],
        ['V'] = ["10001","10001","10001","10001","10001","01010","00100"],
        ['W'] = ["10001","10001","10001","10101","10101","10101","01010"],
        ['X'] = ["10001","10001","01010","00100","01010","10001","10001"],
        ['Y'] = ["10001","10001","01010","00100","00100","00100","00100"],
        ['Z'] = ["11111","00001","00010","00100","01000","10000","11111"],
        ['0'] = ["01110","10011","10101","10101","11001","10001","01110"],
        ['1'] = ["00100","01100","00100","00100","00100","00100","01110"],
        ['2'] = ["01110","10001","00001","00010","00100","01000","11111"],
        ['3'] = ["11110","00001","00001","01110","00001","00001","11110"],
        ['4'] = ["00010","00110","01010","10010","11111","00010","00010"],
        ['5'] = ["11111","10000","10000","11110","00001","00001","11110"],
        ['6'] = ["01111","10000","10000","11110","10001","10001","01110"],
        ['7'] = ["11111","00001","00010","00100","01000","01000","01000"],
        ['8'] = ["01110","10001","10001","01110","10001","10001","01110"],
        ['9'] = ["01110","10001","10001","01111","00001","00001","11110"],
        ['-'] = ["00000","00000","00000","11111","00000","00000","00000"],
        [':'] = ["00000","00100","00100","00000","00100","00100","00000"],
        ['/'] = ["00001","00001","00010","00100","01000","10000","10000"],
        ['%'] = ["11001","11010","00100","01000","10110","00110","00000"],
        ['.'] = ["00000","00000","00000","00000","00000","00100","00100"],
        ['<'] = ["00010","00100","01000","10000","01000","00100","00010"],
        ['>'] = ["01000","00100","00010","00001","00010","00100","01000"],
        ['+'] = ["00000","00100","00100","11111","00100","00100","00000"],
        ['='] = ["00000","11111","00000","11111","00000","00000","00000"]
    };

    /// <summary>Compila recursos GPU una vez en el hilo gráfico.</summary>
    public void Inicializar()
    {
        int vs = Compilar(ShaderType.VertexShader, Vertex);
        int fs = Compilar(ShaderType.FragmentShader, Fragment);
        _shader = GL.CreateProgram();
        GL.AttachShader(_shader, vs); GL.AttachShader(_shader, fs);
        GL.LinkProgram(_shader);
        GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out int ok);
        GL.DetachShader(_shader, vs); GL.DetachShader(_shader, fs);
        GL.DeleteShader(vs); GL.DeleteShader(fs);
        if (ok == 0) throw new InvalidOperationException(GL.GetProgramInfoLog(_shader));
        _matriz = GL.GetUniformLocation(_shader, "projection");
        _vao = GL.GenVertexArray(); _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao); GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    private static int Compilar(ShaderType tipo, string fuente)
    {
        int s = GL.CreateShader(tipo); GL.ShaderSource(s, fuente); GL.CompileShader(s);
        GL.GetShader(s, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0) throw new InvalidOperationException(GL.GetShaderInfoLog(s));
        return s;
    }

    private void Vertice(float x,float y,Vector3 c)
        => _vertices.AddRange([x,y,c.X,c.Y,c.Z]);
    private void Tri(float x1,float y1,float x2,float y2,float x3,float y3,Vector3 c)
    { Vertice(x1,y1,c); Vertice(x2,y2,c); Vertice(x3,y3,c); }

    /// <summary>Rectángulo sólido en coordenadas virtuales 1280x720.</summary>
    public void Rect(float x,float y,float w,float h,Vector3 c)
    {
        Tri(x,y,x+w,y,x+w,y+h,c);
        Tri(x,y,x+w,y+h,x,y+h,c);
    }

    /// <summary>Texto en letras mayúsculas estilo píxel.</summary>
    public void Texto(string texto,float x,float y,float escala,Vector3 color,bool centrado=false)
    {
        texto = texto.ToUpperInvariant();
        if (centrado) x -= texto.Length * 6f * escala / 2f;
        foreach (char letra in texto)
        {
            if (Glifos.TryGetValue(letra,out var patron))
                for (int fila=0;fila<7;fila++)
                for (int col=0;col<5;col++)
                    if (patron[fila][col]=='1')
                        Rect(x+col*escala,y+fila*escala,escala,escala,color);
            x += 6f * escala;
        }
    }

    /// <summary>Botón rectangular elevado que cambia al resaltarse.</summary>
    public void Boton(string titulo,float x,float y,float ancho,bool activo)
    {
        Vector3 sombra = new(.06f,.07f,.08f);
        Vector3 fondo = new(.14f,.18f,.20f);
        Rect(x+4,y+5,ancho,52,sombra);
        Rect(x,y,ancho,52,fondo);
        Rect(x,y,ancho,3,activo ? new(.32f,.77f,.60f) : new(.62f,.68f,.70f));
        Rect(x,y+49,ancho,3,activo ? new(.32f,.77f,.60f) : new(.62f,.68f,.70f));
        Texto(titulo,x+ancho/2f,y+18,2.3f,activo ? new Vector3(.52f,.98f,.78f) : Vector3.One,true);
    }

    /// <summary>Marco con esquinas recortadas y doble línea al estilo de la referencia.</summary>
    public void Marco(float x, float y, float w, float h, bool resaltado)
    {
        Vector3 contorno = resaltado ? new(.28f,.80f,.61f) : new(.48f,.55f,.59f);
        Vector3 oscuro = new(.13f,.18f,.21f);
        Rect(x+7,y,w-14,h,contorno);
        Rect(x,y+7,w,h-14,contorno);
        Rect(x+10,y+4,w-20,h-8,oscuro);
        Rect(x+4,y+10,w-8,h-20,oscuro);
        Rect(x+15,y+8,w-30,2,contorno);
        Rect(x+15,y+h-10,w-30,2,contorno);
    }

    /// <summary>Fila de selección con etiqueta y control independiente de flechas.</summary>
    public void Selector(string etiqueta, string valor, float x, float y, bool activo)
    {
        Marco(x,y,660,66,activo);
        Vector3 gris = new(.79f,.83f,.86f);
        Texto("O",x+30,y+21,2.7f,gris);
        Texto(etiqueta,x+98,y+23,2.6f,gris);
        Marco(x+345,y+8,300,50,activo);
        Texto("<",x+365,y+22,2.6f,gris);
        Texto(valor,x+497,y+22,MathF.Min(2.5f, 185f / Math.Max(1, valor.Length * 6f)),Vector3.One,true);
        Texto(">",x+608,y+22,2.6f,gris);
    }

    /// <summary>Acción central destacada, con flecha y acento verde.</summary>
    public void Accion(string titulo,float x,float y,float ancho,bool activo,bool primaria)
    {
        Marco(x,y,ancho,58,primaria || activo);
        Vector3 tinta = primaria ? new(.48f,.97f,.76f) : new(.82f,.86f,.89f);
        Texto(primaria ? ">" : "<",x+78,y+19,3f,tinta);
        Texto(titulo,x+ancho/2f+18,y+19,2.7f,tinta,true);
    }

    /// <summary>Inicia el dibujo del fotograma de UI.</summary>
    public void Empezar() => _vertices.Clear();

    /// <summary>Envía el menú a pantalla sin alterar el programa del terreno.</summary>
    public void Presentar(int ancho,int alto)
    {
        if (_vertices.Count==0)return;
        GL.Disable(EnableCap.DepthTest);
        GL.UseProgram(_shader);
        Matrix4 proyeccion = Matrix4.CreateOrthographicOffCenter(0,1280,720,0,-1,1);
        GL.UniformMatrix4(_matriz,false,ref proyeccion);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer,_vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,_vertices.Count*sizeof(float),_vertices.ToArray(),BufferUsageHint.DynamicDraw);
        GL.DrawArrays(PrimitiveType.Triangles,0,_vertices.Count/5);
        GL.BindVertexArray(0); GL.UseProgram(0);
        GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        if(_vbo!=0)GL.DeleteBuffer(_vbo);
        if(_vao!=0)GL.DeleteVertexArray(_vao);
        if(_shader!=0)GL.DeleteProgram(_shader);
    }
}
