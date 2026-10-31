using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Groups adjacent contacts of the same process and plate pair into continuous margins.</summary>
public static class TectonicReliefSystemAnalyzer
{
    public static TectonicReliefSystem[] Analyze(HexTopology topology,
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
                systems.Add(new(systems.Count, key.Item1, key.Item2, key.Item3, key.Item4,
                    component.ToArray(), axisCells, intensity, width));

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
}
