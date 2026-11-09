using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Game.Screens;

public enum GridLabAction { OpenLaboratory, OpenCreateWorld, OpenOptions, Exit, Back, Seed, RandomSeed, Small, Medium, Large, Enormous,
    CreateSizeSelect, CreateTypeSelect, CreateSeed, CreateRandomSeed, GenerateUnavailable, Continents, Pangaea, Archipelagos }
public readonly record struct GridLabRect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
public readonly record struct GridLabButton(GridLabAction Action, string Text, GridLabRect Bounds, bool Enabled = true);

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
    public static string SizeLabel(GridSize size) => size switch
    { GridSize.Small => "Pequeño", GridSize.Medium => "Mediano", GridSize.Large => "Grande", GridSize.Enormous => "Enorme", _ => throw new ArgumentOutOfRangeException(nameof(size)) };
    public static LandmassType Type(GridLabAction action) => action switch
    { GridLabAction.Continents => LandmassType.Continents, GridLabAction.Pangaea => LandmassType.Pangaea,
        GridLabAction.Archipelagos => LandmassType.Archipelagos, _ => throw new ArgumentOutOfRangeException(nameof(action)) };
    public static string TypeLabel(LandmassType type) => type switch
    { LandmassType.Continents => "Continentes", LandmassType.Pangaea => "Pangea", LandmassType.Archipelagos => "Archipiélagos", _ => throw new ArgumentOutOfRangeException(nameof(type)) };
    public static IReadOnlyList<GridLabButton> Dropdown(GameScreen screen, int width, int height, GridLabAction? selector)
    {
        if (screen != GameScreen.CreateWorld || selector is not (GridLabAction.CreateSizeSelect or GridLabAction.CreateTypeSelect)) return [];
        var anchor = Buttons(screen, width, height).Single(b => b.Action == selector).Bounds;
        var actions = selector == GridLabAction.CreateSizeSelect
            ? new[] { GridLabAction.Small, GridLabAction.Medium, GridLabAction.Large, GridLabAction.Enormous }
            : new[] { GridLabAction.Pangaea, GridLabAction.Continents, GridLabAction.Archipelagos };
        return actions.Select((a, i) => new GridLabButton(a, IsSize(a) ? SizeLabel(Size(a)) : TypeLabel(Type(a)),
            new(anchor.X, anchor.Y + anchor.Height + 4 + i * anchor.Height, anchor.Width, anchor.Height))).ToArray();
    }
    public static GridLabAction? Hit(GameScreen screen, int width, int height, float x, float y, GridLabAction? selector = null)
        => Dropdown(screen, width, height, selector).Concat(Buttons(screen, width, height))
            .Where(b => b.Enabled && b.Bounds.Contains(x, y)).Select(b => (GridLabAction?)b.Action).FirstOrDefault();
    public static IReadOnlyList<GridLabButton> Buttons(GameScreen screen, int width, int height)
    {
        if (screen == GameScreen.MainMenu)
        {
            float w = MathF.Min(360, MathF.Max(180, width - 32)), x = (width - w) / 2;
            float h = Math.Clamp(height / 14f, 40, 54), gap = Math.Clamp(height / 50f, 8, 15), y = MathF.Max(150, height * .42f);
            return [new(GridLabAction.OpenCreateWorld, "Crear mundo", new(x, y, w, h)),
                new(GridLabAction.OpenLaboratory, "Laboratorio", new(x, y + h + gap, w, h)),
                new(GridLabAction.OpenOptions, "Opciones", new(x, y + 2 * (h + gap), w, h)),
                new(GridLabAction.Exit, "Salir", new(x, y + 3 * (h + gap), w, h))];
        }
        if (screen == GameScreen.Options)
        {
            float w = MathF.Min(360, MathF.Max(180, width - 32));
            return [new(GridLabAction.Back, "Volver", new((width - w) / 2, MathF.Max(260, height * .55f), w, Math.Clamp(height / 14f, 40, 54)))];
        }
        if (screen == GameScreen.CreateWorld)
        {
            float total = MathF.Min(850, width - 32), left = (width - total) / 2;
            float gap = MathF.Max(10, total * .025f), column = (total - gap) / 2;
            float top = MathF.Max(105, height * .23f), h = Math.Clamp(height / 16f, 32, 45), rowGap = Math.Clamp(height / 80f, 6, 9);
            float seedY = top + 4 * (h + rowGap) + 32, seedH = Math.Clamp(height / 18f, 35, 44), field = total * .64f;
            float actionY = seedY + seedH + Math.Clamp(height / 35f, 17, 25), actionW = MathF.Min(390, total), actionH = Math.Clamp(height / 15f, 40, 52);
            return [new(GridLabAction.CreateSizeSelect, "Tamaño", new(left, top, column, h)),
                new(GridLabAction.CreateTypeSelect, "Tipo de mundo", new(left + column + gap, top, column, h)),
                new(GridLabAction.CreateSeed, "Semilla", new(left, seedY, field, seedH)),
                new(GridLabAction.CreateRandomSeed, "Aleatoria", new(left + field + gap, seedY, total - field - gap, seedH)),
                new(GridLabAction.GenerateUnavailable, "Generar mundo", new((width - actionW) / 2, actionY, actionW, actionH), Enabled: false),
                new(GridLabAction.Back, "Volver", new((width - actionW) / 2, actionY + actionH + rowGap, actionW, actionH))];
        }
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
