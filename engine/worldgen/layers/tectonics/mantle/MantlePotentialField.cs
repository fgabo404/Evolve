// Combines immutable Influence and periodic signals into the bounded experimental Model A potential.
// Stores canonical floats without map-dependent normalization or feedback into earlier layers.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal sealed class MantlePotentialField
{
    private readonly float[] values;
    internal int Count => values.Length;
    internal float this[int cell] => values[cell];
    private MantlePotentialField(float[] values) => this.values = values;

    internal static MantlePotentialField Combine(HexGridData grid, TectonicInfluenceFields influence,
        MantleNoiseFields noise, MantleCalibrationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(grid); ArgumentNullException.ThrowIfNull(influence); ArgumentNullException.ThrowIfNull(noise);
        parameters.Validate();
        if (influence.Count != grid.CellCount || noise.Count != grid.CellCount) throw new ArgumentException("Field dimensions differ.");
        float[] values = new float[grid.CellCount];
        for (int cell = 0; cell < values.Length; cell++)
            values[cell] = Evaluate(influence[cell].Compression, influence[cell].Extension, noise.Broad(cell), noise.Regional(cell), parameters);
        return new(values);
    }

    internal static float Evaluate(float compression, float extension, double broad, double regional, MantleCalibrationParameters parameters)
    {
        parameters.Validate(); MantleNoiseFields.Validate(broad); MantleNoiseFields.Validate(regional);
        if (!float.IsFinite(compression) || compression < 0 || !float.IsFinite(extension) || extension < 0)
            throw new ArgumentException("Influence channels must be finite and nonnegative.");
        double z = ((double)extension - compression) / (((double)parameters.ReferenceSpeed + extension) + compression);
        double numerator = ((double)parameters.TectonicWeight * z + (double)parameters.BroadWeight * broad) + (double)parameters.RegionalWeight * regional;
        double denominator = ((double)parameters.TectonicWeight + parameters.BroadWeight) + parameters.RegionalWeight;
        float value = (float)(numerator / denominator);
        if (!float.IsFinite(value) || value < -1 || value > 1) throw new ArgumentException("Invalid mantle potential.");
        return value == 0 ? 0 : value;
    }

    internal static MantlePotentialField FromValues(ReadOnlySpan<float> input)
    {
        float[] values = input.ToArray();
        for (int cell = 0; cell < values.Length; cell++)
        {
            if (!float.IsFinite(values[cell]) || values[cell] < -1 || values[cell] > 1) throw new ArgumentException("Invalid mantle potential.");
            if (values[cell] == 0) values[cell] = 0;
        }
        return new(values);
    }
}
