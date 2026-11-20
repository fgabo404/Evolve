// Composes Laboratory controls, real progress and selected-cell information using the existing bitmap font.
// The separate static grid renderer draws the viewport without rebuilding cell geometry each frame.
using OpenTK.Mathematics;

namespace Evolve.Game;

internal static class LaboratoryPresentation
{
    public static readonly Color4 Background = new(0.075f, 0.09f, 0.10f, 1);

    public static void Draw(PixelMenuRenderer renderer, LaboratorySession session,
        LaboratoryLayout layout, int width, int height, bool editingSeed)
    {
        renderer.Begin(width, height);
        renderer.Rectangle(0, 0, width, 178, MainMenuStyle.Background);
        renderer.Rectangle(0, height - 55, width, 55, MainMenuStyle.Background);
        Text(renderer, "LABORATORIO - GRID", width / 2f, 12, width - 24, 2);
        foreach (var action in Enum.GetValues<LaboratoryAction>())
        {
            var bounds = layout.Control(action);
            string label = action switch
            {
                LaboratoryAction.Size => $"TAMANO: {session.Size}",
                LaboratoryAction.WorldType => $"TIPO: {session.WorldType}",
                LaboratoryAction.Seed => $"SEED: {session.SeedText}" + (editingSeed ? "_" : ""),
                LaboratoryAction.Fit => "AJUSTAR (F)",
                LaboratoryAction.Create => "CREAR SESION",
                LaboratoryAction.RunGrid => "GRID: RUNNEXT",
                _ => "VOLVER (ESC)"
            };
            bool enabled = action != LaboratoryAction.RunGrid || session.CanRunGrid;
            renderer.Rectangle((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height,
                enabled ? MainMenuStyle.Button : Background);
            renderer.Border((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height, 1,
                editingSeed && action == LaboratoryAction.Seed ? MainMenuStyle.HighlightedBorder : MainMenuStyle.Border);
            Text(renderer, label, (float)(bounds.X + bounds.Width / 2), (float)bounds.Y + 9, (float)bounds.Width - 8, 2);
        }
        string status = $"GRID: {session.GridStatus?.ToString() ?? "SIN SESION"} | ETAPAS 01-07: NO DISPONIBLES";
        Text(renderer, status, width / 2f, 158, width - 24, 2);
        var view = layout.Viewport;
        renderer.Border((float)view.X, (float)view.Y, (float)view.Width, (float)view.Height, 1, MainMenuStyle.Border);
        string information = "CLIC: CELDA | RUEDA: ZOOM | DERECHO: MOVER | F: AJUSTAR";
        if (session.SelectedCell is int id && session.Grid is { } grid)
        {
            var cell = grid.GetOffset(id);
            var geo = grid.GetGeographicCenter(id);
            information = FormattableString.Invariant($"ID {id} COL {cell.Column} FILA {cell.Row} LON {geo.Longitude:F3} LAT {geo.Latitude:F3}");
        }
        Text(renderer, information, width / 2f, height - 48, width - 24, 2);
        // Wrap notices at small client sizes rather than shrinking glyph pixels below one framebuffer pixel.
        int columns = Math.Max(1, (width - 24) / LegacyBitmapFont.Advance);
        for (int line = 0; line < 2 && line * columns < session.Notice.Length; line++)
        {
            string part = session.Notice.Substring(line * columns, Math.Min(columns, session.Notice.Length - line * columns));
            Text(renderer, part, width / 2f, height - 27 + line * 10, width - 24, 1);
        }
        renderer.End();
    }

    private static void Text(PixelMenuRenderer renderer, string value, float center, float top, float availableWidth, int maximumPixel)
    {
        int pixel = Math.Max(1, Math.Min(maximumPixel, (int)(availableWidth / Math.Max(1, value.Length * LegacyBitmapFont.Advance))));
        renderer.CenteredText(value, center, top, pixel, MainMenuStyle.Text);
    }
}
