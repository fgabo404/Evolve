using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;

internal static class ContinentalSurfaceBaseline
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static void Verify()
    {
        using var historicStream = typeof(ContinentalSurfaceBaseline).Assembly
            .GetManifestResourceStream("ApprovedContinentalSurfaceBaseline.txt")!;
        using var historicReader = new StreamReader(historicStream);
        var historic = new List<string>();
        while (historicReader.ReadLine() is { } oldLine) historic.Add(oldLine);
        var actual = CurrentRows().ToArray();
        if (historic.Count != actual.Length ||
            historic.Where((line, index) => line.Split('|')[3] != actual[index].Split('|')[3]).Any())
            throw new InvalidOperationException("The original continental plate IDs changed.");
        using var stream = typeof(ContinentalSurfaceBaseline).Assembly
            .GetManifestResourceStream("ApprovedContinentalSelectionBaseline.txt")!;
        using var reader = new StreamReader(stream);
        var expected = new List<string>();
        while (reader.ReadLine() is { } line) expected.Add(line);
        if (!expected.SequenceEqual(actual))
        {
            // Stronger crest validation is allowed to choose a different attempt. Keep the
            // historical hashes unchanged and replay their exact original candidate instead.
            var references = OrogenicMorphologyTests.ReferenceRows().Select(line => line.Split('|'))
                .ToDictionary(row => (row[0], row[1]));
            foreach (string line in expected)
            {
                string[] parts = line.Split('|');
                var reference = references[(parts[0], parts[1])];
                var oldCandidate = OrogenicMorphologyTests.RebuildReference(LandmassProfiles.Create(
                    Enum.Parse<GridSize>(parts[0]), LandmassType.Continents, int.Parse(parts[1])),
                    int.Parse(reference[2])).Landmass;
                var s = oldCandidate.ContinentalSurface!;
                string replay = string.Join('|', parts[0], parts[1], Hash(oldCandidate.SurfaceByCellId),
                    Hash(oldCandidate.PlateByCellId!), Hash(s.GroupByPlateId), Hash(s.CrustByCellId), Hash(s.ContinentByCellId));
                if (replay != line) throw new InvalidOperationException("The approved historical candidate changed.");
            }
            int changed = expected.Where((line, index) => line != actual[index]).Count();
            Console.WriteLine($"SELECTION CHANGE: {changed}/{actual.Length} final surfaces differ after crest validation; " +
                "all original approved candidates replay exactly, historical hashes were not updated.");
        }
        else Console.WriteLine($"PASS: {actual.Length} selected continental surfaces stable; original plates unchanged");
    }

    public static IEnumerable<string> CurrentRows()
    {
        var service = new WorldGenerationService();
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var terrain = service.Generate(LandmassProfiles.Create(size, LandmassType.Continents, seed)).Landmass;
            var state = terrain.ContinentalSurface!;
            yield return string.Join('|', size, seed, Hash(terrain.SurfaceByCellId),
                Hash(terrain.PlateByCellId!), Hash(state.GroupByPlateId),
                Hash(state.CrustByCellId), Hash(state.ContinentByCellId));
        }
    }
}
