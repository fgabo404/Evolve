// Verifies geometric alternatives with independent Offset geometry and a shared continuous resolution control.
// Protects exact CAL01 controls and sampling semantics without changing canonical numerical implementations.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;
using static Evolve.WorldGeneration.Tectonics.Checks.MantleSpatialAlternatives;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleSpatialChecks
{
    internal sealed record ContinuousRow(string Size, string Operator, int InteriorCells, double HorizontalRmsError, double VerticalRmsError, double MaximumTaylorFraction);

    internal static void Controlled() => Continuous(null);

    internal static void Continuous(List<ContinuousRow>? output)
    {
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size); double width = 1.5d * grid.Columns;
            float[] values = Enumerable.Range(0, grid.CellCount).Select(cell =>
            { var xy = Center(grid, cell); return (float)(Math.Sin(2 * Math.PI * xy.X / width) + .5 * Math.Cos(2 * Math.PI * xy.Y / width)); }).ToArray();
            // Divide the analytic field by 1.5 to honor the bounded Model A potential contract.
            for (int cell = 0; cell < values.Length; cell++) values[cell] /= 1.5f;
            var potential = MantlePotentialField.FromValues(values);
            foreach (var operation in Enum.GetValues<Operator>())
            {
                var field = Result.Build(grid, potential, operation); field.Validate(grid); Oracle(grid, field, operation);
                double hError = 0, vError = 0, maxFraction = 0; int count = 0;
                for (int cell = 2 * grid.Columns; cell < grid.CellCount - 2 * grid.Columns; cell++)
                {
                    var center = Center(grid, cell); double x = center.X / width, y = center.Y / width;
                    double gx = -2 * Math.PI * Math.Cos(2 * Math.PI * x) / 1.5, gy = Math.PI * Math.Sin(2 * Math.PI * y) / 1.5;
                    double lap = (4 * Math.PI * Math.PI * Math.Sin(2 * Math.PI * x) + 2 * Math.PI * Math.PI * Math.Cos(2 * Math.PI * y)) / 1.5;
                    double hScale = operation switch { Operator.R0 => 3 * Math.Sqrt(3) / width, Operator.R1Native or Operator.R2Native => 1 / width, _ => 1 };
                    double vScale = operation switch { Operator.R0 => 4.5 / (width * width), Operator.R1Native => Math.Sqrt(3) / (2 * width * width), Operator.R1Width => Math.Sqrt(3) / (2 * width), Operator.R2Native => 1 / (width * width), _ => 1 };
                    var actual = field[cell];
                    hError += Math.Pow(actual.FlowX - hScale * gx, 2) + Math.Pow(actual.FlowY - hScale * gy, 2);
                    vError += Math.Pow(actual.SignedVertical - vScale * lap, 2); count++;
                    if (operation == Operator.R2Width)
                    {
                        // Conservative first/second-moment + third-derivative Taylor bound, with measured input rounding.
                        double phi = (Math.Sin(2 * Math.PI * x) + .5 * Math.Cos(2 * Math.PI * y)) / 1.5;
                        double mx = 0, my = 0, mxx = 0, myy = 0, remainder = 0, quantization = 0, absolute = 0;
                        foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
                        {
                            var delta = Delta(grid, cell, next); double dx = delta.X / width, dy = delta.Y / width;
                            var nxy = Center(grid, next);
                            double other = (Math.Sin(2 * Math.PI * nxy.X / width) + .5 * Math.Cos(2 * Math.PI * nxy.Y / width)) / 1.5;
                            double coefficient = ScalarCoefficients(grid, cell, next, operation).V;
                            mx += coefficient * dx; my += coefficient * dy; mxx += coefficient * dx * dx; myy += coefficient * dy * dy;
                            remainder += coefficient * (Math.Pow(2 * Math.PI, 3) / 1.5 * Math.Pow(Math.Abs(dx), 3) + Math.Pow(2 * Math.PI, 3) / 3 * Math.Pow(Math.Abs(dy), 3)) / 6;
                            quantization += coefficient * (Math.Abs(potential[cell] - phi) + Math.Abs(potential[next] - other));
                            absolute += Math.Abs(coefficient * ((double)potential[cell] - potential[next]));
                        }
                        double hxx = -4 * Math.PI * Math.PI * Math.Sin(2 * Math.PI * x) / 1.5, hyy = -2 * Math.PI * Math.PI * Math.Cos(2 * Math.PI * y) / 1.5;
                        double geometry = Math.Abs(-gx * mx - gy * my) + Math.Abs(hxx * (1 - .5 * mxx) + hyy * (1 - .5 * myy));
                        // A full adjacent output ULP covers the final float rounding, including binade boundaries.
                        double floatUlp = Math.Abs((double)MathF.BitIncrement(actual.SignedVertical) - actual.SignedVertical);
                        double budget = Math.BitIncrement(geometry + remainder + quantization + floatUlp + 128 * Math.ScaleB(1d, -53) * (absolute + Math.Abs(lap) + 1));
                        double error = Math.Abs(actual.SignedVertical - lap);
                        Require(error <= budget, "Controlled finite-volume Taylor/rounding bound.");
                        maxFraction = Math.Max(maxFraction, error / budget);
                    }
                }
                output?.Add(new(size.ToString(), operation.ToString(), count, Math.Sqrt(hError / count), Math.Sqrt(vError / count), maxFraction));
            }
        }
        var small = new HexGridData(MapSize.Small);
        float[] constant = Enumerable.Repeat(.5f, small.CellCount).ToArray(); var owned = MantlePotentialField.FromValues(constant); constant[0] = -1;
        foreach (var operation in Enum.GetValues<Operator>())
        {
            var field = Result.Build(small, owned, operation); field.Validate(small);
            for (int cell = 0; cell < field.Count; cell++) Require(field[cell] == new MantleCell(.5f, 0, 0, 0), "Constant-field operator/ownership.");
        }
        Expect<ArgumentOutOfRangeException>(() => Result.Build(small, owned, (Operator)99));
        Expect<ArgumentException>(() => Coefficients(small, 0, 0, Operator.R2Width));
    }

    internal static void SamplingAndGeometry()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (var geometry in Enum.GetValues<Geometry>())
        foreach (int frequency in new[] { 3, 6, 12, 24 })
        {
            var grid = new HexGridData(size); var screen = Sampling(grid, frequency, geometry);
            Require(screen.SamplesX > 0 && screen.SamplesY > 0 && screen.EdgeAdvance > 0, "Positive sampling measures.");
            if (size == MapSize.Small && frequency == 24) Require(screen.Classification == "INSUFFICIENT", "Sampling screen must reject underresolved diagnostic.");
            double period = 1.5d * grid.Columns;
            var seeds = MantleNoiseFields.Seeds(long.MinValue);
            foreach (int cell in new[] { 0, grid.Columns - 1, grid.CellCount / 2, grid.CellCount - 1 })
            {
                var center = Center(grid, cell);
                double actual = Sample(grid, center.X + .375, center.Y + Math.Sqrt(3) / 4, frequency, seeds.Broad, geometry);
                double height = geometry == Geometry.A0 ? Math.Sqrt(3) * (grid.Rows + .5) : period;
                double expected = PeriodicTectonicNoise.Sample((center.X + .375) / period * grid.Columns,
                    (center.Y + Math.Sqrt(3) / 4) / height * grid.Rows, grid.Columns, grid.Rows, frequency, seeds.Broad);
                Require(actual == expected, "Independent coordinate policy/shift adapter.");
                double wrapped = Sample(grid, center.X + .375 + period, center.Y + Math.Sqrt(3) / 4, frequency, seeds.Broad, geometry);
                // Mapping plus three octave interpolations: conservative first-order guard at tested f<=24.
                Require(Math.Abs(actual - wrapped) <= 2048 * Math.ScaleB(1d, -52), "Shifted seam roundoff.");
            }
        }
        var small = new HexGridData(MapSize.Small); var p = new MantleCalibrationParameters(2,6,2,2,2,1);
        Expect<ArgumentOutOfRangeException>(() => Sampling(small, 0, Geometry.A0));
        Expect<ArgumentOutOfRangeException>(() => Sample(small, 0, 0, 2, 0, (Geometry)99));
        Expect<ArgumentException>(() => Signals.Build(small, Request(MapSize.Medium), p, Geometry.A1));
        Expect<ArgumentException>(() => Signals.Build(small, Request(MapSize.Small), p, Geometry.A1, double.NaN));
    }

    internal static void RealMaps()
    {
        var matrix = MantleSpatialDesign.Matrix(MantleSpatialDesign.Resolve());
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in MantleSpatialDesign.Seeds)
        {
            var (grid, packed) = GenerateComplete(size, seed); var original = packed.ToArray();
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var influence = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources); string earlier = TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances));
            foreach (var row in matrix)
            {
                var signals = Signals.Build(grid, Request(size, seed), row.Parameters, row.SpatialPolicy);
                var potential = Potential(grid, influence, signals, row.Parameters);
                var result = Result.Build(grid, potential, row.Operation); result.Validate(grid); Oracle(grid, result, row.Operation);
                string hash = MantleSpatialDesign.Hash(result); MantleSpatialDesign.VerifyHistorical(size.ToString(), seed, row.Name, hash);
                var replay = Result.Build(grid, Potential(grid, influence, Signals.Build(grid, Request(size, seed), row.Parameters, row.SpatialPolicy), row.Parameters), row.Operation);
                Require(hash == MantleSpatialDesign.Hash(replay), "Alternative full replay differs.");
            }
            Require(original.SequenceEqual(packed) && packed[1] == 1 && packed[2] == 7 && earlier == TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances)), "Frozen results/publication changed.");
            Console.WriteLine($"CAL02 REAL {size}/{seed}: complete 15-row operator oracle/replay matrix; exact CAL01 controls and frozen fields preserved.");
        }
        Console.WriteLine("CAL02-INFO-01: 60/60 complete historical controls automated, including 24 LOW/HIGH cases; candidates remain separate from the selected profile.");
    }

    internal static void Oracle(HexGridData grid, Result field, Operator operation)
    {
        if (operation == Operator.R0) return; // Existing CAL01 independent R0 oracle remains in the full suite.
        double[] x = new double[grid.CellCount], y = new double[grid.CellCount], w = new double[grid.CellCount];
        int edges = 0;
        for (int first = 0; first < grid.CellCount; first++)
        foreach (int second in Neighbors(first, grid.Columns, grid.Rows))
        {
            if (first >= second) continue;
            var delta = Delta(grid, first, second); double length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            var coefficient = ScalarCoefficients(grid, first, second, operation);
            double f = (double)field[first].Potential - field[second].Potential;
            double q = f * coefficient.V, h = f * coefficient.H;
            double u = h * (delta.X / length), v = h * (delta.Y / length);
            x[first] += u; x[second] += u; y[first] += v; y[second] += v; w[first] += q; w[second] -= q; edges++;
        }
        Require(edges == field.Balance.Edges, "Independent alternative edge count.");
        for (int cell = 0; cell < grid.CellCount; cell++)
            Require(Bits(field[cell].FlowX) == Bits((float)x[cell]) && Bits(field[cell].FlowY) == Bits((float)y[cell]) && Bits(field[cell].SignedVertical) == Bits((float)w[cell]), "Exact complete geometric operator oracle.");
    }

    private static (double H, double V) ScalarCoefficients(HexGridData grid, int first, int second, Operator operation)
    {
        var delta = Delta(grid, first, second);
        double unit = operation is Operator.R1Width or Operator.R2Width ? 1d / (1.5d * grid.Columns) : 1d;
        double d = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) * unit;
        if (operation is Operator.R1Native or Operator.R1Width) { double c = 1d / (3d * d); return (c, c); }
        double area = (3d * Math.Sqrt(3d) / 2d) * unit * unit;
        return (unit / (2d * area), unit / (area * d));
    }
    private static (double X, double Y) Delta(HexGridData grid, int first, int second)
    {
        var a = Center(grid, first); var b = Center(grid, second); double x = (double)b.X - a.X, y = (double)b.Y - a.Y, p = 1.5d * grid.Columns;
        if (x > p / 2) x -= p; if (x < -p / 2) x += p;
        return (x, y);
    }
    private static (float X, float Y) Center(HexGridData grid, int cell) => ((float)(1d + 1.5d * (cell % grid.Columns)),
        (float)(Math.Sqrt(3d) * (cell / grid.Columns + .5d * ((cell % grid.Columns) & 1) + .5d)));
    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value == 0 ? 0 : value);
}
