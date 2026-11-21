// Freezes complete production R buffers and checks them against independent region checks and archived CAL01 evidence.
// Export is explicit and separate from normal tests; historical P0/experimental revisions are never treated as final R.
using System.Globalization;
using System.Text;
using System.Text.Json;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicFrozenVectors
{
    private sealed record Vector(string Size, string Seed, string File, string Digest,
        int[] Origins, int[] Areas, uint[] GrowthCostBits);
    private sealed record Manifest(string Algorithm, uint SchemaVersion, uint PartitionRevision, string WordEncoding,
        string Provenance, Vector[] Vectors);
    private sealed record CalCase(string Profile, string Size, string Seed, int[] Areas, int[] Origins, float[] Costs, string Digest);

    internal static void Export(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (File.Exists(Path.Combine(directory, "manifest.json")))
            throw new InvalidOperationException("Frozen vectors already exist; never overwrite them during a normal check.");
        Directory.CreateDirectory(directory);
        var vectors = new List<Vector>();
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            // Generate uses the actual registered HexGridLayer and TectonicPlatesLayer through WG-CORE.
            var (grid, data) = Generate(size, seed);
            var view = TectonicData.FromLayerData(data, grid);
            string filename = $"{size}-{seed.ToString(CultureInfo.InvariantCulture)}.words";
            using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, filename))))
                foreach (uint word in data) writer.Write(word);
            vectors.Add(new(size.ToString(), seed.ToString(CultureInfo.InvariantCulture), filename,
                TectonicCalibrationChecks.Digest(data),
                Enumerable.Range(0, 14).Select(id => view.GetPlate((byte)id).SeedCellId).ToArray(),
                Enumerable.Range(0, 14).Select(id => view.GetPlate((byte)id).CellCount).ToArray(),
                Enumerable.Range(0, 14).Select(id => BitConverter.SingleToUInt32Bits(view.GetPlate((byte)id).GrowthCost)).ToArray()));
        }
        var manifest = new Manifest(TectonicGenerationProfile.AlgorithmVersion, 1, TectonicGenerationProfile.PartitionRevision,
            "little-endian uint32 words; complete schema-1 Ownership payload",
            "Generated explicitly from production R through WG-CORE after owner selection; not from CAL01 summary tables.",
            vectors.ToArray());
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(manifest,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Console.WriteLine($"EXPORTED {vectors.Count} complete production R vectors; revision={manifest.PartitionRevision}");
    }

    internal static void Verify()
    {
        string vectorDirectory = Path.Combine(AppContext.BaseDirectory, "vectors");
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(vectorDirectory, "manifest.json")))!;
        Require(manifest.Algorithm == "tectonic-partition-r-v2" && manifest.SchemaVersion == 1 && manifest.PartitionRevision == 2,
            "Frozen algorithm identity or schema changed");
        Require(manifest.Vectors.Length == 15 && manifest.Vectors.Select(row => (row.Size, row.Seed)).Distinct().Count() == 15,
            "Incomplete frozen vector inventory");
        foreach (var row in manifest.Vectors)
        {
            var size = Enum.Parse<MapSize>(row.Size);
            long seed = long.Parse(row.Seed, CultureInfo.InvariantCulture);
            var (grid, actual) = Generate(size, seed);
            using var stream = File.OpenRead(Path.Combine(vectorDirectory, row.File));
            Require(stream.Length == actual.Count * 4L, "Frozen word length");
            using var reader = new BinaryReader(stream);
            // Compare every word to a frozen real buffer, not just a summary or hash.
            for (int index = 0; index < actual.Count; index++)
                Require(reader.ReadUInt32() == actual[index], $"Frozen R word mismatch: {row.Size}/{row.Seed}/{index}");
            Require(TectonicCalibrationChecks.Digest(actual) == row.Digest, "Frozen textual digest");
            var view = TectonicData.FromLayerData(actual, grid);
            for (byte plate = 0; plate < 14; plate++)
                Require(view.GetPlate(plate).SeedCellId == row.Origins[plate] &&
                    view.GetPlate(plate).CellCount == row.Areas[plate] &&
                    BitConverter.SingleToUInt32Bits(view.GetPlate(plate).GrowthCost) == row.GrowthCostBits[plate],
                    "Frozen metadata vector mismatch");
        }

        string historyDirectory = Path.Combine(AppContext.BaseDirectory, "experiments", "cal01");
        using (var profiles = JsonDocument.Parse(File.ReadAllText(Path.Combine(historyDirectory, "profiles.json"))))
        {
            var approved = profiles.RootElement.EnumerateArray().Single(row => row.GetProperty("Id").GetString() == "R");
            uint[] costBits = approved.GetProperty("Costs").EnumerateArray()
                .Select(value => BitConverter.SingleToUInt32Bits(value.GetSingle())).ToArray();
            Require(costBits.SequenceEqual(Enumerable.Range(0, 14).Select(rank =>
                BitConverter.SingleToUInt32Bits(TectonicGenerationProfile.Approved.GetCost(rank)))), "Production costs differ from exact CAL01 R");
            Require(approved.GetProperty("CandidateSamples").GetInt32() == TectonicGenerationProfile.Approved.CandidateSamples &&
                approved.GetProperty("Irregularity").GetDouble() == TectonicGenerationProfile.Approved.Irregularity &&
                approved.GetProperty("WarpFraction").GetDouble() == TectonicGenerationProfile.Approved.WarpFraction &&
                approved.GetProperty("MinimumResistance").GetDouble() == TectonicGenerationProfile.Approved.MinimumResistance &&
                approved.GetProperty("MaximumResistance").GetDouble() == TectonicGenerationProfile.Approved.MaximumResistance &&
                approved.GetProperty("BroadWeight").GetDouble() == TectonicGenerationProfile.Approved.BroadWeight &&
                approved.GetProperty("MediumWeight").GetDouble() == TectonicGenerationProfile.Approved.MediumWeight &&
                approved.GetProperty("FineWeight").GetDouble() == TectonicGenerationProfile.Approved.FineWeight &&
                approved.GetProperty("Frequencies").EnumerateArray().Select(value => value.GetInt32())
                    .SequenceEqual(TectonicGenerationProfile.Approved.Frequencies.ToArray()), "Production constants differ from CAL01 R");
        }
        var cases = JsonSerializer.Deserialize<CalCase[]>(File.ReadAllText(Path.Combine(historyDirectory, "cases.json")))!
            .Where(row => row.Profile == "R").ToArray();
        Require(cases.Length == 75 && cases.Select(row => (row.Size, row.Seed)).Distinct().Count() == 75, "Incomplete historical R sample");
        foreach (var row in cases)
        {
            var size = Enum.Parse<MapSize>(row.Size);
            long seed = long.Parse(row.Seed, CultureInfo.InvariantCulture);
            var (grid, actual) = Generate(size, seed);
            var orchestrator = Orchestrator();
            var context = orchestrator.CreateGeneration(Request(size, seed, WorldType.RealEarth));
            orchestrator.RunNext(context); orchestrator.RunNext(context);
            Require(actual.SequenceEqual(Published(context)), "Complete R replay differs across execution mode/world type");
            var view = TectonicData.FromLayerData(actual, grid);
            CheckRegions(grid, view);
            for (byte plate = 0; plate < 14; plate++)
                Require(view.GetPlate(plate).CellCount == row.Areas[plate] &&
                    view.GetPlate(plate).SeedCellId == row.Origins[plate] &&
                    view.GetPlate(plate).GrowthCost == row.Costs[plate], "Production R differs from CAL01 plate metadata");
            // Only the documented revision word differs from the archived experiment; normalize a private copy.
            uint[] historicalEncoding = actual.ToArray();
            historicalEncoding[16] = 0xCA010003;
            Require(TectonicCalibrationChecks.Digest(historicalEncoding) == row.Digest, "Production partition differs from CAL01 R");
        }
        Console.WriteLine("FROZEN R: 15 full golden buffers; 75 CAL01 R cases matched; 75 full mode/type replays and independent BFS checks");
    }
}
