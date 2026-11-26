// Reuses the accepted periodic multiscale sampler on physical Grid centers for experimental mantle signals.
// A fresh private seed domain leaves the three original Tectonics random draws untouched.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal sealed class MantleNoiseFields
{
    internal const ulong ExperimentalDomain = 0x31544E4D31304757;
    private readonly double[] broad, regional;
    internal int Count => broad.Length;
    internal double Broad(int cell) => broad[cell];
    internal double Regional(int cell) => regional[cell];
    private MantleNoiseFields(double[] broad, double[] regional) => (this.broad, this.regional) = (broad, regional);

    internal static (ulong Broad, ulong Regional) Seeds(long seed)
    {
        var random = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics) ^ ExperimentalDomain);
        return (random.NextUInt64(), random.NextUInt64());
    }

    internal static MantleNoiseFields Build(HexGridData grid, GenerationRequest request, MantleCalibrationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(grid); ArgumentNullException.ThrowIfNull(request);
        parameters.Validate();
        if (new HexGridData(request.Settings.MapSize).Columns != grid.Columns) throw new ArgumentException("Request and Grid sizes differ.");
        var seeds = Seeds(request.Seed);
        double[] broad = new double[grid.CellCount], regional = new double[grid.CellCount];
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var center = FlatHexGeometry.Center(grid.GetOffset(cell));
            broad[cell] = Sample(grid, center.X, center.Y, parameters.BroadFrequency, seeds.Broad);
            regional[cell] = Sample(grid, center.X, center.Y, parameters.RegionalFrequency, seeds.Regional);
        }
        return new(broad, regional);
    }

    internal static double Sample(HexGridData grid, double physicalX, double physicalY, int frequency, ulong seed) =>
        PeriodicTectonicNoise.Sample(physicalX / FlatHexGeometry.PeriodWidth(grid.Columns) * grid.Columns,
            physicalY / FlatHexGeometry.MapHeight(grid.Rows) * grid.Rows, grid.Columns, grid.Rows, frequency, seed);

    internal static MantleNoiseFields FromSamples(ReadOnlySpan<double> broad, ReadOnlySpan<double> regional)
    {
        if (broad.Length != regional.Length) throw new ArgumentException("Signal dimensions differ.");
        foreach (double value in broad) Validate(value);
        foreach (double value in regional) Validate(value);
        return new(broad.ToArray(), regional.ToArray());
    }

    internal static void Validate(double value)
    {
        if (!double.IsFinite(value) || value < -1 || value > 1) throw new ArgumentException("Signal must be finite in [-1,1].");
    }
}
