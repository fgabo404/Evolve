using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Evolve.Game;

// Owns the UI-01 window and solid background without introducing simulation logic.
internal sealed class EvolveWindow : GameWindow
{
    // Configure the native window and graphics context required by the minimal UI.
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

    // Initialize graphics state after OpenTK has made the context available.
    protected override void OnLoad()
    {
        base.OnLoad();

        VSync = VSyncMode.On;
        GL.ClearColor(0.035f, 0.075f, 0.16f, 1.0f);
    }

    // Clear each back buffer before presenting it so the background stays solid.
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}
