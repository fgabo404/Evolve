namespace GEvolve.Definitions.WorldGeneration.Configuration;

/// <summary>
/// Player-facing types only. Historical LandmassType values remain valid for saves,
/// deterministic generation, and regression testing.
/// </summary>
public static class SelectableWorldTypes
{
    private static readonly IReadOnlyList<LandmassType> Types =
        Array.AsReadOnly(new[]
        {
            LandmassType.Pangaea,
            LandmassType.Continents,
            LandmassType.Archipelagos
        });

    public static IReadOnlyList<LandmassType> All => Types;

    public static bool IsSelectable(LandmassType type) =>
        type is LandmassType.Pangaea or LandmassType.Continents or LandmassType.Archipelagos;
}
