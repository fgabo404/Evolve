// Composes the approved Laboratory sidebar, stage list, information cards and map empty state.
// Retains Evolve's blue palette and bitmap font; only completed published data is presented as a map.
using Evolve.Engine.WorldGeneration;
using Evolve.Definitions.WorldGeneration;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Evolve.Game;

internal static class LaboratoryPresentation
{
    public static readonly Color4 Background = new(0.075f, 0.09f, 0.10f, 1);

    public static void Draw(PixelMenuRenderer renderer, LaboratorySession session,
        LaboratoryLayout layout, int width, int height, LaboratoryUiState ui)
    {
        var view = layout.Viewport;
        renderer.Begin(width, height);
        renderer.Border((float)view.X, (float)view.Y, (float)view.Width, (float)view.Height, 1, MainMenuStyle.Border);
        if (session.Grid is null)
        {
            double center = view.X + view.Width / 2;
            Line(renderer, "MAPA", center, view.Y + view.Height / 2 - 28 * layout.Scale, view.Width - 16, 2 * layout.Scale);
            string prompt = "CREE UNA SESION Y EJECUTE GRID";
            Lines(renderer, prompt, new(view.X + 8, view.Y + view.Height / 2, view.Width - 16, 50 * layout.Scale), layout.Scale, true);
        }
        else
            Lines(renderer, session.SelectedView == GenerationStage.Tectonics
                ? $"RUEDA: ZOOM | DERECHO: MOVER | F: AJUSTAR | B: LIMITES {(session.ShowBoundaries ? "SI" : "NO")} | V: FLECHAS {(session.ShowMotion ? "SI" : "NO")}" : "RUEDA: ZOOM | DERECHO: MOVER | F: AJUSTAR",
                new(view.X + 8, view.Y + view.Height - 22 * layout.Scale, view.Width - 16, 20 * layout.Scale), layout.Scale, true);
        renderer.Rectangle(0, 0, (float)layout.SidebarWidth, height, MainMenuStyle.Background);
        renderer.End();

        // Sidebar contents scroll as a single clipped column; back navigation stays outside the clip.
        var clip = layout.SidebarClip;
        GL.Enable(EnableCap.ScissorTest);
        try
        {
            GL.Scissor((int)clip.X, (int)(height - clip.Y - clip.Height), (int)clip.Width, (int)clip.Height);
            renderer.Begin(width, height);
            Label(renderer, "GENERACION DE MUNDO", layout.Card(0, 18), 2 * layout.Scale);
            Label(renderer, "LABORATORIO", layout.Card(20, 12), layout.Scale);
            Label(renderer, "TIPO DE MAPA", layout.Card(40, 10), layout.Scale);
            Label(renderer, "TAMANO", layout.Card(90, 10), layout.Scale);
            Label(renderer, "SEMILLA", layout.Card(140, 10), layout.Scale);
            Label(renderer, "VISTA", layout.Card(190, 10), layout.Scale);
            foreach (var action in LaboratoryLayout.Controls.Where(action => action != LaboratoryAction.Back))
            {
                string label = action switch
                {
                    LaboratoryAction.Size => session.Size.ToString(),
                    LaboratoryAction.WorldType => session.WorldType.ToString(),
                    LaboratoryAction.Seed => session.SeedText + (ui.EditingSeed ? "_" : ""),
                    LaboratoryAction.Randomize => "R",
                    LaboratoryAction.View => session.SelectedView?.ToString() ?? "SIN DATOS",
                    LaboratoryAction.Fit => "AJUSTAR",
                    LaboratoryAction.Create => "CREAR SESION",
                    _ => "CONFIGURACION"
                };
                Card(renderer, layout.Control(action), action == LaboratoryAction.Seed && ui.EditingSeed);
                Label(renderer, label, layout.Control(action), Math.Min(2, layout.Scale));
                if (action is LaboratoryAction.Size or LaboratoryAction.WorldType or LaboratoryAction.View)
                {
                    var field = layout.Control(action);
                    for (int row = 0; row < 3; row++)
                    {
                        double x = field.X + field.Width - (12 - row) * layout.Scale;
                        renderer.Rectangle((float)x, (float)(field.Y + (10 + row) * layout.Scale),
                            (float)layout.Scale, (float)layout.Scale, MainMenuStyle.Text);
                        renderer.Rectangle((float)(x + (4 - 2 * row) * layout.Scale), (float)(field.Y + (10 + row) * layout.Scale),
                            (float)layout.Scale, (float)layout.Scale, MainMenuStyle.Text);
                    }
                }
            }
            Card(renderer, layout.Card(312, 44));
            Lines(renderer, session.Notice, layout.Card(314, 40), layout.Scale);
            Label(renderer, "GENERACION MANUAL",
                layout.Card(366, 10), layout.Scale);
            foreach (var stage in Enum.GetValues<GenerationStage>())
            {
                var bounds = layout.Stage(stage);
                bool enabled = session.IsCompleted(stage) ||
                    (session.Context?.GetProgress().NextStage == stage && session.CanAdvance);
                Card(renderer, bounds, session.SelectedView == stage, enabled);
                Label(renderer, $"{(int)stage:00} - {stage}", new(bounds.X + 4, bounds.Y + 2, bounds.Width - 8, 9 * layout.Scale), layout.Scale);
                Label(renderer, session.StageStatus(stage), new(bounds.X + 4, bounds.Y + 12 * layout.Scale, bounds.Width - 8, 9 * layout.Scale), layout.Scale,
                    enabled ? MainMenuStyle.Text : MainMenuStyle.VersionText);
            }
            Card(renderer, layout.Card(610, layout.InformationHeight));
            Label(renderer, "INFORMACION DE CELDA", layout.Card(614, 12), layout.Scale);
            Lines(renderer, session.DescribeSelection(), layout.Card(632, layout.InformationHeight - 28), layout.Scale);
            if (ui.Dropdown is { } popup)
            {
                string[] options = popup switch
                {
                    LaboratoryAction.Size => Enum.GetNames<MapSize>(),
                    LaboratoryAction.WorldType => Enum.GetNames<WorldType>(),
                    _ => ["Grid", "Tectonics"]
                };
                for (int index = 0; index < options.Length; index++)
                {
                    var bounds = layout.DropdownOption(popup, index, options.Length);
                    Card(renderer, bounds, ui.HighlightedOption == index);
                    Label(renderer, options[index], bounds, layout.Scale,
                        popup == LaboratoryAction.View && !session.IsCompleted((GenerationStage)index) ? MainMenuStyle.VersionText : MainMenuStyle.Text);
                }
            }
            renderer.End();
        }
        finally { GL.Disable(EnableCap.ScissorTest); }

        renderer.Begin(width, height);
        Card(renderer, layout.Control(LaboratoryAction.Back));
        Label(renderer, "< SALIR AL MENU", layout.Control(LaboratoryAction.Back), layout.Scale);
        if (layout.MaximumScroll > 0)
        {
            double track = clip.Height;
            double thumb = Math.Max(18 * layout.Scale, track * track / (track + layout.MaximumScroll));
            double y = clip.Y + (track - thumb) * layout.Scroll / layout.MaximumScroll;
            renderer.Rectangle((float)(layout.SidebarWidth - 5 * layout.Scale), (float)y, (float)(2 * layout.Scale), (float)thumb, MainMenuStyle.Border);
        }
        renderer.End();
    }

    private static void Card(PixelMenuRenderer renderer, LaboratoryBounds bounds, bool selected = false, bool enabled = true)
    {
        renderer.Rectangle((float)bounds.X + 2, (float)bounds.Y + 2, (float)bounds.Width, (float)bounds.Height, MainMenuStyle.Shadow);
        renderer.Rectangle((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height, enabled ? MainMenuStyle.Button : Background);
        renderer.Border((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height, 1,
            selected ? MainMenuStyle.HighlightedBorder : MainMenuStyle.Border);
    }

    private static void Label(PixelMenuRenderer renderer, string text, LaboratoryBounds bounds, double pixel, Color4? color = null, bool centered = false)
    {
        double actual = Math.Min(pixel, Math.Max(1, Math.Floor((bounds.Width - 8) / Math.Max(1, text.Length * LegacyBitmapFont.Advance))));
        double center = centered ? bounds.X + bounds.Width / 2 : bounds.X + 6 + (text.Length * LegacyBitmapFont.Advance - 1) * actual / 2;
        renderer.CenteredText(text, (float)center,
            (float)(bounds.Y + (bounds.Height - LegacyBitmapFont.Height * actual) / 2), (float)actual, color ?? MainMenuStyle.Text);
    }

    private static void Line(PixelMenuRenderer renderer, string text, double center, double top, double width, double pixel) =>
        Label(renderer, text, new(center - width / 2, top, width, 16 * pixel), pixel, centered: true);

    private static void Lines(PixelMenuRenderer renderer, string text, LaboratoryBounds bounds, double pixel, bool centered = false)
    {
        int columns = Math.Max(1, (int)((bounds.Width - 12) / (LegacyBitmapFont.Advance * pixel)));
        int row = 0;
        foreach (var paragraph in text.Split('\n'))
        {
            for (int start = 0; start < paragraph.Length && (row + 1) * 10 * pixel <= bounds.Height; start += columns)
            {
                string part = paragraph.Substring(start, Math.Min(columns, paragraph.Length - start));
                double center = centered ? bounds.X + bounds.Width / 2 : bounds.X + 6 + (part.Length * LegacyBitmapFont.Advance - 1) * pixel / 2;
                renderer.CenteredText(part, (float)center, (float)(bounds.Y + row++ * 10 * pixel), (float)pixel, MainMenuStyle.Text);
            }
        }
    }
}
