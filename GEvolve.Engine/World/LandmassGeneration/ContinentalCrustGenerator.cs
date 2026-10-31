using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed record ContinentalCrustMap(byte[] CrustByCellId, float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells, byte[] MainContinentCorePlates, byte[] ProvinceByCellId,
    float[] CollisionByCellId, float[] RiftByCellId);

/// <summary>Connected regional growth from stable cratons across the real plate graph.</summary>
public sealed class ContinentalCrustGenerator
{
    public const string Version = "continental-growth-3";

    public ContinentalCrustMap Generate(HexTopology topology, ReadOnlySpan<byte> plateIds,
        int continentCount, int seed, TectonicMotionState? motion = null, int assemblyAttempt = 0)
    {
        if (continentCount is < 2 or > 6 || plateIds.Length != topology.Count)
            throw new ArgumentException("Invalid continental crust inputs.");
        var plates = plateIds.ToArray();
        motion ??= new TectonicMotionGenerator().Generate(topology,
            new(GEvolve.Definitions.WorldGeneration.Configuration.LandmassProfiles.Create(
                topology.Grid.Definition.Size, GEvolve.Definitions.WorldGeneration.Configuration.LandmassType.Continents, seed),
                new byte[topology.Count], 0) { PlateByCellId = plates });

        int count = topology.Count, width = topology.Columns, rows = topology.Rows;
        var rng = new DeterministicRandom(seed ^ (assemblyAttempt * 0x138E2), 0x43524154);
        var stable = new double[count];
        var broad = new double[count];
        var regional = new double[count];
        var detail = new double[count];
        for (int id = 0; id < count; id++)
        {
            int x = id % width, y = id / width;
            broad[id] = PeriodicNoise.Sample(x, y, width, rows, 5, seed ^ 0x17A27);
            regional[id] = PeriodicNoise.Sample(x, y, width, rows, 13, seed ^ 0x13A27);
            detail[id] = PeriodicNoise.Sample(x, y, width, rows, 29, seed ^ 0x74C31);
            double latitude = Math.Abs(y / (double)(rows - 1) - .5);
            stable[id] = motion.DistanceToBoundaryByCellId[id] - 48 * latitude + 3 * broad[id];
        }

        // Stable plate interiors supply candidates. Spacing is global on the wrapped hex grid.
        var plateCores = new int[16];
        var bestDepth = Enumerable.Repeat(double.NegativeInfinity, 16).ToArray();
        for (int id = 0; id < count; id++)
            if (id / width >= rows * .12 && id / width <= rows * .88 &&
                stable[id] > bestDepth[plates[id]])
            { bestDepth[plates[id]] = stable[id]; plateCores[plates[id]] = id; }
        for (int plate = 0; plate < 16; plate++)
            if (bestDepth[plate] == double.NegativeInfinity)
                for (int id = 0; id < count; id++)
                    if (plates[id] == plate && stable[id] > bestDepth[plate])
                    { bestDepth[plate] = stable[id]; plateCores[plate] = id; }
        var hubs = new int[continentCount]; var hubPlates = new byte[continentCount];
        hubPlates[0] = (byte)rng.NextInt(16); hubs[0] = plateCores[hubPlates[0]];
        for (int owner = 1; owner < continentCount; owner++)
        {
            int best = -1; double bestScore = double.NegativeInfinity;
            for (int p = 0; p < 16; p++)
            {
                if (hubPlates.AsSpan(0, owner).Contains((byte)p)) continue;
                double score = hubs.Take(owner).Min(hub => topology.Distance(hub, plateCores[p])) +
                    .2 * bestDepth[p] + 8 * rng.NextDouble();
                if (score > bestScore) { bestScore = score; best = p; }
            }
            hubPlates[owner] = (byte)best; hubs[owner] = plateCores[best];
        }

        var contacts = new Dictionary<long, double>(motion.Interactions.Length);
        foreach (var edge in motion.Interactions)
        {
            // Accretion crosses convergent boundaries; rifting resists the same growth.
            double modifier = Math.Clamp(1 + .07 * edge.Divergence - .05 * edge.Convergence +
                .015 * edge.Shear, .55, 1.8);
            contacts[EdgeKey(edge.FirstCellId, edge.SecondCellId)] = modifier;
        }

        var distance = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
        var province = new byte[count];
        var queue = new PriorityQueue<int, (double Cost, int Id)>();
        for (byte owner = 0; owner < continentCount; owner++)
        { distance[hubs[owner]] = 0; province[hubs[owner]] = owner; queue.Enqueue(hubs[owner], (0, hubs[owner])); }
        while (queue.TryDequeue(out int id, out var priority))
        {
            if (priority.Cost != distance[id]) continue;
            foreach (int next in topology.Neighbors(id))
            {
                double local = 1 + .8 * broad[next] + .5 * regional[next] + .12 * detail[next] +
                    .014 * motion.ExtensionByCellId[next] - .009 * motion.CompressionByCellId[next];
                double crossing = plates[id] == plates[next] ? 1 : contacts[EdgeKey(id, next)];
                double candidate = priority.Cost + Math.Max(.35, local * crossing);
                if (candidate >= distance[next]) continue;
                distance[next] = candidate; province[next] = province[id];
                queue.Enqueue(next, (candidate, next));
            }
        }

        // The same global field supplies the continental shelf and the eventual sea datum.
        // Broad, regional and fine scales deform shores without replacing craton growth.
        var affinity = new float[count];
        for (int id = 0; id < count; id++)
            affinity[id] = (float)(-distance[id] + 25 * broad[id] + 12 * regional[id] + 3 * detail[id]);
        var sorted = affinity.Order().ToArray();
        float shelfDatum = sorted[(int)(.45 * (count - 1))];
        var crust = affinity.Select(value => value >= shelfDatum ? (byte)CrustKind.Continental : (byte)CrustKind.Oceanic).ToArray();
        foreach (int hub in hubs) crust[hub] = (byte)CrustKind.Continental;
        return new(crust, affinity, hubs, hubPlates, province,
            motion.CompressionByCellId, motion.ExtensionByCellId);
    }

    private static long EdgeKey(int first, int second)
        => ((long)Math.Min(first, second) << 32) | (uint)Math.Max(first, second);
}
