using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Game.Screens;

public enum UiAction
{
    Create, Options, Exit, Back, Generate,
    SizeSelect, TypeSelect,
    Small, Medium, Large, Enormous,
    Continents, Pangaea, SmallContinents, Archipelagos,
    SeedField, RandomSeed, Regenerate, ToggleSurface, ToggleGrayscale, ToggleInfrared, ToggleRainfall, ToggleTectonic
}

public readonly record struct UiRect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
public readonly record struct UiButton(UiAction Action, string Text, UiRect Bounds);

/// <summary>Posiciones adaptables compartidas por dibujo y detección de clics.</summary>
public static class ScreenLayout
{
    public static IReadOnlyList<UiButton> Buttons(GameScreen screen, int width, int height)
    {
        float center = width / 2f;
        float menuWidth = MathF.Min(360, MathF.Max(180, width - 32));
        float menuHeight = Math.Clamp(height / 14f, 40, 54);
        float menuGap = Math.Clamp(height / 50f, 8, 15);
        float menuX = center - menuWidth / 2;
        if (screen == GameScreen.MainMenu)
        {
            float y = MathF.Max(160, height * .48f);
            return [new(UiAction.Create, "Crear mundo", new(menuX, y, menuWidth, menuHeight)),
                new(UiAction.Options, "Opciones", new(menuX, y + menuHeight + menuGap,
                    menuWidth, menuHeight)),
                new(UiAction.Exit, "Salir", new(menuX, y + 2 * (menuHeight + menuGap),
                    menuWidth, menuHeight))];
        }
        if (screen == GameScreen.CreateWorld)
        {
            float total = MathF.Min(850, width - 32);
            float left = center - total / 2;
            float gapX = MathF.Max(10, total * .025f);
            float columnWidth = (total - gapX) / 2;
            float top = MathF.Max(105, height * .23f);
            float optionHeight = Math.Clamp(height / 16f, 32, 45);
            float optionGap = Math.Clamp(height / 80f, 6, 9);
            float right = left + columnWidth + gapX;
            float seedY = top + 4 * (optionHeight + optionGap) + 32;
            float seedHeight = Math.Clamp(height / 18f, 35, 44);
            float fieldWidth = total * .64f;
            float actionHeight = Math.Clamp(height / 15f, 40, 52);
            float actionY = seedY + seedHeight + Math.Clamp(height / 35f, 17, 25);
            float actionWidth = MathF.Min(390, total);
            float actionX = center - actionWidth / 2;
            return [
                new(UiAction.SizeSelect, "Tamaño",
                    new(left, top, columnWidth, optionHeight)),
                new(UiAction.TypeSelect, "Tipo de mundo",
                    new(right, top, columnWidth, optionHeight)),
                new(UiAction.SeedField, "Semilla", new(left, seedY,
                    fieldWidth, seedHeight)),
                new(UiAction.RandomSeed, "Aleatoria", new(left + fieldWidth + gapX, seedY,
                    total - fieldWidth - gapX, seedHeight)),
                new(UiAction.Generate, "Generar mundo", new(actionX, actionY,
                    actionWidth, actionHeight)),
                new(UiAction.Back, "Volver", new(actionX,
                    actionY + actionHeight + optionGap, actionWidth, actionHeight))
            ];
        }
        if (screen == GameScreen.Options)
            return [new(UiAction.Back, "Volver", new(menuX,
                MathF.Max(260, height * .55f), menuWidth, menuHeight))];
        if (screen == GameScreen.World)
            return [
                new(UiAction.SizeSelect, "Tamaño", new(12, 146, 261, 36)),
                new(UiAction.TypeSelect, "Tipo de mundo", new(12, 192, 261, 36)),
                new(UiAction.ToggleSurface, "CAPA 02: SUPERFICIE", new(12, 248, 261, 36)),
                new(UiAction.ToggleGrayscale, "VER ALTURAS", new(12, 290, 261, 36)),
                new(UiAction.ToggleTectonic, "CAPA 01: PLACAS", new(12, 332, 261, 36)),
                new(UiAction.Regenerate, "Regenerar mundo",
                new(MathF.Max(8, width - 390), 11, MathF.Min(190, width - 16), 36)),
                new(UiAction.Back, "Menú principal",
                new(MathF.Max(8, width - 190), 11, MathF.Min(180, width - 16), 36))];
        return [];
    }

    /// <summary>Dropdown entries share their coordinates with hit testing and rendering.</summary>
    public static IReadOnlyList<UiButton> DropdownOptions(GameScreen screen,
        int width, int height, UiAction selector)
    {
        if (screen is not (GameScreen.CreateWorld or GameScreen.World) ||
            selector is not (UiAction.SizeSelect or UiAction.TypeSelect))
            return [];

        var anchor = Buttons(screen, width, height)
            .First(button => button.Action == selector).Bounds;
        var actions = selector == UiAction.SizeSelect
            ? SizeActions : TypeActions;
        var labels = selector == UiAction.SizeSelect
            ? SizeLabels : TypeLabels;
        float itemHeight = anchor.Height;
        var options = new UiButton[actions.Length];
        for (int i = 0; i < actions.Length; i++)
            options[i] = new UiButton(actions[i], labels[i],
                new(anchor.X, anchor.Y + anchor.Height + 4 + i * itemHeight,
                    anchor.Width, itemHeight));
        return options;
    }

    private static readonly UiAction[] SizeActions =
        [UiAction.Small, UiAction.Medium, UiAction.Large, UiAction.Enormous];
    private static readonly UiAction[] TypeActions =
        [UiAction.Continents, UiAction.Pangaea, UiAction.SmallContinents, UiAction.Archipelagos];
    private static readonly string[] SizeLabels = ["Pequeño", "Mediano", "Grande", "Enorme"];
    private static readonly string[] TypeLabels =
        ["Continentes", "Pangea", "Continentes pequeños", "Archipiélagos"];

    public static string SizeLabel(GridSize size) => size switch
    {
        GridSize.Small => SizeLabels[0],
        GridSize.Medium => SizeLabels[1],
        GridSize.Large => SizeLabels[2],
        GridSize.Enormous => SizeLabels[3],
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };

    public static string TypeLabel(LandmassType type) => type switch
    {
        LandmassType.Continents => TypeLabels[0],
        LandmassType.Pangaea => TypeLabels[1],
        LandmassType.SmallContinents => TypeLabels[2],
        LandmassType.Archipelagos => TypeLabels[3],
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static GridSize SizeFor(UiAction action) => action switch
    {
        UiAction.Small => GridSize.Small,
        UiAction.Medium => GridSize.Medium,
        UiAction.Large => GridSize.Large,
        UiAction.Enormous => GridSize.Enormous,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static LandmassType TypeFor(UiAction action) => action switch
    {
        UiAction.Continents => LandmassType.Continents,
        UiAction.Pangaea => LandmassType.Pangaea,
        UiAction.SmallContinents => LandmassType.SmallContinents,
        UiAction.Archipelagos => LandmassType.Archipelagos,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
