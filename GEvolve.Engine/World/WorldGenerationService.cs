using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>
/// Stage 01 universal tectonics, then stage 02 type-specific surface and relief.
/// Temperature, precipitation and biome classification remain shared future stages.
/// </summary>
public sealed class WorldGenerationService
{
    private readonly PangaeaFromPlatesGenerator _pangaea = new();
    private readonly PlateAwareElevationGenerator _relief = new();

    public GeneratedWorld Generate(LandmassConfiguration configuration)
    {
        Validate(configuration);
        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);

        // The same 16 plate IDs exist for every world type at the same size/seed.
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);
        var tectonicMotion = new TectonicMotionGenerator().Generate(topology,
            new LandmassState(configuration, new byte[topology.Count], 0) { PlateByCellId = plates });

        IPlateAwareLandmassGenerator? landmassGenerator = configuration.Type switch
        {
            LandmassType.Pangaea => null,
            LandmassType.Continents => new ContinentsGenerator(),
            LandmassType.SmallContinents => new SmallContinentsGenerator(),
            LandmassType.Archipelagos => new ArchipelagosGenerator(),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration))
        };

        LandmassStatistics? last = null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            byte[] surface;
            byte[] elevation;
            if (configuration.Type == LandmassType.Pangaea)
                (surface, elevation) = _pangaea.Generate(topology, configuration, plates, attempt);
            else
            {
                // Each existing land generator preserves its distinct distribution:
                // large separate continents, small landmasses or island chains.
                // Plate guidance gently influences their surface *and* shared relief.
                surface = landmassGenerator!.Generate(topology, configuration, attempt, plates);
                last = LandmassAnalysis.Analyze(topology, surface);
                if (!LandmassGenerationService.MeetsProfile(configuration.Type, last))
                    continue;
                elevation = _relief.Generate(
                    topology, surface, plates, configuration.Type, configuration.Seed, attempt);
            }

            if (configuration.Type == LandmassType.Pangaea)
            {
                last = LandmassAnalysis.Analyze(topology, surface);
                if (!LandmassGenerationService.MeetsProfile(configuration.Type, last))
                    continue;
            }

            var terrain = new LandmassState(configuration, surface, attempt)
            {
                PlateByCellId = plates,
                TectonicMotion = tectonicMotion,
                ElevationByCellId = elevation
            };
            return new GeneratedWorld(grid, terrain, last
                ?? throw new InvalidOperationException("Landmass statistics were not calculated."));
        }

        throw new InvalidOperationException(
            $"Stage 02 {configuration.Type} did not meet the profile after four attempts " +
            $"(land={last?.LandFraction:P1}, components={last?.ComponentCount}, " +
            $"dominant={last?.DominantLandShare:P1}).");
    }

    /// <summary>Stage 01 alone, independent of the selected map type.</summary>
    public GeneratedWorld GeneratePlatePreview(LandmassConfiguration configuration)
    {
        Validate(configuration);
        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);

        // Placeholder only: zero is not an actual ocean until stage 02 is run.
        var preview = new LandmassState(configuration, new byte[topology.Count], 0)
        {
            PlateByCellId = plates,
            TectonicPreviewOnly = true
        };
        preview = preview with { TectonicMotion = new TectonicMotionGenerator().Generate(topology, preview) };
        return new GeneratedWorld(grid, preview,
            new LandmassStatistics(0, topology.Count, [], 0));
    }

    private static void Validate(LandmassConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration != LandmassProfiles.Create(
                configuration.Size, configuration.Type, configuration.Seed))
            throw new ArgumentException("World configuration differs from official profile.",
                nameof(configuration));
    }
}
