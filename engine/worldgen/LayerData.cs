// Defines stage-owned immutable output buffers for generation-context storage.
// Copies only one stage's unmanaged values, preventing mutable references from entering published data.

using System.Collections;

namespace Evolve.Engine.WorldGeneration;

public abstract class LayerData
{
    public GenerationStage Stage { get; }

    // Prevent arbitrary external subclasses from adding mutable state to the output contract.
    private protected LayerData(GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported generation stage.");
        Stage = stage;
    }
}

public sealed class LayerData<T> : LayerData, IReadOnlyList<T> where T : unmanaged
{
    private readonly T[] values;
    public IReadOnlyList<T> Values => this;
    public int Count => values.Length;
    public T this[int index] => values[index];

    // Own a defensive copy; unmanaged elements cannot hide mutable lists, arrays or object references.
    public LayerData(GenerationStage stage, IEnumerable<T> values) : base(stage)
    {
        ArgumentNullException.ThrowIfNull(values);
        this.values = values.ToArray();
    }

    // Expose value copies only, without a mutable collection or an underlying-array accessor.
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

    // Support normal collection iteration through the same read-only value path.
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
