using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed record LandmassStatistics(int LandCells, int TotalCells,
    IReadOnlyList<int> ComponentSizes, int IslandComponents)
{
    public double LandFraction => (double)LandCells / TotalCells;
    public int ComponentCount => ComponentSizes.Count;
    public int DominantMassSize => ComponentSizes.Count == 0 ? 0 : ComponentSizes[0];
    public double DominantLandShare => LandCells == 0 ? 0 : (double)DominantMassSize / LandCells;
    public int SmallComponents => ComponentSizes.Count(size => size <= 40);
    public int MediumComponents => ComponentSizes.Count(size => size is > 40 and <= 400);
    public int LargeComponents => ComponentSizes.Count(size => size > 400);
}

public static class LandmassAnalysis
{
    /// <summary>Componentes calculadas con la vecindad cilíndrica de Engine.</summary>
    public static LandmassStatistics Analyze(HexTopology topology, ReadOnlySpan<byte> surface)
    {
        if (surface.Length != topology.Count)
            throw new ArgumentException("Surface length differs from grid size.", nameof(surface));
        var visited = new bool[topology.Count];
        var queue = new int[topology.Count];
        var sizes = new List<int>();
        int land = 0;
        for (int id = 0; id < topology.Count; id++)
        {
            if (surface[id] == 0) continue;
            if (surface[id] != 1) throw new ArgumentException("Invalid surface value.", nameof(surface));
            land++;
            if (visited[id]) continue;
            int head = 0, tail = 0;
            queue[tail++] = id;
            visited[id] = true;
            while (head < tail)
            {
                foreach (int neighbor in topology.Neighbors(queue[head++]))
                {
                    if (surface[neighbor] == 0 || visited[neighbor]) continue;
                    visited[neighbor] = true;
                    queue[tail++] = neighbor;
                }
            }
            sizes.Add(tail);
        }
        sizes.Sort((a, b) => b.CompareTo(a));
        return new LandmassStatistics(land, topology.Count, sizes,
            sizes.Count(size => size <= 40));
    }
}
