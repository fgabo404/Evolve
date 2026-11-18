// Validates and retains the fixed, ordered set of world-generation layer contracts.
// Exposes read-only registration for future orchestration without executing any layer.

namespace Evolve.Engine.WorldGeneration;

public sealed class WorldGenerationLayerRegistry
{
    public IReadOnlyList<IWorldGenerationLayer> Layers { get; }

    // Validate a private membership snapshot completely before publishing the registry.
    public WorldGenerationLayerRegistry(IEnumerable<IWorldGenerationLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        IWorldGenerationLayer[] snapshot = layers.ToArray();
        int expectedCount = (int)GenerationStage.Biomes + 1;
        if (snapshot.Length != expectedCount)
            throw new ArgumentException("Exactly eight generation layers are required.", nameof(layers));

        var identities = new HashSet<GenerationStage>();
        for (int index = 0; index < snapshot.Length; index++)
        {
            var layer = snapshot[index];
            if (layer is null)
                throw new ArgumentException("Generation layers cannot contain null entries.", nameof(layers));

            GenerationStage stage = layer.Stage;
            if (!Enum.IsDefined(stage))
                throw new ArgumentException($"Unknown generation stage: {(int)stage}.", nameof(layers));
            if (!identities.Add(stage))
                throw new ArgumentException($"Duplicate generation stage: {stage}.", nameof(layers));
            if (stage != (GenerationStage)index)
                throw new ArgumentException("Generation layers must be registered in canonical order from Grid to Biomes.", nameof(layers));
        }

        Layers = Array.AsReadOnly(snapshot);
    }

    // Lookup is registration inspection only and does not grant stage execution eligibility.
    public IWorldGenerationLayer GetLayer(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        return Layers[(int)stage];
    }
}
