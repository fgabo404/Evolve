using System.Numerics;
using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GEvolve.Game;

internal sealed class EvolveWindow : GameWindow
{
    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector4 Muted = new(.65f, .76f, .83f, 1);
    private static readonly Vector4 Accent = new(.38f, .76f, .89f, 1);
    private readonly GameSession _session = new();
    private readonly MapCamera2D _camera = new();
    private readonly Dictionary<UiAction, float> _hover = [];
    private UiRenderer? _ui;
    private HexGridRenderer? _gridRenderer;
    private UiAction? _pressed;
    private bool _dragging;
    private float _dragDistance;
    private Vector2 _lastPointer;
    private bool _dirty = true;
    private readonly bool _graphicsSmoke;
    private int _smokeStep;

    public EvolveWindow(bool graphicsSmoke = false) : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1280, 720),
        Title = "Evolve",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core
    }) { _graphicsSmoke = graphicsSmoke; }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _ui = new UiRenderer();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        if (_session.Grid is { } grid)
            _camera.Fit(grid.Definition.Columns, grid.Definition.Rows, ClientSize.X, ClientSize.Y);
        _dirty = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        var mouse = MousePosition;
        UiAction? hovered = Hit(mouse.X, mouse.Y);
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
        {
            _hover.TryGetValue(button.Action, out float value);
            float target = hovered == button.Action ? 1 : 0;
            float next = value + (target - value) *
                (1 - MathF.Exp(-10f * (float)args.Time));
            if (MathF.Abs(next - target) < .005f) next = target;
            if (next != value) { _hover[button.Action] = next; _dirty = true; }
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!_dirty || _ui is null) return;
        _dirty = false;
        long started = Stopwatch.GetTimestamp();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.96f, .98f, .99f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _ui.Clear();
        if (_session.Screen == GameScreen.World) RenderWorld();
        else RenderMenu();
        _ui.Flush(ClientSize.X, ClientSize.Y);
        SwapBuffers();
        if (_graphicsSmoke) AdvanceGraphicsSmoke(started);
    }

    private void AdvanceGraphicsSmoke(long started)
    {
        GL.Finish();
        var error = GL.GetError();
        if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
            throw new InvalidOperationException($"OpenGL smoke test failed: {error}");
        if (_session.Screen == GameScreen.World)
            Console.WriteLine($"OpenGL {_session.Grid!.Definition.Size}: " +
                $"{_gridRenderer!.VisibleInstanceCount} visible instances, " +
                $"{Stopwatch.GetElapsedTime(started).TotalMilliseconds:F1} ms frame");
        switch (_smokeStep++)
        {
            case 0: Activate(UiAction.Create); break;
            case 1: Activate(UiAction.Generate); break;
            case 2: Activate(UiAction.Back); break;
            case 3:
                Activate(UiAction.Create); Activate(UiAction.Medium);
                Activate(UiAction.Generate); break;
            case 4: Activate(UiAction.Back); break;
            case 5:
                Activate(UiAction.Create); Activate(UiAction.Large);
                Activate(UiAction.Generate); break;
            case 6:
                _camera.Zoom(8, new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f));
                _camera.Pan(120, 20);
                _session.SelectAt(_camera.ScreenToWorld(
                    new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f)));
                if (_session.SelectedCell is null)
                    throw new InvalidOperationException("OpenGL smoke selection failed.");
                _dirty = true;
                break;
            default: Close(); break;
        }
    }

    private void RenderMenu()
    {
        if (_ui is null) return;
        float w = ClientSize.X, h = ClientSize.Y;
        _ui.Rect(0, 0, w, h, new Vector4(.055f, .10f, .16f, 1));
        _ui.Rect(0, h * .72f, w, h * .28f, new Vector4(.075f, .16f, .21f, 1));
        _ui.Rect(0, h * .72f, w, 2, new Vector4(.24f, .42f, .48f, 1));
        float center = w / 2;
        float titlePixel = Math.Clamp(w / 145f, 5, 9);
        if (_session.Screen == GameScreen.MainMenu)
        {
            _ui.Text("EVOLVE", center, h * .17f, titlePixel, White);
            _ui.Text("Civilization Simulator", center, h * .17f + titlePixel * 9,
                Math.Clamp(w / 480f, 1.5f, 2.5f), Muted);
        }
        else if (_session.Screen == GameScreen.CreateWorld)
        {
            _ui.Text("Crear nuevo mundo", center, MathF.Max(32, h * .10f),
                Math.Clamp(w / 420f, 2, 3.2f), White);
            _ui.Text("Elige el tamaño del grid", center, MathF.Max(66, h * .16f),
                1.8f, Muted);
        }
        else
        {
            _ui.Text("Opciones", center, h * .24f, 5, White);
            _ui.Text("Opciones avanzadas pendientes", center, h * .42f, 2, Muted);
        }
        DrawButtons();
        if (_session.Screen == GameScreen.CreateWorld && _session.Error is { } error)
            _ui.Text(error, center, h - 32, 1.6f, new Vector4(1, .61f, .59f, 1));
    }

    private void RenderWorld()
    {
        if (_ui is null || _gridRenderer is null || _session.Grid is not { } grid) return;
        int scissorHeight = Math.Max(0,
            (int)(FramebufferSize.Y * (ClientSize.Y - MapCamera2D.TopBarHeight) /
                (float)Math.Max(ClientSize.Y, 1)));
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(0, 0, FramebufferSize.X, scissorHeight);
        _gridRenderer.Draw(_camera, ClientSize.X, ClientSize.Y);
        GL.Disable(EnableCap.ScissorTest);

        if (_session.SelectedCell is { } selected)
        {
            float right = _camera.X + _camera.ViewWidth / _camera.Scale;
            int first = (int)MathF.Floor((_camera.X - 2) / 1.5f);
            int last = (int)MathF.Ceiling((right + 1) / 1.5f);
            for (int column = first; column <= last; column++)
            {
                if (((column % grid.Definition.Columns) + grid.Definition.Columns) %
                    grid.Definition.Columns != selected.Address.Column) continue;
                Vector2 world = FlatHexGeometry.Center(new GridAddress(column, selected.Address.Row));
                Vector2 screen = _camera.WorldToScreen(world);
                if (screen.Y >= MapCamera2D.TopBarHeight && screen.Y <= ClientSize.Y)
                    _ui.Hex(screen.X, screen.Y, _camera.Scale,
                        new Vector4(.54f, .79f, .89f, .9f),
                        new Vector4(.15f, .43f, .59f, 1));
            }
        }

        _ui.Rect(0, 0, ClientSize.X, MapCamera2D.TopBarHeight,
            new Vector4(.06f, .13f, .20f, 1));
        _ui.Rect(0, MapCamera2D.TopBarHeight - 2, ClientSize.X, 2,
            new Vector4(.28f, .54f, .63f, 1));
        _ui.Text("EVOLVE", 18, 18, 3, White, centered: false);
        string size = grid.Definition.Size switch
        {
            GridSize.Small => "PEQUEÑO", GridSize.Medium => "MEDIANO", _ => "GRANDE"
        };
        if (ClientSize.X >= 600)
            _ui.Text($"{size}  {grid.Definition.Columns} X {grid.Definition.Rows}",
                ClientSize.X / 2f, 22, 1.8f, Muted);
        DrawButtons();
        if (_session.SelectedCell is { } cell)
        {
            string label = $"CELDA {cell.Id}  C:{cell.Address.Column}  F:{cell.Address.Row}";
            float labelWidth = (label.Length * 6 - 1) * 1.8f;
            _ui.Rect(12, ClientSize.Y - 43, labelWidth + 20, 31,
                new Vector4(.07f, .16f, .23f, .9f));
            _ui.Text(label, 22, ClientSize.Y - 34, 1.8f, White, centered: false);
        }
    }

    private void DrawButtons()
    {
        if (_ui is null) return;
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
        {
            var r = button.Bounds;
            _hover.TryGetValue(button.Action, out float hover);
            bool selectedSize = _session.Screen == GameScreen.CreateWorld &&
                button.Action is UiAction.Small or UiAction.Medium or UiAction.Large &&
                ScreenLayout.SizeFor(button.Action) == _session.SelectedSize;
            _ui.Rect(r.X + 3, r.Y + 4, r.Width, r.Height,
                new Vector4(.02f, .05f, .09f, .7f));
            Vector4 baseColor = button.Action == UiAction.Generate
                ? new(.12f, .35f, .45f, 1) : new(.16f, .24f, .31f, 1);
            Vector4 color = Vector4.Lerp(baseColor, new Vector4(.27f, .44f, .52f, 1), hover);
            _ui.Rect(r.X, r.Y, r.Width, r.Height, color);
            _ui.Border(r.X, r.Y, r.Width, r.Height, selectedSize ? 2.5f : 1.5f,
                selectedSize ? Accent : new Vector4(.37f, .51f, .59f, 1));
            if (selectedSize || hover > .05f)
                _ui.Rect(r.X, r.Y, 4, r.Height, Accent);
            if (_session.Screen == GameScreen.CreateWorld &&
                button.Action is UiAction.Small or UiAction.Medium or UiAction.Large)
            {
                var definition = HexGridDefinitions.Get(ScreenLayout.SizeFor(button.Action));
                _ui.Text(button.Text, r.X + r.Width / 2, r.Y + 7,
                    MathF.Min(2.4f, r.Height / 18), White);
                _ui.Text($"{definition.Columns} X {definition.Rows}  {(definition.Columns * definition.Rows).ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} HEXÁGONOS",
                    r.X + r.Width / 2, r.Y + r.Height - 16, 1.5f, Muted);
            }
            else
            {
                float pixel = _session.Screen == GameScreen.World ? 1.9f : 2.5f;
                _ui.Text(button.Text, r.X + r.Width / 2,
                    r.Y + (r.Height - 7 * pixel) / 2, pixel, White);
            }
        }
    }

    private UiAction? Hit(float x, float y)
    {
        foreach (var button in ScreenLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y))
            if (button.Bounds.Contains(x, y)) return button.Action;
        return null;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButton.Left) return;
        Vector2 position = new(MousePosition.X, MousePosition.Y);
        _pressed = Hit(position.X, position.Y);
        if (_session.Screen == GameScreen.World &&
            position.Y >= MapCamera2D.TopBarHeight && _pressed is null)
        {
            _dragging = true;
            _dragDistance = 0;
            _lastPointer = position;
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        Vector2 next = new(e.Position.X, e.Position.Y);
        Vector2 delta = next - _lastPointer;
        _dragDistance += delta.Length();
        _lastPointer = next;
        _camera.Pan(delta.X, delta.Y);
        _dirty = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButton.Left) return;
        Vector2 position = new(MousePosition.X, MousePosition.Y);
        if (_dragging)
        {
            _dragging = false;
            if (_dragDistance < 4)
                _session.SelectAt(_camera.ScreenToWorld(position));
        }
        else if (_pressed is { } action && Hit(position.X, position.Y) == action)
            Activate(action);
        _pressed = null;
        _dirty = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_session.Screen != GameScreen.World || MousePosition.Y < MapCamera2D.TopBarHeight) return;
        _camera.Zoom(e.OffsetY, new Vector2(MousePosition.X, MousePosition.Y));
        _dirty = true;
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Keys.Escape && _session.Screen != GameScreen.MainMenu)
        {
            ReturnToMenu();
            _dirty = true;
        }
    }

    private void Activate(UiAction action)
    {
        switch (action)
        {
            case UiAction.Create: _session.OpenCreateWorld(); break;
            case UiAction.Options: _session.OpenOptions(); break;
            case UiAction.Exit: Close(); break;
            case UiAction.Back: ReturnToMenu(); break;
            case UiAction.Small or UiAction.Medium or UiAction.Large:
                _session.ChooseSize(ScreenLayout.SizeFor(action)); break;
            case UiAction.Generate: GenerateWorld(); break;
        }
        _hover.Clear();
        _dirty = true;
    }

    private void GenerateWorld()
    {
        try
        {
            var grid = _session.GenerateWorld();
            _gridRenderer?.Dispose();
            _gridRenderer = new HexGridRenderer(grid);
            _camera.Fit(grid.Definition.Columns, grid.Definition.Rows,
                ClientSize.X, ClientSize.Y);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            _gridRenderer?.Dispose();
            _gridRenderer = null;
            _session.GenerationFailed("No se pudo generar el mundo. Vuelve a intentarlo.");
        }
    }

    private void ReturnToMenu()
    {
        _gridRenderer?.Dispose();
        _gridRenderer = null;
        _session.ReturnToMenu();
        _hover.Clear();
    }

    protected override void OnUnload()
    {
        _gridRenderer?.Dispose();
        _ui?.Dispose();
        base.OnUnload();
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        using var window = new EvolveWindow(args.Contains("--graphics-smoke"));
        window.Run();
    }
}
