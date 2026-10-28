using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>
/// Staged world pipeline. The playable preview currently stops after stage 01.
/// Later phases can reuse the same plate array before producing land, climate and biomes.
/// </summary>
public sealed class WorldGenerationService
{
    public GeneratedWorld Generate(LandmassConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration != LandmassProfiles.Create(
                configuration.Size, configuration.Type, configuration.Seed))
            throw new ArgumentException("World configuration differs from official profile.",
                nameof(configuration));

        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);

        // Stage 01: independent of Pangaea/continents/archipelagos selection.
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);

        // No land/ocean classification exists at this stage: the surface array is
        // an unclassified placeholder, NOT a completed ocean map.
        var preview = new LandmassState(configuration, new byte[topology.Count], 0)
        {
            PlateByCellId = plates,
            TectonicPreviewOnly = true
        };
        return new GeneratedWorld(grid, preview,
            new LandmassStatistics(0, topology.Count, [], 0));
    }
}
