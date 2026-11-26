// Evaluates test-only geometric alternatives without replacing any canonical CAL01 component.
// Reuses Grid, approved normals, noise, seed policy and potential arithmetic; stores compact immutable experimental flows.
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleSpatialAlternatives
{
    internal enum Operator { R0, R1Native, R2Native, R1Width, R2Width }
    internal enum Geometry { A0, A1 }
    internal sealed class Signals
    {
        internal MantleNoiseFields? Canonical { get; }
        private readonly double[]? broad, regional;
        internal int Count { get; }
        internal double Broad(int cell) => Canonical is { } original ? original.Broad(cell) : broad![cell];
        internal double Regional(int cell) => Canonical is { } original ? original.Regional(cell) : regional![cell];
        private Signals(MantleNoiseFields original) => (Canonical, Count) = (original, original.Count);
        private Signals(double[] broad, double[] regional) => (this.broad, this.regional, Count) = (broad, regional, broad.Length);
        internal static Signals Build(HexGridData grid, GenerationRequest request, MantleCalibrationParameters p, Geometry geometry,
            double shiftX = 0, double shiftY = 0)
        {
            p.Validate();
            if (new HexGridData(request.Settings.MapSize).Columns != grid.Columns) throw new ArgumentException("Spatial request and Grid differ.");
            if (!Enum.IsDefined(geometry) || !double.IsFinite(shiftX) || !double.IsFinite(shiftY)) throw new ArgumentException("Invalid spatial policy or shift.");
            if (geometry == Geometry.A0 && shiftX == 0 && shiftY == 0) return new(MantleNoiseFields.Build(grid, request, p));
            var seeds = MantleNoiseFields.Seeds(request.Seed);
            double[] broad = new double[grid.CellCount], regional = new double[grid.CellCount];
            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                var center = FlatHexGeometry.Center(grid.GetOffset(cell));
                broad[cell] = Sample(grid, center.X + shiftX, center.Y + shiftY, p.BroadFrequency, seeds.Broad, geometry);
                regional[cell] = Sample(grid, center.X + shiftX, center.Y + shiftY, p.RegionalFrequency, seeds.Regional, geometry);
            }
            return new(broad, regional);
        }
    }

    internal static double Sample(HexGridData grid, double x, double y, int frequency, ulong seed, Geometry geometry)
    {
        if (!Enum.IsDefined(geometry)) throw new ArgumentOutOfRangeException(nameof(geometry));
        double width = FlatHexGeometry.PeriodWidth(grid.Columns);
        double height = geometry == Geometry.A0 ? FlatHexGeometry.MapHeight(grid.Rows) : width;
        return PeriodicTectonicNoise.Sample(x / width * grid.Columns, y / height * grid.Rows, grid.Columns, grid.Rows, frequency, seed);
    }

    internal static MantlePotentialField Potential(HexGridData grid, TectonicInfluenceFields influence, Signals signals, MantleCalibrationParameters p)
    {
        if (signals.Count != grid.CellCount || influence.Count != grid.CellCount) throw new ArgumentException("Spatial input dimensions differ.");
        if (signals.Canonical is { } original) return MantlePotentialField.Combine(grid, influence, original, p);
        float[] values = new float[grid.CellCount];
        for (int cell = 0; cell < grid.CellCount; cell++)
            values[cell] = MantlePotentialField.Evaluate(influence[cell].Compression, influence[cell].Extension, signals.Broad(cell), signals.Regional(cell), p);
        return MantlePotentialField.FromValues(values);
    }

    internal readonly record struct Flow(float X, float Y, float W);
    internal sealed class Result
    {
        private readonly MantleConvectionField? canonical;
        private readonly Flow[]? flows;
        internal MantlePotentialField Potential { get; }
        internal int Count => Potential.Count;
        internal MantleBalance Balance { get; }
        internal double HorizontalBound { get; }
        internal double VerticalBound { get; }
        internal MantleCell this[int cell] => canonical is { } original ? original[cell] : new(Potential[cell], flows![cell].X, flows[cell].Y, flows[cell].W);
        private Result(MantlePotentialField potential, MantleConvectionField original) =>
            (Potential, canonical, Balance, HorizontalBound, VerticalBound) = (potential, original, original.Balance, 12, 12);
        private Result(MantlePotentialField potential, Flow[] flows, MantleBalance balance, double h, double v) =>
            (Potential, this.flows, Balance, HorizontalBound, VerticalBound) = (potential, flows, balance, h, v);

        internal static Result Build(HexGridData grid, MantlePotentialField potential, Operator operation)
        {
            if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
            if (potential.Count != grid.CellCount) throw new ArgumentException("Potential dimensions differ.");
            if (operation == Operator.R0) return new(potential, MantleConvectionField.Build(grid, potential));
            double[] x = new double[grid.CellCount], y = new double[grid.CellCount], w = new double[grid.CellCount];
            int edges = 0; double absFlux = 0, maxH = 0, maxV = 0;
            for (int first = 0; first < grid.CellCount; first++)
            for (int direction = 0; direction < 6; direction++)
                if (grid.TryGetNeighbor(first, (HexDirection)direction, out int second) && first < second)
                {
                    var n = PlateBoundaryBuilder.Normal(grid, first, second);
                    var coefficient = Coefficients(grid, first, second, operation);
                    double f = (double)potential[first] - potential[second];
                    double vertical = f * coefficient.Vertical, horizontal = f * coefficient.Horizontal;
                    double u = horizontal * n.X, v = horizontal * n.Y;
                    w[first] += vertical; w[second] -= vertical;
                    x[first] += u; x[second] += u; y[first] += v; y[second] += v;
                    absFlux = Up(absFlux + Math.Abs(vertical)); edges++;
                    maxH = Math.Max(maxH, coefficient.Horizontal); maxV = Math.Max(maxV, coefficient.Vertical);
                }
            Flow[] flows = new Flow[grid.CellCount];
            double sumD = 0, sumF = 0, absD = 0, absF = 0, rounding = 0;
            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                flows[cell] = new(Store(x[cell]), Store(y[cell]), Store(w[cell]));
                sumD += w[cell]; sumF += flows[cell].W;
                absD = Up(absD + Math.Abs(w[cell])); absF = Up(absF + Math.Abs((double)flows[cell].W));
                rounding = Up(rounding + Up(Math.Abs((double)flows[cell].W - w[cell])));
            }
            double accumulation = Up(Gamma(6) * Up(2 * absFlux));
            var balance = new MantleBalance(edges, sumD, sumF, Up(accumulation + Up(Gamma(grid.CellCount) * absD)),
                Up(Up(accumulation + rounding) + Up(Gamma(grid.CellCount) * absF)), rounding);
            return new(potential, flows, balance, Up(12 * maxH), Up(12 * maxV));
        }

        internal void Validate(HexGridData grid)
        {
            if (canonical is { } original) { original.Validate(grid); return; }
            if (Count != grid.CellCount || Balance.Edges != 3L * grid.Columns * grid.Rows - 2L * grid.Columns ||
                !double.IsFinite(Balance.DoubleBudget) || !double.IsFinite(Balance.FloatBudget) || !double.IsFinite(Balance.DoubleSum) || !double.IsFinite(Balance.FloatSum) ||
                Math.Abs(Balance.DoubleSum) > Balance.DoubleBudget || Math.Abs(Balance.FloatSum) > Balance.FloatBudget)
                throw new ArgumentException("Invalid alternative dimensions or balance.");
            double sum = 0;
            for (int cell = 0; cell < Count; cell++)
            {
                var value = this[cell];
                Check(value.Potential, 1); Check(value.FlowX, HorizontalBound); Check(value.FlowY, HorizontalBound); Check(value.SignedVertical, VerticalBound);
                sum += value.SignedVertical;
            }
            if (sum != Balance.FloatSum) throw new ArgumentException("Alternative balance differs.");
        }
    }

    internal static (double Horizontal, double Vertical) Coefficients(HexGridData grid, int first, int second, Operator operation)
    {
        if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        if (operation == Operator.R0) return (1, 1);
        if (first == second) throw new ArgumentException("Edge endpoints must differ.");
        var a = FlatHexGeometry.Center(grid.GetOffset(first)); var b = FlatHexGeometry.Center(grid.GetOffset(second));
        double width = FlatHexGeometry.PeriodWidth(grid.Columns), dx = (double)b.X - a.X, dy = (double)b.Y - a.Y;
        if (dx > width / 2) dx -= width; else if (dx < -width / 2) dx += width;
        double scale = operation is Operator.R1Width or Operator.R2Width ? 1d / width : 1;
        double distance = Math.Sqrt(dx * dx + dy * dy) * scale;
        if (operation is Operator.R1Native or Operator.R1Width) return (1d / (3d * distance), 1d / (3d * distance));
        double area = (3d * Math.Sqrt(3d) / 2d) * scale * scale;
        return (scale / (2d * area), scale / (area * distance));
    }

    internal static (double SamplesX, double SamplesY, double EdgeAdvance, string Classification) Sampling(HexGridData grid, int frequency, Geometry geometry)
    {
        if (frequency <= 0 || frequency > int.MaxValue / 4 || !Enum.IsDefined(geometry)) throw new ArgumentOutOfRangeException(nameof(frequency));
        double width = FlatHexGeometry.PeriodWidth(grid.Columns), height = geometry == Geometry.A0 ? FlatHexGeometry.MapHeight(grid.Rows) : width;
        double k = 4d * frequency, maxAdvance = Math.Max(Math.Sqrt(3d) * k / height, 1.5d * k / width + Math.Sqrt(3d) / 2d * k / height);
        return (width / (k * 1.5d), height / (k * Math.Sqrt(3d)), maxAdvance,
            maxAdvance >= .5 ? "INSUFFICIENT" : maxAdvance > .25 ? "WARNING" : "BASE-OCTAVE-MARGIN");
    }

    private static double Gamma(int count) { double nu = count * Math.ScaleB(1d, -53); return Up(nu / Math.BitDecrement(1 - nu)); }
    private static double Up(double value) => Math.BitIncrement(value);
    private static float Store(double value) { float f = (float)value; if (!double.IsFinite(value) || !float.IsFinite(f)) throw new ArgumentException("Nonfinite alternative flow."); return f == 0 ? 0 : f; }
    private static void Check(float value, double bound)
    {
        float limit = MathF.BitIncrement((float)bound);
        if (!float.IsFinite(value) || Math.Abs(value) > limit || BitConverter.SingleToUInt32Bits(value) == 0x80000000) throw new ArgumentException("Invalid alternative value.");
    }
}
