using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Two to six distinct, unequal continents. Their count is selected only by
/// the world seed, never by the hex resolution or the retry number.
/// </summary>
public sealed class ContinentsGenerator : IPlateAwareLandmassGenerator
{
    private readonly record struct Frontier(int Id, int Owner, double Cost);
    public LandmassType Type => LandmassType.Continents;

    // The original three-argument API remains available to the legacy pipeline.
    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
        => Generate(topology, configuration, attempt, ReadOnlySpan<byte>.Empty);

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration,
        int attempt, ReadOnlySpan<byte> plates)
    {
        var profile = configuration.Continents
            ?? throw new ArgumentException("Continents parameters are missing.");
        if (profile.CoreCount is < 2 or > 6)
            throw new ArgumentException("Continents need between two and six continental cores.");
        var tectonics = new PlateSurfaceInfluence(topology, plates, configuration.Seed);
        var random = new DeterministicRandom(configuration.Seed,
            unchecked((uint)(0xC011 + attempt)));

        int count = topology.Count;
        int[] cores = ChooseDistantCores();
        int islandCells = (int)(count * profile.IslandFraction);
        int[] budgets = UnequalBudgets(
            (int)Math.Round(count * profile.TargetLandFraction) - islandCells,
            cores.Length, ref random);

        // Warp the spatial coordinates before sampling three coast scales.
        // The periodic fields also work at the horizontal wrap of the world.
        var coastResistance = new float[count];
        int width = topology.Columns, height = topology.Rows;
        int noiseSeed = unchecked(configuration.Seed + attempt * 1543);
        for (int cell = 0; cell < count; cell++)
        {
            int x = cell % width, y = cell / width;
            double warpX = PeriodicNoise.Sample(x, y, width, height, 3,
                noiseSeed ^ 0x2C190D) * width * .06;
            double warpY = PeriodicNoise.Sample(x, y, width, height, 3,
                noiseSeed ^ 0x34AC2B) * height * .06;
            double wx = x + warpX, wy = y + warpY;
            double broad = PeriodicNoise.Sample(wx, wy, width, height, 4,
                noiseSeed ^ 0x17F0AD);
            double medium = PeriodicNoise.Sample(wx, wy, width, height, profile.NoiseFrequency,
                noiseSeed ^ 0x5AE213);
            double fine = PeriodicNoise.Sample(wx, wy, width, height, profile.NoiseFrequency * 2 + 4,
                noiseSeed ^ 0x473BB8);
            coastResistance[cell] = (float)(
                1.35 * broad + .80 * medium + .28 * fine);
        }

        var owners = new int[count];
        Array.Fill(owners, -1);
        var owned = new int[cores.Length];
        var frontier = new PriorityQueue<Frontier, double>();
        for (int owner = 0; owner < cores.Length; owner++)
        {
            owners[cores[owner]] = owner;
            owned[owner] = 1;
            EnqueueNeighbors(cores[owner], owner, 0);
        }

        int mainLand = cores.Length;
        int target = budgets.Sum();
        while (frontier.TryDequeue(out var item, out _) && mainLand < target)
        {
            if (owners[item.Id] != -1 || owned[item.Owner] >= budgets[item.Owner])
                continue;

            // Require at least two water hexagons between different continents,
            // not just one arbitrarily thin channel. Neighbors include the seam.
            if (TooCloseToOtherContinent(item.Id, item.Owner)) continue;

            owners[item.Id] = item.Owner;
            owned[item.Owner]++;
            mainLand++;
            EnqueueNeighbors(item.Id, item.Owner, item.Cost);
        }

        var surface = new byte[count];
        for (int id = 0; id < count; id++)
            if (owners[id] >= 0) surface[id] = 1;

        // Tiny isolated islands must not accidentally bridge two continents.
        AddIsolatedIslands(surface, islandCells, ref random);
        return surface;

        int[] ChooseDistantCores()
        {
            var seeds = new int[profile.CoreCount];
            seeds[0] = RandomCandidate();
            for (int index = 1; index < seeds.Length; index++)
            {
                int best = -1;
                double bestScore = double.NegativeInfinity;
                for (int sample = 0; sample < 800; sample++)
                {
                    int cell = RandomCandidate();
                    int nearest = int.MaxValue;
                    for (int p = 0; p < index; p++)
                        nearest = Math.Min(nearest, topology.Distance(cell, seeds[p]));
                    if (nearest < profile.MinimumCoreDistance) continue;
                    double score = nearest * (.87 + .26 * random.NextDouble());
                    if (score > bestScore)
                    {
                        best = cell;
                        bestScore = score;
                    }
                }
                if (best < 0)
                    throw new InvalidOperationException(
                        "Not enough space to separate the continental cores.");
                seeds[index] = best;
            }
            return seeds;

            int RandomCandidate()
            {
                int x = random.NextInt(width);
                int y = Math.Clamp((int)(height * (.10 + .80 * random.NextDouble())),
                    0, height - 1);
                return topology.Id(x, y);
            }
        }

        bool TooCloseToOtherContinent(int cell, int owner)
        {
            foreach (int neighbor in topology.Neighbors(cell))
            {
                if (owners[neighbor] >= 0 && owners[neighbor] != owner)
                    return true;
                foreach (int second in topology.Neighbors(neighbor))
                    if (owners[second] >= 0 && owners[second] != owner)
                        return true;
            }
            return false;
        }

        void EnqueueNeighbors(int from, int owner, double cost)
        {
            if (owned[owner] >= budgets[owner]) return;
            foreach (int neighbor in topology.Neighbors(from))
            {
                if (owners[neighbor] != -1) continue;
                double next = cost + Math.Max(.18,
                    1 + profile.CoastRoughness * coastResistance[neighbor] +
                    tectonics.ExpansionCost(from, neighbor));
                frontier.Enqueue(new Frontier(neighbor, owner, next),
                    next + neighbor * 1e-9);
            }
        }

        void AddIsolatedIslands(byte[] land, int desired, ref DeterministicRandom rng)
        {
            int added = 0;
            for (int attemptIsland = 0;
                 attemptIsland < desired * 60 && added < desired; attemptIsland++)
            {
                int cell = rng.NextInt(count);
                int row = cell / width;
                if (row < 2 || row >= height - 2 || land[cell] != 0) continue;
                bool isolated = true;
                foreach (int neighbor in topology.Neighbors(cell))
                    if (land[neighbor] != 0) { isolated = false; break; }
                if (!isolated) continue;

                land[cell] = 1;
                added++;

                // A two-hex island is allowed only if it cannot contact existing
                // land outside its own seed hex.
                if (added >= desired || rng.NextDouble() >= .50) continue;
                var neighbors = topology.Neighbors(cell);
                int second = neighbors[rng.NextInt(neighbors.Length)];
                if (land[second] != 0) continue;
                bool safe = true;
                foreach (int touching in topology.Neighbors(second))
                    if (touching != cell && land[touching] != 0)
                    { safe = false; break; }
                if (safe) { land[second] = 1; added++; }
            }
        }
    }

    private static int[] UnequalBudgets(int total, int numberOfContinents,
        ref DeterministicRandom random)
    {
        var weights = new double[numberOfContinents];
        double weightSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Math.Pow(numberOfContinents - i, 1.08) *
                (.88 + .24 * random.NextDouble());
            weightSum += weights[i];
        }
        // Largest continent should not always inherit the first geographical seed.
        for (int i = weights.Length - 1; i > 0; i--)
        {
            int swap = random.NextInt(i + 1);
            (weights[i], weights[swap]) = (weights[swap], weights[i]);
        }

        var budgets = new int[numberOfContinents];
        int assigned = 0;
        for (int i = 0; i < budgets.Length - 1; i++)
            assigned += budgets[i] = (int)Math.Round(total * weights[i] / weightSum);
        budgets[^1] = total - assigned;
        return budgets;
    }
}
