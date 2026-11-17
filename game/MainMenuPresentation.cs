namespace Evolve.Game;

// Composes the UI-02 menu as visuals only; input and navigation belong to UI-03.
internal sealed class MainMenuPresentation
{
    private static readonly string[] ButtonLabels = ["INICIAR", "LAB", "OPCIONES", "SALIR"];

    // Draw the background accents, heading, metadata version and four static buttons.
    public void Draw(PixelMenuRenderer renderer, int width, int height)
    {
        var layout = MainMenuLayout.Create(width, height);
        int scale = layout.Scale;
        int center = layout.Left + MainMenuLayout.Width * scale / 2;

        renderer.Begin(width, height);
        for (int y = 0; y < height; y += 32 * scale)
        {
            for (int x = 0; x < width; x += 32 * scale)
            {
                renderer.Rectangle(x, y, 2 * scale, 2 * scale, MainMenuStyle.BackgroundDetail);
            }
        }

        renderer.CenteredText("EVOLVE", center + 2 * scale, layout.Top + 3 * scale, 4 * scale, MainMenuStyle.Shadow);
        renderer.CenteredText("EVOLVE", center, layout.Top, 4 * scale, MainMenuStyle.Text);
        renderer.CenteredText(GameVersion.Display, center, layout.Top + 50 * scale, scale, MainMenuStyle.Text);

        for (int index = 0; index < ButtonLabels.Length; index++)
        {
            int x = center - MainMenuLayout.ButtonWidth * scale / 2;
            int y = layout.Top + (MainMenuLayout.ButtonTop + index * MainMenuLayout.ButtonSpacing) * scale;
            int buttonWidth = MainMenuLayout.ButtonWidth * scale;
            int buttonHeight = MainMenuLayout.ButtonHeight * scale;

            renderer.Rectangle(x + 2 * scale, y + 3 * scale, buttonWidth, buttonHeight, MainMenuStyle.Shadow);
            renderer.Rectangle(x, y, buttonWidth, buttonHeight, MainMenuStyle.Border);
            renderer.Rectangle(x + scale, y + scale, buttonWidth - 2 * scale, buttonHeight - 2 * scale, MainMenuStyle.Highlight);
            renderer.Rectangle(x + 2 * scale, y + 3 * scale, buttonWidth - 3 * scale, buttonHeight - 4 * scale, MainMenuStyle.Shade);
            renderer.Rectangle(x + 2 * scale, y + 3 * scale, buttonWidth - 4 * scale, buttonHeight - 6 * scale, MainMenuStyle.Button);
            renderer.CenteredText(ButtonLabels[index], center, y + 8 * scale, scale, MainMenuStyle.Text);
        }

        renderer.End();
    }
}
