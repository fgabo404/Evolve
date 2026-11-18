// Carries the main seed and validated settings into the world-generation boundary.
// This immutable request contains no context, random stream, progress or generated data.

using Evolve.Definitions.WorldGeneration;

namespace Evolve.Engine.WorldGeneration;

public sealed record GenerationRequest
{
    public long Seed { get; }
    public WorldGenerationSettings Settings { get; }

    // Preserve the supplied seed verbatim; seed derivation is a later CORE-04 decision.
    public GenerationRequest(long seed, WorldGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Seed = seed;
        Settings = settings;
    }
}
