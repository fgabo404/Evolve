// Exports and checks losslessly compressed complete Influence reference rows, separate from published R/M data.
// Binds all world types to exact raw-field hashes and unchanged historical B/M hashes for review.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceVectors
{
    internal sealed record Case(string Size, long Seed, string WorldType, string File, int CellCount, int RawBytes,
        string Sha256, string CompressedSha256, string OwnershipSha256, string MotionSha256);
    internal sealed record Manifest(string Profile, uint Revision, string FractionBits, string MinimumBits, string CutoffBits,
        string Encoding, string Status, Case[] Cases);

    internal static byte[] Serialize(TectonicInfluenceFields fields, TectonicBoundaryDistances distances)
    {
        byte[] bytes = new byte[checked(fields.Count * 16)];
        for (int cell = 0; cell < fields.Count; cell++)
        {
            var value = fields[cell];
            Write(0, distances[cell]); Write(4, BitConverter.SingleToUInt32Bits(value.Compression));
            Write(8, BitConverter.SingleToUInt32Bits(value.Extension)); Write(12, BitConverter.SingleToUInt32Bits(value.Shear));
            void Write(int offset, uint word) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell * 16 + offset), word);
        }
        return bytes;
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static void Export(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any()) throw new ArgumentException("Reference export requires an empty directory.");
        TectonicInfluenceFinalChecks.PolicyAndDense();
        Directory.CreateDirectory(directory);
        var cases = new List<Case>();
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in TectonicInfluenceFinalChecks.Seeds)
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var reference = TectonicInfluenceOracle.Extract(grid, packed);
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            TectonicInfluenceOracle.CheckSources(reference, sources);
            var fields = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources);
            TectonicInfluenceOracle.Compare(TectonicInfluenceOracle.Compute(reference, fields.Parameters), fields, distances);
            fields.ValidateComplete(sources, distances);
            var raw = Serialize(fields, distances);
            string file = $"{size}-{seed}.influence.br";
            using (var output = File.Create(Path.Combine(directory, file)))
            using (var compressed = new BrotliStream(output, CompressionLevel.SmallestSize)) compressed.Write(raw);
            string compressedHash = Hash(File.ReadAllBytes(Path.Combine(directory, file)));
            string ownershipHash = VerifyLegacy(packed, size, seed, ownership: true), motionHash = VerifyLegacy(packed, size, seed, ownership: false);
            foreach (var type in Enum.GetValues<WorldType>())
            {
                var (_, replay) = GenerateComplete(size, seed, type);
                Require(packed.SequenceEqual(replay), "World type alters R/M during export.");
                var replaySources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(replay, grid));
                var replayFields = TectonicInfluenceFields.Build(replaySources, TectonicInfluenceProfile.Resolve(grid));
                Require(raw.SequenceEqual(Serialize(replayFields, TectonicBoundaryDistances.Build(replaySources))), "World-type Influence mismatch.");
                cases.Add(new(size.ToString(), seed, type.ToString(), file, grid.CellCount, raw.Length, Hash(raw), compressedHash, ownershipHash, motionHash));
            }
            Console.WriteLine($"INFLUENCE EXPORT {size}/{seed}: complete oracle passed; SHA256={Hash(raw)}");
        }
        var manifest = new Manifest(TectonicInfluenceProfile.Identifier, TectonicInfluenceProfile.Revision, "3CA3D70A", "3F800000", "40400000",
            "Brotli; decoded CellId-order uintLE distance,float32LE compression,extension,shear; 16 bytes/cell; no publication schema header",
            "Review reference; definitive profile acceptance pending independent audit and owner decision", cases.ToArray());
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("INFLUENCE EXPORT: 15 unique full buffers; 60 size/seed/world-type cases; no R/M fixture regenerated.");
    }

    internal static void Verify()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "influence-vectors");
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")))!;
        Require(manifest.Profile == TectonicInfluenceProfile.Identifier && manifest.Revision == TectonicInfluenceProfile.Revision &&
            manifest.FractionBits == "3CA3D70A" && manifest.MinimumBits == "3F800000" && manifest.CutoffBits == "40400000", "Reference policy mismatch.");
        var expectedKeys = (from size in Enum.GetValues<MapSize>() from seed in TectonicInfluenceFinalChecks.Seeds
            from type in Enum.GetValues<WorldType>() select $"{size}/{seed}/{type}").ToHashSet();
        Require(manifest.Cases.Length == expectedKeys.Count && manifest.Cases.Select(row => $"{row.Size}/{row.Seed}/{row.WorldType}").ToHashSet().SetEquals(expectedKeys), "Missing/duplicate reference case.");
        foreach (var row in manifest.Cases)
        {
            var size = Enum.Parse<MapSize>(row.Size); var type = Enum.Parse<WorldType>(row.WorldType);
            Require(row.File == $"{size}-{row.Seed}.influence.br", "Unexpected fixture path.");
            var compressed = File.ReadAllBytes(Path.Combine(directory, row.File));
            Require(Hash(compressed) == row.CompressedSha256, "Compressed fixture checksum.");
            var (grid, packed) = GenerateComplete(size, row.Seed, type);
            Require(row.CellCount == grid.CellCount && row.RawBytes == checked(grid.CellCount * 16), "Fixture dimensions.");
            byte[] expected = new byte[row.RawBytes];
            using (var input = new MemoryStream(compressed))
            using (var decoder = new BrotliStream(input, CompressionMode.Decompress))
            {
                decoder.ReadExactly(expected);
                Require(decoder.ReadByte() == -1, "Trailing decoded fixture data.");
            }
            Require(Hash(expected) == row.Sha256, "Raw reference checksum.");
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var fields = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources);
            Require(expected.SequenceEqual(Serialize(fields, distances)), "Complete Influence reference differs.");
            Require(VerifyLegacy(packed, size, row.Seed, true) == row.OwnershipSha256 &&
                VerifyLegacy(packed, size, row.Seed, false) == row.MotionSha256, "R/M reference binding differs.");
            Require(packed[1] == 1 && packed[2] == 7, "Influence activated in normal publication.");
        }
        Console.WriteLine("INFLUENCE VECTORS: all 60 cases matched every decoded byte and SHA-256; original 15 B + 15 M buffers unchanged.");
    }

    private static string VerifyLegacy(LayerData<uint> packed, MapSize size, long seed, bool ownership)
    {
        byte[] expected = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, ownership ? "vectors" : "motion-vectors", $"{size}-{seed}.words"));
        var values = ownership ? OwnershipOnly(packed) : packed;
        byte[] bytes = new byte[checked(values.Count * 4)];
        for (int index = 0; index < values.Count; index++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * 4), values[index]);
        Require(expected.SequenceEqual(bytes), "Original R/M vector differs; do not regenerate it.");
        return Hash(bytes);
    }
}
