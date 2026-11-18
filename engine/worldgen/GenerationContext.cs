// Holds immutable request metadata and stage-owned temporary generation state.
// Exposes read-only inspection while reserving storage transitions for the engine orchestrator.

namespace Evolve.Engine.WorldGeneration;

public sealed class GenerationContext
{
    private readonly StageRecord[] stages;
    private readonly object stateLock = new();
    private int operationActive;
    public GenerationRequest Request { get; }

    // The sealed request and its sealed settings contain only immutable scalar selections.
    public GenerationContext(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
        stages = Enum.GetValues<GenerationStage>()
            .Select(_ => new StageRecord(GenerationStageStatus.Pending, null, null)).ToArray();
    }

    // Derive every progress observation from the context's single authoritative stage table.
    public GenerationProgress GetProgress()
    {
        lock (stateLock)
        {
            var snapshot = (StageRecord[])stages.Clone();
            return new GenerationProgress(
                snapshot.Select(entry => entry.Status), snapshot.Select(entry => entry.Failure));
        }
    }

    // Only successfully published data is available; pending, running, failed and invalidated slots return null.
    public LayerData? GetLayerData(GenerationStage stage)
    {
        lock (stateLock)
        {
            var entry = stages[Index(stage)];
            return entry.Status == GenerationStageStatus.Completed ? entry.Data : null;
        }
    }

    // Prepare a single slot for publication; this does not choose, execute or validate prerequisites of a layer.
    internal void MarkRunning(GenerationStage stage)
    {
        lock (stateLock)
        {
            int index = Index(stage);
            if (stages[index].Status is not (GenerationStageStatus.Pending or GenerationStageStatus.Failed or GenerationStageStatus.Invalidated))
                throw new InvalidOperationException("Only pending, failed or invalidated storage can become running.");
            stages[index] = new StageRecord(GenerationStageStatus.Running, null, null);
        }
    }

    // Publish ownership and completion together by replacing one immutable slot record.
    internal void Publish(GenerationStage stage, LayerData data)
    {
        lock (stateLock)
        {
            int index = Index(stage);
            ArgumentNullException.ThrowIfNull(data);
            if (data.Stage != stage)
                throw new ArgumentException("Layer data belongs to a different generation stage.", nameof(data));
            if (stages[index].Status != GenerationStageStatus.Running)
                throw new InvalidOperationException("Only running storage can publish completed data.");
            stages[index] = new StageRecord(GenerationStageStatus.Completed, data, null);
        }
    }

    // Record a failed storage attempt without publishing a candidate or changing another stage.
    internal void MarkFailed(GenerationStage stage, string diagnostic)
    {
        lock (stateLock)
        {
            int index = Index(stage);
            if (string.IsNullOrWhiteSpace(diagnostic))
                throw new ArgumentException("A failure diagnostic is required.", nameof(diagnostic));
            if (stages[index].Status != GenerationStageStatus.Running)
                throw new InvalidOperationException("Only running storage can become failed.");
            stages[index] = new StageRecord(GenerationStageStatus.Failed, null, diagnostic);
        }
    }

    // Discard one published slot only; cascade policy belongs to RebuildFrom.
    internal void InvalidateStage(GenerationStage stage)
    {
        lock (stateLock)
        {
            int index = Index(stage);
            if (stages[index].Status != GenerationStageStatus.Completed)
                throw new InvalidOperationException("Only published storage can be invalidated.");
            stages[index] = new StageRecord(GenerationStageStatus.Invalidated, null, null);
        }
    }

    // The lease covers whole operations across orchestrator instances and rejects reentrancy without waiting.
    internal void EnterOperation()
    {
        if (Interlocked.CompareExchange(ref operationActive, 1, 0) != 0)
            throw new InvalidOperationException("A generation operation is already active.");
    }

    internal void ExitOperation() => Volatile.Write(ref operationActive, 0);

    // Publish the entire invalidation range under the same observation lock.
    internal void RebuildFrom(GenerationStage stage)
    {
        int index = Index(stage);
        lock (stateLock)
        {
            if (stages.Any(entry => entry.Status == GenerationStageStatus.Running))
                throw new InvalidOperationException("A generation stage is already running.");
            for (int previous = 0; previous < index; previous++)
                if (stages[previous].Status != GenerationStageStatus.Completed)
                    throw new InvalidOperationException("Rebuild requires completed prerequisites.");
            for (int current = index; current < stages.Length; current++)
            {
                var status = stages[current].Status is GenerationStageStatus.Completed or GenerationStageStatus.Invalidated
                    ? GenerationStageStatus.Invalidated : GenerationStageStatus.Pending;
                stages[current] = new StageRecord(status, null, null);
            }
        }
    }

    // Reject unsupported identities before addressing the fixed stage table.
    private static int Index(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        return (int)stage;
    }

    // Keep state, published data and diagnostics in one private unit rather than independent mutable stores.
    private sealed record StageRecord(GenerationStageStatus Status, LayerData? Data, string? Failure);
}
