// Composes the approved main-menu visuals and footer notices.
// Uses layout, style and interaction state to submit drawing to PixelMenuRenderer.

namespace Evolve.Game;

internal sealed class MainMenuPresentation
{
    private static readonly string[] ButtonLabels = ["INICIAR", "LAB", "OPCIONES", "SALIR"];

    // Keep normal visuals unchanged and apply only subtle input feedback and footer notices.
    public void Draw(PixelMenuRenderer renderer, int width, int height, MainMenuInteraction interaction)
    {
        var layout = MainMenuLayout.Create(width, height);
        float scale = layout.Scale;

        renderer.Begin(width, height);
        // Anchor the footer to the bottom and keep its divider below button shadows.
        float buttonsBottom = layout.ButtonTop + ButtonLabels.Length * layout.ButtonHeight
            + (ButtonLabels.Length - 1) * layout.ButtonGap + 4 * scale;
        float footerTop = Math.Min(height, Math.Max(height * .88f, buttonsBottom));
        float footerHeight = height - footerTop;
        renderer.Rectangle(0, footerTop, width, footerHeight, MainMenuStyle.BackgroundBand);
        renderer.Rectangle(0, footerTop, width, Math.Min(2 * scale, footerHeight), MainMenuStyle.BackgroundDivider);

        renderer.CenteredText("EVOLVE", layout.Center, layout.TitleTop, layout.TitlePixel, MainMenuStyle.Text);
        renderer.CenteredText(GameVersion.Display, layout.Center, layout.VersionTop, layout.VersionPixel, MainMenuStyle.VersionText);

        for (int index = 0; index < ButtonLabels.Length; index++)
        {
            var button = (MainMenuButton)index;
            float x = layout.ButtonLeft;
            float y = layout.ButtonY(button);
            float buttonWidth = layout.ButtonWidth;
            float buttonHeight = layout.ButtonHeight;
            float textPixel = Math.Min(2.5f * scale,
                (buttonWidth - 16 * scale) / (ButtonLabels[index].Length * LegacyBitmapFont.Advance));

            renderer.Rectangle(x + 3 * scale, y + 4 * scale, buttonWidth, buttonHeight, MainMenuStyle.Shadow);
            var fill = interaction.IsPressed(button) ? MainMenuStyle.PressedButton
                : interaction.IsHighlighted(button) ? MainMenuStyle.HighlightedButton : MainMenuStyle.Button;
            var border = interaction.IsHighlighted(button) ? MainMenuStyle.HighlightedBorder : MainMenuStyle.Border;
            renderer.Rectangle(x, y, buttonWidth, buttonHeight, fill);
            renderer.Border(x, y, buttonWidth, buttonHeight, 1.5f * scale, border);
            renderer.CenteredText(ButtonLabels[index], layout.Center,
                y + (buttonHeight - LegacyBitmapFont.Height * textPixel) / 2, textPixel, MainMenuStyle.Text);
        }

        if (interaction.FooterMessage is { } message && footerHeight > 4 * scale)
        {
            float pixel = Math.Min(2 * scale,
                (width - 32 * scale) / (message.Length * LegacyBitmapFont.Advance - 1));
            pixel = Math.Min(pixel, (footerHeight - 4 * scale) / LegacyBitmapFont.Height);
            renderer.CenteredText(message, layout.Center,
                footerTop + (footerHeight - LegacyBitmapFont.Height * pixel) / 2,
                pixel, MainMenuStyle.Text);
        }

        renderer.End();
    }
}
