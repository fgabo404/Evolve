using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Game.Screens;

public enum UiAction { Create, Options, Exit, Back, Generate, Small, Medium, Large }
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
        float w = MathF.Min(360, MathF.Max(180, width - 32));
        float h = Math.Clamp(height / 14f, 40, 54);
        float gap = Math.Clamp(height / 50f, 8, 15);
        float x = center - w / 2;
        if (screen == GameScreen.MainMenu)
        {
            float y = MathF.Max(160, height * .48f);
            return [new(UiAction.Create, "Crear mundo", new(x, y, w, h)),
                new(UiAction.Options, "Opciones", new(x, y + h + gap, w, h)),
                new(UiAction.Exit, "Salir", new(x, y + 2 * (h + gap), w, h))];
        }
        if (screen == GameScreen.CreateWorld)
        {
            float y = MathF.Max(100, height * .25f);
            float cardH = Math.Clamp(height / 12f, 42, 58);
            float cardGap = Math.Clamp(height / 90f, 5, 10);
            float buttonY = y + 3 * (cardH + cardGap) + 10;
            return [new(UiAction.Small, "Pequeño", new(x, y, w, cardH)),
                new(UiAction.Medium, "Mediano", new(x, y + cardH + cardGap, w, cardH)),
                new(UiAction.Large, "Grande", new(x, y + 2 * (cardH + cardGap), w, cardH)),
                new(UiAction.Generate, "Generar mundo", new(x, buttonY, w, h)),
                new(UiAction.Back, "Volver", new(x, buttonY + h + gap, w, h))];
        }
        if (screen == GameScreen.Options)
            return [new(UiAction.Back, "Volver", new(x, MathF.Max(260, height * .55f), w, h))];
        return [new(UiAction.Back, "Menú principal",
            new(MathF.Max(8, width - 190), 11, MathF.Min(180, width - 16), 36))];
    }

    public static GridSize SizeFor(UiAction action) => action switch
    {
        UiAction.Small => GridSize.Small,
        UiAction.Medium => GridSize.Medium,
        UiAction.Large => GridSize.Large,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
