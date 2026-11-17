using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Evolve.Game;

// Owns the native window and menu graphics without introducing simulation logic.
internal sealed class EvolveWindow : GameWindow
{
    private readonly MainMenuPresentation menu = new();
    private PixelMenuRenderer? renderer;

    // Configure the native window and graphics context required by the minimal UI.
    public EvolveWindow()
        : base(
            new GameWindowSettings { UpdateFrequency = 60 },
            new NativeWindowSettings
            {
                Title = "Evolve",
                ClientSize = new Vector2i(1280, 720),
                MinimumClientSize = new Vector2i(400, 300),
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
        GL.ClearColor(MainMenuStyle.Background);
        renderer = new PixelMenuRenderer();
    }

    // Skip minimized framebuffers and redraw the menu at the current physical size.
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (FramebufferSize.X <= 0 || FramebufferSize.Y <= 0)
        {
            return;
        }

        GL.Clear(ClearBufferMask.ColorBufferBit);
        if (renderer is not null)
        {
            menu.Draw(renderer, FramebufferSize.X, FramebufferSize.Y);
        }
        SwapBuffers();
    }

    // Delete menu resources while the graphics context is still available.
    protected override void OnUnload()
    {
        renderer?.Dispose();
        renderer = null;
        base.OnUnload();
    }
}
