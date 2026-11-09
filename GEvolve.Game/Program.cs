using System.Numerics;
using System.Reflection;
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
    private readonly GameSession _session = new();
    private readonly MapCamera2D _camera = new();
    private readonly TextFieldEditor _editor = new();
    private UiRenderer? _ui;
    private HexGridRenderer? _renderer;
    private HexGrid? _renderedGrid;
    private GridLabAction? _pressed;
    private GridLabAction? _openSelector, _hovered;
    private bool _dragging, _dirty = true;
    private Vector2 _lastPointer;
    private static readonly Vector4 White = Vector4.One, Muted = new(.65f, .76f, .83f, 1), Accent = new(.38f, .76f, .89f, 1);

    public EvolveWindow() : base(GameWindowSettings.Default, new NativeWindowSettings
    { ClientSize = new Vector2i(1280, 720), Title = "Evolve", APIVersion = new Version(3, 3), Profile = ContextProfile.Core }) { }
    private static string GameVersion => "v" + (typeof(EvolveWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.1");

    protected override void OnLoad()
    {
        base.OnLoad(); GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); _ui = new();
    }
    protected override void OnResize(ResizeEventArgs e) { base.OnResize(e); _openSelector = null; _pressed = null; FitCamera(); _dirty = true; }
    private void FitCamera()
    {
        if (_session.Laboratory is not { } lab) return;
        var r = GridLaboratoryLayout.Map(ClientSize.X, ClientSize.Y);
        _camera.FitViewport(lab.Grid.Definition.Columns, lab.Grid.Definition.Rows, (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
    }
    private GridLabAction? Hit(Vector2 point) => GridLaboratoryLayout.Hit(_session.Screen, ClientSize.X, ClientSize.Y, point.X, point.Y, _openSelector);
    private bool InMap(Vector2 point) => _session.Screen == GameScreen.GridLaboratory &&
        GridLaboratoryLayout.Map(ClientSize.X, ClientSize.Y).Contains(point.X, point.Y);

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!_dirty || _ui is null || ClientSize.X <= 0 || ClientSize.Y <= 0) return;
        _dirty = false;
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(.055f, .10f, .16f, 1); GL.Clear(ClearBufferMask.ColorBufferBit);
        _ui.Clear();
        if (_session.Laboratory is { } lab)
        {
            if (!ReferenceEquals(_renderedGrid, lab.Grid))
            {
                _renderer?.Dispose(); _renderer = new(lab.Grid); _renderedGrid = lab.Grid; FitCamera();
            }
            int left = (int)(GridLaboratoryLayout.PanelWidth(ClientSize.X) * FramebufferSize.X / ClientSize.X);
            GL.Enable(EnableCap.ScissorTest); GL.Scissor(left, 0, Math.Max(0, FramebufferSize.X - left), FramebufferSize.Y);
            _renderer!.Draw(_camera, ClientSize.X, ClientSize.Y); GL.Disable(EnableCap.ScissorTest);
            _ui.Rect(0, 0, GridLaboratoryLayout.PanelWidth(ClientSize.X), ClientSize.Y, new(.055f, .10f, .16f, 1));
            _ui.Text("LABORATORIO DE CUADRÍCULA", 12, 20, Math.Min(1.25f, (GridLaboratoryLayout.PanelWidth(ClientSize.X) - 24) / ("LABORATORIO DE CUADRÍCULA".Length * 6f)), Accent, false);
            _ui.Text("SEMILLA", 12, 55, 1.25f, Muted, false);
            _ui.Text("TAMAÑO", 12, 160, 1.25f, Muted, false);
            _ui.Text($"{lab.Grid.Definition.Columns} X {lab.Grid.Definition.Rows}", 12, 336, 1.25f, Muted, false);
            _ui.Text("RUEDA: ZOOM", 12, 354, 1.2f, Muted, false);
            _ui.Text("ARRASTRAR: DESPLAZAR", 12, 370, 1.2f, Muted, false);
            if (lab.Error is not null) _ui.Text(lab.Error, 12, 395, 1.1f, new(1, .6f, .4f, 1), false);
        }
        else DrawMenu();
        foreach (var button in GridLaboratoryLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y)) DrawButton(button);
        foreach (var button in GridLaboratoryLayout.Dropdown(_session.Screen, ClientSize.X, ClientSize.Y, _openSelector)) DrawButton(button);
        _ui.Flush(ClientSize.X, ClientSize.Y); SwapBuffers();
    }
    private void DrawMenu()
    {
        float w = ClientSize.X, h = ClientSize.Y, center = w / 2;
        _ui!.Rect(0, h * .72f, w, h * .28f, new(.075f, .16f, .21f, 1));
        _ui.Rect(0, h * .72f, w, 2, new(.24f, .42f, .48f, 1));
        if (_session.Screen == GameScreen.MainMenu)
        {
            float versionPixel = Math.Clamp(w / 205f, 4, 6.5f);
            _ui.Text("Evolve " + GameVersion, center, h * .17f, versionPixel, White);
            _ui.Text("Civilization Simulator", center, h * .17f + versionPixel * 9, Math.Clamp(w / 480f, 1.5f, 2.5f), Muted);
        }
        else if (_session.Screen == GameScreen.CreateWorld)
        {
            _ui.Text("Crear nuevo mundo", center, MathF.Max(32, h * .10f), Math.Clamp(w / 420f, 2, 3.2f), White);
            _ui.Text("Elige el tamaño del grid", center, MathF.Max(66, h * .16f), 1.8f, Muted);
            var buttons = GridLaboratoryLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y);
            foreach (var action in new[] { GridLabAction.CreateSizeSelect, GridLabAction.CreateTypeSelect, GridLabAction.CreateSeed })
            {
                var button = buttons.Single(b => b.Action == action);
                _ui.Text(button.Text, button.Bounds.X, button.Bounds.Y - 24, 1.8f, Muted, false);
            }
            _ui.Text(GameVersion, 16, h - 27, 1.7f, Muted, false);
            _ui.Text(GameSession.WorldUnavailableMessage, center, h - 32, Math.Min(1.6f, (w - 100) / (GameSession.WorldUnavailableMessage.Length * 6)), new(1, .61f, .59f, 1));
        }
        else if (_session.Screen == GameScreen.Options)
        {
            _ui.Text("Opciones", center, h * .24f, 5, White);
            _ui.Text("Opciones avanzadas pendientes", center, h * .42f, Math.Min(2, (w - 32) / ("Opciones avanzadas pendientes".Length * 6f)), Muted);
        }
    }
    private void DrawButton(GridLabButton button)
    {
        var r = button.Bounds; var lab = _session.Laboratory;
        var form = _session.Creation;
        bool typeOption = button.Action is GridLabAction.Continents or GridLabAction.Pangaea or GridLabAction.Archipelagos;
        bool selected = GridLaboratoryLayout.IsSize(button.Action) && (lab?.Size ?? form?.Size) == GridLaboratoryLayout.Size(button.Action) ||
            typeOption && form?.Type == GridLaboratoryLayout.Type(button.Action) || button.Action == _openSelector;
        if (lab is null) _ui!.Rect(r.X + 3, r.Y + 4, r.Width, r.Height, new(.02f, .05f, .09f, .7f));
        Vector4 color = lab is not null ? selected ? new(.15f, .32f, .38f, 1) : new(.09f, .17f, .23f, 1) :
            button.Action == GridLabAction.GenerateUnavailable ? new(.12f, .35f, .45f, 1) : new(.16f, .24f, .31f, 1);
        if (button.Enabled && _hovered == button.Action) color = new(.27f, .44f, .52f, 1);
        _ui!.Rect(r.X, r.Y, r.Width, r.Height, color);
        Vector4 border = selected ? Accent : lab is null ? new(.37f, .51f, .59f, 1) : Muted;
        _ui.Border(r.X, r.Y, r.Width, r.Height, lab is null ? selected ? 2.5f : 1.5f : 1, border);
        if (lab is null && button.Enabled && (selected || _hovered == button.Action)) _ui.Rect(r.X, r.Y, 4, r.Height, Accent);
        string text = button.Action switch
        {
            GridLabAction.Seed => lab!.SeedText, GridLabAction.CreateSeed => form!.SeedText,
            GridLabAction.CreateSizeSelect => "TAMAÑO: " + GridLaboratoryLayout.SizeLabel(form!.Size),
            GridLabAction.CreateTypeSelect => "MUNDO: " + GridLaboratoryLayout.TypeLabel(form!.Type), _ => button.Text
        };
        bool selector = button.Action is GridLabAction.CreateSizeSelect or GridLabAction.CreateTypeSelect;
        float desired = lab is not null ? 1.4f : _session.Screen == GameScreen.MainMenu || button.Action == GridLabAction.Back ? 2.5f : 2.1f;
        float pixel = Math.Min(desired, (r.Width - (selector ? 48 : 16)) / (Math.Max(1, text.Length) * 6));
        float x = r.X + 6, y = r.Y + (r.Height - pixel * 7) / 2;
        bool seedField = button.Action is GridLabAction.Seed or GridLabAction.CreateSeed;
        if (seedField && _editor.HasFocus)
        {
            if (_editor.SelectionLength > 0) _ui.Rect(x + _editor.SelectionStart * 6 * pixel, y - 1, _editor.SelectionLength * 6 * pixel, 7 * pixel + 2, new(.20f, .46f, .65f, .65f));
            _ui.Rect(x + _editor.Cursor * 6 * pixel, y - 1, 1.5f, 7 * pixel + 2, Accent);
        }
        bool centered = lab is null && !selector && !seedField;
        _ui.Text(text, centered ? r.X + r.Width / 2 : x, y, pixel, button.Enabled ? White : Muted, centered);
        if (selector) _ui.Text(_openSelector == button.Action ? "^" : "V", r.X + r.Width - 18, r.Y + (r.Height - 14) / 2, 2, Accent);
    }
    private void Activate(GridLabAction action)
    {
        if (action is not (GridLabAction.Seed or GridLabAction.CreateSeed)) _editor.Blur();
        if (GridLaboratoryLayout.IsSize(action))
        {
            if (_session.Screen == GameScreen.CreateWorld) _session.Creation?.ChooseSize(GridLaboratoryLayout.Size(action));
            else _session.Laboratory?.ChooseSize(GridLaboratoryLayout.Size(action));
            _openSelector = null;
        }
        else switch (action)
        {
            case GridLabAction.OpenLaboratory: _session.OpenLaboratory(); break;
            case GridLabAction.OpenCreateWorld: _session.OpenCreateWorld(); break;
            case GridLabAction.OpenOptions: _session.OpenOptions(); break;
            case GridLabAction.Exit: _session.RequestExit(); if (_session.ExitRequested) Close(); break;
            case GridLabAction.Back: ReturnToMenu(); break;
            case GridLabAction.RandomSeed: _session.Laboratory?.RandomizeSeed(); break;
            case GridLabAction.CreateRandomSeed: _session.Creation?.RandomizeSeed(); break;
            case GridLabAction.CreateSizeSelect or GridLabAction.CreateTypeSelect: _openSelector = _openSelector == action ? null : action; break;
            case GridLabAction.Continents or GridLabAction.Pangaea or GridLabAction.Archipelagos:
                _session.Creation?.ChooseType(GridLaboratoryLayout.Type(action)); _openSelector = null; break;
            case GridLabAction.CreateSeed:
                if (_session.Creation is { } form) _editor.Focus(form, () => form.SeedText, form.SetSeedText);
                break;
            case GridLabAction.Seed:
                if (_session.Laboratory is { } lab) _editor.Focus(lab, () => lab.SeedText, lab.SetSeedText);
                break;
        }
        _dirty = true;
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e); if (e.Button != MouseButton.Left) return;
        var point = new Vector2(MousePosition.X, MousePosition.Y); _pressed = Hit(point);
        if (_openSelector is { } selector && _pressed != selector && !GridLaboratoryLayout.Dropdown(_session.Screen, ClientSize.X, ClientSize.Y, selector).Any(b => b.Action == _pressed))
        { _openSelector = null; _pressed = null; _dirty = true; return; }
        if (_pressed is not (GridLabAction.Seed or GridLabAction.CreateSeed)) _editor.Blur();
        _dragging = _pressed is null && InMap(point); _lastPointer = point; _dirty = true;
    }
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e); var point = new Vector2(MousePosition.X, MousePosition.Y);
        var hovered = Hit(point); if (hovered != _hovered) { _hovered = hovered; _dirty = true; }
        if (_dragging) { var delta = point - _lastPointer; _camera.Pan(delta.X, delta.Y); _dirty = true; }
        _lastPointer = point;
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e); if (e.Button != MouseButton.Left) return;
        var point = new Vector2(MousePosition.X, MousePosition.Y);
        if (!_dragging && _pressed is { } action && Hit(point) == action) Activate(action);
        _pressed = null; _dragging = false;
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e); var point = new Vector2(MousePosition.X, MousePosition.Y);
        if (InMap(point)) { _camera.Zoom(e.OffsetY, point); _dirty = true; }
    }
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_editor.KeyDown(e.Key, e.Control, e.Shift, e.Alt, e.Command, () => ClipboardString, value => ClipboardString = value)) _dirty = true;
        else if (e.Key == Keys.Escape)
        {
            if (_openSelector is not null) _openSelector = null;
            else ReturnToMenu();
            _dirty = true;
        }
    }
    protected override void OnKeyUp(KeyboardKeyEventArgs e) { base.OnKeyUp(e); _editor.UpdateModifiers(e.Control, e.Alt, e.Command); }
    protected override void OnTextInput(TextInputEventArgs e) { base.OnTextInput(e); if (_editor.TextInput(e.AsString)) _dirty = true; }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    { base.OnFocusedChanged(e); if (!e.IsFocused) { _editor.Blur(); _dragging = false; _pressed = null; _dirty = true; } }
    private void ReturnToMenu()
    {
        _editor.Blur(); _session.ReturnToMenu(); _renderer?.Dispose(); _renderer = null; _renderedGrid = null;
        _dragging = false; _pressed = null; _openSelector = null; _hovered = null;
    }
    protected override void OnUnload() { _renderer?.Dispose(); _ui?.Dispose(); base.OnUnload(); }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 0) throw new ArgumentException("R02 no admite modos geográficos ni pruebas visuales automatizadas.");
        using var window = new EvolveWindow(); window.Run();
    }
}
