// Accumulates paired Model A fluxes over canonical real Grid edges using the approved periodic normals.
// Retains immutable dimensionless floats and derived floating-point conservation budgets for calibration only.
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal readonly record struct MantleCell(float Potential, float FlowX, float FlowY, float SignedVertical);
internal readonly record struct MantleBalance(int Edges, double DoubleSum, double FloatSum,
    double DoubleBudget, double FloatBudget, double RoundingErrorUpper);

internal sealed class MantleConvectionField
{
    private readonly MantlePotentialField potential;
    private readonly (float X, float Y, float W)[] flows;
    internal int Count => potential.Count;
    internal MantleBalance Balance { get; }
    internal MantleCell this[int cell] => new(potential[cell], flows[cell].X, flows[cell].Y, flows[cell].W);
    private MantleConvectionField(MantlePotentialField potential, (float, float, float)[] flows, MantleBalance balance) =>
        (this.potential, this.flows, Balance) = (potential, flows, balance);

    internal static MantleConvectionField Build(HexGridData grid, MantlePotentialField potential)
    {
        ArgumentNullException.ThrowIfNull(grid); ArgumentNullException.ThrowIfNull(potential);
        if (potential.Count != grid.CellCount) throw new ArgumentException("Potential dimensions differ from Grid.");
        double[] u = new double[grid.CellCount], v = new double[grid.CellCount], w = new double[grid.CellCount];
        double absoluteFluxUpper = 0; int edges = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        for (int direction = 0; direction < 6; direction++)
            if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && cell < next)
            {
                var normal = PlateBoundaryBuilder.Normal(grid, cell, next);
                double f = (double)potential[cell] - potential[next];
                double x = f * normal.X, y = f * normal.Y;
                w[cell] += f; w[next] -= f;
                u[cell] += x; u[next] += x;
                v[cell] += y; v[next] += y;
                absoluteFluxUpper = AddUp(absoluteFluxUpper, Math.Abs(f));
                edges++;
            }
        var flows = new (float, float, float)[grid.CellCount];
        double doubleSum = 0, floatSum = 0, absDoubleUpper = 0, absFloatUpper = 0, roundingUpper = 0;
        for (int cell = 0; cell < flows.Length; cell++)
        {
            float storedW = Store(w[cell]);
            flows[cell] = (Store(u[cell]), Store(v[cell]), storedW);
            doubleSum += w[cell]; floatSum += storedW;
            absDoubleUpper = AddUp(absDoubleUpper, Math.Abs(w[cell]));
            absFloatUpper = AddUp(absFloatUpper, Math.Abs((double)storedW));
            roundingUpper = AddUp(roundingUpper, Math.BitIncrement(Math.Abs((double)storedW - w[cell])));
        }
        // At most six rounded additions per vertex; exact paired f/-f terms cancel globally.
        double accumulationUpper = MultiplyUp(Gamma(6), MultiplyUp(2, absoluteFluxUpper));
        double doubleBudget = AddUp(accumulationUpper, MultiplyUp(Gamma(grid.CellCount), absDoubleUpper));
        double floatBudget = AddUp(AddUp(accumulationUpper, roundingUpper), MultiplyUp(Gamma(grid.CellCount), absFloatUpper));
        return new(potential, flows, new(edges, doubleSum, floatSum, doubleBudget, floatBudget, roundingUpper));
    }

    // Structural/balance validation is separate from the independent all-edge oracle in tests.
    internal void Validate(HexGridData grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (Count != grid.CellCount || flows.Length != Count) throw new ArgumentException("Mantle dimensions differ.");
        long expectedEdges = checked(3L * grid.Columns * grid.Rows - 2L * grid.Columns);
        if (Balance.Edges != expectedEdges || !double.IsFinite(Balance.DoubleBudget) || !double.IsFinite(Balance.FloatBudget) ||
            Balance.DoubleBudget < 0 || Balance.FloatBudget < 0 || !double.IsFinite(Balance.DoubleSum) || !double.IsFinite(Balance.FloatSum) ||
            Math.Abs(Balance.DoubleSum) > Balance.DoubleBudget || Math.Abs(Balance.FloatSum) > Balance.FloatBudget)
            throw new ArgumentException("Invalid mantle conservation diagnostics.");
        double sum = 0;
        for (int cell = 0; cell < Count; cell++)
        {
            var value = this[cell];
            Check(value.Potential, 1); Check(value.FlowX, MathF.BitIncrement(12));
            Check(value.FlowY, MathF.BitIncrement(12)); Check(value.SignedVertical, MathF.BitIncrement(12));
            sum += value.SignedVertical;
        }
        if (sum != Balance.FloatSum) throw new ArgumentException("Stored mantle balance differs.");
    }

    private static void Check(float value, float bound)
    {
        if (!float.IsFinite(value) || Math.Abs(value) > bound || BitConverter.SingleToUInt32Bits(value) == 0x80000000)
            throw new ArgumentException("Invalid or noncanonical mantle value.");
    }
    private static float Store(double value)
    {
        float result = (float)value;
        if (!double.IsFinite(value) || !float.IsFinite(result)) throw new ArgumentException("Nonfinite mantle flux.");
        return result == 0 ? 0 : result;
    }
    private static double AddUp(double a, double b) => Math.BitIncrement(a + b);
    private static double MultiplyUp(double a, double b) => Math.BitIncrement(a * b);
    private static double Gamma(int count)
    {
        double nu = count * Math.ScaleB(1d, -53);
        return Math.BitIncrement(nu / Math.BitDecrement(1 - nu));
    }
}
