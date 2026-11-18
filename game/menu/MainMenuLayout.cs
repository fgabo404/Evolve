// Calculates the approved main-menu geometry and button hit bounds.
// Shares row positions between presentation and interaction while adapting to window size.

namespace Evolve.Game;

internal readonly record struct MainMenuLayout(
    float Center, float TitleTop, float TitlePixel, float VersionPixel,
    float ButtonLeft, float ButtonTop, float ButtonWidth, float ButtonHeight,
    float ButtonGap, float Scale)
{
    public float VersionTop => TitleTop + TitlePixel * 9;

    // Rendering and hit detection use this same row position to avoid drifting after resize.
    public float ButtonY(MainMenuButton button) => ButtonTop + (int)button * (ButtonHeight + ButtonGap);

    // Accept framebuffer coordinates and exclude gaps, shadows and the right/bottom edges.
    public MainMenuButton? HitTest(float x, float y)
    {
        if (x < ButtonLeft || x >= ButtonLeft + ButtonWidth)
        {
            return null;
        }

        for (int index = 0; index < MainMenuInteraction.ButtonCount; index++)
        {
            var button = (MainMenuButton)index;
            float top = ButtonY(button);
            if (y >= top && y < top + ButtonHeight)
            {
                return button;
            }
        }

        return null;
    }

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
