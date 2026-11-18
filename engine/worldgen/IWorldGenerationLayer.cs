// Identifies and executes one world-generation layer using read-only earlier results.
// Returns an isolated candidate for validation and publication by the orchestrator.

namespace Evolve.Engine.WorldGeneration;

public interface IWorldGenerationLayer
{
    // Implementations must keep their stage identity stable for their entire lifetime.
    GenerationStage Stage { get; }

    // Layers cannot publish results or mutate progress through their input view.
    LayerExecutionResult Execute(GenerationLayerInputs inputs);
}
