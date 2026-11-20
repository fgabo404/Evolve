// Owns the single OpenTK window, native input routing and menu/shared generation-screen graphics lifetime.
// Connects presentation to existing engine session APIs without implementing generation algorithms.

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Game;

internal sealed class EvolveWindow : GameWindow
{
    private readonly MainMenuPresentation menu = new();
    private readonly MainMenuInteraction interaction = new();
    private PixelMenuRenderer? renderer;
    private bool pointerInside;
    private readonly LaboratorySession laboratory = new();
    private readonly GridCamera gridCamera = new();
    private HexGridRenderer? gridRenderer;
    private readonly LaboratoryUiState laboratoryUi = new();
    private bool panning, selectingCell;
    private LaboratoryHit? laboratoryPress;
    private (double X, double Y) previousPointer;
    private HexGridData? cameraGrid;

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
        try
        {
            renderer = new PixelMenuRenderer();
            gridRenderer = new HexGridRenderer();
        }
        catch
        {
            gridRenderer?.Dispose();
            renderer?.Dispose();
            throw;
        }
    }

    // Skip minimized framebuffers and draw the active screen at the current physical size.
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (WindowState == OpenTK.Windowing.Common.WindowState.Minimized ||
            FramebufferSize.X <= 0 || FramebufferSize.Y <= 0)
        {
            return;
        }

        GL.ClearColor(laboratory.IsOpen ? LaboratoryPresentation.Background : MainMenuStyle.Background);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        if (renderer is not null)
        {
            if (laboratory.IsOpen)
            {
                var layout = CurrentLaboratoryLayout();
                gridRenderer?.Synchronize(laboratory);
                gridRenderer?.Draw(renderer, gridCamera, FramebufferSize.X, FramebufferSize.Y);
                LaboratoryPresentation.Draw(renderer, laboratory, layout, FramebufferSize.X, FramebufferSize.Y, laboratoryUi);
            }
            else
            {
                interaction.UpdateHover(HitTestPointer());
                menu.Draw(renderer, FramebufferSize.X, FramebufferSize.Y, interaction);
            }
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

    // Route pointer movement to the camera/dropdown or the unchanged main-menu interaction.
    protected override void OnMouseMove(MouseMoveEventArgs args)
    {
        base.OnMouseMove(args);
        pointerInside = true;
        if (!IsFocused) return;
        if (laboratory.IsOpen)
        {
            var point = FramePointer();
            if (panning) gridCamera.Pan(point.X - previousPointer.X, point.Y - previousPointer.Y);
            previousPointer = point;
            var hover = laboratoryUi.HitTest(CurrentLaboratoryLayout(), point.X, point.Y);
            if (hover is { Action: LaboratoryAction.Option } option) laboratoryUi.HoverOption(option.Index);
            return;
        }
        interaction.MovePointer(HitTestPointer());
    }

    // Refresh hover even when the pointer enters without moving inside the client area.
    protected override void OnMouseEnter()
    {
        base.OnMouseEnter();
        pointerInside = true;
        if (laboratory.IsOpen) return;
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
        if (laboratory.IsOpen)
        {
            if (!IsFocused) return;
            previousPointer = FramePointer();
            var layout = CurrentLaboratoryLayout();
            if (layout.Viewport.Contains(previousPointer.X, previousPointer.Y)) laboratoryUi.EditingSeed = false;
            if (args.Button == MouseButton.Right && layout.Viewport.Contains(previousPointer.X, previousPointer.Y))
                panning = true;
            if (args.Button == MouseButton.Left)
            {
                laboratoryPress = laboratoryUi.HitTest(layout, previousPointer.X, previousPointer.Y);
                if (laboratoryPress is null) laboratoryUi.CloseTransient();
                selectingCell = layout.Viewport.Contains(previousPointer.X, previousPointer.Y);
            }
            return;
        }
        if (!IsFocused || args.Button != MouseButton.Left) return;
        pointerInside = true;
        interaction.PressPointer(HitTestPointer());
    }

    // Read fresh bounds at release rather than relying on the last rendered hover state.
    protected override void OnMouseUp(MouseButtonEventArgs args)
    {
        base.OnMouseUp(args);
        if (laboratory.IsOpen)
        {
            if (args.Button == MouseButton.Right) panning = false;
            if (args.Button != MouseButton.Left || !IsFocused) return;
            var point = FramePointer();
            var layout = CurrentLaboratoryLayout();
            if (laboratoryPress is { } action && laboratoryUi.HitTest(layout, point.X, point.Y) == action)
                ActivateLaboratory(action);
            else if (selectingCell && layout.Viewport.Contains(point.X, point.Y) && laboratory.Grid is { } grid)
            {
                var world = gridCamera.ToWorld(point.X, point.Y);
                laboratory.Select(GridVisualGeometry.Pick(grid, world.X, world.Y));
            }
            laboratoryPress = null;
            selectingCell = false;
            return;
        }
        if (!IsFocused || args.Button != MouseButton.Left) return;
        ActivateButton(interaction.ReleasePointer(HitTestPointer()));
    }

    // Route keyboard input to the active shared screen or the original menu interaction.
    protected override void OnKeyDown(KeyboardKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (!IsFocused) return;
        if (laboratory.IsOpen)
        {
            if (args.Key == Keys.Escape)
            {
                if (laboratoryUi.Dropdown is not null || laboratoryUi.EditingSeed) laboratoryUi.CloseTransient();
                else ReturnToMenu();
            }
            else if (laboratoryUi.Dropdown is not null)
            {
                if (args.Key == Keys.Up) laboratoryUi.MoveOption(-1);
                if (args.Key == Keys.Down) laboratoryUi.MoveOption(1);
                if (args.Key == Keys.Enter) ActivateLaboratory(new(LaboratoryAction.Option, laboratoryUi.HighlightedOption));
            }
            else if (laboratoryUi.EditingSeed)
            {
                if (args.Key == Keys.Backspace && laboratory.SeedText.Length > 0)
                    laboratory.SetSeedText(laboratory.SeedText[..^1]);
                else if (args.Key == Keys.Delete) laboratory.SetSeedText("");
                else if (args.Key == Keys.Enter) CreateConfiguredWorld();
            }
            else if (!args.IsRepeat)
            {
                if (args.Key == Keys.S) laboratory.CycleSize();
                if (args.Key == Keys.T) laboratory.CycleWorldType();
                if (args.Key == Keys.N) CreateConfiguredWorld();
                if (args.Key == Keys.G && laboratory.Mode == GenerationScreenMode.Laboratory) laboratory.RunNextStage();
                if (args.Key == Keys.F) gridCamera.Fit();
                if (args.Key == Keys.Left) gridCamera.Pan(40, 0);
                if (args.Key == Keys.Right) gridCamera.Pan(-40, 0);
                if (args.Key == Keys.Up) gridCamera.Pan(0, 40);
                if (args.Key == Keys.Down) gridCamera.Pan(0, -40);
            }
            return;
        }

        if (args.Key is Keys.Up or Keys.Down)
        {
            interaction.MoveSelection(args.Key == Keys.Up ? -1 : 1);
        }
        else if (args.Key is Keys.Enter or Keys.Space)
        {
            interaction.PressSelection(args.Key == Keys.Enter
                ? MainMenuActivationKey.Enter : MainMenuActivationKey.Space, args.IsRepeat);
        }
    }

    // Release must match the activation key that began the pending keyboard press.
    protected override void OnKeyUp(KeyboardKeyEventArgs args)
    {
        base.OnKeyUp(args);
        if (laboratory.IsOpen) return;
        if (!IsFocused || args.Key is not (Keys.Enter or Keys.Space)) return;
        ActivateButton(interaction.ReleaseSelection(args.Key == Keys.Enter
            ? MainMenuActivationKey.Enter : MainMenuActivationKey.Space));
    }

    // The input model supplies notices; only the native window owns graceful shutdown.
    private void ActivateButton(MainMenuButton? button)
    {
        if (laboratory.OpenFromMenu(button))
        {
            interaction.ResetInput();
            laboratoryUi.Reset();
            return;
        }
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
            interaction.ResetInput();
            CancelPendingInput();
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
        panning = selectingCell = false;
        laboratoryPress = null;
        laboratoryUi.CloseTransient();
        interaction.CancelPendingPress();
        interaction.UpdateHover(null);
    }

    // Delete menu and grid resources while the graphics context is still available.
    protected override void OnUnload()
    {
        gridRenderer?.Dispose();
        gridRenderer = null;
        renderer?.Dispose();
        renderer = null;
        base.OnUnload();
    }

    private LaboratoryLayout CurrentLaboratoryLayout()
    {
        double scale = ClientSize.X > 0 && ClientSize.Y > 0
            ? Math.Min(FramebufferSize.X / (double)ClientSize.X, FramebufferSize.Y / (double)ClientSize.Y) : 1;
        var layout = new LaboratoryLayout(FramebufferSize.X, FramebufferSize.Y, laboratoryUi.Scroll, scale);
        gridCamera.SetViewport(layout.Viewport);
        if (!ReferenceEquals(cameraGrid, laboratory.Grid))
        {
            cameraGrid = laboratory.Grid;
            if (cameraGrid is not null) gridCamera.SetBounds(GridVisualGeometry.Bounds(cameraGrid));
        }
        return layout;
    }

    // All Laboratory interaction uses the same physical coordinates as rendering, including high DPI.
    private (double X, double Y) FramePointer() => LaboratoryLayout.ToFramebuffer(
        MousePosition.X, MousePosition.Y, ClientSize.X, ClientSize.Y, FramebufferSize.X, FramebufferSize.Y);

    private void ActivateLaboratory(LaboratoryHit hit)
    {
        var action = hit.Action;
        if (action == LaboratoryAction.Option && laboratoryUi.Dropdown is { } popup)
        {
            if (hit.Index < 0 || hit.Index >= laboratoryUi.OptionCount) return;
            if (popup == LaboratoryAction.Size) laboratory.SetSize(Enum.GetValues<Evolve.Definitions.WorldGeneration.MapSize>()[hit.Index]);
            if (popup == LaboratoryAction.WorldType) laboratory.SetWorldType(Enum.GetValues<Evolve.Definitions.WorldGeneration.WorldType>()[hit.Index]);
            if (popup == LaboratoryAction.View) laboratory.SelectView(Evolve.Engine.WorldGeneration.GenerationStage.Grid);
            laboratoryUi.CloseTransient();
            return;
        }
        if (action is LaboratoryAction.Size or LaboratoryAction.WorldType or LaboratoryAction.View)
        {
            laboratoryUi.Toggle(action);
            return;
        }
        laboratoryUi.CloseTransient();
        laboratoryUi.EditingSeed = action == LaboratoryAction.Seed;
        switch (action)
        {
            case LaboratoryAction.Randomize: laboratory.RandomizeSeed(); break;
            case LaboratoryAction.Fit: gridCamera.Fit(); break;
            case LaboratoryAction.Create: CreateConfiguredWorld(); break;
            case LaboratoryAction.Configure: laboratory.ClearSession(); laboratoryUi.Reset(); break;
            case LaboratoryAction.Back: ReturnToMenu(); break;
            case LaboratoryAction.Stage:
                var stage = (Evolve.Engine.WorldGeneration.GenerationStage)hit.Index;
                if (laboratory.IsCompleted(stage)) laboratory.SelectView(stage);
                else if (laboratory.Mode == GenerationScreenMode.Laboratory && laboratory.Context?.GetProgress().NextStage == stage)
                    laboratory.RunNextStage();
                break;
        }
    }

    private void CreateConfiguredWorld()
    {
        if (laboratory.Mode == GenerationScreenMode.WorldCreation) laboratory.CreateWorld();
        else laboratory.CreateSession();
    }

    private void ReturnToMenu()
    {
        laboratory.ReturnToMenu();
        laboratoryUi.EditingSeed = false;
        CancelPendingInput();
        interaction.ResetInput();
    }

    protected override void OnTextInput(TextInputEventArgs args)
    {
        base.OnTextInput(args);
        if (!IsFocused || !laboratory.IsOpen || !laboratoryUi.EditingSeed) return;
        foreach (char character in args.AsString)
            if (laboratory.SeedText.Length < 20 && (char.IsAsciiDigit(character) ||
                ((character == '-' || character == '+') && laboratory.SeedText.Length == 0)))
                laboratory.SetSeedText(laboratory.SeedText + character);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        if (!IsFocused || !laboratory.IsOpen) return;
        var point = FramePointer();
        var layout = CurrentLaboratoryLayout();
        if (point.X < layout.SidebarWidth)
        {
            laboratoryUi.ScrollBy(-args.OffsetY * 45 * layout.Scale, layout);
            laboratoryPress = null;
        }
        else if (layout.Viewport.Contains(point.X, point.Y))
            gridCamera.Zoom(args.OffsetY, point.X, point.Y);
    }
}
