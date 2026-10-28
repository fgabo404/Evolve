using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>
/// Staged pipeline: universal plates, then Pangaea land and elevation.
/// Other landmass generators and the climate/biome stages remain preserved,
/// but are not yet wired into this new pipeline.
/// </summary>
public sealed class WorldGenerationService
{
    public GeneratedWorld Generate(LandmassConfiguration configuration)
    {
        Validate(configuration);
        if (configuration.Type != LandmassType.Pangaea)
            throw new NotSupportedException(
                "Stage 02 currently supports Pangaea. Other map types are not yet integrated.");

        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);

        // Stage 01: exactly the same plate layer for every map type.
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);

        // Stage 02: produce geography from those plate IDs, without replacing
        // or changing them. The old legacy generators remain available unchanged.
        var generator = new PangaeaFromPlatesGenerator();
        LandmassStatistics? last = null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var (surface, elevation) = generator.Generate(topology, configuration, plates, attempt);
            last = LandmassAnalysis.Analyze(topology, surface);
            if (!LandmassGenerationService.MeetsProfile(LandmassType.Pangaea, last))
                continue;
            var terrain = new LandmassState(configuration, surface, attempt)
            {
                PlateByCellId = plates,
                ElevationByCellId = elevation
            };
            return new GeneratedWorld(grid, terrain, last);
        }

        throw new InvalidOperationException(
            $"Pangaea from tectonic plates did not meet landmass profile after four attempts " +
            $"(land={last?.LandFraction:P1}, components={last?.ComponentCount}).");
    }

    /// <summary>Diagnostic stage 01 only, independent of the chosen map type.</summary>
    public GeneratedWorld GeneratePlatePreview(LandmassConfiguration configuration)
    {
        Validate(configuration);
        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);
        // Surface is intentionally unclassified: zeros do NOT represent oceans.
        var preview = new LandmassState(configuration, new byte[topology.Count], 0)
        {
            PlateByCellId = plates,
            TectonicPreviewOnly = true
        };
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
