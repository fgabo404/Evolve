namespace GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>SplitMix64 local a cada generación; el resultado no depende de Random global.</summary>
internal struct DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(int seed, uint stream = 0)
        => _state = unchecked((uint)seed) | ((ulong)stream << 32);

    public ulong NextUInt64()
    {
        ulong z = (_state += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
    public int NextInt(int exclusiveMaximum)
        => (int)(NextUInt64() % checked((uint)exclusiveMaximum));
}
