namespace Evolve.Game;

// Composes the UI-02 menu as visuals only; input and navigation belong to UI-03.
internal sealed class MainMenuPresentation
{
    private static readonly string[] ButtonLabels = ["INICIAR", "LAB", "OPCIONES", "SALIR"];

    // Reproduce the legacy background and static buttons, with the version replacing the subtitle.
    public void Draw(PixelMenuRenderer renderer, int width, int height)
    {
        var layout = MainMenuLayout.Create(width, height);
        float scale = layout.Scale;

        renderer.Begin(width, height);
        renderer.Rectangle(0, height * .72f, width, height * .28f, MainMenuStyle.BackgroundBand);
        renderer.Rectangle(0, height * .72f, width, 2 * scale, MainMenuStyle.BackgroundDivider);

        renderer.CenteredText("EVOLVE", layout.Center, layout.TitleTop, layout.TitlePixel, MainMenuStyle.Text);
        renderer.CenteredText(GameVersion.Display, layout.Center, layout.VersionTop, layout.VersionPixel, MainMenuStyle.VersionText);

        for (int index = 0; index < ButtonLabels.Length; index++)
        {
            float x = layout.ButtonLeft;
            float y = layout.ButtonTop + index * (layout.ButtonHeight + layout.ButtonGap);
            float buttonWidth = layout.ButtonWidth;
            float buttonHeight = layout.ButtonHeight;
            float textPixel = Math.Min(2.5f * scale,
                (buttonWidth - 16 * scale) / (ButtonLabels[index].Length * LegacyBitmapFont.Advance));

            renderer.Rectangle(x + 3 * scale, y + 4 * scale, buttonWidth, buttonHeight, MainMenuStyle.Shadow);
            renderer.Rectangle(x, y, buttonWidth, buttonHeight, MainMenuStyle.Button);
            renderer.Border(x, y, buttonWidth, buttonHeight, 1.5f * scale, MainMenuStyle.Border);
            renderer.CenteredText(ButtonLabels[index], layout.Center,
                y + (buttonHeight - LegacyBitmapFont.Height * textPixel) / 2, textPixel, MainMenuStyle.Text);
        }

        renderer.End();
    }
}
