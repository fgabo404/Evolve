using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Game.Screens;

public enum GridLabAction { OpenLaboratory, CreateUnavailable, Exit, Back, Seed, RandomSeed, Small, Medium, Large, Enormous }
public readonly record struct GridLabRect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
public readonly record struct GridLabButton(GridLabAction Action, string Text, GridLabRect Bounds);

public static class GridLaboratoryLayout
{
    public static float PanelWidth(int width) => Math.Min(280, Math.Max(180, width * .28f));
    public static GridLabRect Map(int width, int height) => new(PanelWidth(width), 0, Math.Max(1, width - PanelWidth(width)), Math.Max(1, height));
    public static readonly GridSize[] Sizes = [GridSize.Small, GridSize.Medium, GridSize.Large, GridSize.Enormous];
    public static GridSize Size(GridLabAction action) => action switch
    {
        GridLabAction.Small => GridSize.Small, GridLabAction.Medium => GridSize.Medium,
        GridLabAction.Large => GridSize.Large, GridLabAction.Enormous => GridSize.Enormous,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
    public static bool IsSize(GridLabAction action) => action is GridLabAction.Small or GridLabAction.Medium or GridLabAction.Large or GridLabAction.Enormous;
    public static IReadOnlyList<GridLabButton> Buttons(GameScreen screen, int width, int height)
    {
        if (screen == GameScreen.MainMenu)
        {
            float w = Math.Min(370, Math.Max(120, width - 32)), x = (width - w) / 2;
            return [new(GridLabAction.OpenLaboratory, "LABORATORIO DE CUADRÍCULA", new(x, 180, w, 40)),
                new(GridLabAction.CreateUnavailable, "CREAR MUNDO - NO DISPONIBLE", new(x, 234, w, 40)),
                new(GridLabAction.Exit, "SALIR", new(x, 288, w, 40))];
        }
        if (screen == GameScreen.WorldUnavailable)
            return [new(GridLabAction.Back, "VOLVER AL MENÚ", new(24, 200, Math.Min(260, width - 48), 36))];
        float panel = PanelWidth(width), inner = panel - 24;
        var buttons = new List<GridLabButton> { new(GridLabAction.Seed, "SEMILLA", new(12, 75, inner, 30)),
            new(GridLabAction.RandomSeed, "SEMILLA ALEATORIA", new(12, 116, inner, 28)) };
        var actions = new[] { GridLabAction.Small, GridLabAction.Medium, GridLabAction.Large, GridLabAction.Enormous };
        var labels = new[] { "PEQUEÑO", "MEDIANO", "GRANDE", "ENORME" };
        for (int i = 0; i < actions.Length; i++) buttons.Add(new(actions[i], labels[i], new(12, 184 + i * 36, inner, 28)));
        buttons.Add(new(GridLabAction.Back, "SALIR AL MENÚ", new(12, Math.Max(380, height - 48), inner, 32)));
        return buttons;
    }
}
