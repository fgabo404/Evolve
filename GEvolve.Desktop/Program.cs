using GEvolve.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GEvolve.Desktop;

/// <summary>
/// Ventana principal del motor G-Evolve.
/// Controla la cámara orbital; la geometría pertenece a HexRenderer.
/// </summary>
public sealed class MotorGEvolve : GameWindow
{
    private HexRenderer _hexRenderer = new();
    private readonly MenuRenderer _menu = new();
    private Pantalla _pantalla = Pantalla.Inicio;
    private int _seleccion;
    private WorldSettings _ajustes = new(200, 150, 2026, 10, 30,
        TipoMundo.Continentes, EdadGeologica.Normal, 60, 40);
    private enum Pantalla { Inicio, Configuracion, Jugando, Pausa }
    // Giro horizontal continuo de 360°; puede repetirse sin detenerse en los extremos.
    private float _angulo = MathHelper.DegreesToRadians(40f);
    // Inclinación orbital variable: de vista rasante a vista casi cenital.
    private float _inclinacion = MathHelper.DegreesToRadians(35f);
    private const float InclinacionMinima = MathF.PI * 25f / 180f;
    private const float InclinacionMaxima = MathF.PI * 85f / 180f;
    // La cámara orbita alrededor de un objetivo desplazable sobre el terreno.
    private float _distancia = 38f;
    private Vector3 _objetivo = new(0f, 0.5f, 0f);
    private const float Velocidad = 24f;
    private WorldMap? _mundo;
    // Ancho físico X del mundo en hexágonos pointy-top con radio estándar.
    private float _anchoMundo;

    public MotorGEvolve() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "G-Evolve | 200x150 — boton derecho: giro 360 y altura 25-85 grados"
        })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.16f, 0.22f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        _menu.Inicializar();
        // El terreno se genera cuando el jugador pulsa GENERAR MUNDO.
    }

    /// <summary>Reconstruye el mundo únicamente al iniciar una nueva partida.</summary>
    private void CrearMundo()
    {
        _hexRenderer.Dispose();
        _hexRenderer = new HexRenderer();
        _mundo = WorldSystem.Generar(_ajustes);
        _objetivo = new Vector3(0f, 0.5f, 0f);
        _distancia = 38f;
        _anchoMundo = MathF.Sqrt(3f) * HexGeometry.RadioEstandar * _mundo.Configuracion.Ancho;
        // La altura lógica va de 0 a 100. Se transforma a unidades gráficas
        // sin modificar los datos del terreno. El mar queda horizontal.
        CeldaMundo[] representacion = _mundo.Celdas.Select(c =>
            new CeldaMundo(c.Coordenada,
                c.Superficie == TipoSuperficie.Oceano ? 0f
                : 0.13f + c.Altura / 100f * 2.8f,
                c.Superficie, c.Relieve, c.Altura)).ToArray();
        _hexRenderer.Inicializar(representacion);
    }

    /// <summary>
    /// Permite giro horizontal continuo de 360° y ajuste vertical de 25° a 85°
    /// manteniendo el botón derecho, sin atravesar ni invertir el terreno.
    /// </summary>
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (_pantalla != Pantalla.Jugando || !MouseState.IsButtonDown(MouseButton.Right)) return;

        // Normalización circular: evita límites y permite múltiples vueltas completas.
        _angulo = (_angulo - e.DeltaX * 0.009f) % (2f * MathF.PI);
        if (_angulo < 0f) _angulo += 2f * MathF.PI;
        _inclinacion = Math.Clamp(_inclinacion + e.DeltaY * 0.007f,
            InclinacionMinima, InclinacionMaxima);
    }

    /// <summary>Acerca o aleja la cámara sin cambiar el tamaño de la malla.</summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_pantalla != Pantalla.Jugando) return;
        // El zoom no puede alejarse tanto que el mundo se pierda en pantalla.
        _distancia = Math.Clamp(_distancia - e.OffsetY * 2f, 8f, 52f);
    }

    /// <summary>Desplazamiento WASD en el plano XZ, relativo al giro de cámara.</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (_pantalla != Pantalla.Jugando) return;
        float paso = Velocidad * (float)args.Time;
        if (KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift))
            paso *= 2.5f;

        Vector3 adelante = new(-MathF.Sin(_angulo), 0f, -MathF.Cos(_angulo));
        Vector3 derecha = new(-adelante.Z, 0f, adelante.X);
        Vector3 movimiento = Vector3.Zero;
        if (KeyboardState.IsKeyDown(Keys.W)) movimiento += adelante;
        if (KeyboardState.IsKeyDown(Keys.S)) movimiento -= adelante;
        if (KeyboardState.IsKeyDown(Keys.D)) movimiento += derecha;
        if (KeyboardState.IsKeyDown(Keys.A)) movimiento -= derecha;
        if (movimiento.LengthSquared > 0f)
        {
            movimiento.Normalize();
            _objetivo += movimiento * paso;
            // Mundo cilíndrico: caminar al este reaparece por el oeste.
            // Se usa el período físico de la cuadrícula en X, no el de celdas.
            float mitad = _anchoMundo * 0.5f;
            _objetivo.X = ((_objetivo.X + mitad) % _anchoMundo + _anchoMundo) % _anchoMundo - mitad;
            // Norte y sur mantienen sus límites polares: no hay envolvimiento.
            _objetivo.Z = Math.Clamp(_objetivo.Z,
                -(_mundo!.Configuracion.Alto / 2f - 3f) * 1.5f,
                 (_mundo.Configuracion.Alto / 2f - 3f) * 1.5f);
        }
        if (KeyboardState.IsKeyDown(Keys.Home)) _objetivo = new Vector3(0f, 0.5f, 0f);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_pantalla != Pantalla.Jugando)
        {
            DibujarMenu();
            SwapBuffers();
            return;
        }
        // Cámara esférica dirigida al origen. El eje Y representa la altura.
        float horizontal = _distancia * MathF.Cos(_inclinacion);
        Vector3 objetivo = _objetivo;
        Vector3 camara = objetivo + new Vector3(
            horizontal * MathF.Sin(_angulo),
            _distancia * MathF.Sin(_inclinacion),
            horizontal * MathF.Cos(_angulo));

        Matrix4 vista = Matrix4.LookAt(camara, objetivo, Vector3.UnitY);
        float aspecto = (float)Size.X / Math.Max(1, Size.Y);
        // Proyección ortográfica: conserva escala y silueta de las columnas 3D,
        // independientemente de su profundidad, como en el boceto aprobado.
        // El zoom controla el área visible; la elevación sigue siendo geometría real.
        float alturaVisible = _distancia * 0.85f;
        Matrix4 proyeccion = Matrix4.CreateOrthographic(
            alturaVisible * aspecto, alturaVisible, 0.1f, 300f);

        _hexRenderer.Dibujar(vista * proyeccion, _anchoMundo);
        SwapBuffers();
    }

    /// <summary>Entrada de menú por teclado; no afecta controles del mundo.</summary>
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Keys.Escape)
        {
            if (_pantalla == Pantalla.Jugando) { _pantalla = Pantalla.Pausa; _seleccion = 0; }
            else if (_pantalla == Pantalla.Configuracion) { _pantalla = Pantalla.Inicio; _seleccion = 0; }
            else if (_pantalla == Pantalla.Pausa) { _pantalla = Pantalla.Jugando; }
            return;
        }
        if (_pantalla == Pantalla.Jugando) return;
        int opciones = _pantalla == Pantalla.Configuracion ? 8 : 3;
        if (e.Key == Keys.Up || e.Key == Keys.W) _seleccion = (_seleccion + opciones - 1) % opciones;
        if (e.Key == Keys.Down || e.Key == Keys.S) _seleccion = (_seleccion + 1) % opciones;
        if (_pantalla == Pantalla.Configuracion && (e.Key == Keys.Left || e.Key == Keys.Right
            || e.Key == Keys.A || e.Key == Keys.D))
            ModificarOpcion(e.Key == Keys.Right || e.Key == Keys.D ? 1 : -1);
        if (e.Key == Keys.Enter || e.Key == Keys.Space) ActivarSeleccion();
    }

    /// <summary>Los botones funcionan también mediante clic izquierdo.</summary>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButton.Left || _pantalla == Pantalla.Jugando) return;
        float x = MouseState.Position.X * 1280f / Math.Max(1, Size.X);
        float y = MouseState.Position.Y * 720f / Math.Max(1, Size.Y);
        if (_pantalla == Pantalla.Configuracion)
        {
            if (x < 270f || x > 1010f) return;
            int idx = (int)MathF.Floor((y - 162f) / 57f);
            if (idx >= 0 && idx < 8)
            {
                _seleccion = idx;
                if (idx < 6) ModificarOpcion(1);
                else ActivarSeleccion();
            }
        }
        else if (x >= 420f && x <= 860f)
        {
            int idx = (int)MathF.Floor((y - 300f) / 70f);
            if (idx >= 0 && idx < 3) { _seleccion = idx; ActivarSeleccion(); }
        }
    }

    /// <summary>Modifica valores configurables antes de generar el mundo.</summary>
    private void ModificarOpcion(int delta)
    {
        switch (_seleccion)
        {
            case 0:
                int tipos = Enum.GetValues<TipoMundo>().Length;
                _ajustes = _ajustes with { Tipo = (TipoMundo)(((int)_ajustes.Tipo + delta + tipos) % tipos) };
                break;
            case 1:
                _ajustes = _ajustes with { Edad = (EdadGeologica)(((int)_ajustes.Edad + delta + 3) % 3) };
                break;
            case 2:
                _ajustes = _ajustes with { PorcentajeOceanos = Math.Clamp(_ajustes.PorcentajeOceanos + 5 * delta, 5, 95) };
                break;
            case 3:
                _ajustes = _ajustes with { IntensidadMontanosa = Math.Clamp(_ajustes.IntensidadMontanosa + 10 * delta, 0, 100) };
                break;
            case 4:
                _ajustes = _ajustes with { Semilla = _ajustes.Semilla + delta };
                break;
            case 5:
                int[] anchos = [100, 200, 300];
                int[] altos = [50, 150, 200];
                int posicion = _ajustes.Ancho == 100 ? 0 : _ajustes.Ancho == 200 ? 1 : 2;
                posicion = (posicion + delta + 3) % 3;
                _ajustes = _ajustes with { Ancho = anchos[posicion], Alto = altos[posicion] };
                break;
        }
    }

    /// <summary>Procesa la opción elegida en la pantalla activa.</summary>
    private void ActivarSeleccion()
    {
        if (_pantalla == Pantalla.Inicio)
        {
            if (_seleccion == 0) { _pantalla = Pantalla.Configuracion; _seleccion = 0; }
            else if (_seleccion == 1 && _mundo is not null) { _pantalla = Pantalla.Jugando; }
            else if (_seleccion == 2) Close();
        }
        else if (_pantalla == Pantalla.Configuracion)
        {
            if (_seleccion == 6) { CrearMundo(); _pantalla = Pantalla.Jugando; _seleccion = 0; }
            else if (_seleccion == 7) { _pantalla = Pantalla.Inicio; _seleccion = 0; }
        }
        else if (_pantalla == Pantalla.Pausa)
        {
            if (_seleccion == 0) _pantalla = Pantalla.Jugando;
            else if (_seleccion == 1) { _pantalla = Pantalla.Configuracion; _seleccion = 0; }
            else if (_seleccion == 2) { _pantalla = Pantalla.Inicio; _seleccion = 0; }
        }
    }

    /// <summary>Menú pixelado independiente del mundo 3D.</summary>
    private void DibujarMenu()
    {
        _menu.Empezar();
        var fondo = new Vector3(.08f,.13f,.15f);
        _menu.Rect(0,0,1280,720,fondo);
        // Bandas de paisaje estilizado sin imágenes externas.
        for (int i=0;i<22;i++)
        {
            float x = i * 65f;
            float altura = 40f + 45f * MathF.Abs(MathF.Sin(i * 1.7f));
            _menu.Rect(x,630f-altura,63f,altura+90f,new Vector3(.11f,.19f,.18f));
        }
        _menu.Texto("G-EVOLVE",640,60,10f,new Vector3(.86f,.93f,.77f),true);
        _menu.Texto("MUNDO HEXAGONAL",640,155,2.5f,new Vector3(.63f,.74f,.67f),true);

        if (_pantalla == Pantalla.Configuracion)
        {
            string[] opciones =
            [
                "TIPO: " + _ajustes.Tipo,
                "EDAD: " + _ajustes.Edad,
                "OCEANOS: " + _ajustes.PorcentajeOceanos + "%",
                "MONTANAS: " + _ajustes.IntensidadMontanosa + "%",
                "SEMILLA: " + _ajustes.Semilla,
                "TAMANO: " + _ajustes.Ancho + "X" + _ajustes.Alto,
                "GENERAR MUNDO", "VOLVER"
            ];
            _menu.Texto("CONFIGURAR MUNDO",640,125,3.5f,Vector3.One,true);
            for (int i=0;i<opciones.Length;i++)
                _menu.Boton(opciones[i],270,162+i*57,740,i==_seleccion);
            _menu.Texto("FLECHAS: ELEGIR Y CAMBIAR   ENTER: CONFIRMAR",640,645,1.8f,Vector3.One,true);
        }
        else
        {
            string[] opciones = _pantalla == Pantalla.Pausa
                ? ["CONTINUAR", "NUEVO MUNDO", "MENU PRINCIPAL"]
                : ["UN JUGADOR", "CONTINUAR", "SALIR"];
            _menu.Texto(_pantalla == Pantalla.Pausa ? "PAUSA" : "PROTOTIPO PC",
                640,245,3f,Vector3.One,true);
            for (int i=0;i<3;i++)
                _menu.Boton(opciones[i],420,300+i*70,440,i==_seleccion);
            _menu.Texto("W S: ELEGIR   ENTER: CONFIRMAR   ESC: VOLVER",
                640,575,1.8f,Vector3.One,true);
        }
        _menu.Texto("G-EVOLVE - MOTOR PROPIO - VERSION PROTOTIPO",
            640,690,1.5f,new Vector3(.57f,.65f,.61f),true);
        _menu.Presentar(Size.X,Size.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _hexRenderer.Dispose();
        _menu.Dispose();
        base.OnUnload();
    }
}

public static class Program
{
    public static void Main()
    {
        using var motor = new MotorGEvolve();
        motor.Run();
    }
}
