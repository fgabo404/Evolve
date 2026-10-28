using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.Engine.World.BiomeGeneration;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed record GeneratedWorld(HexGrid Grid, LandmassState Landmass,
    LandmassStatistics Statistics);

/// <summary>Elige una estrategia propia y valida el resultado con intentos acotados.</summary>
public sealed class LandmassGenerationService
{
    public IReadOnlyList<ILandmassGenerator> Generators { get; } =
        [new ContinentsGenerator(), new PangaeaGenerator(),
         new SmallContinentsGenerator(), new ArchipelagosGenerator()];

    public GeneratedWorld Generate(LandmassConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration != LandmassProfiles.Create(configuration.Size,
            configuration.Type, configuration.Seed))
            throw new ArgumentException("Configuration is not an official v0.001 profile.",
                nameof(configuration));
        var grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);
        ILandmassGenerator generator = Generators.Single(item => item.Type == configuration.Type);
        LandmassStatistics? last = null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            byte[] surface;
            byte[]? elevation = null, plates = null;
            if (generator is PangaeaGenerator tectonic)
            {
                var result = tectonic.GenerateTectonic(topology, configuration, attempt);
                surface = result.Surface;
                elevation = result.Elevation;
                plates = result.Plates;
            }
            else surface = generator.Generate(topology, configuration, attempt);
            LandmassStatistics statistics = LandmassAnalysis.Analyze(topology, surface);
            last = statistics;
            if (MeetsProfile(configuration.Type, statistics))
                return new GeneratedWorld(grid,
                    new BiomeClassifier().Apply(new LandmassState(configuration, surface, attempt)
                    { ElevationByCellId = elevation, PlateByCellId = plates }), statistics);
        }
        throw new InvalidOperationException(
            $"Landmass generator could not satisfy {configuration.Type} after four deterministic attempts " +
            $"(land={last?.LandFraction:P1}, components={last?.ComponentCount}, " +
            $"dominant={last?.DominantLandShare:P1}, small={last?.SmallComponents}).");
    }

    public static bool MeetsProfile(LandmassType type, LandmassStatistics result)
        => type switch
        {
            LandmassType.Continents => result.LandFraction is >= .27 and <= .43 &&
                result.LargeComponents >= 2 && result.DominantLandShare < .75,
            LandmassType.Pangaea => result.LandFraction is >= .30 and <= .55 &&
                result.ComponentCount == 1 && result.DominantLandShare == 1,
            LandmassType.SmallContinents => result.LandFraction is >= .23 and <= .38 &&
                result.ComponentCount >= 8 && result.DominantLandShare < .23,
            LandmassType.Archipelagos => result.LandFraction is >= .14 and <= .27 &&
                result.ComponentCount >= 20 && result.DominantLandShare < .12 &&
                result.SmallComponents * 2 >= result.ComponentCount,
            _ => false
        };
}
