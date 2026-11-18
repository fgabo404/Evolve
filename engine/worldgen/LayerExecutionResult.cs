// Carries either an immutable candidate buffer or an explicit layer failure diagnostic.
// Does not publish data or change context state; the orchestrator validates every candidate.

namespace Evolve.Engine.WorldGeneration;

public sealed class LayerExecutionResult
{
    public LayerData? Candidate { get; }
    public string? Failure { get; }

    private LayerExecutionResult(LayerData? candidate, string? failure)
    {
        Candidate = candidate;
        Failure = failure;
    }

    // Candidate ownership is checked against the executed stage before publication.
    public static LayerExecutionResult Success(LayerData candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new LayerExecutionResult(candidate, null);
    }

    // Explicit failures carry no partially computed candidate into the publication boundary.
    public static LayerExecutionResult Failed(string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(diagnostic))
            throw new ArgumentException("A failure diagnostic is required.", nameof(diagnostic));
        return new LayerExecutionResult(null, diagnostic);
    }
}
