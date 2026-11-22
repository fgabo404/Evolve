// Computes immutable nearest-boundary graph distances with one multi-source Grid BFS.
// Boundary membership includes quiet edges and never depends on channel amplitude.
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal sealed class TectonicBoundaryDistances
{
    private readonly uint[] distances;
    internal int Count => distances.Length;
    internal uint this[int cell] => distances[cell];
    private TectonicBoundaryDistances(uint[] distances) => this.distances = distances;

    internal static TectonicBoundaryDistances Build(TectonicInfluenceSources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var grid = sources.Grid;
        uint[] distances = new uint[grid.CellCount];
        Array.Fill(distances, uint.MaxValue);
        int[] queue = new int[grid.CellCount];
        int head = 0, tail = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
            if (sources.IsBoundary(cell)) { distances[cell] = 0; queue[tail++] = cell; }
        if (tail == 0) throw new ArgumentException("Nearest-boundary distance requires at least one boundary cell.");
        while (head < tail)
        {
            int cell = queue[head++];
            uint nextDistance = checked(distances[cell] + 1);
            for (int direction = 0; direction < 6; direction++)
                if (grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) && distances[next] == uint.MaxValue)
                {
                    distances[next] = nextDistance;
                    queue[tail++] = next;
                }
        }
        if (tail != grid.CellCount) throw new InvalidOperationException("Grid contains unreachable cells.");
        return new(distances);
    }
}
