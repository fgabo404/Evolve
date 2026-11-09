namespace GEvolve.Engine.World.LandmassGeneration.Shared;

internal static class HexRegionUtilities
{
    public static int[] SeparatedSeeds(HexTopology topology, int count,
        int minimumDistance, ref DeterministicRandom random)
    {
        var seeds = new int[count];
        for (int index = 0; index < count; index++)
        {
            int best = -1, bestDistance = -1;
            for (int attempt = 0; attempt < 3000; attempt++)
            {
                int column = random.NextInt(topology.Columns);
                int row = Math.Clamp((int)(topology.Rows * (.08 + .84 * random.NextDouble())),
                    0, topology.Rows - 1);
                int candidate = topology.Id(column, row);
                int distance = int.MaxValue;
                for (int previous = 0; previous < index; previous++)
                    distance = Math.Min(distance, topology.Distance(candidate, seeds[previous]));
                if (distance >= minimumDistance) { best = candidate; break; }
                if (distance > bestDistance) { best = candidate; bestDistance = distance; }
            }
            seeds[index] = best;
        }
        return seeds;
    }

    public static int[] Budgets(int total, int count, ref DeterministicRandom random)
    {
        var weights = new double[count];
        double sum = 0;
        for (int i = 0; i < count; i++) sum += weights[i] = .75 + .5 * random.NextDouble();
        var budgets = new int[count];
        int assigned = 0;
        for (int i = 0; i < count - 1; i++)
            assigned += budgets[i] = (int)Math.Round(total * weights[i] / sum);
        budgets[^1] = Math.Max(1, total - assigned);
        return budgets;
    }

    public static int AddSecondaryIslands(HexTopology topology, byte[] surface,
        int desiredCells, ref DeterministicRandom random)
    {
        int added = 0;
        for (int attempt = 0; attempt < desiredCells * 40 && added < desiredCells; attempt++)
        {
            int id = random.NextInt(topology.Count);
            int row = id / topology.Columns;
            if (row < 2 || row >= topology.Rows - 2 || surface[id] != 0) continue;
            bool clear = true;
            foreach (int neighbor in topology.Neighbors(id))
                if (surface[neighbor] != 0) { clear = false; break; }
            if (!clear) continue;
            surface[id] = 1;
            added++;
            if (added < desiredCells && random.NextDouble() < .55)
            {
                var neighbors = topology.Neighbors(id);
                int other = neighbors[random.NextInt(neighbors.Length)];
                if (surface[other] == 0) { surface[other] = 1; added++; }
            }
        }
        return added;
    }
}
