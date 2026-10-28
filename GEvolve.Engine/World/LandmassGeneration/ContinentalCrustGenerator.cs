using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// A procedural continental/oceanic crust hypothesis built from the existing
/// tectonic mosaic. Crust does not equal emerged land: submerged continental
/// shelves are allowed, as are small islands over oceanic crust.
/// This is a geographic heuristic, not a plate-kinematics simulation.
/// </summary>
public sealed record ContinentalCrustMap(
    byte[] CrustByCellId,
    float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells,
    byte[] MainContinentCorePlates);

public sealed class ContinentalCrustGenerator
{
    public const string Version = "continental-crust-1";

    public ContinentalCrustMap Generate(
        HexTopology topology, ReadOnlySpan<byte> plateIds, int continentCount, int seed)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (continentCount is < 2 or > 6)
            throw new ArgumentOutOfRangeException(nameof(continentCount));
        if (plateIds.Length != topology.Count)
            throw new ArgumentException("Stage 01 plates must match the grid.", nameof(plateIds));

        const int plateCount = TectonicPlateGenerator.NumberOfPlates;
        var plates = plateIds.ToArray();
        var population = new int[plateCount];
        var adjacent = new bool[plateCount, plateCount];
        var depth = new int[topology.Count];
        Array.Fill(depth, -1);
        var frontier = new Queue<int>();

        // Real hex-neighbor borders determine the plate graph and a distance
        // transform to each plate's interior, including the east/west seam.
        for (int cell = 0; cell < plates.Length; cell++)
        {
            int p = plates[cell];
            if (p >= plateCount)
                throw new ArgumentException("Invalid tectonic plate ID.", nameof(plateIds));
            population[p]++;
            foreach (int other in topology.Neighbors(cell))
            {
                int q = plates[other];
                if (q >= plateCount)
                    throw new ArgumentException("Invalid tectonic plate ID.", nameof(plateIds));
                if (p == q) continue;
                adjacent[p, q] = true;
                depth[cell] = 0;
            }
            if (depth[cell] == 0) frontier.Enqueue(cell);
        }
        if (population.Any(n => n == 0))
            throw new ArgumentException("All sixteen tectonic plates must be present.", nameof(plateIds));
        while (frontier.TryDequeue(out int cell))
            foreach (int next in topology.Neighbors(cell))
            {
                if (plates[next] != plates[cell] || depth[next] >= 0) continue;
                depth[next] = depth[cell] + 1;
                frontier.Enqueue(next);
            }

        var center = new int[plateCount];
        var bestDepth = new int[plateCount];
        Array.Fill(bestDepth, -1);
        int low = topology.Rows / 12, high = topology.Rows - low - 1;
        for (int cell = 0; cell < plates.Length; cell++)
        {
            int row = cell / topology.Columns;
            if (row < low || row > high) continue;
            int p = plates[cell];
            if (depth[cell] <= bestDepth[p]) continue;
            center[p] = cell;
            bestDepth[p] = depth[cell];
        }
        // All plate IDs have many interior cells in the official four grids;
        // this also handles narrow plates on unusual custom grids.
        for (int p = 0; p < plateCount; p++)
            if (bestDepth[p] < 0)
                center[p] = Array.IndexOf(plates, (byte)p);

        var random = new DeterministicRandom(seed, 0x43525553);
        var hubs = new int[continentCount];
        var hubPlates = new byte[continentCount];
        // Larger plates are more likely to host the first continental root.
        double totalWeight = population.Sum(n => Math.Sqrt(n));
        double ticket = random.NextDouble() * totalWeight;
        int firstPlate = 0;
        for (int p = 0; p < plateCount; p++)
        {
            ticket -= Math.Sqrt(population[p]);
            if (ticket <= 0) { firstPlate = p; break; }
        }
        hubs[0] = center[firstPlate];
        hubPlates[0] = (byte)firstPlate;

        // Farthest-first selection at ACTUAL plate interiors. No arbitrary
        // continent centers are sampled independently of tectonic geography.
        for (int index = 1; index < continentCount; index++)
        {
            int selected = -1;
            double best = double.NegativeInfinity;
            for (int p = 0; p < plateCount; p++)
            {
                if (hubPlates.AsSpan(0, index).Contains((byte)p)) continue;
                int nearest = int.MaxValue;
                for (int prior = 0; prior < index; prior++)
                    nearest = Math.Min(nearest, topology.Distance(center[p], hubs[prior]));
                double score = nearest * (.92 + .16 * Math.Sqrt(population[p] /
                    (double)topology.Count)) + .12 * bestDepth[p];
                if (score > best || score == best && p < selected)
                { best = score; selected = p; }
            }
            if (selected < 0) throw new InvalidOperationException("Missing continental plate core.");
            hubs[index] = center[selected];
            hubPlates[index] = (byte)selected;
        }

        // Graph distance from each continental root plate: neighboring plates
        // can share a continent, while plates farther away are predominantly
        // oceanic. This deliberately does NOT label complete plates as land.
        var graphSteps = new int[continentCount, plateCount];
        for (int owner = 0; owner < continentCount; owner++)
        {
            var steps = new int[plateCount];
            Array.Fill(steps, int.MaxValue);
            var queue = new Queue<int>();
            steps[hubPlates[owner]] = 0;
            queue.Enqueue(hubPlates[owner]);
            while (queue.TryDequeue(out int plate))
                for (int next = 0; next < plateCount; next++)
                    if (adjacent[plate, next] && steps[next] == int.MaxValue)
                    {
                        steps[next] = steps[plate] + 1;
                        queue.Enqueue(next);
                    }
            for (int p = 0; p < plateCount; p++)
                graphSteps[owner, p] = steps[p];
        }

        var crust = new byte[topology.Count];
        var affinity = new float[topology.Count];
        double radius = Math.Sqrt(topology.Count * .35 /
            (continentCount * Math.PI)) * 1.55;
        int width = topology.Columns, height = topology.Rows;
        for (int cell = 0; cell < topology.Count; cell++)
        {
            int x = cell % width, y = cell / width, plate = plates[cell];
            double warpX = PeriodicNoise.Sample(x, y, width, height, 3, seed ^ 0x41516E) * width * .045;
            double warpY = PeriodicNoise.Sample(x, y, width, height, 3, seed ^ 0x513BB0) * height * .045;
            double wx = x + warpX, wy = y + warpY;
            double broad = PeriodicNoise.Sample(wx, wy, width, height, 4, seed ^ 0x2EDB21);
            double medium = PeriodicNoise.Sample(wx, wy, width, height, 9, seed ^ 0x6B1843);
            double detail = PeriodicNoise.Sample(wx, wy, width, height, 18, seed ^ 0x135E48);

            double best = double.NegativeInfinity;
            for (int owner = 0; owner < continentCount; owner++)
            {
                int steps = graphSteps[owner, plate];
                double plateAffinity = steps switch
                {
                    0 => .67,
                    1 => .27,
                    2 => -.08,
                    _ => -.45
                };
                double distance = topology.Distance(cell, hubs[owner]);
                double candidate = .98 - distance / radius + plateAffinity +
                    .50 * broad + .25 * medium + .08 * detail;
                best = Math.Max(best, candidate);
            }
            affinity[cell] = (float)best;
            crust[cell] = best >= .08 ? (byte)1 : (byte)0;
        }
        return new ContinentalCrustMap(crust, affinity, hubs, hubPlates);
    }
}
