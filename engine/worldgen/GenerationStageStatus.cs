// Describes the storage lifecycle of each stage in a generation context.
// Execution eligibility and orchestration remain responsibilities of later CORE increments.

namespace Evolve.Engine.WorldGeneration;

public enum GenerationStageStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Invalidated = 4
}
