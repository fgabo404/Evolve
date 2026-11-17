using System.Reflection;

namespace Evolve.Game;

// Reads the exact display version embedded from the root VERSION file at build time.
internal static class GameVersion
{
    public static string Display { get; } = ReadDisplayVersion();

    // Informational metadata preserves the prefix and zeros lost by AssemblyVersion.
    private static string ReadDisplayVersion()
    {
        return typeof(GameVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? throw new InvalidOperationException("Game version metadata is missing.");
    }
}
