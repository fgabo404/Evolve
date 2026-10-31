using System.IO.Compression;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;

/// <summary>Comparable medium-resolution surface captures, rendered directly from generated cells.</summary>
public static class ContinentalMapCaptures
{
    public static void Generate()
    {
        string directory = Path.Combine(Environment.CurrentDirectory, "continental-captures");
        Directory.CreateDirectory(directory);
        foreach (int seed in new[] { 2026, 42, -731, 9, 2 })
        {
            var world = new WorldGenerationService().Generate(
                LandmassProfiles.Create(GridSize.Medium, LandmassType.Continents, seed));
            int columns = world.Grid.Definition.Columns, rows = world.Grid.Definition.Rows;
            // Flat-top cells use staggered columns. Identical scale and palette across seeds.
            int width = columns * 3, height = rows * 4 + 2;
            var pixels = new byte[height * (1 + width * 3)];
            for (int y = 0; y < height; y++)
            {
                pixels[y * (1 + width * 3)] = 0;
                for (int x = 0; x < width; x++)
                    Paint(x, y, 40, 125, 181);
            }
            for (int id = 0; id < world.Landmass.SurfaceByCellId.Length; id++)
            {
                if (world.Landmass.SurfaceByCellId[id] == 0) continue;
                int x = 3 * (id % columns), y = 4 * (id / columns) + 2 * (id % columns % 2);
                for (int dy = 0; dy < 4; dy++)
                for (int dx = 0; dx < 3; dx++) Paint(x + dx, y + dy, 198, 164, 123);
            }
            string file = Path.Combine(directory, $"continents-medium-{seed}.png");
            WritePng(file, width, height, pixels);
            Console.WriteLine($"CAPTURE {file}: {world.Statistics.LandFraction:P2} land");

            void Paint(int x, int y, byte red, byte green, byte blue)
            {
                int offset = y * (1 + width * 3) + 1 + x * 3;
                pixels[offset] = red; pixels[offset + 1] = green; pixels[offset + 2] = blue;
            }
        }
    }

    private static void WritePng(string path, int width, int height, byte[] pixels)
    {
        using var file = File.Create(path);
        file.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; header[9] = 2;
        Chunk(file, "IHDR", header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(pixels);
        Chunk(file, "IDAT", compressed.ToArray());
        Chunk(file, "IEND", []);
    }

    private static void Chunk(Stream output, string type, byte[] payload)
    {
        var name = System.Text.Encoding.ASCII.GetBytes(type);
        Span<byte> number = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(number, payload.Length);
        output.Write(number); output.Write(name); output.Write(payload);
        uint crc = 0xffffffff;
        foreach (byte value in name.Concat(payload))
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = crc >> 1 ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        output.Write(number);
    }
}
