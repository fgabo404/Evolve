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
    private WorldSettings _ajustes = WorldPresets.Aplicar(TipoMundo.ContinentesGrandes, 200, 150, 2026);
    private enum Pantalla { Inicio, Configuracion, Cargando, Jugando, Pausa }
    private Task<WorldMap>? _generacion;
    private string _errorGeneracion = "";
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

    /// <summary>Inicia el cálculo sin bloquear el hilo de OpenGL.</summary>
    private void IniciarGeneracion()
    {
        if (_pantalla == Pantalla.Cargando) return;
        WorldSettings configuracion = _ajustes;
        _errorGeneracion = "";
        _generacion = Task.Run(() => WorldSystem.Generar(configuracion));
        _pantalla = Pantalla.Cargando;
    }

    /// <summary>Construye los recursos gráficos únicamente en el hilo de la ventana.</summary>
    private void PresentarMundo(WorldMap nuevo)
    {
        var renderer = new HexRenderer();
        CeldaMundo[] representacion = nuevo.Celdas.Select(c =>
            new CeldaMundo(c.Coordenada,
                c.Superficie == TipoSuperficie.Oceano ? 0f
                : 0.13f + c.Altura / 100f * 2.8f,
                c.Superficie, c.Relieve, c.Altura, c.Bioma)).ToArray();
        renderer.Inicializar(representacion);
        _hexRenderer.Dispose();
        _hexRenderer = renderer;
        _mundo = nuevo;
        _objetivo = new Vector3(0f, 0.5f, 0f);
        _distancia = 38f;
        _anchoMundo = MathF.Sqrt(3f) * HexGeometry.RadioEstandar * nuevo.Configuracion.Ancho;
        _pantalla = Pantalla.Jugando;
    }

    /// <summary>Regenera conservando tipo y tamaño y cambiando la semilla.</summary>
    private void RegenerarMundo()
    {
        _ajustes = _ajustes with { Semilla = unchecked(_ajustes.Semilla + 1) };
        IniciarGeneracion();
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
        _distancia = Math.Clamp(_distancia * MathF.Pow(1.12f, -e.OffsetY), 8f, 750f);
    }

    /// <summary>Desplazamiento WASD en el plano XZ, relativo al giro de cámara.</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (_pantalla == Pantalla.Cargando && _generacion is { IsCompleted: true } tarea)
        {
            _generacion = null;
            try { PresentarMundo(tarea.GetAwaiter().GetResult()); }
            catch (Exception ex)
            {
                _errorGeneracion = ex.Message;
                _pantalla = Pantalla.Configuracion;
                _seleccion = 3;
            }
        }
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
            alturaVisible * aspecto, alturaVisible, 0.1f, 2000f);

        _hexRenderer.Dibujar(vista * proyeccion, _anchoMundo);
        // HUD discreto, ubicado arriba a la derecha; se dibuja sobre la escena.
        _menu.Empezar();
        _menu.Boton("REGENERAR MUNDO", 910f, 18f, 350f, false);
        _menu.Texto("SEMILLA: " + _ajustes.Semilla, 1085f, 81f, 1.65f,
            new Vector3(.94f, .96f, .97f), true);
        _menu.Presentar(Size.X, Size.Y);
        SwapBuffers();
    }

    /// <summary>Entrada de menú por teclado; no afecta controles del mundo.</summary>
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_pantalla == Pantalla.Cargando) return;
        if (e.Key == Keys.Escape)
        {
            if (_pantalla == Pantalla.Jugando) { _pantalla = Pantalla.Pausa; _seleccion = 0; }
            else if (_pantalla == Pantalla.Configuracion) { _pantalla = Pantalla.Inicio; _seleccion = 0; }
            else if (_pantalla == Pantalla.Pausa) { _pantalla = Pantalla.Jugando; }
            return;
        }
        if (_pantalla == Pantalla.Jugando)
        {
            if (e.Key == Keys.F5) RegenerarMundo();
            return;
        }
        int opciones = _pantalla == Pantalla.Configuracion ? 5 : 3;
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
        if (e.Button != MouseButton.Left || _pantalla == Pantalla.Cargando) return;
        float x = MouseState.Position.X * 1280f / Math.Max(1, Size.X);
        float y = MouseState.Position.Y * 720f / Math.Max(1, Size.Y);
        if (_pantalla == Pantalla.Jugando)
        {
            if (x >= 910f && x <= 1260f && y >= 18f && y <= 70f)
                RegenerarMundo();
            return;
        }
        if (_pantalla == Pantalla.Configuracion)
        {
            if (x >= 310f && x <= 970f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float arriba = 285f + i * 83f;
                    if (y >= arriba && y <= arriba + 66f)
                    {
                        _seleccion = i;
                        if (x >= 655f) ModificarOpcion(x < 805f ? -1 : 1);
                        return;
                    }
                }
            }
            if (x >= 390f && x <= 890f && y >= 547f && y <= 605f)
            { _seleccion = 3; ActivarSeleccion(); }
            else if (x >= 390f && x <= 890f && y >= 615f && y <= 673f)
            { _seleccion = 4; ActivarSeleccion(); }
        }
        else if (x >= 420f && x <= 860f)
        {
            int idx = (int)MathF.Floor((y - 330f) / 78f);
            if (idx >= 0 && idx < 3) { _seleccion = idx; ActivarSeleccion(); }
        }
    }

    /// <summary>Modifica valores configurables antes de generar el mundo.</summary>
    private void ModificarOpcion(int delta)
    {
        switch (_seleccion)
        {
            case 0:
                int cantidad = Enum.GetValues<TipoMundo>().Length;
                TipoMundo tipo = (TipoMundo)(((int)_ajustes.Tipo + delta + cantidad) % cantidad);
                _ajustes = WorldPresets.Aplicar(tipo, _ajustes.Ancho, _ajustes.Alto, _ajustes.Semilla);
                break;
            case 1:
                int[] anchos = [100, 200, 300];
                int[] altos = [50, 150, 200];
                int indice = _ajustes.Ancho == 100 ? 0 : _ajustes.Ancho == 200 ? 1 : 2;
                indice = (indice + delta + 3) % 3;
                _ajustes = _ajustes with { Ancho = anchos[indice], Alto = altos[indice] };
                break;
            case 2:
                _ajustes = _ajustes with { Semilla = _ajustes.Semilla + delta };
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
            if (_seleccion == 3) { IniciarGeneracion(); _seleccion = 0; }
            else if (_seleccion == 4) { _pantalla = Pantalla.Inicio; _seleccion = 0; }
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
        Vector3 tinta = new(.13f, .18f, .20f);
        Vector3 tenue = new(.38f, .45f, .47f);
        Vector3 verde = new(.21f, .62f, .46f);
        // Fondo gris claro y sobrio, sin fotografías ni paisajes.
        _menu.Rect(0, 0, 1280, 720, new Vector3(.86f, .88f, .88f));
        _menu.Texto("G-EVOLVE", 640, 84, 11f, tinta, true);
        // Marca visual verde únicamente sobre la G.
        _menu.Texto("G", 640 - 8 * 6f * 11f / 2f, 84, 11f, verde);
        _menu.Texto("V0.1.0", 640, 182, 2.5f, tenue, true);

        if (_pantalla == Pantalla.Cargando)
        {
            _menu.Texto("CARGANDO MUNDO", 640, 326, 5f, tinta, true);
            _menu.Texto("GENERANDO TERRENO Y BIOMAS", 640, 390, 2f, tenue, true);
        }
        else if (_pantalla == Pantalla.Configuracion)
        {
            if (_errorGeneracion.Length > 0)
                _menu.Texto("ERROR AL GENERAR MUNDO", 640, 686, 2f, new Vector3(.7f,.18f,.18f), true);
            _menu.Texto("NUEVA PARTIDA", 640, 230, 4.5f, tinta, true);
            // Separadores finos a ambos lados del encabezado.
            _menu.Rect(255, 245, 230, 2, tenue);
            _menu.Rect(795, 245, 230, 2, tenue);
            _menu.Selector("TIPO DE MUNDO", _ajustes.Tipo.ToString().ToUpperInvariant(),
                310, 285, _seleccion == 0);
            _menu.Selector("TAMANO", _ajustes.Ancho + " X " + _ajustes.Alto,
                310, 368, _seleccion == 1);
            _menu.Selector("SEMILLA", _ajustes.Semilla.ToString(),
                310, 451, _seleccion == 2);
            _menu.Accion("GENERAR MUNDO", 390, 547, 500, _seleccion == 3, true);
            _menu.Accion("VOLVER", 390, 615, 500, _seleccion == 4, false);
        }
        else
        {
            string[] opciones = _pantalla == Pantalla.Pausa
                ? ["CONTINUAR", "NUEVA PARTIDA", "MENU PRINCIPAL"]
                : ["NUEVA PARTIDA", "CONTINUAR", "SALIR"];
            if (_pantalla == Pantalla.Pausa)
                _menu.Texto("PAUSA", 640, 258, 4f, tinta, true);
            for (int i = 0; i < 3; i++)
                _menu.Boton(opciones[i], 420, 330 + i * 78, 440, i == _seleccion);
        }
        _menu.Presentar(Size.X, Size.Y);
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
