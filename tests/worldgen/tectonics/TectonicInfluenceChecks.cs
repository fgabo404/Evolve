// Checks experimental deformation against independent Offset BFS and direct all-source envelopes.
// Protects frozen published R/M results while exercising internal-only Influence builders.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceChecks
{
    internal static void Sources()
    {
        var grid = new HexGridData(MapSize.Small);
        int a = grid.GetCellId(new(40, 40)), b = a + grid.Columns, c = b + grid.Columns;
        TectonicBoundary Edge(int first, int second, float normal, float tangent) =>
            new(first, second, 0, 1, new(0, 1), normal, tangent, TectonicInteractionKind.Mixed);
        var single = TectonicInfluenceSources.FromBoundaries(grid, [Edge(a, b, -2, -3)]);
        Require(single[a] == new TectonicInfluenceChannels(2, 0, 3) && single[b] == single[a], "Both endpoints receive stored projections.");
        var shared = TectonicInfluenceSources.FromBoundaries(grid, [Edge(a, b, -2, -3), Edge(b, c, 5, 1), Edge(a, b, -1, 2)]);
        Require(shared[b] == new TectonicInfluenceChannels(2, 5, 3) && shared.ActiveCount == 3, "Independent maxima, not a sum.");
        var quiet = TectonicInfluenceSources.FromBoundaries(grid, [Edge(a, b, 0, -0f)]);
        Require(quiet.ActiveCount == 0 && quiet.IsBoundary(a) && quiet.IsBoundary(b), "Quiet boundary membership.");
        var distances = TectonicBoundaryDistances.Build(quiet);
        var fields = TectonicInfluenceFields.Build(quiet, new(1, 3));
        fields.Validate(quiet, distances);
        Require(fields.VisitedCells == 0 && distances[a] == 0 && distances[b] == 0, "Zero channels do not remove boundary distance.");
        Expect<ArgumentException>(() => TectonicInfluenceSources.FromBoundaries(grid, [Edge(a, c, 1, 1)]));
        Expect<ArgumentException>(() => TectonicInfluenceSources.FromBoundaries(grid, [Edge(a, b, float.NaN, 0)]));
        var input = new[] { new TectonicInfluenceSource(a, new(1, -0f, 2)) };
        var owned = TectonicInfluenceSources.Create(grid, input, [a]);
        input[0] = new(a, new(99, 99, 99));
        Require(owned[a] == new TectonicInfluenceChannels(1, 0, 2) && BitConverter.SingleToUInt32Bits(owned[a].Extension) == 0, "Input ownership and canonical zero.");
    }

    internal static void Numeric()
    {
        var grid = new HexGridData(MapSize.Small);
        foreach (float bad in new[] { -1f, 0f, -0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Expect<ArgumentOutOfRangeException>(() => new TectonicInfluenceParameters(bad, 3));
            Expect<ArgumentOutOfRangeException>(() => new TectonicInfluenceParameters(1, bad));
            Expect<ArgumentOutOfRangeException>(() => TectonicInfluenceParameters.Resolve(grid, 1, bad, 3));
        }
        Expect<ArgumentOutOfRangeException>(() => default(TectonicInfluenceParameters).GetReach(grid));
        Expect<OverflowException>(() => new TectonicInfluenceParameters(float.MaxValue, float.MaxValue).GetReach(grid));
        Expect<OverflowException>(() => new TectonicInfluenceParameters(2147483648f, 1).GetReach(grid));
        Expect<OverflowException>(() => TectonicInfluenceParameters.Resolve(grid, 1, float.MaxValue, 3));
        Require(new TectonicInfluenceParameters(1, 0.25f).GetReach(grid) == 1 &&
            new TectonicInfluenceParameters(1, 6).GetReach(grid) == 6, "Historical cutoff range is not silently imposed.");
        Require(new TectonicInfluenceParameters(1, 2147483520f).GetReach(grid) == grid.CellCount - 1, "Representable reach is clipped only after validation.");
        foreach (float bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        foreach (int channel in Enumerable.Range(0, 3))
        {
            var value = channel switch { 0 => new TectonicInfluenceChannels(bad, 0, 0), 1 => new(0, bad, 0), _ => new(0, 0, bad) };
            Expect<ArgumentException>(() => TectonicInfluenceSources.Create(grid, [new(0, value)], [0]));
        }
        Expect<ArgumentOutOfRangeException>(() => TectonicInfluenceSources.Create(grid, [new(-1, new())], [0]));
        Expect<ArgumentOutOfRangeException>(() => TectonicInfluenceSources.Create(grid, [], [grid.CellCount]));
        Expect<ArgumentException>(() => TectonicBoundaryDistances.Build(TectonicInfluenceSources.Create(grid, [], [])));
        var extreme = TectonicInfluenceSources.Create(grid, [new(0, new(float.MaxValue, float.Epsilon, -0f))], [0]);
        var tiny = TectonicInfluenceFields.Build(extreme, new(float.Epsilon, float.Epsilon));
        tiny.Validate(extreme, TectonicBoundaryDistances.Build(extreme));
        Require(tiny[0].Compression == float.MaxValue && tiny[0].Extension == float.Epsilon && tiny[grid.Columns].Compression == 0,
            "Finite extreme amplitudes and underflow are valid; zero stays canonical.");
    }

    internal static void Propagation()
    {
        var grid = new HexGridData(MapSize.Small);
        int first = grid.GetCellId(new(0, 50)), far = grid.GetCellId(new(0, 54));
        TectonicInfluenceSource[] samples = [new(first, new(0.01f, 3, 0)), new(far, new(8, 0, 2)), new(0, new(0, 0, 0))];
        var sources = TectonicInfluenceSources.Create(grid, samples, [first, far, 0]);
        var fields = TectonicInfluenceFields.Build(sources, new(2, 1.5f));
        var distances = TectonicBoundaryDistances.Build(sources);
        fields.Validate(sources, distances);
        VerifyOracle(grid, sources, fields, Enumerable.Range(0, grid.CellCount));
        var expectedDistances = new[] { ReferenceDistances(grid, first), ReferenceDistances(grid, far), ReferenceDistances(grid, 0) };
        for (int cell = 0; cell < grid.CellCount; cell++)
            Require(distances[cell] == expectedDistances.Min(array => array[cell]), "Independent nearest-boundary BFS.");
        var permuted = TectonicInfluenceSources.Create(grid, samples.Reverse(), [0, far, first]);
        var reversed = TectonicInfluenceFields.Build(permuted, new(2, 1.5f));
        var reverseDistances = TectonicBoundaryDistances.Build(permuted);
        for (int cell = 0; cell < grid.CellCount; cell++)
            Require(fields[cell] == reversed[cell] && distances[cell] == reverseDistances[cell], "Source order changes result.");
        Require(fields[grid.GetCellId(new(0, 51))].Compression > 0.01f, "A stronger farther source wins.");

        var single = TectonicInfluenceSources.Create(grid, [new(first, new(4, 2, 1))], [first]);
        var truncated = TectonicInfluenceFields.Build(single, new(2, 1.5f));
        for (int d = 0; d <= 4; d++)
        {
            float weight = (float)Math.Exp(-Math.Pow(d / 2d, 2));
            float expected = d <= 3 ? 4f * weight : 0;
            EqualBits(truncated[grid.GetCellId(new(0, 50 + d))].Compression, expected, "Inclusive final shell and next-cell cutoff.");
        }
        var polar = TectonicInfluenceSources.Create(grid, [new(0, new(1, 1, 1))], [0]);
        var polarFields = TectonicInfluenceFields.Build(polar, new(1, 3));
        Require(polarFields[grid.GetCellId(new(grid.Columns - 1, 0))].Compression > 0 &&
            polarFields[grid.GetCellId(new(0, grid.Rows - 1))].Compression == 0, "Horizontal seam and no artificial polar wrap.");
        var distanceOne = TectonicInfluenceFields.Build(single, new(1.234567f, 2.345678f));
        VerifyOracle(grid, single, distanceOne, Enumerable.Range(0, grid.CellCount));
    }

    internal static void Compatibility()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            uint[] original = packed.ToArray();
            var data = TectonicData.FromLayerData(packed, grid);
            var sources = TectonicInfluenceSources.Extract(grid, data);
            var parameters = TectonicInfluenceParameters.Resolve(grid, 1, 0.02f, 3);
            var fields = TectonicInfluenceFields.Build(sources, parameters);
            var distances = TectonicBoundaryDistances.Build(sources);
            fields.Validate(sources, distances);
            int[] probes = [0, grid.Columns - 1, grid.CellCount - grid.Columns, grid.CellCount - 1,
                grid.CellCount / 2, grid.CellCount / 2 + grid.Columns / 2];
            VerifyOracle(grid, sources, fields, probes);
            foreach (var type in Enum.GetValues<WorldType>())
            {
                var (_, again) = GenerateComplete(size, seed, type);
                var againSources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(again, grid));
                var replay = TectonicInfluenceFields.Build(againSources, parameters);
                var replayDistances = TectonicBoundaryDistances.Build(againSources);
                for (int cell = 0; cell < grid.CellCount; cell++)
                    Require(fields[cell] == replay[cell] && distances[cell] == replayDistances[cell], "Complete deformation replay/world-type invariance.");
                Require(original.SequenceEqual(again), "Published R/M changed.");
            }
            Require(data.PlateCount == 14 && packed[1] == 1 && packed[2] == 7 && !data.HasInfluence && original.SequenceEqual(packed), "Experimental fields leaked into publication.");
        }
    }

    // Full independent Offset BFS uses neither production Grid traversal nor its distance formula.
    internal static int[] ReferenceDistances(HexGridData grid, int start)
    {
        int[] result = Enumerable.Repeat(-1, grid.CellCount).ToArray();
        var queue = new Queue<int>(); queue.Enqueue(start); result[start] = 0;
        while (queue.TryDequeue(out int cell))
            foreach (int next in Neighbors(cell, grid.Columns, grid.Rows))
                if (result[next] < 0) { result[next] = result[cell] + 1; queue.Enqueue(next); }
        return result;
    }

    // Scan every source directly for each target; no production BFS stamps or attenuation table are reused.
    private static void VerifyOracle(HexGridData grid, TectonicInfluenceSources sources, TectonicInfluenceFields actual, IEnumerable<int> targets)
    {
        int reach = checked((int)Math.Ceiling((double)actual.Parameters.Radius * actual.Parameters.Cutoff));
        var active = Enumerable.Range(0, grid.CellCount).Where(cell => sources[cell] != new TectonicInfluenceChannels()).ToArray();
        // Sparse full-map fixtures use independently traversed source distances; real dense maps use target BFS.
        var sourceDistances = active.Length <= 8 ? active.Select(cell => ReferenceDistances(grid, cell)).ToArray() : null;
        foreach (int target in targets)
        {
            var targetDistances = sourceDistances is null ? ReferenceDistances(grid, target) : null;
            float[] expected = new float[3];
            for (int index = active.Length - 1; index >= 0; index--)
            {
                int source = active[index], distance = sourceDistances is null ? targetDistances![source] : sourceDistances[index][target];
                if (distance > reach) continue;
                float attenuation = (float)Math.Exp(-Math.Pow(distance / (double)actual.Parameters.Radius, 2d));
                var value = sources[source];
                expected[0] = Math.Max(expected[0], value.Compression * attenuation);
                expected[1] = Math.Max(expected[1], value.Extension * attenuation);
                expected[2] = Math.Max(expected[2], value.Shear * attenuation);
            }
            EqualBits(actual[target].Compression, expected[0], "Compression oracle.");
            EqualBits(actual[target].Extension, expected[1], "Extension oracle.");
            EqualBits(actual[target].Shear, expected[2], "Shear oracle.");
        }
    }

    private static void EqualBits(float actual, float expected, string message) =>
        Require(BitConverter.SingleToUInt32Bits(actual) == BitConverter.SingleToUInt32Bits(expected), message);
}
