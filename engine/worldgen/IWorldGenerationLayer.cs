// Identifies a layer for validation by the world-generation registry.
// Execution inputs and results remain deferred until context and orchestration contracts exist.

namespace Evolve.Engine.WorldGeneration;

public interface IWorldGenerationLayer
{
    // Implementations must keep their stage identity stable for their entire lifetime.
    GenerationStage Stage { get; }
}
