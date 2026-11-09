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
            ["desert"] = Rgb(0xD8BC82),
            ["forest"] = Rgb(0x43845A),
            ["plains"] = Rgb(0xA7C47C),
            ["marine"] = Rgb(0x347FA8),
            ["tundra"] = Rgb(0x91A99A),
            ["mountain"] = Rgb(0x918B80),
            ["polar_north"] = Rgb(0xDCEEF1),
            ["polar_south"] = Rgb(0xEDF4F5)
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
