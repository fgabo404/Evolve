using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Evolve.Game;

// Owns the native window and menu graphics without introducing simulation logic.
internal sealed class EvolveWindow : GameWindow
{
    private readonly MainMenuPresentation menu = new();
    private readonly MainMenuInteraction interaction = new();
    private PixelMenuRenderer? renderer;
    private Keys? activationKey;
    private bool pointerInside;

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
            interaction.UpdateHover(HitTestPointer());
            menu.Draw(renderer, FramebufferSize.X, FramebufferSize.Y, interaction);
        }
        SwapBuffers();
    }

    // Convert window-coordinate cursor positions to the exact framebuffer layout used for drawing.
    private MainMenuButton? HitTestPointer()
    {
        if (!IsFocused || !pointerInside || ClientSize.X <= 0 || ClientSize.Y <= 0 ||
            FramebufferSize.X <= 0 || FramebufferSize.Y <= 0)
        {
            return null;
        }

        var position = MousePosition;
        float x = position.X * FramebufferSize.X / ClientSize.X;
        float y = position.Y * FramebufferSize.Y / ClientSize.Y;
        return MainMenuLayout.Create(FramebufferSize.X, FramebufferSize.Y).HitTest(x, y);
    }

    // Mouse movement takes precedence over keyboard highlighting without changing menu geometry.
    protected override void OnMouseMove(MouseMoveEventArgs args)
    {
        base.OnMouseMove(args);
        pointerInside = true;
        if (!IsFocused) return;
        activationKey = null;
        interaction.MovePointer(HitTestPointer());
    }

    // Refresh hover even when the pointer enters without moving inside the client area.
    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        pointerInside = true;
        interaction.UpdateHover(HitTestPointer());
    }

    // Leaving the client area cancels a drag so re-entry cannot complete an old press.
    protected override void OnMouseLeave()
    {
        base.OnMouseLeave();
        pointerInside = false;
        CancelPendingInput();
    }

    // Only the left button can begin menu activation.
    protected override void OnMouseDown(MouseButtonEventArgs args)
    {
        base.OnMouseDown(args);
        if (!IsFocused || args.Button != MouseButton.Left) return;
        pointerInside = true;
        activationKey = null;
        interaction.PressPointer(HitTestPointer());
    }

    // Read fresh bounds at release rather than relying on the last rendered hover state.
    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (!IsFocused || args.Button != MouseButton.Left) return;
        ActivateButton(interaction.ReleasePointer(HitTestPointer()));
    }

    // Arrows wrap through the menu; activation keys ignore OS repeat events.
    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (!IsFocused) return;

        if (args.Key is Keys.Up or Keys.Down)
        {
            activationKey = null;
            interaction.MoveSelection(args.Key == Keys.Up ? -1 : 1);
        }
        else if ((args.Key is Keys.Enter or Keys.Space) && !args.IsRepeat && activationKey is null)
        {
            activationKey = args.Key;
            interaction.PressSelection();
        }
    }

    // Release must match the activation key that began the pending keyboard press.
    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args);
        if (!IsFocused || activationKey != args.Key) return;
        activationKey = null;
        ActivateButton(interaction.ReleaseSelection());
    }

    // The input model supplies notices; only the native window owns graceful shutdown.
    private void ActivateButton(MainMenuButton? button)
    {
        if (button is { } activated && interaction.Activate(activated))
        {
            Close();
        }
    }

    // Discard input held across focus changes so delayed releases cannot activate buttons.
    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args);
        if (!args.IsFocused)
        {
            activationKey = null;
            interaction.ResetInput();
        }
    }

    // A layout change invalidates a press that began against the old client bounds.
    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        CancelPendingInput();
    }

    // DPI changes can resize only the framebuffer, so invalidate pending input here too.
    protected override void OnFramebufferResize(FramebufferResizeEventArgs args)
    {
        base.OnFramebufferResize(args);
        CancelPendingInput();
    }

    // Cancel both input sources while allowing keyboard selection and footer notices to persist.
    private void CancelPendingInput()
    {
        activationKey = null;
        interaction.CancelPendingPress();
        interaction.UpdateHover(null);
    }

    // Delete menu resources while the graphics context is still available.
    protected override void OnUnload()
    {
        renderer?.Dispose();
        renderer = null;
        base.OnUnload();
    }
}
