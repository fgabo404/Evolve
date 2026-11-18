// Holds the minimum approved, immutable world-generation selections.
// Provides validated configuration values to the engine without UI or simulation state.

namespace Evolve.Definitions.WorldGeneration;

public sealed record WorldGenerationSettings
{
    public WorldType WorldType { get; }
    public MapSize MapSize { get; }

    // Reject undefined enum values without introducing defaults or unapproved dimensions.
    public WorldGenerationSettings(WorldType worldType, MapSize mapSize)
    {
        if (!Enum.IsDefined(worldType))
            throw new ArgumentOutOfRangeException(nameof(worldType), worldType, "Unsupported world type.");
        if (!Enum.IsDefined(mapSize))
            throw new ArgumentOutOfRangeException(nameof(mapSize), mapSize, "Unsupported map size.");

        WorldType = worldType;
        MapSize = mapSize;
    }
}
