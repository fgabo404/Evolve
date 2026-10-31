using System.Text;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;

internal static class ContinentalElevationDiagnostics
{
    public static void Export(string path, GeneratedWorld world)
    {
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 3, gap = 24, margin = 18, top = 54;
        int width = columns * scale * 3 + gap * 2 + margin * 2;
        int height = rows * scale + top + margin;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");
        svg.Append("<rect width='100%' height='100%' fill='#edf2f6'/>");
        svg.Append($"<text x='{margin}' y='26' font-family='Arial' font-size='18' fill='#19324a'>Continentes: {world.Landmass.Configuration.Size} / semilla {world.Landmass.Configuration.Seed}</text>");
        for (int panel = 0; panel < 3; panel++)
        {
            int left = margin + panel * (columns * scale + gap);
            string label = new[] { "SUPERFICIE", "VER ALTURAS: 0-100", "COMPRESION" }[panel];
            svg.Append($"<text x='{left}' y='{top - 7}' font-family='Arial' font-size='13' fill='#23425c'>{label}</text>");
            for (int row = 0; row < rows; row++)
            {
                int start = 0;
                while (start < columns)
                {
                    int rgb = Color(panel, row * columns + start, world.Landmass);
                    int end = start + 1;
                    while (end < columns && Color(panel, row * columns + end, world.Landmass) == rgb) end++;
                    svg.Append($"<rect x='{left + start * scale}' y='{top + row * scale}' width='{(end - start) * scale}' height='{scale}' fill='#{rgb:X6}'/>");
                    start = end;
                }
            }
        }
        svg.Append("</svg>");
        File.WriteAllText(path, svg.ToString(), Encoding.UTF8);
    }

    public static void ExportBitmap(string path, GeneratedWorld world)
    {
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 2, gap = 12, panelWidth = columns * scale;
        int width = panelWidth * 3 + gap * 2, height = rows * scale;
        int stride = (width * 3 + 3) & ~3;
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(54 + stride * height);
        writer.Write(0); writer.Write(54); writer.Write(40);
        writer.Write(width); writer.Write(height); writer.Write((short)1); writer.Write((short)24);
        writer.Write(0); writer.Write(stride * height); writer.Write(2835); writer.Write(2835);
        writer.Write(0); writer.Write(0);
        var line = new byte[stride];
        for (int row = height - 1; row >= 0; row--)
        {
            Array.Fill(line, (byte)238);
            for (int panel = 0; panel < 3; panel++)
            for (int x = 0; x < panelWidth; x++)
            {
                int rgb = Color(panel, (row / scale) * columns + x / scale, world.Landmass);
                int offset = (panel * (panelWidth + gap) + x) * 3;
                line[offset] = (byte)rgb;
                line[offset + 1] = (byte)(rgb >> 8);
                line[offset + 2] = (byte)(rgb >> 16);
            }
            writer.Write(line);
        }
    }

    private static int Color(int panel, int id, LandmassState terrain)
    {
        if (panel == 0) return terrain.SurfaceByCellId[id] == 1 ? 0x5D9A68 : 0x287DB5;
        if (panel == 1)
        {
            int height = terrain.ElevationByCellId![id];
            int gray = (int)Math.Round(height * 255.0 / 100);
            return (gray << 16) | (gray << 8) | gray;
        }
        var color = TectonicStressPalette.ForValue(terrain.TectonicMotion!.CompressionByCellId[id],
            MapViewMode.Compression);
        int red = (int)Math.Round(color.X * 255);
        int green = (int)Math.Round(color.Y * 255);
        int blue = (int)Math.Round(color.Z * 255);
        return red << 16 | green << 8 | blue;
    }
}
