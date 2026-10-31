using System.Text;
using GEvolve.Engine.World.LandmassGeneration;

internal static class ContinentalSurfaceDiagnostics
{
    private static readonly string[] Colors =
    ["#b64e52", "#d99b42", "#9bad47", "#50a269", "#3f9fa2", "#558bd2",
     "#8466bc", "#b26aa4", "#9d7054", "#658d53", "#547b9b", "#b27d70",
     "#777db4", "#b7974e", "#748e93", "#a36984"];

    public static void Export(string path, GeneratedWorld world)
    {
        var terrain = world.Landmass;
        var data = terrain.ContinentalSurface ?? throw new ArgumentException("Continental surface required.");
        var plates = terrain.PlateByCellId!;
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 3, panelWidth = columns * scale, panelHeight = rows * scale;
        int gap = 20, margin = 18, top = 54;
        int width = 2 * panelWidth + 3 * gap + 2 * margin;
        int height = 2 * panelHeight + top + 2 * gap + margin;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");
        svg.Append("<rect width='100%' height='100%' fill='#eef2f6'/>");
        svg.Append($"<text x='{margin}' y='26' font-family='Arial' font-size='18' fill='#19324a'>Continentes: {terrain.Configuration.Size} / semilla {terrain.Configuration.Seed}</text>");
        for (int panel = 0; panel < 4; panel++)
        {
            int left = margin + (panel % 2) * (panelWidth + gap);
            int yTop = top + (panel / 2) * (panelHeight + gap);
            string name = new[] { "1. PLACAS", "2. AGRUPACION", "3. CORTEZA", "4. SUPERFICIE" }[panel];
            svg.Append($"<text x='{left}' y='{yTop - 7}' font-family='Arial' font-size='13' fill='#23425c'>{name}</text>");
            for (int row = 0; row < rows; row++)
            {
                int start = 0;
                while (start < columns)
                {
                    string color = Color(panel, row * columns + start);
                    int end = start + 1;
                    while (end < columns && Color(panel, row * columns + end) == color) end++;
                    svg.Append($"<rect x='{left + start * scale}' y='{yTop + row * scale}' width='{(end - start) * scale}' height='{scale}' fill='{color}'/>");
                    start = end;
                }
            }
            string Color(int view, int id) => view switch
            {
                0 => Colors[plates[id]],
                1 => data.GroupByPlateId[plates[id]] == byte.MaxValue ? "#8e9baa" :
                    Colors[data.GroupByPlateId[plates[id]] * 2],
                2 => data.CrustByCellId[id] == 1 ? "#bd9362" : "#287db5",
                _ => terrain.SurfaceByCellId[id] == 1 ?
                    Colors[data.ContinentByCellId[id] * 2] : "#287db5"
            };
        }
        svg.Append("</svg>");
        File.WriteAllText(path, svg.ToString(), Encoding.UTF8);
    }

    public static void ExportBitmap(string path, GeneratedWorld world)
    {
        var terrain = world.Landmass;
        var data = terrain.ContinentalSurface!;
        var plates = terrain.PlateByCellId!;
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 2, gap = 12, panelWidth = columns * scale;
        int width = 4 * panelWidth + 3 * gap, height = rows * scale;
        int stride = (width * 3 + 3) & ~3;
        using var file = File.Create(path);
        using var writer = new BinaryWriter(file);
        writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(54 + stride * height);
        writer.Write(0); writer.Write(54); writer.Write(40);
        writer.Write(width); writer.Write(height); writer.Write((short)1); writer.Write((short)24);
        writer.Write(0); writer.Write(stride * height); writer.Write(2835); writer.Write(2835);
        writer.Write(0); writer.Write(0);
        var row = new byte[stride];
        for (int y = height - 1; y >= 0; y--)
        {
            Array.Fill(row, (byte)238);
            for (int panel = 0; panel < 4; panel++)
            for (int x = 0; x < panelWidth; x++)
            {
                int id = (y / scale) * columns + x / scale;
                string color = panel switch
                {
                    0 => Colors[plates[id]],
                    1 => data.GroupByPlateId[plates[id]] == byte.MaxValue ? "#8e9baa" :
                        Colors[data.GroupByPlateId[plates[id]] * 2],
                    2 => data.CrustByCellId[id] == 1 ? "#bd9362" : "#287db5",
                    _ => terrain.SurfaceByCellId[id] == 1 ?
                        Colors[data.ContinentByCellId[id] * 2] : "#287db5"
                };
                int rgb = Convert.ToInt32(color[1..], 16);
                int offset = (panel * (panelWidth + gap) + x) * 3;
                row[offset] = (byte)rgb;
                row[offset + 1] = (byte)(rgb >> 8);
                row[offset + 2] = (byte)(rgb >> 16);
            }
            writer.Write(row);
        }
    }
}
