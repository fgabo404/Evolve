// Reads the exact game display version from assembly informational metadata.
// Exposes the value generated from root VERSION to the menu presentation.

using System.Reflection;

namespace Evolve.Game;

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
