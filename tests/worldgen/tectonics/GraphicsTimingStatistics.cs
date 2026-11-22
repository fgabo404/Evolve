// Supplies the conventional median for sorted finite timing samples in diagnostic graphics tools.
// Shared by the temporary capture harness and headless checks; never referenced by production rendering.
namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class GraphicsTimingStatistics
{
    internal static double MedianOfSortedSamples(ReadOnlySpan<double> samples)
    {
        if (samples.IsEmpty) throw new ArgumentException("At least one sample is required.", nameof(samples));
        for (int index = 0; index < samples.Length; index++)
            if (!double.IsFinite(samples[index]) || (index > 0 && samples[index] < samples[index - 1]))
                throw new ArgumentException("Samples must be sorted and finite.", nameof(samples));
        int middle = samples.Length / 2;
        if (samples.Length % 2 != 0) return samples[middle];
        double lower = samples[middle - 1], upper = samples[middle];
        double sum = lower + upper;
        // Preserve subnormal rounding normally; halve first only when a finite pair overflows its sum.
        return double.IsFinite(sum) ? sum / 2 : lower / 2 + upper / 2;
    }
}
