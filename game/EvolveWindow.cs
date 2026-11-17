using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Evolve.Game;

internal sealed class EvolveWindow : GameWindow
{
    public EvolveWindow()
        : base(
            new GameWindowSettings { UpdateFrequency = 60 },
            new NativeWindowSettings
            {
                Title = "Evolve",
                ClientSize = new Vector2i(1280, 720),
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        VSync = VSyncMode.On;
        GL.ClearColor(0.035f, 0.075f, 0.16f, 1.0f);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}
