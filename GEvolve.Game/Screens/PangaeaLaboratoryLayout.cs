using GEvolve.Engine.World;
using GEvolve.Game.Graphics;

namespace GEvolve.Game.Screens;

/// <summary>Fixed sidebar; scroll only its controls on short windows. Map and inspector never overlap.</summary>
public static class PangaeaLaboratoryLayout
{
    public const float HeaderHeight = 42;
    public const float StatusTop = 586;
    public static float PanelWidth(int width) => width < 800 ? 240 : 280;
    public static UiRect Map(int width, int height) => new(PanelWidth(width), 0, width - PanelWidth(width), height);
    public static UiRect Inspector(int width, int height) => new(12, height - 190, PanelWidth(width) - 24, 136);
    public static UiRect ControlsViewport(int width, int height) => new(0, HeaderHeight, PanelWidth(width), Inspector(width, height).Y - HeaderHeight - 8);
    public static float MaximumScroll(int width, int height, int statusLines = 3) => Math.Max(0,
        StatusTop + Math.Max(3, statusLines) * 12 + 8 - (ControlsViewport(width, height).Y + ControlsViewport(width, height).Height));
    public static readonly PangaeaGenerationStage[] VisibleStages = [PangaeaGenerationStage.Plates,
        PangaeaGenerationStage.Surface, PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final,
        PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes];
    public static readonly string[] StageLabels = ["GENERAR PLACAS Y MOVIMIENTOS", "GENERAR SUPERFICIES",
        "GENERAR ELEVACIONES", "GENERAR SISTEMAS MONTAÑOSOS", "GENERAR ELEVACIONES FINALES",
        "GENERAR TEMPERATURA", "GENERAR PRECIPITACIÓN Y HUMEDAD", "GENERAR BIOMAS"];
    public static readonly MapViewMode[] Views = [MapViewMode.Grid, MapViewMode.Tectonic, MapViewMode.Compression,
        MapViewMode.Extension, MapViewMode.Shear, MapViewMode.Surface, MapViewMode.ElevationBase,
        MapViewMode.Geology, MapViewMode.ElevationContribution, MapViewMode.ElevationFinal, MapViewMode.Motion,
        MapViewMode.Infrared, MapViewMode.Rainfall, MapViewMode.Moisture, MapViewMode.Biomes];
    private static readonly UiAction[] ViewActions = [UiAction.LabViewGrid, UiAction.LabViewPlates,
        UiAction.LabViewCompression, UiAction.LabViewExtension, UiAction.LabViewShear, UiAction.LabViewSurface,
        UiAction.LabViewBase, UiAction.LabViewSystems, UiAction.LabViewContribution, UiAction.LabViewFinal, UiAction.LabViewMotion,
        UiAction.LabViewTemperature, UiAction.LabViewRainfall, UiAction.LabViewMoisture, UiAction.LabViewBiomes];
    private static readonly UiAction[] StageActions = [UiAction.LabGrid, UiAction.LabPlates, UiAction.LabMotion,
        UiAction.LabSurface, UiAction.LabBase, UiAction.LabSystems, UiAction.LabFinal, UiAction.LabTemperature, UiAction.LabPrecipitation, UiAction.LabBiomes];
    public static UiAction StageAction(PangaeaGenerationStage stage) => StageActions[(int)stage - 1];
    public static bool IsStageAction(UiAction action) => VisibleStages.Any(s => StageAction(s) == action);
    public static bool IsViewAction(UiAction action) => ViewActions.Contains(action);
    public static PangaeaGenerationStage Stage(UiAction action) => (PangaeaGenerationStage)(Array.IndexOf(StageActions, action) + 1);
    public static UiAction ViewAction(MapViewMode view) => ViewActions[Array.IndexOf(Views, view)];
    public static MapViewMode View(UiAction action) => Views[Array.IndexOf(ViewActions, action)];
    public static IReadOnlyList<UiButton> Buttons(int width, int height, float scroll = 0)
    {
        float panel = PanelWidth(width), w = panel - 24;
        var buttons = new List<UiButton>
        {
            new(UiAction.TypeSelect, "PANGEA - ÚNICO DISPONIBLE", new(12, 60 - scroll, w, 24)),
            new(UiAction.SizeSelect, "TAMAÑO", new(12, 106 - scroll, w, 24)),
            new(UiAction.SeedField, "SEMILLA", new(12, 152 - scroll, w - 44, 26)),
            new(UiAction.RandomSeed, "ALEATORIA", new(panel - 48, 152 - scroll, 36, 26)),
            new(UiAction.LabView, "VISTA", new(12, 200 - scroll, w, 26)),
            new(UiAction.LabRestart, "INICIAR / REINICIAR", new(12, 550 - scroll, w * .62f - 4, 26)),
            new(UiAction.LabCancel, "CANCELAR", new(12 + w * .62f + 4, 550 - scroll, w * .38f - 4, 26)),
            new(UiAction.Back, "SALIR AL MENÚ", new(12, height - 42, w, 30))
        };
        for (int i = 0; i < VisibleStages.Length; i++)
            buttons.Add(new(StageAction(VisibleStages[i]), $"{i + 1:00} - {StageLabels[i]}", new(12, 254 + i * 36 - scroll, w, 32)));
        return buttons;
    }
    public static IEnumerable<UiButton> HitButtons(int width, int height, float scroll)
    {
        var viewport = ControlsViewport(width, height);
        foreach (var button in Buttons(width, height, scroll))
        {
            if (button.Action == UiAction.Back) { yield return button; continue; }
            var r = button.Bounds;
            float x = Math.Max(r.X, viewport.X), y = Math.Max(r.Y, viewport.Y);
            float right = Math.Min(r.X + r.Width, viewport.X + viewport.Width), bottom = Math.Min(r.Y + r.Height, viewport.Y + viewport.Height);
            if (right > x && bottom > y) yield return button with { Bounds = new(x, y, right - x, bottom - y) };
        }
    }
    public static IReadOnlyList<UiButton> Dropdown(int width, int height, UiAction selector, PangaeaMapData? map, float scroll = 0)
    {
        var anchor = Buttons(width, height, scroll).Single(b => b.Action == selector).Bounds;
        (UiAction Action, string Text)[] options = selector switch
        {
            UiAction.TypeSelect => [(UiAction.Pangaea, "PANGEA - ÚNICO DISPONIBLE")],
            UiAction.SizeSelect => new[] { UiAction.Small, UiAction.Medium, UiAction.Large, UiAction.Enormous }
                .Select(a => (a, ScreenLayout.SizeLabel(ScreenLayout.SizeFor(a)))).ToArray(),
            _ => (map?.AvailableViews ?? []).Select(v => (ViewAction(v), PangaeaMapData.ViewLabel(v))).ToArray()
        };
        if (options.Length == 0) return [];
        var viewport = ControlsViewport(width, height);
        float pitch = Math.Min(24, viewport.Height / options.Length);
        float top = Math.Clamp(anchor.Y + anchor.Height + 2, viewport.Y, viewport.Y + viewport.Height - pitch * options.Length);
        return options.Select((o, i) => new UiButton(o.Action, o.Text, new(anchor.X, top + i * pitch, anchor.Width, pitch))).ToArray();
    }
    public static IReadOnlyList<string> Wrap(IEnumerable<string> lines, float width, float pixel = 1.2f)
    {
        int length = Math.Max(1, (int)(width / (6 * pixel)));
        var output = new List<string>();
        foreach (string line in lines)
        for (int start = 0; start < line.Length; start += length)
            output.Add(line.Substring(start, Math.Min(length, line.Length - start)));
        return output;
    }
}
