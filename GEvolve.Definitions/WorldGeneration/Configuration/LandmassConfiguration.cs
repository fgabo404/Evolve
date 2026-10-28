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
        return type switch
        {
            LandmassType.Continents => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Continents: new(.35, size switch { GridSize.Small => 3,
                    GridSize.Medium => 4, _ => 6 }, size switch { GridSize.Small => 25,
                    GridSize.Medium => 32, _ => 48 }, .48, 9, .002)),
            LandmassType.Pangaea => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Pangaea: new(.40, .58, 7, .0015, .32)),
            LandmassType.SmallContinents => new(size, type, seed, EvolveVersion.LandmassGenerator,
                SmallContinents: new(.30, size switch { GridSize.Small => 12,
                    GridSize.Medium => 22, _ => 58 }, size switch { GridSize.Small => 9.5,
                    GridSize.Medium => 10.5, _ => 11.5 }, 2, .38, 15, .004)),
            LandmassType.Archipelagos => new(size, type, seed, EvolveVersion.LandmassGenerator,
                Archipelagos: new(.20, size switch { GridSize.Small => 14,
                    GridSize.Medium => 28, _ => 92 }, 12, 1.2, 3.5, .35, 20)),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
