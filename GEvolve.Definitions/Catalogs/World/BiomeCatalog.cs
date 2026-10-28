using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GEvolve.Definitions.Catalogs.World;

public sealed record BiomeRule(string BiomeId, SubbiomeDefinition Subbiome);

/// <summary>Los rangos ejecutables proceden exclusivamente de biomes.json.</summary>
public sealed class BiomeCatalog
{
    private static readonly Lazy<BiomeCatalog> DefaultCatalog = new(() =>
    {
        using var stream = typeof(BiomeCatalog).Assembly.GetManifestResourceStream(
            "Evolve.biomes.json") ?? throw new InvalidDataException("Missing biomes.json resource.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    });

    public static BiomeCatalog Default => DefaultCatalog.Value;
    public IReadOnlyList<BiomeRule> Rules { get; }
    public string ContentHash { get; }

    private BiomeCatalog(List<BiomeRule> rules, string hash)
        => (Rules, ContentHash) = (rules.AsReadOnly(), hash);

    public static BiomeCatalog Parse(string json)
    {
        var source = JsonSerializer.Deserialize<WorldCatalog>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Empty biome catalog.");
        var rules = new List<BiomeRule>();
        var biomes = new HashSet<string>(StringComparer.Ordinal);
        var subbiomes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var biome in source.Biomes)
        {
            if (string.IsNullOrWhiteSpace(biome.Id) || !biomes.Add(biome.Id) ||
                biome.Subbiomes.Count == 0)
                throw new InvalidDataException("Invalid or duplicate biome.");
            foreach (var sub in biome.Subbiomes)
            {
                if (string.IsNullOrWhiteSpace(sub.Id) || !subbiomes.Add(sub.Id) ||
                    sub.ElevationLevel is not { } elevation ||
                    sub.LatitudeDegrees is not { } latitude ||
                    !ValidRange(elevation.Min, elevation.Max, 100) ||
                    !ValidRange(latitude.Min, latitude.Max, 90) ||
                    latitude.Hemisphere is not ("either" or "north" or "south"))
                    throw new InvalidDataException("Invalid subbiome ranges or hemisphere.");
                rules.Add(new(biome.Id, sub));
            }
        }
        if (rules.Count == 0 || rules.Count > 254)
            throw new InvalidDataException("Biome catalog must contain 1..254 subbiomes.");
        return new(rules, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
    }

    private static bool ValidRange(double min, double max, double ceiling)
        => double.IsFinite(min) && double.IsFinite(max) && min >= 0 && min <= max && max <= ceiling;
}
