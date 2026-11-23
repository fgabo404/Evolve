// Builds private maximum-Gaussian deformation fields for headless calibration only.
// Shares a bounded Grid BFS across channels, retaining values without exposing mutable arrays.
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal sealed class TectonicInfluenceFields
{
    private readonly TectonicInfluenceChannels[] values;
    internal int Count => values.Length;
    internal TectonicInfluenceChannels this[int cell] => values[cell];
    internal long VisitedCells { get; }
    internal int AttenuationEvaluations { get; }
    internal TectonicInfluenceParameters Parameters { get; }

    private TectonicInfluenceFields(TectonicInfluenceChannels[] values, long visits, int evaluations, TectonicInfluenceParameters parameters) =>
        (this.values, VisitedCells, AttenuationEvaluations, Parameters) = (values, visits, evaluations, parameters);

    internal static TectonicInfluenceFields Build(TectonicInfluenceSources sources, TectonicInfluenceParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var grid = sources.Grid;
        int reach = parameters.GetReach(grid);
        float[] weights = new float[checked(reach + 1)];
        for (int distance = 0; distance < weights.Length; distance++)
            weights[distance] = (float)Math.Exp(-Math.Pow(distance / (double)parameters.Radius, 2));
        var values = new TectonicInfluenceChannels[grid.CellCount];
        int[] visited = new int[grid.CellCount], distances = new int[grid.CellCount], queue = new int[grid.CellCount];
        long visits = 0;
        for (int source = 0; source < grid.CellCount; source++)
        {
            var amplitude = sources[source];
            if (amplitude.Compression == 0 && amplitude.Extension == 0 && amplitude.Shear == 0) continue;
            int head = 0, tail = 1, stamp = checked(source + 1);
            queue[0] = source; visited[source] = stamp; distances[source] = 0;
            while (head < tail)
            {
                int cell = queue[head++], distance = distances[cell];
                visits++;
                float weight = weights[distance];
                var old = values[cell];
                // Each multiplication rounds to float before max, as in historical Spread.
                float c = amplitude.Compression * weight, e = amplitude.Extension * weight, s = amplitude.Shear * weight;
                values[cell] = new(Math.Max(old.Compression, c), Math.Max(old.Extension, e), Math.Max(old.Shear, s));
                if (distance == reach) continue;
                for (int direction = 0; direction < 6; direction++)
                    if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && visited[next] != stamp)
                    {
                        visited[next] = stamp;
                        distances[next] = checked(distance + 1);
                        queue[tail++] = next;
                    }
            }
        }
        return new(values, visits, weights.Length, parameters);
    }

    // This structural/range check is timed separately; independent tests verify the full Gaussian semantics.
    internal void Validate(TectonicInfluenceSources sources, TectonicBoundaryDistances distances)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(distances);
        var grid = sources.Grid;
        if (Count != grid.CellCount || distances.Count != Count) throw new ArgumentException("Field dimensions do not match.");
        for (int cell = 0; cell < Count; cell++)
        {
            var value = values[cell];
            TectonicInfluenceSources.Validate(value);
            if (value.Compression > sources.Maximum.Compression || value.Extension > sources.Maximum.Extension || value.Shear > sources.Maximum.Shear ||
                NegativeZero(value.Compression) || NegativeZero(value.Extension) || NegativeZero(value.Shear))
                throw new ArgumentException("Invalid influence range or zero encoding.");
            uint distance = distances[cell];
            if (distance >= Count || (distance == 0) != sources.IsBoundary(cell)) throw new ArgumentException("Invalid boundary distance.");
            bool descends = distance == 0;
            for (int direction = 0; direction < 6; direction++)
                if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next))
                {
                    long delta = (long)distance - distances[next];
                    if (Math.Abs(delta) > 1) throw new ArgumentException("Nonminimal distance field.");
                    descends |= delta == 1;
                }
            if (!descends) throw new ArgumentException("Distance has no path to a boundary.");
        }
    }

    private static bool NegativeZero(float value) => BitConverter.SingleToUInt32Bits(value) == 0x80000000;

    // Measure the proposed E4 trust-boundary cost without enabling a codec or normal executor integration.
    // This is a complete production recomputation, not an independent numerical oracle.
    internal void ValidateComplete(TectonicInfluenceSources sources, TectonicBoundaryDistances distances)
    {
        Validate(sources, distances);
        var expectedDistances = TectonicBoundaryDistances.Build(sources);
        var expected = Build(sources, Parameters);
        for (int cell = 0; cell < Count; cell++)
        {
            var a = values[cell];
            var b = expected[cell];
            if (distances[cell] != expectedDistances[cell] || Bits(a.Compression) != Bits(b.Compression) ||
                Bits(a.Extension) != Bits(b.Extension) || Bits(a.Shear) != Bits(b.Shear))
                throw new ArgumentException("Complete influence recomputation differs from the supplied fields.");
        }
    }

    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);
}
