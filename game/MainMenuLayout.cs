namespace Evolve.Game;

// Adapts worldgeneration's main-menu proportions without importing its screen layout.
internal readonly record struct MainMenuLayout(
    float Center, float TitleTop, float TitlePixel, float VersionPixel,
    float ButtonLeft, float ButtonTop, float ButtonWidth, float ButtonHeight,
    float ButtonGap, float Scale)
{
    public float VersionTop => TitleTop + TitlePixel * 9;

    // Retain the original dimensions at normal sizes; fit the whole menu on short windows.
    public static MainMenuLayout Create(int width, int height)
    {
        float scale = Math.Min(1f, height / 480f);
        float logicalWidth = width / scale;
        float logicalHeight = height / scale;
        float buttonWidth = MathF.Min(360, MathF.Max(180, logicalWidth - 32));
        float buttonHeight = Math.Clamp(logicalHeight / 14f, 40, 54);
        float gap = Math.Clamp(logicalHeight / 50f, 8, 15);
        float top = MathF.Max(150, logicalHeight * .42f);

        return new(width / 2f, height * .17f,
            Math.Clamp(logicalWidth / 205f, 4, 6.5f) * scale,
            Math.Max(1f, Math.Clamp(logicalWidth / 480f, 1.5f, 2.5f) * scale),
            (width - buttonWidth * scale) / 2,
            top * scale, buttonWidth * scale, buttonHeight * scale, gap * scale, scale);
    }
}
