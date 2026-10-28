using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

/// <summary>Única asociación de bioma a color; compartida por todos sus subbiomas.</summary>
public static class BiomePalette
{
    public static Vector3 ImpassableSummitColor { get; } = Rgb(0xFFFFFF);
    private static readonly IReadOnlyDictionary<string, Vector3> Colors =
        new Dictionary<string, Vector3>(StringComparer.Ordinal)
        {
            ["desert"] = Rgb(0xE8C77A),
            ["forest"] = Rgb(0x287D46),
            ["plains"] = Rgb(0xA4C96B),
            ["marine"] = Rgb(0x287DB5),
            ["tundra"] = Rgb(0x91A89A),
            ["mountain"] = Rgb(0x887C72),
            ["polar_north"] = Rgb(0xD5EDF5),
            ["polar_south"] = Rgb(0xF0F7FA)
        };

    public static Vector3 ForBiome(string id) => Colors.TryGetValue(id, out var color)
        ? color : throw new ArgumentException($"No color for biome '{id}'.", nameof(id));

    public static Vector3 ForCell(LandmassState terrain, int id)
        => terrain.IsImpassableSummit(id) ? ImpassableSummitColor
            : terrain.Biomes?.Get(id) is { Status: BiomeClassificationStatus.Assigned, BiomeId: { } biome }
            ? ForBiome(biome)
            : terrain.IsLand(id) ? new(.35f, .57f, .32f) : new(.32f, .55f, .71f);

    private static Vector3 Rgb(int hex) => new Vector3(
        (hex >> 16) & 255, (hex >> 8) & 255, hex & 255) / 255f;
}
