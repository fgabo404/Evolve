using System.Numerics;
using GEvolve.Engine.World;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game;

internal sealed partial class EvolveWindow
{
    private PangaeaLaboratory? _laboratory;
    private PangaeaMapData? _laboratoryData;
    private long _laboratoryConfiguration, _laboratoryGrid;

    private IEnumerable<UiButton> LaboratoryControls()
    {
        foreach (var button in PangaeaLaboratoryLayout.Buttons(ClientSize.X, ClientSize.Y, _laboratory?.ConfirmRestart == true))
            yield return button;
        if (_openDropdown is UiAction.SizeSelect or UiAction.LabView)
            foreach (var button in PangaeaLaboratoryLayout.Dropdown(ClientSize.X, ClientSize.Y, _openDropdown.Value, _laboratory?.Map))
                yield return button;
    }
    private bool LaboratoryMapContains(Vector2 point) => _laboratory?.Map is not null && _openDropdown is null &&
        point.X > PangaeaLaboratoryLayout.PanelWidth(ClientSize.X) && point.Y > 200 &&
        !PangaeaLaboratoryLayout.Inspector(ClientSize.X, ClientSize.Y).Contains(point.X, point.Y);

    private void RefreshLaboratoryRenderer()
    {
        var data = _laboratory?.Map;
        if (ReferenceEquals(data, _laboratoryData)) return;
        _laboratoryData = data;
        if (data is null) { _gridRenderer?.Dispose(); _gridRenderer = null; _dirty = true; return; }
        var snapshot = data.Snapshot;
        long gridRevision = snapshot.Stages.Single(s => s.Stage == PangaeaGenerationStage.Grid).Revision;
        if (_gridRenderer is null || _laboratoryConfiguration != snapshot.ConfigurationRevision || _laboratoryGrid != gridRevision)
        {
            _gridRenderer?.Dispose(); _gridRenderer = new HexGridRenderer(data);
            _laboratoryConfiguration = snapshot.ConfigurationRevision; _laboratoryGrid = gridRevision;
            var grid = snapshot.Grid!;
            _camera.Fit(grid.Definition.Columns, grid.Definition.Rows, ClientSize.X, ClientSize.Y);
        }
        else _gridRenderer.UpdateLaboratory(data);
        _dirty = true;
    }
    private bool LaboratoryEnabled(UiAction action)
    {
        var lab = _laboratory!;
        if (PangaeaLaboratoryLayout.IsStageAction(action)) return lab.CanExecute(PangaeaLaboratoryLayout.Stage(action));
        if (action == UiAction.LabCancel) return lab.Busy;
        if (action == UiAction.LabView) return lab.Map is not null;
        return true;
    }
    private void ActivateLaboratory(UiAction action)
    {
        var lab = _laboratory!;
        if (!LaboratoryEnabled(action)) return;
        if (PangaeaLaboratoryLayout.IsStageAction(action)) lab.Execute(PangaeaLaboratoryLayout.Stage(action));
        else if (PangaeaLaboratoryLayout.IsViewAction(action))
        { lab.ChooseView(PangaeaLaboratoryLayout.View(action)); _openDropdown = null; }
        else switch (action)
        {
            case UiAction.Back: ReturnToMenu(); break;
            case UiAction.SizeSelect or UiAction.LabView: _openDropdown = _openDropdown == action ? null : action; break;
            case UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous:
                lab.ChooseSize(ScreenLayout.SizeFor(action)); _openDropdown = null; break;
            case UiAction.SeedField: _seedFocused = true; break;
            case UiAction.LabRestart: lab.RequestRestart(); break;
            case UiAction.LabConfirm: lab.ConfirmReset(); break;
            case UiAction.LabKeep: lab.CancelReset(); break;
            case UiAction.LabCancel: lab.Cancel(); break;
        }
        if (action != UiAction.SeedField) _seedFocused = false;
        _dirty = true;
    }

    private void RenderLaboratory()
    {
        if (_ui is null || _laboratory is not { } lab) return;
        float panel = PangaeaLaboratoryLayout.PanelWidth(ClientSize.X);
        if (lab.Map is not null && _gridRenderer is not null)
        {
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor((int)(panel * FramebufferSize.X / ClientSize.X), 0,
                (int)((ClientSize.X - panel) * FramebufferSize.X / ClientSize.X),
                Math.Max(0, (int)((ClientSize.Y - MapCamera2D.TopBarHeight) * FramebufferSize.Y / ClientSize.Y)));
            _gridRenderer.Draw(_camera, ClientSize.X, ClientSize.Y, lab.View);
            GL.Disable(EnableCap.ScissorTest);
            if (lab.View == MapViewMode.Tectonic)
            { _gridRenderer.DrawPlateBoundaries(_ui, _camera); _gridRenderer.DrawPlateMotions(_ui, _camera); }
            if (lab.View == MapViewMode.Geology) _gridRenderer.DrawPangaeaMountainRanges(_ui, _camera);
            if (lab.SelectedCell is { } cell)
            {
                int columns = lab.Map.Snapshot.Grid!.Definition.Columns;
                var visible = _camera.VisibleRange();
                for (int copy = (int)Math.Ceiling((visible.FirstColumn - cell.Address.Column) / (double)columns);
                    cell.Address.Column + copy * columns <= visible.LastColumn; copy++)
                {
                    var position = _camera.WorldToScreen(GEvolve.Engine.Grid.FlatHexGeometry.Center(new(cell.Address.Column + copy * columns, cell.Address.Row)));
                    _ui.Hex(position.X, position.Y, _camera.Scale, new(1, 1, 1, .08f), new(1, .9f, .3f, 1));
                }
            }
        }
        _ui.Rect(0, 0, ClientSize.X, 58, new(.055f, .10f, .16f, 1));
        _ui.Rect(0, 58, panel, ClientSize.Y - 58, new(.075f, .13f, .19f, 1));
        _ui.Text("LABORATORIO DE PANGEA", 12, 21, ClientSize.X < 800 ? 1.7f : 2.2f, White, false);
        _ui.Text("TIPO: PANGEA", 12, 68, 1.5f, Muted, false);
        foreach (var button in PangaeaLaboratoryLayout.Buttons(ClientSize.X, ClientSize.Y, lab.ConfirmRestart))
        {
            var rect = button.Bounds; bool enabled = LaboratoryEnabled(button.Action);
            _ui.Rect(rect.X, rect.Y, rect.Width, rect.Height, enabled ? new(.12f, .24f, .31f, 1) : new(.12f, .15f, .18f, 1));
            _ui.Border(rect.X, rect.Y, rect.Width, rect.Height, 1, enabled ? Accent : Muted * .4f);
            if (PangaeaLaboratoryLayout.IsStageAction(button.Action))
            {
                var stage = lab.Stages.FirstOrDefault(s => s.Stage == PangaeaLaboratoryLayout.Stage(button.Action));
                _ui.Text(button.Text, rect.X + 5, rect.Y + 4, Math.Min(1.25f, (rect.Width - 10) / (button.Text.Length * 6)), enabled ? White : Muted, false);
                string state = stage is null ? "PENDIENTE" : lab.StageState(stage);
                string duration = stage?.Duration is { } time ? $"  {time.TotalMilliseconds:F0} MS" : "";
                _ui.Text(state + duration, rect.X + 5, rect.Y + rect.Height - 12, 1.1f, Accent, false);
            }
            else
            {
                string text = button.Action switch
                {
                    UiAction.SizeSelect => "TAMAÑO: " + ScreenLayout.SizeLabel(lab.Size),
                    UiAction.SeedField => "SEMILLA: " + lab.SeedText + (_seedFocused ? "-" : ""),
                    UiAction.LabView => "VER: " + (lab.Map is null ? "PENDIENTE" : PangaeaMapData.ViewLabel(lab.View)),
                    _ => button.Text
                };
                _ui.Text(text, rect.X + 5, rect.Y + 9, Math.Min(1.4f, (rect.Width - 10) / (text.Length * 6)), enabled ? White : Muted, false);
            }
        }
        var inspector = PangaeaLaboratoryLayout.Inspector(ClientSize.X, ClientSize.Y);
        _ui.Rect(inspector.X, inspector.Y, inspector.Width, inspector.Height, new(.055f, .10f, .16f, .96f));
        _ui.Text("INSPECCIÓN DE CELDA", inspector.X + 8, inspector.Y + 8, 1.3f, Accent, false);
        var lines = lab.Inspect();
        if (lines.Count == 0) LabText("SELECCIONA UN HEXÁGONO. RUEDA: ZOOM. ARRASTRAR: MOVER.", inspector.X + 8, inspector.Y + 28, inspector.Width - 16, 1.2f);
        else for (int i = 0; i < lines.Count; i++)
            _ui.Text(lines[i], inspector.X + 8, inspector.Y + 26 + i * 12, Math.Min(1.2f, (inspector.Width - 16) / (lines[i].Length * 6)), White, false);
        float statusX = panel + 10;
        string status = lab.Error ?? lab.Notice ?? "SESIÓN LISTA";
        LabText(status, statusX, lab.ConfirmRestart ? 190 : 140, ClientSize.X - statusX - 12, 1.2f);
        var hovered = Hit(MousePosition.X, MousePosition.Y);
        if (hovered.HasValue && PangaeaLaboratoryLayout.IsStageAction(hovered.Value) && !LaboratoryEnabled(hovered.Value))
            LabText(lab.UnavailableReason(PangaeaLaboratoryLayout.Stage(hovered.Value)), statusX, 235, ClientSize.X - statusX - 12, 1.2f);
        // Dropdown entries are last in the UI batch and first in hit testing.
        if (_openDropdown is UiAction.SizeSelect or UiAction.LabView)
        foreach (var button in PangaeaLaboratoryLayout.Dropdown(ClientSize.X, ClientSize.Y, _openDropdown.Value, lab.Map))
        {
            var r = button.Bounds;
            _ui.Rect(r.X, r.Y, r.Width, r.Height, new(.10f, .19f, .26f, 1));
            _ui.Border(r.X, r.Y, r.Width, r.Height, 1, Accent);
            _ui.Text(button.Text, r.X + 5, r.Y + 8, Math.Min(1.3f, (r.Width - 10) / (button.Text.Length * 6)), White, false);
        }
    }
    private void LabText(string text, float x, float y, float width, float pixel)
    {
        int length = Math.Max(1, (int)(width / (6 * pixel)));
        for (int start = 0, row = 0; start < text.Length; start += length, row++)
            _ui!.Text(text.Substring(start, Math.Min(length, text.Length - start)), x, y + row * 12, pixel, White, false);
    }
}
