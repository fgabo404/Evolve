using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Two to six distinct, unequal continents. The staged generator extracts
/// emergent land with a global sea level from plate-aware continental crust.
/// The older quota-based generator remains only for legacy callers.
/// Count depends only on the world seed, never on resolution or retries.
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
        if (!plates.IsEmpty)
            return GenerateFromSeaLevel(topology, configuration, plates, attempt);
        var tectonics = new PlateSurfaceInfluence(topology, plates, configuration.Seed);
        var random = new DeterministicRandom(configuration.Seed,
            unchecked((uint)(0xC011 + attempt)));

        int count = topology.Count;
        int width = topology.Columns, height = topology.Rows;
        // Continental/oceanic crust is computed FROM the actual plate mosaic,
        // before emerging coastlines are drawn. With no plate array the old
        // land-only route is retained for historical callers.
        ContinentalCrustMap? crust = plates.IsEmpty ? null :
            new ContinentalCrustGenerator().Generate(
                topology, plates, profile.CoreCount, configuration.Seed);
        int[] cores = crust?.MainContinentCoreCells ?? ChooseDistantCores();
        int islandCells = (int)(count * profile.IslandFraction);
        int[] budgets = UnequalBudgets(
            (int)Math.Round(count * profile.TargetLandFraction) - islandCells,
            cores.Length, ref random);

        // Warp the spatial coordinates before sampling three coast scales.
        // The periodic fields also work at the horizontal wrap of the world.
        var coastResistance = new float[count];
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
                // Prefer buoyant continental crust, including shelves shared
                // across plates. Oceanic sectors remain mostly submerged, though
                // a growing mainland can traverse a marginal sector if needed.
                // Crust affinity now dominates the continental growth costs;
                // plate borders alone are not mistaken for coastlines.
                double geology = crust is null
                    ? tectonics.ExpansionCost(from, neighbor)
                    : Math.Clamp(.95 - 1.55 * crust.EmergenceAffinityByCellId[neighbor] +
                        (crust.CrustByCellId[neighbor] == 0 ? 1.20 : 0), -.65, 3.5) +
                        .18 * tectonics.ExpansionCost(from, neighbor);
                double next = cost + Math.Max(.18,
                    1 + profile.CoastRoughness * coastResistance[neighbor] + geology);
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

    /// <summary>
    /// Actual stage 02 pathway: tectonic provinces define continental crust,
    /// relief potential defines sea level, and water basins separate fragments.
    /// Unlike the historical Dijkstra surface algorithm, no continent receives
    /// a quota of land cells to claim. This model is not geological time travel.
    /// </summary>
    private static byte[] GenerateFromSeaLevel(HexTopology topology,
        LandmassConfiguration configuration, ReadOnlySpan<byte> plates, int attempt)
    {
        var profile = configuration.Continents
            ?? throw new ArgumentException("Continents profile is required.");
        var geology = new ContinentalCrustGenerator().Generate(
            topology, plates, profile.CoreCount, configuration.Seed);
        int count = topology.Count, width = topology.Columns, height = topology.Rows;
        var random = new DeterministicRandom(configuration.Seed,
            unchecked((uint)(0x5345414C + attempt)));

        // A common sea level cuts a continuous geological potential. Marginal
        // continental shelves can be submerged; oceanic crust has a lower
        // buoyancy, so most deep-ocean cells cannot become mainlands.
        var potential = new float[count];
        for (int cell = 0; cell < count; cell++)
        {
            int x = cell % width, y = cell / width;
            double wrinkle = PeriodicNoise.Sample(x, y, width, height,
                profile.NoiseFrequency * 2 + 3,
                unchecked(configuration.Seed ^ 0x65A2D7 ^ (attempt * 73)));
            potential[cell] = geology.EmergenceAffinityByCellId[cell] +
                (float)(.07 * wrinkle) -
                (geology.CrustByCellId[cell] == 0 ? .85f : 0);
        }

        var sorted = potential.ToArray();
        Array.Sort(sorted);

        // Sea-level trials select a global datum, not separate land budgets.
        // The accepted land percentage may vary within the geographic profile.
        byte[]? best = null;
        double bestPenalty = double.PositiveInfinity;
        foreach (double percentileLand in new[] { .40, .44, .48, .52, .56, .36, .60 })
        {
            float seaLevel = sorted[Math.Clamp(
                (int)Math.Round((1 - percentileLand) * (count - 1)), 0, count - 1)];
            var candidates = new byte[count];
            for (int cell = 0; cell < count; cell++)
                if (potential[cell] >= seaLevel && geology.CrustByCellId[cell] == 1)
                    candidates[cell] = 1;

            // Failed rifts between unrelated proto-continental provinces
            // create narrow submerged basins. Mark both shores simultaneously,
            // so no order-dependent seam bridges are formed.
            var rift = new bool[count];
            for (int cell = 0; cell < count; cell++)
            {
                if (candidates[cell] == 0) continue;
                foreach (int neighbor in topology.Neighbors(cell))
                {
                    if (candidates[neighbor] == 0 ||
                        geology.ProvinceByCellId[cell] ==
                            geology.ProvinceByCellId[neighbor]) continue;
                    rift[cell] = true;
                    rift[neighbor] = true;
                }
            }
            for (int cell = 0; cell < count; cell++)
                if (rift[cell]) candidates[cell] = 0;

            // Keep the emerged mass attached to each geological continental
            // nucleus. Isolated scraps are accepted only as small islands:
            // large floating components would be spurious extra continents.
            var accepted = new byte[count];
            var queue = new int[count];
            for (int owner = 0; owner < profile.CoreCount; owner++)
            {
                int origin = geology.MainContinentCoreCells[owner];
                if (candidates[origin] == 0) continue;
                int head = 0, tail = 0;
                accepted[origin] = 1;
                queue[tail++] = origin;
                while (head < tail)
                    foreach (int next in topology.Neighbors(queue[head++]))
                    {
                        if (candidates[next] == 0 || accepted[next] == 1 ||
                            geology.ProvinceByCellId[next] != owner) continue;
                        accepted[next] = 1;
                        queue[tail++] = next;
                    }
            }

            // Preserve tiny natural continental fragments near shelves, not
            // additional large continents unrelated to the geological roots.
            var seen = new bool[count];
            for (int cell = 0; cell < count; cell++)
            {
                if (candidates[cell] == 0 || accepted[cell] != 0 || seen[cell]) continue;
                int head = 0, tail = 0;
                queue[tail++] = cell;
                seen[cell] = true;
                while (head < tail)
                    foreach (int next in topology.Neighbors(queue[head++]))
                    {
                        if (candidates[next] == 0 || accepted[next] != 0 || seen[next])
                            continue;
                        seen[next] = true;
                        queue[tail++] = next;
                    }
                if (tail > 24) continue;
                for (int i = 0; i < tail; i++) accepted[queue[i]] = 1;
            }

            var statistics = LandmassAnalysis.Analyze(topology, accepted);
            int principalCount = statistics.ComponentSizes.Count(size => size >= 100);
            double penalty = Math.Abs(statistics.LandFraction -
                profile.TargetLandFraction) +
                .20 * Math.Abs(principalCount - profile.CoreCount) +
                (statistics.DominantLandShare >= .75 ? .2 : 0);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                best = accepted;
            }
            if (principalCount == profile.CoreCount &&
                LandmassGenerationService.MeetsProfile(
                    LandmassType.Continents, statistics))
            {
                // Oceanic hotspot islands are sparse and never determine the
                // position of the main continents.
                AddHotspotIslands(accepted, geology.CrustByCellId, topology,
                    ref random, (int)(count * profile.IslandFraction));
                return accepted;
            }
        }

        // The caller's bounded retries provide a different minor sea-floor
        // wrinkle for unusual seeds, while preserving the tectonic geometry.
        if (best is null)
            throw new InvalidOperationException("No sea-level candidates were evaluated.");
        AddHotspotIslands(best, geology.CrustByCellId, topology,
            ref random, (int)(count * profile.IslandFraction));
        return best;
    }

    private static void AddHotspotIslands(byte[] land, byte[] crust,
        HexTopology topology, ref DeterministicRandom random, int desired)
    {
        int added = 0;
        int count = land.Length;
        for (int attempt = 0; attempt < desired * 45 && added < desired; attempt++)
        {
            int cell = random.NextInt(count);
            int row = cell / topology.Columns;
            if (row < 2 || row >= topology.Rows - 2 ||
                land[cell] != 0 || crust[cell] != 0)
                continue;
            bool touchesLand = false;
            foreach (int next in topology.Neighbors(cell))
                if (land[next] != 0) { touchesLand = true; break; }
            if (touchesLand) continue;
            land[cell] = 1;
            added++;
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
