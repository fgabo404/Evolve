using System.Numerics;
using GEvolve.Engine.World;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using GEvolve.Engine.World.Experimental;
using OpenTK.Graphics.OpenGL4;

namespace GEvolve.Game;

internal sealed partial class EvolveWindow
{
    private PangaeaLaboratory? _laboratory;
    private PangaeaMapData? _laboratoryData;
    private long _laboratorySession, _laboratoryGrid;
    private float _laboratoryScroll;
    private int _inspectorScroll, _inspectorLines, _inspectorVisibleLines;
    private int? _inspectorCell;

    private IEnumerable<UiButton> LaboratoryControls()
    {
        foreach (var button in PangaeaLaboratoryLayout.HitButtons(ClientSize.X, ClientSize.Y, _laboratoryScroll, _laboratory?.CanCompareBase == true)) yield return button;
        if (_openDropdown is UiAction.TypeSelect or UiAction.SizeSelect or UiAction.LabView or UiAction.LabBaseVariant)
            foreach (var button in PangaeaLaboratoryLayout.Dropdown(ClientSize.X, ClientSize.Y, _openDropdown.Value, _laboratory?.Map, _laboratoryScroll))
                yield return button;
    }
    private bool LaboratoryMapContains(Vector2 point) => _laboratory?.Map is not null && _openDropdown is null &&
        PangaeaLaboratoryLayout.Map(ClientSize.X, ClientSize.Y).Contains(point.X, point.Y);
    private void FitLaboratoryCamera()
    {
        if (_laboratory?.Map?.Snapshot.Grid is not { } grid) return;
        var viewport = PangaeaLaboratoryLayout.Map(ClientSize.X, ClientSize.Y);
        _camera.FitViewport(grid.Definition.Columns, grid.Definition.Rows, (int)viewport.X, (int)viewport.Y, (int)viewport.Width, (int)viewport.Height);
    }
    private void RefreshLaboratoryRenderer()
    {
        var data = _laboratory?.Map;
        if (ReferenceEquals(data, _laboratoryData)) return;
        _laboratoryData = data;
        if (data is null) { _gridRenderer?.Dispose(); _gridRenderer = null; _dirty = true; return; }
        long gridRevision = data.Snapshot.Stages.Single(s => s.Stage == PangaeaGenerationStage.Grid).Revision;
        if (_gridRenderer is null || _laboratorySession != _laboratory!.SessionRevision || _laboratoryGrid != gridRevision)
        {
            _gridRenderer?.Dispose(); _gridRenderer = new HexGridRenderer(data);
            _laboratorySession = _laboratory!.SessionRevision; _laboratoryGrid = gridRevision;
            _inspectorScroll = 0; _inspectorCell = null;
            FitLaboratoryCamera();
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
        if (action == UiAction.LabBaseVariant || PangaeaLaboratoryLayout.IsVariantAction(action)) return lab.CanCompareBase;
        return true;
    }
    private void ActivateLaboratory(UiAction action)
    {
        var lab = _laboratory!;
        if (!LaboratoryEnabled(action)) return;
        if (action == UiAction.LabPlates) lab.ExecutePlatesAndMotion();
        else if (PangaeaLaboratoryLayout.IsStageAction(action)) lab.Execute(PangaeaLaboratoryLayout.Stage(action));
        else if (PangaeaLaboratoryLayout.IsViewAction(action))
        { lab.ChooseView(PangaeaLaboratoryLayout.View(action)); _openDropdown = null; }
        else if (PangaeaLaboratoryLayout.IsVariantAction(action))
        { lab.ChooseBaseVariant(PangaeaLaboratoryLayout.Variant(action)); _openDropdown = null; }
        else switch (action)
        {
            case UiAction.Back: ReturnToMenu(); break;
            case UiAction.TypeSelect or UiAction.SizeSelect or UiAction.LabView or UiAction.LabBaseVariant:
                _openDropdown = _openDropdown == action ? null : action; break;
            case UiAction.Pangaea: _openDropdown = null; break; // Explicitly the only supported laboratory type.
            case UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous:
                lab.ChooseSize(ScreenLayout.SizeFor(action)); ResetLaboratoryNavigation(); break;
            case UiAction.SeedField: FocusSeedInput(); break;
            case UiAction.RandomSeed: lab.RandomizeSeed(); ResetLaboratoryNavigation(); break;
            case UiAction.LabRestart: lab.RequestRestart(); _inspectorScroll = 0; break;
            case UiAction.LabCancel: lab.Cancel(); break;
        }
        if (action != UiAction.SeedField) _textEditor.Blur();
        _dirty = true;
    }
    private void ResetLaboratoryNavigation()
    {
        _laboratoryScroll = 0; _inspectorScroll = 0; _inspectorCell = null; _openDropdown = null;
    }
    private IReadOnlyList<string> LaboratoryStatus()
    {
        var lab = _laboratory!;
        string text = lab.Error ?? lab.Notice ?? "INICIA LA GENERACIÓN MANUAL.";
        var hovered = Hit(MousePosition.X, MousePosition.Y);
        if (hovered is { } action && PangaeaLaboratoryLayout.IsStageAction(action) && !LaboratoryEnabled(action))
            text = lab.UnavailableReason(PangaeaLaboratoryLayout.Stage(action));
        return PangaeaLaboratoryLayout.Wrap([text], PangaeaLaboratoryLayout.PanelWidth(ClientSize.X) - 24, 1.1f);
    }
    private void LaboratoryWheel(float amount, Vector2 pointer)
    {
        if (_openDropdown is not null) return;
        var info = PangaeaLaboratoryLayout.Inspector(ClientSize.X, ClientSize.Y);
        if (info.Contains(pointer.X, pointer.Y))
            _inspectorScroll = Math.Clamp(_inspectorScroll - (int)(amount * 3), 0, Math.Max(0, _inspectorLines - _inspectorVisibleLines));
        else if (PangaeaLaboratoryLayout.ControlsViewport(ClientSize.X, ClientSize.Y).Contains(pointer.X, pointer.Y))
            _laboratoryScroll = Math.Clamp(_laboratoryScroll - amount * 36, 0,
                PangaeaLaboratoryLayout.MaximumScroll(ClientSize.X, ClientSize.Y, LaboratoryStatus().Count, _laboratory?.CanCompareBase == true));
        else if (LaboratoryMapContains(pointer)) _camera.Zoom(amount, pointer);
        _dirty = true;
    }
    private void LaboratoryClip(UiRect rect)
    {
        GL.Enable(EnableCap.ScissorTest);
        int left = (int)(rect.X * FramebufferSize.X / ClientSize.X);
        int right = (int)((rect.X + rect.Width) * FramebufferSize.X / ClientSize.X);
        int bottom = (int)((ClientSize.Y - rect.Y - rect.Height) * FramebufferSize.Y / ClientSize.Y);
        int top = (int)((ClientSize.Y - rect.Y) * FramebufferSize.Y / ClientSize.Y);
        GL.Scissor(left, bottom, Math.Max(0, right - left), Math.Max(0, top - bottom));
    }
    private void FlushLaboratoryClip(UiRect rect)
    {
        LaboratoryClip(rect); _ui!.Flush(ClientSize.X, ClientSize.Y); _ui.Clear(); GL.Disable(EnableCap.ScissorTest);
    }
    private void RenderLaboratory()
    {
        if (_ui is null || _laboratory is not { } lab) return;
        int width = ClientSize.X, height = ClientSize.Y;
        var mapRect = PangaeaLaboratoryLayout.Map(width, height);
        if (lab.Map is { } map && _gridRenderer is not null)
        {
            LaboratoryClip(mapRect);
            _gridRenderer.Draw(_camera, width, height, lab.View);
            GL.Disable(EnableCap.ScissorTest);
            if (lab.View is MapViewMode.Tectonic or MapViewMode.Motion) _gridRenderer.DrawPlateBoundaries(_ui, _camera);
            if (lab.View == MapViewMode.Motion) _gridRenderer.DrawPlateMotions(_ui, _camera);
            if (lab.View == MapViewMode.Geology) _gridRenderer.DrawPangaeaMountainRanges(_ui, _camera);
            if (lab.SelectedCell is { } cell)
            {
                int columns = map.Snapshot.Grid!.Definition.Columns;
                var visible = _camera.VisibleRange();
                for (int copy = (int)Math.Ceiling((visible.FirstColumn - cell.Address.Column) / (double)columns);
                    cell.Address.Column + copy * columns <= visible.LastColumn; copy++)
                {
                    var p = _camera.WorldToScreen(GEvolve.Engine.Grid.FlatHexGeometry.Center(new(cell.Address.Column + copy * columns, cell.Address.Row)));
                    _ui.Hex(p.X, p.Y, _camera.Scale, new(1, 1, 1, .08f), new(1, .9f, .3f, 1));
                }
            }
            // Each bitmap number must fit its own cell and the map viewport at a readable size.
            foreach (var label in PangaeaHeightLabels.Build(map, lab.View, _camera))
            {
                _ui.Text(label.Text, label.TextOrigin.X, label.TextOrigin.Y, label.Pixel, label.TextColor, centered: false);
            }
            FlushLaboratoryClip(mapRect);
        }
        float panel = PangaeaLaboratoryLayout.PanelWidth(width);
        _ui.Rect(0, 0, panel, height, new(.055f, .10f, .16f, 1));
        _ui.Rect(panel - 1, 0, 1, height, new(.24f, .42f, .48f, 1));
        _ui.Text("GENERACION DE MUNDO LAB", 12, 15, 1.5f, White, false);
        _ui.Flush(width, height); _ui.Clear();
        var status = LaboratoryStatus();
        float comparisonOffset = PangaeaLaboratoryLayout.ComparisonOffset(lab.CanCompareBase);
        _laboratoryScroll = Math.Clamp(_laboratoryScroll, 0, PangaeaLaboratoryLayout.MaximumScroll(width, height, status.Count, lab.CanCompareBase));
        foreach (var (label, y) in new[] { ("TIPO MAPA", 46f), ("TAMAÑO", 92f), ("SEMILLA - CTRL V: PEGAR", 138f), ("VISTA", 186f), ("GENERACIÓN MANUAL", 238 + comparisonOffset) })
            _ui.Text(label, 12, y - _laboratoryScroll, 1.15f, Muted, false);
        if (lab.CanCompareBase) _ui.Text("COMPARADOR BASE - EXPERIMENTAL", 12, 232 - _laboratoryScroll, 1.1f, Accent, false);
        foreach (var button in PangaeaLaboratoryLayout.Buttons(width, height, _laboratoryScroll, lab.CanCompareBase).Where(b => b.Action != UiAction.Back)) DrawLaboratoryButton(button);
        for (int i = 0; i < status.Count; i++)
            _ui.Text(status[i], 12, PangaeaLaboratoryLayout.StatusTop + comparisonOffset - _laboratoryScroll + i * 12, 1.1f,
                lab.Error is null ? Muted : new(1, .62f, .48f, 1), false);
        FlushLaboratoryClip(PangaeaLaboratoryLayout.ControlsViewport(width, height));
        DrawLaboratoryInspector();
        DrawLaboratoryButton(PangaeaLaboratoryLayout.Buttons(width, height).Single(b => b.Action == UiAction.Back));
        if (_openDropdown is UiAction.TypeSelect or UiAction.SizeSelect or UiAction.LabView or UiAction.LabBaseVariant)
        foreach (var button in PangaeaLaboratoryLayout.Dropdown(width, height, _openDropdown.Value, lab.Map, _laboratoryScroll))
        {
            var r = button.Bounds; _ui.Rect(r.X, r.Y, r.Width, r.Height, new(.10f, .19f, .26f, 1));
            _ui.Border(r.X, r.Y, r.Width, r.Height, 1, Accent);
            _ui.Text(button.Text, r.X + 6, r.Y + 7, Math.Min(1.2f, (r.Width - 12) / (button.Text.Length * 6)), White, false);
        }
    }
    private void DrawLaboratoryButton(UiButton button)
    {
        var lab = _laboratory!; var r = button.Bounds; bool enabled = LaboratoryEnabled(button.Action);
        bool stageButton = PangaeaLaboratoryLayout.IsStageAction(button.Action);
        var stage = stageButton ? lab.Stages.FirstOrDefault(s => s.Stage == PangaeaLaboratoryLayout.Stage(button.Action)) : null;
        bool grouped = button.Action == UiAction.LabPlates;
        string state = grouped ? lab.PlatesAndMotionState() : stage is null ? "BLOQUEADA" : lab.StageState(stage);
        bool completed = stageButton && state == "COMPLETADA";
        var edge = completed ? new Vector4(.38f, .70f, .50f, 1) : enabled ? Accent : new(.24f, .32f, .39f, 1);
        _ui!.Rect(r.X, r.Y, r.Width, r.Height, enabled ? new(.12f, .24f, .31f, 1) : new(.09f, .14f, .19f, 1));
        _ui.Border(r.X, r.Y, r.Width, r.Height, 1, edge);
        if (stageButton)
        {
            _ui.Text(button.Text, r.X + 6, r.Y + 4, Math.Min(1.25f, (r.Width - 12) / (button.Text.Length * 6)), enabled ? White : Muted, false);
            var times = lab.Stages.Where(s => s.Stage == stage?.Stage || grouped && s.Stage == PangaeaGenerationStage.Motion)
                .Where(s => s.Duration.HasValue).Select(s => s.Duration!.Value.TotalMilliseconds).ToArray();
            _ui.Text(state + (times.Length == 0 ? "" : $"  {times.Sum():F0} MS"), r.X + 6, r.Y + 20, 1.05f, edge, false);
            return;
        }
        if (button.Action == UiAction.RandomSeed)
        {
            _ui.Border(r.X + 10, r.Y + 6, 15, 15, 1, White);
            foreach (var (x, y) in new[] { (2, 2), (10, 2), (6, 6), (2, 10), (10, 10) }) _ui.Rect(r.X + 10 + x, r.Y + 6 + y, 2, 2, White);
            return;
        }
        string text = button.Action switch
        {
            UiAction.SizeSelect => ScreenLayout.SizeLabel(lab.Size),
            UiAction.SeedField => lab.SeedText,
            UiAction.LabView => lab.Map is null ? "SIN CAPAS DISPONIBLES" : PangaeaMapData.ViewLabel(lab.View),
            UiAction.LabBaseVariant => lab.PendingBaseVariant is { } pending ? "CALCULANDO " + pending : PangaeaBaseComparison.Label(lab.BaseVariant),
            _ => button.Text
        };
        float textPixel = Math.Min(1.4f, (r.Width - 12) / (Math.Max(1, text.Length) * 6));
        if (button.Action == UiAction.SeedField) DrawSeedEditing(r.X + 6, r.Y + 8, textPixel);
        _ui.Text(text, r.X + 6, r.Y + 8, textPixel, enabled ? White : Muted, false);
    }
    private void DrawLaboratoryInspector()
    {
        var lab = _laboratory!; var rect = PangaeaLaboratoryLayout.Inspector(ClientSize.X, ClientSize.Y);
        if (_inspectorCell != lab.SelectedCell?.Id) { _inspectorCell = lab.SelectedCell?.Id; _inspectorScroll = 0; }
        _ui!.Rect(rect.X, rect.Y, rect.Width, rect.Height, new(.075f, .14f, .21f, 1));
        _ui.Text("INFORMACIÓN DE CELDA", rect.X + 7, rect.Y + 8, 1.15f, Accent, false);
        _ui.Text("RUEDA: MÁS DATOS", rect.X + 7, rect.Y + rect.Height - 11, 1, Muted, false);
        _ui.Flush(ClientSize.X, ClientSize.Y); _ui.Clear();
        var original = lab.Inspect();
        var ordered = original.Take(2).Concat(original.Where(s => s.StartsWith("BASE:") || s.StartsWith("APORTE:") || s.StartsWith("FINAL:")))
            .Concat(original.Skip(2).Where(s => !s.StartsWith("BASE:") && !s.StartsWith("APORTE:") && !s.StartsWith("FINAL:")));
        var lines = PangaeaLaboratoryLayout.Wrap(original.Count == 0 ? ["SELECCIONA UN HEXÁGONO DEL MAPA."] : ordered, rect.Width - 14);
        _inspectorLines = lines.Count; _inspectorVisibleLines = (int)((rect.Height - 40) / 12);
        _inspectorScroll = Math.Clamp(_inspectorScroll, 0, Math.Max(0, _inspectorLines - _inspectorVisibleLines));
        for (int i = _inspectorScroll; i < Math.Min(lines.Count, _inspectorScroll + _inspectorVisibleLines); i++)
            _ui.Text(lines[i], rect.X + 7, rect.Y + 25 + (i - _inspectorScroll) * 12, 1.2f, White, false);
        FlushLaboratoryClip(new(rect.X, rect.Y + 23, rect.Width, rect.Height - 38));
    }
}
