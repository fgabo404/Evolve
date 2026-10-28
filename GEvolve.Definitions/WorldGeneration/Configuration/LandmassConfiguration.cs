using GEvolve.Definitions.Versioning;

namespace GEvolve.Definitions.WorldGeneration.Configuration;

public enum LandmassType { Continents, Pangaea, SmallContinents, Archipelagos }

public sealed record ContinentsParameters(double TargetLandFraction, int CoreCount,
    int MinimumCoreDistance, double CoastRoughness, int NoiseFrequency,
    double IslandFraction);
public sealed record PangaeaParameters(double TargetLandFraction, double CoastRoughness,
    int NoiseFrequency, double IslandFraction, double DirectionalVariation);
public sealed record SmallContinentsParameters(double TargetLandFraction, int CoreCount,
    double MaximumRadius, int CorridorWidth, double CoastRoughness, int NoiseFrequency,
    double SecondaryIslandFraction);
public sealed record ArchipelagosParameters(double TargetLandFraction, int ChainCount,
    int StepsPerChain, double MinimumIslandRadius, double MaximumIslandRadius,
    double CoastRoughness, int NoiseFrequency);

/// <summary>Configuración efectiva: solo el bloque del algoritmo elegido está presente.</summary>
public sealed record LandmassConfiguration(GridSize Size, LandmassType Type, int Seed,
    string GeneratorVersion, ContinentsParameters? Continents = null,
    PangaeaParameters? Pangaea = null,
    SmallContinentsParameters? SmallContinents = null,
    ArchipelagosParameters? Archipelagos = null);

/// <summary>Objetivos experimentales de v0.001, no reglas definitivas del juego.</summary>
public static class LandmassProfiles
{
    public static LandmassConfiguration Create(GridSize size, LandmassType type, int seed)
    {
        _ = HexGridDefinitions.Get(size);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        // The four profiles now cover more cells; keep geographic feature
        // density proportional to area instead of reusing the old 7k/15k/50k profiles.
        return type switch
        {
            LandmassType.Continents => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Continents: new(.35, size switch {
                    GridSize.Small => 4, GridSize.Medium => 5,
                    GridSize.Large => 6, _ => 8
                }, size switch {
                    GridSize.Small => 34, GridSize.Medium => 39,
                    GridSize.Large => 48, _ => 60
                }, .48, 9, .002)),
            LandmassType.Pangaea => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Pangaea: new(.40, .58, 7, .0015, .32)),
            LandmassType.SmallContinents => new(size, type, seed, EvolveVersion.LandmassGenerator,
                SmallContinents: new(.30, size switch {
                    GridSize.Small => 36, GridSize.Medium => 52,
                    GridSize.Large => 76, _ => 124
                }, size switch {
                    GridSize.Small => 11.5, GridSize.Medium => 12.5,
                    GridSize.Large => 13.5, _ => 14.5
                }, 2, .38, 15, .004)),
            LandmassType.Archipelagos => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Archipelagos: new(.20, size switch {
                    GridSize.Small => 55, GridSize.Medium => 88,
                    GridSize.Large => 128, _ => 220
                }, 12, 1.2, 3.5, .35, 20)),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
