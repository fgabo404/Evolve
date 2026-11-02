using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Measures actual connected high terrain, independently of the marine margin length.</summary>
public static class OrogenicRangeMetrics
{
    public static int MinimumCrestLength(HexTopology topology) => ContinentalOrogenySettings.MinimumCrestLength(topology.Rows);

    public static TectonicReliefSystem Measure(HexTopology topology, TectonicReliefSystem system,
        byte[] heights, IEnumerable<int> footprint, int[] flanks)
    {
        var mountain = footprint.Where(id => heights[id] >= 44).ToHashSet();
        int[] longest = [];
        var highCrest = system.CrestCellIds.Where(id => heights[id] >= 44).ToHashSet();
        foreach (var component in Components(topology, highCrest))
        {
            int far = Farthest(component.Min(), component, out _);
            int end = Farthest(far, component, out var parents);
            var path = new List<int> { end };
            while (end != far) { end = parents[end]; path.Add(end); }
            if (path.Count > longest.Length) longest = path.ToArray();
        }
        double prominence = longest.Length == 0 || flanks.Length == 0 ? 0 :
            Math.Max(0, longest.Average(id => heights[id]) - flanks.Average(id => heights[id]));
        var connected = longest.Length == 0 ? [] : Components(topology, mountain)
            .First(component => component.Contains(longest[0]));
        var crestDistances = new Dictionary<int, int>();
        var front = new Queue<int>();
        foreach (int id in highCrest.Where(connected.Contains).Order())
        { crestDistances[id] = 0; front.Enqueue(id); }
        while (front.Count > 0)
        {
            int id = front.Dequeue();
            foreach (int next in topology.Neighbors(id))
                if (connected.Contains(next) && crestDistances.TryAdd(next, crestDistances[id] + 1))
                    front.Enqueue(next);
        }
        return system with
        {
            MainCrestCellIds = longest,
            ConnectedMountainCells = connected.Count,
            ContinuousCrestLength = Math.Max(0, longest.Length - 1),
            // In a symmetric strip of radius R the mean distance to the axis is R/2.
            // Four times that mean estimates the full width, without inflating branch junctions
            // by dividing their entire area by only the longest branch's length.
            EffectiveBeltWidth = crestDistances.Count == 0 ? 0 : 1 + 4 * crestDistances.Values.Average(),
            MeanCrestProminence = prominence
        };

        int Farthest(int start, HashSet<int> domain, out Dictionary<int, int> parents)
        {
            parents = new Dictionary<int, int> { [start] = start };
            var queue = new Queue<int>(); queue.Enqueue(start);
            int last = start;
            while (queue.Count > 0)
            {
                last = queue.Dequeue();
                foreach (int next in topology.Neighbors(last))
                    if (domain.Contains(next) && parents.TryAdd(next, last)) queue.Enqueue(next);
            }
            return last;
        }
    }

    public static bool IsPrincipal(HexTopology topology, TectonicReliefSystem system)
    {
        return system.Kind is GeologicalContactKind.ContinentalCollision or
                GeologicalContactKind.OceanContinentSubduction &&
            ContinentalOrogenySettings.MeetsPrincipalGeometry(topology.Rows, system.ContinuousCrestLength,
                system.ConnectedMountainCells, system.EffectiveBeltWidth, system.MeanCrestProminence);
    }

    private static IEnumerable<HashSet<int>> Components(HexTopology topology, HashSet<int> cells)
    {
        var remaining = new HashSet<int>(cells);
        foreach (int start in cells.Order())
        {
            if (!remaining.Remove(start)) continue;
            var component = new HashSet<int> { start };
            var queue = new Queue<int>(); queue.Enqueue(start);
            while (queue.Count > 0)
                foreach (int next in topology.Neighbors(queue.Dequeue()))
                    if (remaining.Remove(next)) { component.Add(next); queue.Enqueue(next); }
            yield return component;
        }
    }
}
