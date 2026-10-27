using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GEvolve.Game;

internal sealed class EvolveWindow : GameWindow
{
    public EvolveWindow() : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1280, 720),
        Title = "Evolve"
    }) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(1f, 1f, 1f, 1f);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        SwapBuffers();
    }
}

internal static class Program
{
    private static void Main()
    {
        using var window = new EvolveWindow();
        window.Run();
    }
}
