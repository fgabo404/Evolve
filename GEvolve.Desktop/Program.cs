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
    // Giro horizontal limitado a medio círculo para no orbitar completamente el mundo.
    private float _angulo = MathHelper.DegreesToRadians(40f);
    // Inclinación fija por encima del plano: nunca invierte el horizonte.
    private const float Inclinacion = MathF.PI * 35f / 180f;
    private const float AnguloMinimo = 0f;
    private const float AnguloMaximo = MathF.PI;
    private float _distancia = 4.8f;

    public MotorGEvolve() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720),
            Title = "G-Evolve | Hexágono 3D — giro 0°-180° / rueda: zoom"
        })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.08f, 0.16f, 0.22f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        _hexRenderer.Inicializar();
    }

    /// <summary>
    /// Gira horizontalmente la cámara entre 0° y 180° manteniendo pulsado
    /// el botón derecho. El desplazamiento vertical no cambia la inclinación:
    /// de esta forma nunca vemos el terreno desde abajo ni invertimos horizonte.
    /// </summary>
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!MouseState.IsButtonDown(MouseButton.Right)) return;

        _angulo = Math.Clamp(_angulo - e.DeltaX * 0.009f, AnguloMinimo, AnguloMaximo);
    }

    /// <summary>Acerca o aleja la cámara sin cambiar el tamaño de la malla.</summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _distancia = Math.Clamp(_distancia - e.OffsetY * 0.35f, 2.4f, 12f);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Cámara esférica dirigida al origen. El eje Y representa la altura.
        float horizontal = _distancia * MathF.Cos(Inclinacion);
        Vector3 camara = new(
            horizontal * MathF.Sin(_angulo),
            _distancia * MathF.Sin(Inclinacion),
            horizontal * MathF.Cos(_angulo));

        Matrix4 vista = Matrix4.LookAt(camara, Vector3.Zero, Vector3.UnitY);
        float aspecto = (float)Size.X / Math.Max(1, Size.Y);
        // Perspectiva usada aquí para comprobar el volumen; podremos adoptar
        // proyección ortográfica inclinada al integrar la cuadrícula completa.
        Matrix4 proyeccion = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f), aspecto, 0.1f, 100f);

        _hexRenderer.Dibujar(vista * proyeccion);
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
