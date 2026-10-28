using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Continental assembly and global sea level. Neither land quotas nor
/// radial Dijkstra expansion decide where continents appear.
/// </summary>
public sealed class ContinentsGenerator : IPlateAwareLandmassGenerator
{
    public LandmassType Type => LandmassType.Continents;

    // The legacy API also uses the new geological algorithm. If stage 01 was
    // not supplied by the caller, generate those same deterministic plate IDs.
    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
        => Generate(topology, configuration, attempt,
            TectonicPlateGenerator.Generate(topology, configuration.Seed));

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration,
        int attempt, ReadOnlySpan<byte> plates)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        var profile = configuration.Continents
            ?? throw new ArgumentException("Continents parameters are required.");
        if (configuration.Type != LandmassType.Continents || profile.CoreCount is < 2 or > 6)
            throw new ArgumentException("Invalid Continents world configuration.");

        if (plates.IsEmpty)
            return Generate(topology, configuration, attempt);

        var geology = new ContinentalCrustGenerator().Generate(
            topology, plates, profile.CoreCount, configuration.Seed);
        int count = topology.Count;
        var affinity = geology.EmergenceAffinityByCellId;
        var province = geology.ProvinceByCellId;

        // A single global sea level is applied to ALL geological fragments.
        // We search a few global datums to find a plausible ocean/land ratio
        // without ever assigning a quota to an individual continent.
        var sorted = affinity.ToArray();
        Array.Sort(sorted);
        byte[]? chosen = null;
        double bestPenalty = double.PositiveInfinity;
        foreach (double landQuantile in new[] { .37, .40, .43, .46, .49, .52, .55, .59 })
        {
            float seaLevel = sorted[Math.Clamp(
                (int)Math.Round((1 - landQuantile) * (count - 1)), 0, count - 1)];
            var emerged = new byte[count];
            for (int cell = 0; cell < count; cell++)
            {
                if (geology.CrustByCellId[cell] == 1 && affinity[cell] >= seaLevel)
                    emerged[cell] = 1;
            }
            for (int owner = 0; owner < profile.CoreCount; owner++)
                emerged[geology.MainContinentCoreCells[owner]] = 1;

            // Rifted fragments of different ancestry must not accidentally
            // become a supercontinent when two separate terranes touch.
            // Both sides of a fresh rift subside; the water gap is >=2 hexes.
            var flooded = new bool[count];
            for (int cell = 0; cell < count; cell++)
            {
                if (emerged[cell] == 0) continue;
                foreach (int next in topology.Neighbors(cell))
                    if (emerged[next] != 0 && province[cell] != province[next])
                    {
                        flooded[cell] = true;
                        flooded[next] = true;
                    }
            }
            for (int cell = 0; cell < count; cell++)
                if (flooded[cell]) emerged[cell] = 0;

            // A continent is the connected geological assembly of its craton
            // and attached terranes. Disconnected large scraps are submerged,
            // while tiny scraps can remain as offshore islands.
            var land = new byte[count];
            var visited = new byte[count];
            var queue = new int[count];
            for (int owner = 0; owner < profile.CoreCount; owner++)
            {
                int core = geology.MainContinentCoreCells[owner];
                // Preserve the ancient root when sea datum is high.
                emerged[core] = 1;
                int head = 0, tail = 0;
                visited[core] = 1;
                queue[tail++] = core;
                while (head < tail)
                {
                    int cell = queue[head++];
                    if (province[cell] != owner) continue;
                    land[cell] = 1;
                    foreach (int next in topology.Neighbors(cell))
                        if (emerged[next] != 0 && visited[next] == 0 &&
                            province[next] == owner)
                        {
                            visited[next] = 1;
                            queue[tail++] = next;
                        }
                }
            }

            // Some small pieces of old continental margin may be left as
            // islands, but large disconnected blocks must not silently
            // become extra main continents.
            for (int cell = 0; cell < count; cell++)
            {
                if (emerged[cell] == 0 || land[cell] != 0 || visited[cell] != 0)
                    continue;
                int head = 0, tail = 0;
                visited[cell] = 1;
                queue[tail++] = cell;
                while (head < tail)
                {
                    int current = queue[head++];
                    foreach (int next in topology.Neighbors(current))
                        if (emerged[next] != 0 && land[next] == 0 && visited[next] == 0)
                        {
                            visited[next] = 1;
                            queue[tail++] = next;
                        }
                }
                if (tail > 18) continue;
                for (int i = 0; i < tail; i++) land[queue[i]] = 1;
            }

            LandmassStatistics statistics = LandmassAnalysis.Analyze(topology, land);
            int largeCount = statistics.ComponentSizes.Count(size => size >= 100);
            double penalty = 8 * Math.Abs(statistics.LandFraction -
                profile.TargetLandFraction) +
                3 * Math.Abs(largeCount - profile.CoreCount) +
                (statistics.DominantLandShare >= .75 ? 2 : 0) +
                (statistics.LargeComponents < 2 ? 2 : 0);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                chosen = land;
            }
            if (largeCount == profile.CoreCount &&
                LandmassGenerationService.MeetsProfile(
                    LandmassType.Continents, statistics))
            {
                AddOceanicIslands(land, topology, geology.CrustByCellId,
                    configuration.Seed, attempt, profile.IslandFraction);
                return land;
            }
        }

        if (chosen is null)
            throw new InvalidOperationException("No continental assembly could be evaluated.");
        AddOceanicIslands(chosen, topology, geology.CrustByCellId,
            configuration.Seed, attempt, profile.IslandFraction);
        return chosen;
    }

    private static void AddOceanicIslands(byte[] surface, HexTopology topology,
        ReadOnlySpan<byte> crust, int seed, int attempt, double fraction)
    {
        var random = new DeterministicRandom(seed,
            unchecked((uint)(0x49534C45 + attempt)));
        int desired = (int)(surface.Length * fraction);
        int added = 0;
        for (int trial = 0; trial < desired * 55 && added < desired; trial++)
        {
            int cell = random.NextInt(topology.Count);
            if (cell / topology.Columns < 2 ||
                cell / topology.Columns >= topology.Rows - 2 ||
                crust[cell] != 0 || surface[cell] != 0) continue;
            bool shore = false;
            foreach (int neighbor in topology.Neighbors(cell))
                if (surface[neighbor] != 0) { shore = true; break; }
            if (shore) continue;
            surface[cell] = 1;
            added++;
        }
    }
}
