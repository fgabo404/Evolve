// Independently reconstructs Offset adjacency and float-center periodic normals for complete Model A checks.
// Adds per-vertex divergence checks and an analytical balance bound rather than recalling the mantle builder.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleIndependentOracle
{
    internal static void CheckPotential(HexGridData grid, TectonicInfluenceFields influence, MantleNoiseFields noise,
        MantleCalibrationParameters p, MantlePotentialField actual)
    {
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var source = influence[cell];
            double signal = ((double)source.Extension - source.Compression) / (((double)p.ReferenceSpeed + source.Extension) + source.Compression);
            double a = (double)p.TectonicWeight * signal, b = (double)p.BroadWeight * noise.Broad(cell), c = (double)p.RegionalWeight * noise.Regional(cell);
            float expected = (float)(((a + b) + c) / (((double)p.TectonicWeight + p.BroadWeight) + p.RegionalWeight));
            Require(BitConverter.SingleToUInt32Bits(actual[cell]) == BitConverter.SingleToUInt32Bits(expected == 0 ? 0 : expected), "Complete potential oracle.");
        }
    }

    internal static void Check(HexGridData grid, MantleConvectionField actual)
    {
        double[] x = new double[grid.CellCount], y = new double[grid.CellCount], w = new double[grid.CellCount];
        var pairs = new HashSet<(int, int)>();
        for (int first = 0; first < grid.CellCount; first++)
        foreach (int second in Neighbors(first, grid.Columns, grid.Rows))
        {
            if (first >= second) continue;
            Require(pairs.Add((first, second)), "Duplicate canonical edge.");
            var n = Normal(grid, first, second);
            double flux = (double)actual[first].Potential - actual[second].Potential;
            double a = flux * n.X, b = flux * n.Y;
            w[first] += flux; w[second] -= flux;
            x[first] += a; x[second] += a;
            y[first] += b; y[second] += b;
            Require(flux + -flux == 0, "Vertical edge antisymmetry.");
        }
        Require(pairs.Count == actual.Balance.Edges && pairs.Count == 3L * grid.Columns * grid.Rows - 2L * grid.Columns, "Exact edge count.");
        double floatSum = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var value = actual[cell];
            Require(Bits(value.FlowX) == Bits((float)x[cell]) && Bits(value.FlowY) == Bits((float)y[cell]) && Bits(value.SignedVertical) == Bits((float)w[cell]), "Complete independent edge accumulation.");
            double local = 0, absolute = 0;
            foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
            {
                double term = (double)value.Potential - actual[next].Potential;
                local += term; absolute += Math.Abs(term);
            }
            double castError = Math.Abs((double)(float)local - local);
            double budget = Math.BitIncrement(castError + 32 * Math.ScaleB(1d, -53) * absolute);
            Require(Math.Abs((double)value.SignedVertical - local) <= budget, "Per-vertex divergence rounding bound.");
            floatSum += value.SignedVertical;
        }
        Require(floatSum == actual.Balance.FloatSum && Math.Abs(actual.Balance.DoubleSum) <= actual.Balance.DoubleBudget &&
            Math.Abs(floatSum) <= actual.Balance.FloatBudget, "Derived global balance bounds.");
    }

    private static (double X, double Y) Normal(HexGridData grid, int first, int second)
    {
        (float X, float Y) Center(int cell) => ((float)(1d + 1.5d * (cell % grid.Columns)),
            (float)(Math.Sqrt(3d) * (cell / grid.Columns + 0.5d * ((cell % grid.Columns) & 1) + .5d)));
        var a = Center(first); var b = Center(second);
        double x = (double)b.X - a.X, y = (double)b.Y - a.Y, width = 1.5d * grid.Columns;
        if (x > width / 2) x -= width;
        if (x < -width / 2) x += width;
        double length = Math.Sqrt(x * x + y * y);
        return (x / length, y / length);
    }
    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value == 0 ? 0 : value);
}
