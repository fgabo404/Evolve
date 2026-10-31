using System.Text;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.State.Game;

internal static class ContinentalElevationDiagnostics
{
    private static readonly int[] PlateColors =
    [
        0x506F9B, 0xA97955, 0x6D9B78, 0xAA7090, 0x927DB1, 0xA99C55,
        0x5D9AA1, 0xB77970, 0x78976B, 0x8F77A5, 0xB39476, 0x6489A9,
        0x9BA663, 0xA476A7, 0x618B82, 0x9A8064
    ];

    /// <summary>Six comparable diagnostic fields for the same generated world.</summary>
    public static void Export(string path, GeneratedWorld world)
    {
        var terrain = world.Landmass;
        var relief = terrain.ContinentalElevation!;
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 3, gap = 24, margin = 18, top = 58;
        int panelWidth = columns * scale;
        int width = panelWidth * 6 + gap * 5 + margin * 2;
        int height = rows * scale + top + margin;
        var axis = new byte[terrain.SurfaceByCellId.Length];
        foreach (var system in relief.Systems)
            foreach (int id in system.AxisCellIds)
                axis[id] = (byte)system.Kind;

        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>");
        svg.Append("<rect width='100%' height='100%' fill='#edf2f6'/>");
        svg.Append($"<text x='{margin}' y='26' font-family='Arial' font-size='18' fill='#19324a'>Continentes: {terrain.Configuration.Size} / semilla {terrain.Configuration.Seed}</text>");
        string[] labels = ["SUPERFICIE", "PLACAS Y MOVIMIENTO", "SISTEMAS TECTONICOS",
            "CORDILLERAS PRINCIPALES / SECUNDARIAS", "LEVANTAMIENTO / SUBSIDENCIA", "ELEVACION FINAL"];
        for (int panel = 0; panel < 6; panel++)
        {
            int left = margin + panel * (panelWidth + gap);
            svg.Append($"<text x='{left}' y='{top - 8}' font-family='Arial' font-size='12' fill='#23425c'>{labels[panel]}</text>");
            for (int row = 0; row < rows; row++)
            {
                int start = 0;
                while (start < columns)
                {
                    int rgb = Color(panel, row * columns + start, terrain, axis);
                    int end = start + 1;
                    while (end < columns && Color(panel, row * columns + end, terrain, axis) == rgb) end++;
                    svg.Append($"<rect x='{left + start * scale}' y='{top + row * scale}' width='{(end - start) * scale}' height='{scale}' fill='#{rgb:X6}'/>");
                    start = end;
                }
            }
            if (panel == 1)
                foreach (var motion in terrain.TectonicMotion!.Motions)
                {
                    int id = terrain.TectonicMotion.ArrowCellByPlateId[motion.PlateId];
                    int x = left + (id % columns) * scale;
                    int y = top + (id / columns) * scale;
                    double dx = motion.UnitVector.X * 13;
                    double dy = motion.UnitVector.Y * 13;
                    svg.Append(FormattableString.Invariant($"<line x1='{x}' y1='{y}' x2='{(x + dx):F1}' y2='{(y + dy):F1}' stroke='#10233a' stroke-width='2'/>"));
                    svg.Append(FormattableString.Invariant($"<circle cx='{(x + dx):F1}' cy='{(y + dy):F1}' r='2' fill='#10233a'/>"));
                }
        }
        svg.Append("</svg>");
        File.WriteAllText(path, svg.ToString(), Encoding.UTF8);
    }

    public static void ExportBitmap(string path, GeneratedWorld world)
    {
        int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
        int scale = 2, gap = 12, panelWidth = columns * scale;
        int width = panelWidth * 6 + gap * 5, height = rows * scale;
        int stride = (width * 3 + 3) & ~3;
        var axis = new byte[world.Landmass.SurfaceByCellId.Length];
        foreach (var system in world.Landmass.ContinentalElevation!.Systems)
            foreach (int id in system.AxisCellIds) axis[id] = (byte)system.Kind;
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
            for (int panel = 0; panel < 6; panel++)
            for (int x = 0; x < panelWidth; x++)
            {
                int rgb = Color(panel, (row / scale) * columns + x / scale,
                    world.Landmass, axis);
                int offset = (panel * (panelWidth + gap) + x) * 3;
                line[offset] = (byte)rgb;
                line[offset + 1] = (byte)(rgb >> 8);
                line[offset + 2] = (byte)(rgb >> 16);
            }
            writer.Write(line);
        }
    }

    private static int Color(int panel, int id, LandmassState terrain, byte[] axis)
    {
        var relief = terrain.ContinentalElevation!;
        if (panel == 0) return terrain.SurfaceByCellId[id] == 1 ? 0x5D9A68 : 0x287DB5;
        if (panel == 1) return PlateColors[terrain.PlateByCellId![id] % PlateColors.Length];
        if (panel == 2) return (GeologicalContactKind)axis[id] switch
        {
            GeologicalContactKind.ContinentalCollision => 0xB52B32,
            GeologicalContactKind.OceanContinentSubduction => 0xC66D22,
            GeologicalContactKind.OceanOceanSubduction => 0xD3A52C,
            GeologicalContactKind.ContinentalRift => 0x6B39B7,
            GeologicalContactKind.OceanRidge => 0x1D849C,
            GeologicalContactKind.Transform => 0xB74795,
            _ => terrain.SurfaceByCellId[id] == 1 ? 0xD2D7D1 : 0xB8CED9
        };
        if (panel == 3) return (TectonicMountainTier)relief.MountainTierByCellId[id] switch
        {
            TectonicMountainTier.Principal => 0x9F263D,
            TectonicMountainTier.Secondary => 0xDA7A35,
            TectonicMountainTier.Minor => 0xDFC986,
            _ => terrain.SurfaceByCellId[id] == 1 ? 0xD2D7D1 : 0xB8CED9
        };
        if (panel == 4)
        {
            int up = relief.UpliftByCellId[id];
            int down = relief.SubsidenceByCellId[id];
            if (up >= down && up > 0) return 0xE8E8E8 - (Math.Min(200, up * 2) << 8) - Math.Min(200, up * 2);
            if (down > 0) return 0xE8E8E8 - (Math.Min(200, down * 4) << 16) - (Math.Min(200, down * 4) << 8);
            return 0xE8E8E8;
        }
        int gray = (int)Math.Round(terrain.ElevationByCellId![id] * 255.0 / 100);
        return (gray << 16) | (gray << 8) | gray;
    }
}
