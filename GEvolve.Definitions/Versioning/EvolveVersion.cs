using System.Globalization;
using System.Reflection;

namespace GEvolve.Definitions.Versioning;

/// <summary>Lee los identificadores centralizados en Directory.Build.props.</summary>
public static class EvolveVersion
{
    public static string GameName => Metadata("EvolveGameName");
    public static string Display => Metadata("EvolveDisplayVersion");
    public static Version Technical => typeof(EvolveVersion).Assembly.GetName().Version
        ?? throw new InvalidOperationException("Assembly version is missing.");
    public static string LandmassGenerator => Metadata("EvolveGeneratorVersion");
    public static int SaveFormat => int.Parse(Metadata("EvolveSaveFormatVersion"),
        CultureInfo.InvariantCulture);

    private static string Metadata(string key) => typeof(EvolveVersion).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == key).Value
        ?? throw new InvalidOperationException($"Missing version metadata: {key}");
}
