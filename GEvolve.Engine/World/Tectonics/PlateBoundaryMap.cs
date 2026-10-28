using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Tectonics;

/// <summary>A unique shared edge between two different tectonic plates.</summary>
public readonly record struct PlateBoundaryEdge(int FirstCellId, int SecondCellId);

/// <summary>Derived diagnostic data. Does not alter or copy the source plate array.</summary>
public sealed class PlateBoundaryMap
{
    public const string Version = "plate-boundaries-1";
    public IReadOnlyList<PlateBoundaryEdge> Edges { get; }
    public bool[] BoundaryByCellId { get; }
    public ushort[] DistanceToBoundaryByCellId { get; }
    public int BoundaryCellCount { get; }

    private PlateBoundaryMap(List<PlateBoundaryEdge> edges, bool[] boundary, ushort[] distance)
    {
        Edges = edges;
        BoundaryByCellId = boundary;
        DistanceToBoundaryByCellId = distance;
        BoundaryCellCount = boundary.Count(value => value);
    }

    public static PlateBoundaryMap? Analyze(HexTopology topology, LandmassState terrain)
    {
        if (terrain.PlateByCellId is not { } plates) return null;
        if (topology.Count != plates.Length || terrain.SurfaceByCellId.Length != topology.Count)
            throw new ArgumentException("Plate and topology dimensions differ.", nameof(terrain));

        var edges = new List<PlateBoundaryEdge>();
        var boundary = new bool[topology.Count];
        for (int id = 0; id < topology.Count; id++)
        foreach (int neighbor in topology.Neighbors(id))
            if (id < neighbor && plates[id] != plates[neighbor])
            {
                edges.Add(new PlateBoundaryEdge(id, neighbor));
                boundary[id] = true;
                boundary[neighbor] = true;
            }

        var distance = new ushort[topology.Count];
        Array.Fill(distance, ushort.MaxValue);
        var queue = new Queue<int>();
        for (int id = 0; id < topology.Count; id++)
            if (boundary[id])
            {
                distance[id] = 0;
                queue.Enqueue(id);
            }

        while (queue.TryDequeue(out int id))
        foreach (int neighbor in topology.Neighbors(id))
            if (distance[neighbor] == ushort.MaxValue)
            {
                distance[neighbor] = checked((ushort)(distance[id] + 1));
                queue.Enqueue(neighbor);
            }
        return new PlateBoundaryMap(edges, boundary, distance);
    }
}
