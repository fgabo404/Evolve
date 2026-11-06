using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Builds land-only mountain axes from connected, convergent H04 contacts.</summary>
public sealed class PangaeaMountainRangeGenerator
{
    public const string Version = "pangaea-ranges-1";

    public LandmassState Apply(HexTopology topology, LandmassState terrain)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(terrain);
        if (terrain.Configuration.Type != LandmassType.Pangaea || !terrain.HasSurface ||
            terrain.ElevationByCellId is not { } original ||
            terrain.TectonicMotion is not { } motion || terrain.PlateByCellId is not { } plates ||
            original.Length != topology.Count || plates.Length != topology.Count ||
            terrain.SurfaceByCellId.Length != topology.Count)
            throw new ArgumentException("Complete Pangaea terrain and official H04 motion are required.", nameof(terrain));

        int count = topology.Count;
        var strength = new float[count];
        var pair = new int[count];
        Array.Fill(pair, -1);
        // Require an emergent contact on both sides, appreciable convergence,
        // and convergence at least comparable to tangential shear.
        foreach (var edge in motion.Interactions)
        {
            if (edge.Kind is not (PlateInteractionKind.Convergent or PlateInteractionKind.Mixed) ||
                edge.Convergence < 1 || edge.Convergence < edge.Shear * .65 ||
                terrain.SurfaceByCellId[edge.FirstCellId] != 1 ||
                terrain.SurfaceByCellId[edge.SecondCellId] != 1)
                continue;
            int key = Math.Min(edge.FirstPlateId, edge.SecondPlateId) * 16 +
                Math.Max(edge.FirstPlateId, edge.SecondPlateId);
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
                if (edge.Convergence > strength[id] ||
                    edge.Convergence == strength[id] && key < pair[id])
                {
                    strength[id] = (float)edge.Convergence;
                    pair[id] = key;
                }
        }

        var visited = new bool[count];
        var stamp = new int[count];
        var distance = new int[count];
        var parent = new int[count];
        int search = 0;
        var queue = new Queue<int>();
        var ranges = new List<PangaeaMountainRange>();
        int minimumLength = Math.Max(7, topology.Rows / 20);
        for (int start = 0; start < count; start++)
        {
            if (pair[start] < 0 || visited[start]) continue;
            var component = new List<int>();
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.TryDequeue(out int id))
            {
                component.Add(id);
                foreach (int neighbor in topology.Neighbors(id))
                    if (!visited[neighbor] && pair[neighbor] == pair[start])
                    {
                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
            }
            if (component.Count < minimumLength) continue;
            // Two graph sweeps find a long, naturally bending axis. Each step is
            // a real hex edge, including edges across the horizontal seam.
            int endA = Farthest(component.Min(), pair[start]);
            int endB = Farthest(endA, pair[start]);
            var axis = new List<int>();
            for (int id = endB; id != -1; id = parent[id]) axis.Add(id);
            axis.Reverse();
            if (axis.Count < minimumLength) continue;
            int baseWidth = Math.Max(3, (int)Math.Round(topology.Rows * .025));
            var widths = new byte[axis.Count];
            float peak = 0;
            for (int i = 0; i < axis.Count; i++)
            {
                peak = Math.Max(peak, strength[axis[i]]);
                widths[i] = (byte)Math.Clamp((int)Math.Round(baseWidth +
                    1.2 * Math.Sin(i * .22 + axis[0] * .01)), 2, 12);
            }
            ranges.Add(new(0, axis.ToArray(), widths, peak));
        }

        ranges.Sort((a, b) => a.AxisCellIds.Min().CompareTo(b.AxisCellIds.Min()));
        var owner = new ushort[count];
        var best = new double[count];
        var final = (byte[])original.Clone();
        var influenceStamp = new int[count];
        for (int index = 0; index < ranges.Count; index++)
        {
            var range = ranges[index] with { Id = index + 1 };
            ranges[index] = range;
            var frontier = new Queue<(int Cell, int Axis, int Distance)>();
            for (int i = 0; i < range.AxisCellIds.Length; i++)
            {
                int cell = range.AxisCellIds[i];
                influenceStamp[cell] = index + 1;
                frontier.Enqueue((cell, i, 0));
            }
            while (frontier.TryDequeue(out var step))
            {
                int width = range.WidthByAxisCell[step.Axis];
                double taper = Math.Pow(1 - (double)step.Distance / (width + 1), 2);
                double intensity = Math.Clamp(strength[range.AxisCellIds[step.Axis]] / 4, .35, 1);
                double uplift = 27 * intensity * taper;
                if (uplift > best[step.Cell])
                {
                    best[step.Cell] = uplift;
                    owner[step.Cell] = checked((ushort)range.Id);
                    final[step.Cell] = (byte)Math.Clamp(
                        Math.Max(original[step.Cell], (int)Math.Round(original[step.Cell] + uplift)), 1, 100);
                }
                if (step.Distance >= width) continue;
                foreach (int neighbor in topology.Neighbors(step.Cell))
                    if (terrain.SurfaceByCellId[neighbor] == 1 && influenceStamp[neighbor] != index + 1)
                    {
                        influenceStamp[neighbor] = index + 1;
                        frontier.Enqueue((neighbor, step.Axis, step.Distance + 1));
                    }
            }
        }
        // Axes have distinct eligible cells. Keep their own identity even where
        // another system's wider flank has the greater height contribution.
        foreach (var range in ranges)
            foreach (int id in range.AxisCellIds)
                owner[id] = checked((ushort)range.Id);
        return terrain with { ElevationByCellId = final,
            PangaeaMountainRanges = new(Version, ranges.ToArray(), owner) };

        int Farthest(int source, int key)
        {
            int currentStamp = ++search, farthest = source;
            queue.Enqueue(source);
            stamp[source] = currentStamp;
            distance[source] = 0;
            parent[source] = -1;
            while (queue.TryDequeue(out int id))
            {
                if (distance[id] > distance[farthest] ||
                    distance[id] == distance[farthest] && id < farthest)
                    farthest = id;
                foreach (int neighbor in topology.Neighbors(id))
                    if (pair[neighbor] == key && stamp[neighbor] != currentStamp)
                    {
                        stamp[neighbor] = currentStamp;
                        distance[neighbor] = distance[id] + 1;
                        parent[neighbor] = id;
                        queue.Enqueue(neighbor);
                    }
            }
            return farthest;
        }
    }
}
