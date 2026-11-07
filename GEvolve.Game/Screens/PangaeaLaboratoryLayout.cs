using GEvolve.Engine.World;
using GEvolve.Game.Graphics;

namespace GEvolve.Game.Screens;

public static class PangaeaLaboratoryLayout
{
    public static float PanelWidth(int width) => Math.Clamp(width * .26f, 250, 320);
    public static float StageTop(int height) => height < 650 ? 208 : 244;
    public static float StagePitch(int height) => height < 650 ? 32 : 47;
    public static readonly PangaeaGenerationStage[] VisibleStages = [PangaeaGenerationStage.Plates,
        PangaeaGenerationStage.Surface, PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final];
    public static readonly string[] StageLabels = ["GENERAR PLACAS Y MOVIMIENTO", "GENERAR SUPERFICIE",
        "GENERAR ELEVACIONES BASE", "GENERAR SISTEMAS MONTAÑOSOS", "GENERAR ELEVACIONES FINAL"];
    public static readonly MapViewMode[] Views = [MapViewMode.Grid, MapViewMode.Tectonic, MapViewMode.Compression,
        MapViewMode.Extension, MapViewMode.Shear, MapViewMode.Surface, MapViewMode.ElevationBase,
        MapViewMode.Geology, MapViewMode.ElevationContribution, MapViewMode.ElevationFinal, MapViewMode.Motion];
    private static readonly UiAction[] ViewActions = [UiAction.LabViewGrid, UiAction.LabViewPlates,
        UiAction.LabViewCompression, UiAction.LabViewExtension, UiAction.LabViewShear, UiAction.LabViewSurface,
        UiAction.LabViewBase, UiAction.LabViewSystems, UiAction.LabViewContribution, UiAction.LabViewFinal, UiAction.LabViewMotion];
    public static UiAction StageAction(PangaeaGenerationStage stage) => (UiAction)((int)UiAction.LabGrid + (int)stage - 1);
    public static bool IsStageAction(UiAction action) => VisibleStages.Any(s => StageAction(s) == action);
    public static bool IsViewAction(UiAction action) => ViewActions.Contains(action);
    public static PangaeaGenerationStage Stage(UiAction action) => (PangaeaGenerationStage)((int)action - (int)UiAction.LabGrid + 1);
    public static UiAction ViewAction(MapViewMode view) => ViewActions[Array.IndexOf(Views, view)];
    public static MapViewMode View(UiAction action) => Views[Array.IndexOf(ViewActions, action)];
    public static UiRect Inspector(int width, int height) => new(PanelWidth(width) + 10, height - 170,
        width - PanelWidth(width) - 20, 160);
    public static IReadOnlyList<UiButton> Buttons(int width, int height, bool confirmation)
    {
        float panel = PanelWidth(width), w = panel - 24;
        var buttons = new List<UiButton>
        {
            new(UiAction.Back, "MENÚ", new(width - 108, 10, 96, 34)),
            new(UiAction.SizeSelect, "TAMAÑO", new(12, 90, w, 28)),
            new(UiAction.SeedField, "SEMILLA", new(12, 128, w - 83, 28)),
            new(UiAction.RandomSeed, "ALEATORIA", new(panel - 88, 128, 76, 28)),
            new(UiAction.LabRestart, "INICIAR / REINICIAR SESIÓN", new(12, 166, w, 30)),
            new(UiAction.LabView, "VISTA", new(panel + 10, 66, Math.Min(300, width - panel - 20), 28)),
            new(UiAction.LabCancel, "CANCELAR OPERACIÓN", new(panel + 10, 101, Math.Min(230, width - panel - 20), 26)),
            new(UiAction.LabShowHeights, "MOSTRAR ALTURAS", new(12, StageTop(height) + 5 * StagePitch(height) + 8, w, 28))
        };
        for (int index = 0; index < VisibleStages.Length; index++)
            buttons.Add(new(StageAction(VisibleStages[index]), $"{index + 1:00} - {StageLabels[index]}",
                new(12, StageTop(height) + index * StagePitch(height), w, StagePitch(height) - 3)));
        if (confirmation)
        {
            float x = panel + 10, y = 150, cw = Math.Min(160, (width - panel - 30) / 2);
            buttons.Add(new(UiAction.LabConfirm, "SÍ, BORRAR CAPAS", new(x, y, cw, 30)));
            buttons.Add(new(UiAction.LabKeep, "CONSERVAR SESIÓN", new(x + cw + 10, y, cw, 30)));
        }
        return buttons;
    }
    public static IReadOnlyList<UiButton> Dropdown(int width, int height, UiAction selector, PangaeaMapData? map)
    {
        var anchor = Buttons(width, height, false).Single(b => b.Action == selector).Bounds;
        if (selector == UiAction.SizeSelect)
            return new[] { UiAction.Small, UiAction.Medium, UiAction.Large, UiAction.Enormous }.Select((a, i) =>
                new UiButton(a, ScreenLayout.SizeLabel(ScreenLayout.SizeFor(a)), new(anchor.X, anchor.Y + 32 + i * 28, anchor.Width, 28))).ToArray();
        return (map?.AvailableViews ?? []).Select((view, i) => new UiButton(ViewAction(view), PangaeaMapData.ViewLabel(view),
            new(anchor.X, anchor.Y + 32 + i * 25, anchor.Width, 25))).ToArray();
    }
}
