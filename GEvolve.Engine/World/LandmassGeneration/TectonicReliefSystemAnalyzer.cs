using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Connects classified margins while preserving their original contacts and plate motion.</summary>
public static class TectonicReliefSystemAnalyzer
{
    public static TectonicReliefSystem[] Analyze(HexTopology topology,
        TectonicMotionState motion, GeologicalContact[] contacts)
        => MergeContinentalCollisions(topology, AnalyzeLegacy(topology, motion, contacts));

    // The selection stage uses this original partition so approved continental groups stay fixed.
    public static TectonicReliefSystem[] AnalyzeLegacy(HexTopology topology,
        TectonicMotionState motion, GeologicalContact[] contacts)
    {
        var groups = new Dictionary<(GeologicalContactKind, byte, byte, byte), List<int>>();
        foreach (var contact in contacts)
        {
            if (contact.Kind == GeologicalContactKind.Quiet) continue;
            var edge = motion.Interactions[contact.InteractionIndex];
            byte first = Math.Min(edge.FirstPlateId, edge.SecondPlateId);
            byte second = Math.Max(edge.FirstPlateId, edge.SecondPlateId);
            var key = (contact.Kind, first, second, contact.OverridingPlateId);
            if (!groups.TryGetValue(key, out var indices)) groups[key] = indices = [];
            indices.Add(contact.InteractionIndex);
        }

        var systems = new List<TectonicReliefSystem>();
        foreach (var (key, indices) in groups.OrderBy(group => group.Value[0]))
        {
            var byCell = new Dictionary<int, List<int>>();
            foreach (int index in indices)
            {
                var edge = motion.Interactions[index];
                Add(edge.FirstCellId, index);
                Add(edge.SecondCellId, index);
            }
            var remaining = indices.ToHashSet();
            foreach (int start in indices)
            {
                if (!remaining.Remove(start)) continue;
                var queue = new Queue<int>();
                var component = new List<int>();
                var axis = new HashSet<int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    component.Add(index);
                    var edge = motion.Interactions[index];
                    Visit(edge.FirstCellId);
                    Visit(edge.SecondCellId);
                }
                component.Sort();
                int[] axisCells = axis.Order().ToArray();
                double intensity = component.Average(index =>
                {
                    var edge = motion.Interactions[index];
                    return key.Item1 switch
                    {
                        GeologicalContactKind.ContinentalCollision or
                        GeologicalContactKind.OceanContinentSubduction or
                        GeologicalContactKind.OceanOceanSubduction => edge.Convergence,
                        GeologicalContactKind.ContinentalRift or GeologicalContactKind.OceanRidge => edge.Divergence,
                        _ => edge.Shear
                    };
                });
                // Width responds to the continuous margin length, with one global scale for all seeds.
                double width = Math.Clamp(2.5 + Math.Sqrt(component.Count) * .55, 3, 13);
                systems.Add(new TectonicReliefSystem(systems.Count, key.Item1, key.Item2,
                    key.Item3, key.Item4, component.ToArray(), axisCells, intensity, width)
                { ParticipatingPlateIds = [key.Item2, key.Item3] });

                void Visit(int cell)
                {
                    axis.Add(cell);
                    Enqueue(cell);
                    foreach (int neighbor in topology.Neighbors(cell))
                    {
                        Enqueue(neighbor);
                        // A single quiet or differently classified hex edge must not
                        // split an otherwise continuous margin of the same plate pair.
                        if (key.Item1 is GeologicalContactKind.ContinentalCollision or
                            GeologicalContactKind.OceanContinentSubduction)
                            foreach (int acrossGap in topology.Neighbors(neighbor))
                                if (Enqueue(acrossGap)) axis.Add(neighbor);
                    }
                }
                bool Enqueue(int cell)
                {
                    if (!byCell.TryGetValue(cell, out var candidates)) return false;
                    bool connected = false;
                    foreach (int candidate in candidates)
                        if (remaining.Remove(candidate))
                        {
                            queue.Enqueue(candidate);
                            connected = true;
                        }
                    return connected;
                }
            }

            void Add(int cell, int index)
            {
                if (!byCell.TryGetValue(cell, out var list)) byCell[cell] = list = [];
                list.Add(index);
            }
        }
        return systems.ToArray();
    }

    private static TectonicReliefSystem[] MergeContinentalCollisions(HexTopology topology,
        TectonicReliefSystem[] original)
    {
        int[] parent = Enumerable.Range(0, original.Length).ToArray();
        var bridges = new List<(int First, int Second, int Cell)>();
        for (int first = 0; first < original.Length; first++)
        {
            var a = original[first];
            if (a.Kind != GeologicalContactKind.ContinentalCollision) continue;
            for (int second = first + 1; second < original.Length; second++)
            {
                var b = original[second];
                if (b.Kind != GeologicalContactKind.ContinentalCollision ||
                    !a.ParticipatingPlateIds.Intersect(b.ParticipatingPlateIds).Any() ||
                    Math.Min(a.Intensity, b.Intensity) < Math.Max(a.Intensity, b.Intensity) * .30)
                    continue;
                int bridge = NearbyAxisBridge(topology, a.AxisCellIds, b.AxisCellIds);
                if (bridge == int.MinValue) continue;
                Union(first, second);
                bridges.Add((first, second, bridge));
            }
        }

        var merged = new List<TectonicReliefSystem>();
        foreach (var group in Enumerable.Range(0, original.Length).GroupBy(Find)
            .OrderBy(group => group.Min(index => original[index].InteractionIndices[0])))
        {
            int[] members = group.ToArray();
            var primary = original[members.OrderBy(index => original[index].InteractionIndices[0]).First()];
            if (members.Length == 1)
            {
                merged.Add(primary with { Id = merged.Count });
                continue;
            }
            var memberSet = members.ToHashSet();
            int[] interactions = members.SelectMany(index => original[index].InteractionIndices)
                .Order().ToArray();
            var axis = members.SelectMany(index => original[index].AxisCellIds).ToHashSet();
            foreach (var bridge in bridges)
                if (memberSet.Contains(bridge.First) && memberSet.Contains(bridge.Second) &&
                    bridge.Cell >= 0) axis.Add(bridge.Cell);
            byte[] participating = members.SelectMany(index => original[index].ParticipatingPlateIds)
                .Distinct().Order().ToArray();
            double intensity = members.Sum(index => original[index].Intensity *
                original[index].InteractionIndices.Length) / interactions.Length;
            double width = Math.Clamp(2.5 + Math.Sqrt(interactions.Length) * .55, 3, 13);
            merged.Add(new TectonicReliefSystem(merged.Count,
                GeologicalContactKind.ContinentalCollision, primary.FirstPlateId,
                primary.SecondPlateId, byte.MaxValue, interactions,
                axis.Order().ToArray(), intensity, width)
            { ParticipatingPlateIds = participating });
        }
        return merged.ToArray();

        int Find(int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }
        void Union(int first, int second)
        {
            first = Find(first); second = Find(second);
            if (first != second) parent[second] = first;
        }
    }

    private static int NearbyAxisBridge(HexTopology topology, int[] first, int[] second)
    {
        var target = second.ToHashSet();
        foreach (int cell in first)
        {
            if (target.Contains(cell)) return -1;
            foreach (int neighbor in topology.Neighbors(cell))
            {
                if (target.Contains(neighbor)) return -1;
                foreach (int acrossGap in topology.Neighbors(neighbor))
                    if (target.Contains(acrossGap)) return neighbor;
            }
        }
        return int.MinValue;
    }
}
