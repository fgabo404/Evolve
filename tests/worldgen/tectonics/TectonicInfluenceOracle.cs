// Independently decodes M sources and evaluates each target's complete Gaussian neighborhood.
// Uses explicit Offset topology and target-centered BFS rather than production source propagation or weight tables.
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceOracle
{
    internal sealed record Inputs(HexGridData Grid, TectonicInfluenceChannels[] Sources, bool[] Boundary);
    internal sealed record Result(TectonicInfluenceChannels[] Values, uint[] Distances, long TargetVisits);

    // Read stored projection words directly, bypassing production extraction and boundary-derived properties.
    internal static Inputs Extract(HexGridData grid, LayerData<uint> packed)
    {
        Require(packed[1] == 1 && packed[2] == 7 && packed[4] == grid.CellCount, "Oracle requires published M data.");
        var values = new TectonicInfluenceChannels[grid.CellCount];
        var boundary = new bool[grid.CellCount];
        int offset = checked((int)packed[10]), count = checked((int)packed[11]);
        for (int index = 0; index < count; index++)
        {
            int row = offset + index * 5;
            int first = checked((int)packed[row]), second = checked((int)packed[row + 1]);
            float normal = BitConverter.UInt32BitsToSingle(packed[row + 2]);
            float tangent = BitConverter.UInt32BitsToSingle(packed[row + 3]);
            float c = normal < 0 ? -normal : 0, e = normal > 0 ? normal : 0, s = tangent < 0 ? -tangent : tangent;
            Add(first); Add(second);
            void Add(int cell)
            {
                boundary[cell] = true;
                var old = values[cell];
                values[cell] = new(c > old.Compression ? c : old.Compression,
                    e > old.Extension ? e : old.Extension, s > old.Shear ? s : old.Shear);
            }
        }
        return new(grid, values, boundary);
    }

    internal static void CheckSources(Inputs expected, TectonicInfluenceSources actual)
    {
        for (int cell = 0; cell < expected.Grid.CellCount; cell++)
            Require(expected.Sources[cell] == actual[cell] && expected.Boundary[cell] == actual.IsBoundary(cell), "Complete source oracle mismatch.");
    }

    internal static Result Compute(Inputs inputs, TectonicInfluenceParameters parameters)
    {
        var grid = inputs.Grid;
        int count = grid.CellCount;
        int reach = Math.Min(count - 1, checked((int)Math.Ceiling((double)parameters.Radius * parameters.Cutoff)));
        var values = new TectonicInfluenceChannels[count];
        var boundaryDistances = Distances(inputs);
        int[] queue = new int[count], marks = new int[count];
        Span<int> neighbors = stackalloc int[6];
        long visits = 0;
        for (int target = count - 1; target >= 0; target--)
        {
            int stamp = target + 1, head = 0, tail = 1;
            queue[0] = target; marks[target] = stamp;
            float c = 0, e = 0, s = 0;
            for (int distance = 0; distance <= reach && head < tail; distance++)
            {
                int endOfShell = tail;
                while (head < endOfShell)
                {
                    int source = queue[head++]; visits++;
                    var amplitude = inputs.Sources[source];
                    if (amplitude.Compression > 0 || amplitude.Extension > 0 || amplitude.Shear > 0)
                    {
                        // Directly evaluate the mathematical envelope at each encountered source, without a shared table.
                        float weight = (float)Math.Exp(-Math.Pow(distance / (double)parameters.Radius, 2d));
                        float vc = amplitude.Compression * weight, ve = amplitude.Extension * weight, vs = amplitude.Shear * weight;
                        if (vc > c) c = vc;
                        if (ve > e) e = ve;
                        if (vs > s) s = vs;
                    }
                    if (distance == reach) continue;
                    int neighborCount = FillNeighbors(grid, source, neighbors);
                    for (int index = neighborCount - 1; index >= 0; index--)
                    {
                        int next = neighbors[index];
                        if (marks[next] == stamp) continue;
                        marks[next] = stamp; queue[tail++] = next;
                    }
                }
            }
            values[target] = new(c, e, s);
        }
        return new(values, boundaryDistances, visits);
    }

    internal static uint[] Distances(Inputs inputs)
    {
        var grid = inputs.Grid;
        uint[] values = new uint[grid.CellCount]; Array.Fill(values, uint.MaxValue);
        int[] queue = new int[grid.CellCount]; int head = 0, tail = 0;
        for (int cell = grid.CellCount - 1; cell >= 0; cell--)
            if (inputs.Boundary[cell]) { queue[tail++] = cell; values[cell] = 0; }
        Require(tail > 0, "Oracle requires boundary membership.");
        Span<int> neighbors = stackalloc int[6];
        while (head < tail)
        {
            int cell = queue[head++], count = FillNeighbors(grid, cell, neighbors);
            for (int index = count - 1; index >= 0; index--)
            {
                int next = neighbors[index];
                if (values[next] != uint.MaxValue) continue;
                values[next] = checked(values[cell] + 1); queue[tail++] = next;
            }
        }
        Require(tail == grid.CellCount, "Oracle graph is disconnected.");
        return values;
    }

    internal static void Compare(Result expected, TectonicInfluenceFields fields, TectonicBoundaryDistances distances)
    {
        Require(expected.Values.Length == fields.Count && expected.Distances.Length == distances.Count, "Oracle dimensions.");
        for (int cell = 0; cell < fields.Count; cell++)
        {
            var a = expected.Values[cell]; var b = fields[cell];
            if (expected.Distances[cell] != distances[cell] || Bits(a.Compression) != Bits(b.Compression) ||
                Bits(a.Extension) != Bits(b.Extension) || Bits(a.Shear) != Bits(b.Shear))
                throw new InvalidOperationException($"Full independent oracle mismatch at CellId {cell}.");
        }
    }

    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);

    private static int FillNeighbors(HexGridData grid, int cell, Span<int> output)
    {
        int x = cell % grid.Columns, y = cell / grid.Columns, upper = y - 1 + (x & 1);
        int left = x == 0 ? grid.Columns - 1 : x - 1, right = x == grid.Columns - 1 ? 0 : x + 1;
        int count = 0;
        if (y > 0) output[count++] = cell - grid.Columns;
        if (y + 1 < grid.Rows) output[count++] = cell + grid.Columns;
        if (upper >= 0 && upper < grid.Rows) { output[count++] = upper * grid.Columns + left; output[count++] = upper * grid.Columns + right; }
        if (upper + 1 >= 0 && upper + 1 < grid.Rows) { output[count++] = (upper + 1) * grid.Columns + left; output[count++] = (upper + 1) * grid.Columns + right; }
        return count;
    }
}
