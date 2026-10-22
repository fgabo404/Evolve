
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GEvolve.Desktop;

public class MotorGEvolve : GameWindow
{
    // Renderizador propio; no contiene reglas de simulación.
    private readonly HexRenderer _hexRenderer = new();
    public MotorGEvolve()
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                ClientSize = new Vector2i(1280, 720),
                Title = "G-Evolve | Motor Gráfico v0.1"
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

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(
            ClearBufferMask.ColorBufferBit |
            ClearBufferMask.DepthBufferBit
        );

        _hexRenderer.Dibujar();
        SwapBuffers();
    }

    /// <summary>Libera recursos OpenGL antes de destruir el contexto.</summary>
    protected override void OnUnload()
    {
        _hexRenderer.Dispose();
        base.OnUnload();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
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
