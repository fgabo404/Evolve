// Provides an immutable observation of generation-context stage states and failure details.
// Derives contiguous progress from a snapshot instead of maintaining a second mutable progress source.

namespace Evolve.Engine.WorldGeneration;

public sealed class GenerationProgress
{
    private readonly GenerationStageStatus[] states;
    private readonly string?[] failures;
    public GenerationStage? LastCompletedStage { get; }
    public GenerationStage? NextStage { get; }

    // Snapshot only small status metadata; published layer buffers are not copied during inspection.
    internal GenerationProgress(
        IEnumerable<GenerationStageStatus> states, IEnumerable<string?> failures)
    {
        this.states = states.ToArray();
        this.failures = failures.ToArray();

        int prefixLength = 0;
        while (prefixLength < this.states.Length && this.states[prefixLength] == GenerationStageStatus.Completed)
            prefixLength++;

        LastCompletedStage = prefixLength == 0 ? null : (GenerationStage)(prefixLength - 1);
        NextStage = prefixLength < this.states.Length && !this.states.Contains(GenerationStageStatus.Running)
            ? (GenerationStage)prefixLength : null;
    }

    // Read the captured stage state without exposing the private status array.
    public GenerationStageStatus GetStatus(GenerationStage stage) => states[Index(stage)];

    // Diagnostics are immutable strings; absent failures remain null.
    public string? GetFailure(GenerationStage stage) => failures[Index(stage)];

    // Keep unsupported enum casts out of both metadata arrays.
    private static int Index(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        return (int)stage;
    }
}
