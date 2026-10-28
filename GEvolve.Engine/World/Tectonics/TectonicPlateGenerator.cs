using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.Tectonics;

/// <summary>
/// Planet-wide tectonic mosaic, independent of continent type and grid resolution.
/// Weighted hex-region growth creates contiguous, unequal plates with irregular borders.
/// </summary>
public static class TectonicPlateGenerator
{
    public const string Version = "tectonic-plates-1";
    public const int NumberOfPlates = 16;
    private const byte Unassigned = byte.MaxValue;

    public static byte[] Generate(HexTopology topology, int seed)
    {
        ArgumentNullException.ThrowIfNull(topology);
        int count = topology.Count, columns = topology.Columns, rows = topology.Rows;
        if (count < NumberOfPlates)
            throw new ArgumentException("Not enough hexagons for tectonic plates.", nameof(topology));

        var random = new DeterministicRandom(seed, 0x54454331);
        var origins = new int[NumberOfPlates];

        // Farthest-candidate sampling prevents tiny seed clusters, without imposing a grid.
        origins[0] = random.NextInt(count);
        for (int plate = 1; plate < NumberOfPlates; plate++)
        {
            int best = -1, bestSeparation = -1;
            for (int attempt = 0; attempt < 160; attempt++)
            {
                int candidate = random.NextInt(count);
                int distance = int.MaxValue;
                for (int other = 0; other < plate; other++)
                    distance = Math.Min(distance, topology.Distance(candidate, origins[other]));
                if (distance > bestSeparation ||
                    distance == bestSeparation && candidate < best)
                {
                    best = candidate;
                    bestSeparation = distance;
                }
            }
            origins[plate] = best;
        }

        // A few large plates and several smaller plates, with randomized assignment
        // of growth rates so no plate ID has a privileged geographic position.
        double[] costs =
        [
            .58, .64, .72, .79, .88, .97, 1.05, 1.14,
            1.24, 1.35, 1.48, 1.62, 1.79, 1.97, 2.17, 2.40
        ];
        for (int i = costs.Length - 1; i > 0; i--)
        {
            int swap = random.NextInt(i + 1);
            (costs[i], costs[swap]) = (costs[swap], costs[i]);
        }

        // Periodic, spatially correlated terrain resistance bends whole boundaries.
        // No independent cell-by-cell randomness and no square Voronoi partitions.
        var resistance = new float[count];
        for (int cell = 0; cell < count; cell++)
        {
            int x = cell % columns, y = cell / columns;
            double broad = PeriodicNoise.Sample(x, y, columns, rows, 3, seed ^ 0x231A45);
            double medium = PeriodicNoise.Sample(x, y, columns, rows, 9, seed ^ 0x5E12AB);
            double fine = PeriodicNoise.Sample(x, y, columns, rows, 19, seed ^ 0x7B3741);
            resistance[cell] = (float)Math.Clamp(
                1 + .54 * broad + .28 * medium + .10 * fine, .35, 1.75);
        }

        var owner = new byte[count];
        Array.Fill(owner, Unassigned);
        var frontier = new PriorityQueue<(int Cell, byte Plate), double>();
        for (byte plate = 0; plate < NumberOfPlates; plate++)
            frontier.Enqueue((origins[plate], plate), 0);

        // Each claimed cell is reached from an already claimed cell of its plate:
        // every plate remains one connected hex region (including the world seam).
        while (frontier.TryDequeue(out var step, out double priority))
        {
            if (owner[step.Cell] != Unassigned) continue;
            owner[step.Cell] = step.Plate;
            foreach (int neighbor in topology.Neighbors(step.Cell))
            {
                if (owner[neighbor] != Unassigned) continue;
                double next = priority + costs[step.Plate] *
                    (.5 * (resistance[step.Cell] + resistance[neighbor]));
                frontier.Enqueue((neighbor, step.Plate), next);
            }
        }

        if (owner.Any(plate => plate == Unassigned))
            throw new InvalidOperationException("Some hexagons received no plate.");
        return owner;
    }
}
