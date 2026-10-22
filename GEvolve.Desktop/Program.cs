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
    private readonly HexRenderer _hexRenderer = new();
    // Giro horizontal continuo de 360°; puede repetirse sin detenerse en los extremos.
    private float _angulo = MathHelper.DegreesToRadians(40f);
    // Inclinación fija por encima del plano: nunca invierte el horizonte.
    private const float Inclinacion = MathF.PI * 35f / 180f;
    // La cámara orbita alrededor de un objetivo desplazable sobre el terreno.
    private float _distancia = 38f;
    private Vector3 _objetivo = new(0f, 0.5f, 0f);
    private const float Velocidad = 18f;
    private WorldMap? _mundo;
    // Ancho físico X del mundo en hexágonos pointy-top con radio estándar.
    private float _anchoMundo;

    public MotorGEvolve() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "G-Evolve | Mundo continuo E/O — WASD / giro 360 / zoom limitado"
        })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.16f, 0.22f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        // Nuevo sistema modular de generación y chunks; mundo reproducible 100x50.
        _mundo = WorldSystem.Generar(new WorldSettings(100, 50, 2026, 10, 30));
        _anchoMundo = MathF.Sqrt(3f) * HexGeometry.RadioEstandar * _mundo.Configuracion.Ancho;
        // La altura lógica va de 0 a 100. Se transforma a unidades gráficas
        // sin modificar los datos del terreno. El mar queda horizontal.
        CeldaMundo[] representacion = _mundo.Celdas.Select(c =>
            new CeldaMundo(c.Coordenada,
                c.Superficie == TipoSuperficie.Oceano ? 0.08f
                : 0.13f + c.Altura / 100f * 2.8f,
                c.Superficie, c.Relieve)).ToArray();
        _hexRenderer.Inicializar(representacion);
    }

    /// <summary>
    /// Gira horizontalmente la cámara libremente a lo largo de 360° manteniendo pulsado
    /// el botón derecho. El desplazamiento vertical no cambia la inclinación:
    /// de esta forma nunca vemos el terreno desde abajo ni invertimos horizonte.
    /// </summary>
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!MouseState.IsButtonDown(MouseButton.Right)) return;

        // Normalización circular: evita límites y permite múltiples vueltas completas.
        _angulo = (_angulo - e.DeltaX * 0.009f) % (2f * MathF.PI);
        if (_angulo < 0f) _angulo += 2f * MathF.PI;
    }

    /// <summary>Acerca o aleja la cámara sin cambiar el tamaño de la malla.</summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        // El zoom no puede alejarse tanto que el mundo se pierda en pantalla.
        _distancia = Math.Clamp(_distancia - e.OffsetY * 2f, 8f, 52f);
    }

    /// <summary>Desplazamiento WASD en el plano XZ, relativo al giro de cámara.</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
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
            _objetivo.Z = Math.Clamp(_objetivo.Z, -35f, 34f);
        }
        if (KeyboardState.IsKeyDown(Keys.Home)) _objetivo = new Vector3(0f, 0.5f, 0f);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Cámara esférica dirigida al origen. El eje Y representa la altura.
        float horizontal = _distancia * MathF.Cos(Inclinacion);
        Vector3 objetivo = _objetivo;
        Vector3 camara = objetivo + new Vector3(
            horizontal * MathF.Sin(_angulo),
            _distancia * MathF.Sin(Inclinacion),
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

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _hexRenderer.Dispose();
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
