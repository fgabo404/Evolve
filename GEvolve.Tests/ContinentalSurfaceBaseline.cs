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
            throw new InvalidOperationException("The selected continental surface, crust, groups or identity changed.");
        Console.WriteLine($"PASS: {actual.Length} selected continental surfaces stable; original plates unchanged");
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
