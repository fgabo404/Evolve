using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;

internal static class ContinentalSurfaceBaseline
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static void Verify()
    {
        using var stream = typeof(ContinentalSurfaceBaseline).Assembly
            .GetManifestResourceStream("ApprovedContinentalSurfaceBaseline.txt")!;
        using var reader = new StreamReader(stream);
        var expected = new List<string>();
        while (reader.ReadLine() is { } line) expected.Add(line);
        var actual = Rows().ToArray();
        if (!expected.SequenceEqual(actual))
            throw new InvalidOperationException("Approved continental surface, crust, groups, identity or plates changed.");
        Console.WriteLine($"PASS: {actual.Length} approved continental surfaces, crust maps and identities unchanged");
    }

    private static IEnumerable<string> Rows()
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
