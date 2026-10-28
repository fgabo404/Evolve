using System.Numerics;
using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Definitions.Versioning;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
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
    private Task<GeneratedWorld>? _pendingGeneration;
    private bool _seedFocused;
    private int _smokeScenario = -1;
    private bool _smokeZoomed;
    private long _generationStarted;
    private double _rendererPreparationMilliseconds;
    private static readonly (GridSize Size, LandmassType Type, int Seed)[] SmokeCases =
    [
        (GridSize.Small, LandmassType.Continents, 2026),
        (GridSize.Medium, LandmassType.Continents, 2026),
        (GridSize.Large, LandmassType.Continents, 2026),
        (GridSize.Large, LandmassType.Pangaea, 2026),
        (GridSize.Large, LandmassType.SmallContinents, 2026),
        (GridSize.Large, LandmassType.Archipelagos, 2026)
    ];

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
        if (_pendingGeneration is { IsCompleted: true } completed)
        {
            _pendingGeneration = null;
            if (_session.Screen == GameScreen.Generating)
            {
                try
                {
                    GeneratedWorld result = completed.GetAwaiter().GetResult();
                    _session.CompleteGeneration(result);
                    var preparation = Stopwatch.StartNew();
                    _gridRenderer?.Dispose();
                    _gridRenderer = new HexGridRenderer(result.Grid, result.Landmass);
                    preparation.Stop();
                    _rendererPreparationMilliseconds = preparation.Elapsed.TotalMilliseconds;
                    _camera.Fit(result.Grid.Definition.Columns, result.Grid.Definition.Rows,
                        ClientSize.X, ClientSize.Y);
                    if (_graphicsSmoke)
                        Console.WriteLine($"Generate {result.Grid.Definition.Size} " +
                            $"{result.Landmass.Configuration.Type}: " +
                            $"{Stopwatch.GetElapsedTime(_generationStarted).TotalMilliseconds:F1} ms total, " +
                            $"{_rendererPreparationMilliseconds:F1} ms GPU preparation, " +
                            $"{result.Statistics.LandFraction:P1} land, " +
                            $"{result.Statistics.ComponentCount} components");
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    _gridRenderer?.Dispose();
                    _gridRenderer = null;
                    _session.GenerationFailed("NO SE PUDO GENERAR EL MUNDO");
                }
            }
            else if (completed.IsFaulted) _ = completed.Exception;
            _dirty = true;
        }
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
            Console.WriteLine($"OpenGL {_session.Grid!.Definition.Size} " +
                $"{_session.State.Landmass!.Configuration.Type}: " +
                $"{_gridRenderer!.VisibleInstanceCount} visible instances, " +
                $"{Stopwatch.GetElapsedTime(started).TotalMilliseconds:F1} ms frame");
        if (_session.Screen == GameScreen.MainMenu && _smokeScenario == -1)
        {
            _smokeScenario = 0;
            Activate(UiAction.Create);
            return;
        }
        if (_session.Screen == GameScreen.CreateWorld)
        {
            if (_session.Error is { } generationError)
                throw new InvalidOperationException($"OpenGL smoke generation failed: {generationError}");
            var item = SmokeCases[_smokeScenario];
            _session.ChooseSize(item.Size);
            _session.ChooseType(item.Type);
            _session.SetSeedText(item.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            StartGeneration();
            return;
        }
        if (_session.Screen != GameScreen.World) return;
        if (_smokeScenario < SmokeCases.Length - 1)
        {
            ReturnToMenu();
            _smokeScenario++;
            Activate(UiAction.Create);
        }
        else if (!_smokeZoomed)
        {
            _camera.Zoom(8, new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f));
            _camera.Pan(120, 20);
            _session.SelectAt(_camera.ScreenToWorld(
                new Vector2(ClientSize.X / 2f, ClientSize.Y / 2f)));
            if (_session.SelectedCell is null)
                throw new InvalidOperationException("OpenGL smoke selection failed.");
            _smokeZoomed = true;
            _dirty = true;
        }
        else Close();
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
            float versionPixel = Math.Clamp(w / 205f, 4, 6.5f);
            _ui.Text($"{EvolveVersion.GameName} {EvolveVersion.Display}",
                center, h * .17f, versionPixel, White);
            _ui.Text("Civilization Simulator", center, h * .17f + versionPixel * 9,
                Math.Clamp(w / 480f, 1.5f, 2.5f), Muted);
        }
        else if (_session.Screen == GameScreen.CreateWorld)
        {
            _ui.Text("Crear nuevo mundo", center, MathF.Max(32, h * .10f),
                Math.Clamp(w / 420f, 2, 3.2f), White);
            _ui.Text("Elige el tamaño del grid", center, MathF.Max(66, h * .16f),
                1.8f, Muted);
            var buttons = ScreenLayout.Buttons(GameScreen.CreateWorld, ClientSize.X, ClientSize.Y);
            var size = buttons.First(button => button.Action == UiAction.Small).Bounds;
            var type = buttons.First(button => button.Action == UiAction.Continents).Bounds;
            var seed = buttons.First(button => button.Action == UiAction.SeedField).Bounds;
            _ui.Text("Tamaño", size.X, size.Y - 24, 1.8f, Muted, centered: false);
            _ui.Text("Tipo de mundo", type.X, type.Y - 24, 1.8f, Muted, centered: false);
            _ui.Text("Semilla", seed.X, seed.Y - 24, 1.8f, Muted, centered: false);
            _ui.Text(EvolveVersion.Display, 16, h - 27, 1.7f, Muted, centered: false);
        }
        else if (_session.Screen == GameScreen.Generating)
        {
            _ui.Text("Generando mundo", center, h * .39f, 4, White);
            _ui.Text("Distribuyendo tierra y océanos", center, h * .52f, 2, Muted);
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
        _ui.Text($"EVOLVE {EvolveVersion.Display}", 18, 19, 2.4f, White,
            centered: false);
        string size = grid.Definition.Size switch
        {
            GridSize.Small => "PEQUEÑO", GridSize.Medium => "MEDIANO", _ => "GRANDE"
        };
        if (ClientSize.X >= 600)
            _ui.Text($"{size}  {grid.Definition.Columns} X {grid.Definition.Rows}",
                ClientSize.X / 2f, 22, 1.8f, Muted);
        DrawButtons();
        if (_session.State.Landmass is { } worldInfo)
        {
            string kind = worldInfo.Configuration.Type switch
            {
                LandmassType.Pangaea => "PANGEA",
                LandmassType.Continents => "CONTINENTES",
                LandmassType.SmallContinents => "CONTINENTES PEQUEÑOS",
                _ => "ARCHIPIÉLAGOS"
            };
            float infoWidth = MathF.Min(270, ClientSize.X - 24);
            _ui.Rect(12, MapCamera2D.TopBarHeight + 12, infoWidth, 63,
                new Vector4(.06f, .13f, .20f, .91f));
            _ui.Text($"MUNDO: {kind}", 22, MapCamera2D.TopBarHeight + 22,
                1.6f, White, centered: false);
            _ui.Text($"SEMILLA: {worldInfo.Configuration.Seed}",
                22, MapCamera2D.TopBarHeight + 48, 1.8f, Accent, centered: false);
        }
        if (_session.SelectedCell is { } cell && _session.State.Landmass is { } landmass)
        {
            string surface = landmass.IsLand(cell.Id) ? "TIERRA" : "OCÉANO";
            string label = $"CELDA {cell.Id}  C:{cell.Address.Column}  F:{cell.Address.Row}  {surface}";
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
            bool sizeAction = button.Action is UiAction.Small or UiAction.Medium or UiAction.Large;
            bool typeAction = button.Action is UiAction.Continents or UiAction.Pangaea or
                UiAction.SmallContinents or UiAction.Archipelagos;
            bool selected = sizeAction && ScreenLayout.SizeFor(button.Action) == _session.SelectedSize ||
                typeAction && ScreenLayout.TypeFor(button.Action) == _session.SelectedType ||
                button.Action == UiAction.SeedField && _seedFocused;
            _ui.Rect(r.X + 3, r.Y + 4, r.Width, r.Height,
                new Vector4(.02f, .05f, .09f, .7f));
            Vector4 baseColor = button.Action == UiAction.Generate
                ? new(.12f, .35f, .45f, 1) : new(.16f, .24f, .31f, 1);
            Vector4 color = Vector4.Lerp(baseColor, new Vector4(.27f, .44f, .52f, 1), hover);
            _ui.Rect(r.X, r.Y, r.Width, r.Height, color);
            _ui.Border(r.X, r.Y, r.Width, r.Height, selected ? 2.5f : 1.5f,
                selected ? Accent : new Vector4(.37f, .51f, .59f, 1));
            if (selected || hover > .05f)
                _ui.Rect(r.X, r.Y, 4, r.Height, Accent);
            if (sizeAction)
            {
                var definition = HexGridDefinitions.Get(ScreenLayout.SizeFor(button.Action));
                _ui.Text(button.Text, r.X + r.Width / 2, r.Y + 4,
                    MathF.Min(2.2f, r.Height / 18), White);
                _ui.Text($"{definition.Columns} X {definition.Rows}  {(definition.Columns * definition.Rows).ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} HEXÁGONOS",
                    r.X + r.Width / 2, r.Y + r.Height - 12, 1.2f, Muted);
            }
            else if (button.Action == UiAction.SeedField)
            {
                const float pixel = 2.2f;
                _ui.Text(_session.SeedText, r.X + 13,
                    r.Y + (r.Height - 7 * pixel) / 2, pixel, White, centered: false);
                if (_seedFocused)
                    _ui.Rect(r.X + 13 + _session.SeedText.Length * 6 * pixel,
                        r.Y + 9, 2, r.Height - 18, Accent);
            }
            else
            {
                float pixel = _session.Screen == GameScreen.World ? 1.9f :
                    typeAction || button.Action == UiAction.RandomSeed ?
                        MathF.Min(2.1f, (r.Width - 16) / (button.Text.Length * 6f)) : 2.5f;
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
        else if (_session.Screen == GameScreen.CreateWorld)
            _seedFocused = false;
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
        if (_session.Screen == GameScreen.CreateWorld && _seedFocused &&
            e.Key == Keys.Backspace)
        {
            if (_session.SeedText.Length > 0)
                _session.SetSeedText(_session.SeedText[..^1]);
            _dirty = true;
            return;
        }
        if (e.Key == Keys.Escape && _session.Screen != GameScreen.MainMenu)
        {
            _seedFocused = false;
            ReturnToMenu();
            _dirty = true;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (_session.Screen != GameScreen.CreateWorld || !_seedFocused) return;
        _session.SetSeedText(_session.SeedText + e.AsString);
        _dirty = true;
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
                if (_session.Screen == GameScreen.World)
                {
                    GridSize newSize = ScreenLayout.SizeFor(action);
                    if (_session.SelectedSize != newSize)
                        StartGeneration(resize: newSize);
                }
                else _session.ChooseSize(ScreenLayout.SizeFor(action));
                break;
            case UiAction.Continents or UiAction.Pangaea or UiAction.SmallContinents or
                UiAction.Archipelagos:
                _session.ChooseType(ScreenLayout.TypeFor(action)); break;
            case UiAction.SeedField: _seedFocused = true; break;
            case UiAction.RandomSeed: _session.RandomizeSeed(); break;
            case UiAction.Generate: StartGeneration(); break;
            case UiAction.Regenerate: StartGeneration(regenerate: true); break;
        }
        if (action != UiAction.SeedField) _seedFocused = false;
        _hover.Clear();
        _dirty = true;
    }

    private void StartGeneration(bool regenerate = false, GridSize? resize = null)
    {
        if (_pendingGeneration is not null) return;
        try
        {
            LandmassConfiguration configuration = resize is { } size
                ? _session.BeginResize(size)
                : regenerate ? _session.BeginRegeneration() : _session.BeginGeneration();
            _generationStarted = Stopwatch.GetTimestamp();
            _pendingGeneration = Task.Run(() =>
                new LandmassGenerationService().Generate(configuration));
        }
        catch (FormatException)
        {
            // GameSession ya mantiene un mensaje de validación legible.
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            _session.GenerationFailed("NO SE PUDO GENERAR EL MUNDO");
        }
    }

    private void ReturnToMenu()
    {
        _gridRenderer?.Dispose();
        _gridRenderer = null;
        _session.ReturnToMenu();
        _seedFocused = false;
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
