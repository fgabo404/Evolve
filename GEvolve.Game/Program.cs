using System.Numerics;
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
    private bool _dragging, _dirty = true;
    private Vector2 _lastPointer;
    private static readonly Vector4 White = Vector4.One, Muted = new(.65f, .76f, .83f, 1), Accent = new(.38f, .76f, .89f, 1);

    public EvolveWindow() : base(GameWindowSettings.Default, new NativeWindowSettings
    { ClientSize = new Vector2i(1280, 720), Title = "Evolve — Laboratorio de cuadrícula", APIVersion = new Version(3, 3), Profile = ContextProfile.Core }) { }

    protected override void OnLoad()
    {
        base.OnLoad(); GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); _ui = new();
    }
    protected override void OnResize(ResizeEventArgs e) { base.OnResize(e); FitCamera(); _dirty = true; }
    private void FitCamera()
    {
        if (_session.Laboratory is not { } lab) return;
        var r = GridLaboratoryLayout.Map(ClientSize.X, ClientSize.Y);
        _camera.FitViewport(lab.Grid.Definition.Columns, lab.Grid.Definition.Rows, (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
    }
    private GridLabAction? Hit(Vector2 point) => GridLaboratoryLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y)
        .Where(b => b.Bounds.Contains(point.X, point.Y)).Select(b => (GridLabAction?)b.Action).FirstOrDefault();
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
        else
        {
            _ui.Text("EVOLVE", 24, 50, 4, Accent, false);
            string message = _session.Screen == GameScreen.WorldUnavailable ? GameSession.WorldUnavailableMessage : "R02 - CUADRÍCULA SIN DATOS GEOGRÁFICOS";
            _ui.Text(message, 24, 110, Math.Min(1.5f, Math.Max(.5f, (ClientSize.X - 48) / (message.Length * 6f))), Muted, false);
        }
        foreach (var button in GridLaboratoryLayout.Buttons(_session.Screen, ClientSize.X, ClientSize.Y)) DrawButton(button);
        _ui.Flush(ClientSize.X, ClientSize.Y); SwapBuffers();
    }
    private void DrawButton(GridLabButton button)
    {
        var r = button.Bounds; var lab = _session.Laboratory;
        bool selected = GridLaboratoryLayout.IsSize(button.Action) && lab?.Size == GridLaboratoryLayout.Size(button.Action);
        _ui!.Rect(r.X, r.Y, r.Width, r.Height, selected ? new(.15f, .32f, .38f, 1) : new(.09f, .17f, .23f, 1));
        _ui.Border(r.X, r.Y, r.Width, r.Height, 1, selected ? Accent : Muted);
        string text = button.Action == GridLabAction.Seed ? lab!.SeedText : button.Text;
        float pixel = Math.Min(1.4f, (r.Width - 12) / (Math.Max(1, text.Length) * 6));
        float x = r.X + 6, y = r.Y + (r.Height - pixel * 7) / 2;
        if (button.Action == GridLabAction.Seed && _editor.HasFocus)
        {
            if (_editor.SelectionLength > 0) _ui.Rect(x + _editor.SelectionStart * 6 * pixel, y - 1, _editor.SelectionLength * 6 * pixel, 7 * pixel + 2, new(.20f, .46f, .65f, .65f));
            _ui.Rect(x + _editor.Cursor * 6 * pixel, y - 1, 1.5f, 7 * pixel + 2, Accent);
        }
        _ui.Text(text, x, y, pixel, button.Action == GridLabAction.CreateUnavailable ? Muted : White, false);
    }
    private void Activate(GridLabAction action)
    {
        if (action != GridLabAction.Seed) _editor.Blur();
        if (GridLaboratoryLayout.IsSize(action)) _session.Laboratory?.ChooseSize(GridLaboratoryLayout.Size(action));
        else switch (action)
        {
            case GridLabAction.OpenLaboratory: _session.OpenLaboratory(); break;
            case GridLabAction.CreateUnavailable: _session.OpenCreateWorld(); break;
            case GridLabAction.Exit: Close(); break;
            case GridLabAction.Back: ReturnToMenu(); break;
            case GridLabAction.RandomSeed: _session.Laboratory?.RandomizeSeed(); break;
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
        if (_pressed != GridLabAction.Seed) _editor.Blur();
        _dragging = _pressed is null && InMap(point); _lastPointer = point; _dirty = true;
    }
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e); var point = new Vector2(MousePosition.X, MousePosition.Y);
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
        else if (e.Key == Keys.Escape) { ReturnToMenu(); _dirty = true; }
    }
    protected override void OnKeyUp(KeyboardKeyEventArgs e) { base.OnKeyUp(e); _editor.UpdateModifiers(e.Control, e.Alt, e.Command); }
    protected override void OnTextInput(TextInputEventArgs e) { base.OnTextInput(e); if (_editor.TextInput(e.AsString)) _dirty = true; }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    { base.OnFocusedChanged(e); if (!e.IsFocused) { _editor.Blur(); _dragging = false; _pressed = null; _dirty = true; } }
    private void ReturnToMenu()
    {
        _editor.Blur(); _session.ReturnToMenu(); _renderer?.Dispose(); _renderer = null; _renderedGrid = null;
        _dragging = false; _pressed = null;
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
