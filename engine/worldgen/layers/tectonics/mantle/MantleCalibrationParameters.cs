// Validates provisional Model A weights, scales and reference speed for private calibration.
// Defines no approved Mantle profile and never changes R/M/Influence configuration.
namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal readonly record struct MantleCalibrationParameters(int BroadFrequency, int RegionalFrequency,
    float TectonicWeight, float BroadWeight, float RegionalWeight, float ReferenceSpeed)
{
    internal void Validate()
    {
        if (BroadFrequency <= 0 || RegionalFrequency <= BroadFrequency || RegionalFrequency > int.MaxValue / 4)
            throw new ArgumentOutOfRangeException(nameof(RegionalFrequency), "Expected supported positive broad < regional frequencies.");
        if (!float.IsFinite(TectonicWeight) || TectonicWeight < 0 || !float.IsFinite(BroadWeight) || BroadWeight < 0 ||
            !float.IsFinite(RegionalWeight) || RegionalWeight < 0 || (double)TectonicWeight + BroadWeight + RegionalWeight <= 0 ||
            !float.IsFinite(ReferenceSpeed) || ReferenceSpeed <= 0)
            throw new ArgumentException("Expected finite nonnegative weights, positive sum and positive reference speed.");
    }
}
