// Supplies versioned SplitMix64 streams isolated by canonical generation stage.
// Uses explicit unsigned arithmetic so replay does not depend on runtime hashes or ambient randomness.

namespace Evolve.Engine.WorldGeneration;

public sealed class DeterministicStageRandom
{
    public const string AlgorithmVersion = "splitmix64-stage-v1";
    private const ulong Increment = 0x9E3779B97F4A7C15UL;
    private ulong state;

    public DeterministicStageRandom(ulong seed) => state = seed;

    // A fresh stream is created for every attempt, including retry and rebuild.
    public ulong NextUInt64()
    {
        state = unchecked(state + Increment);
        return Mix(state);
    }

    // Canonical IDs map to distinct initial states for a fixed main seed.
    public static ulong DeriveSeed(long seed, GenerationStage stage)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage));
        return Mix(unchecked((ulong)seed + Increment * ((ulong)stage + 1)));
    }

    // The fixed bijective mixer and modulo-2^64 overflow are part of the compatibility contract.
    private static ulong Mix(ulong value)
    {
        unchecked
        {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
