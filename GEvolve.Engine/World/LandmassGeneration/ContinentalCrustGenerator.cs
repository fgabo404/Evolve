using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed record ContinentalCrustMap(byte[] CrustByCellId, float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells, byte[] MainContinentCorePlates, byte[] ProvinceByCellId,
    float[] CollisionByCellId, float[] RiftByCellId);

/// <summary>Connected cratons and accreted backbones on the actual plate graph.
/// No synthetic velocities, per-continent area budgets or ellipse primitives.</summary>
public sealed class ContinentalCrustGenerator
{
    public const string Version = "continental-assembly-2";

    public ContinentalCrustMap Generate(HexTopology topology, ReadOnlySpan<byte> plateIds,
        int continentCount, int seed, TectonicMotionState? motion = null)
    {
        if (continentCount is < 2 or > 6 || plateIds.Length != topology.Count)
            throw new ArgumentException("Invalid continental crust inputs.");
        var plates = plateIds.ToArray();
        // Compatibility callers reconstruct the SAME official motions, never a private proxy.
        motion ??= new TectonicMotionGenerator().Generate(topology,
            new(GEvolve.Definitions.WorldGeneration.Configuration.LandmassProfiles.Create(
                topology.Grid.Definition.Size, GEvolve.Definitions.WorldGeneration.Configuration.LandmassType.Continents, seed),
                new byte[topology.Count], 0) { PlateByCellId = plates });
        int count = topology.Count, width = topology.Columns, rows = topology.Rows;
        var rng = new DeterministicRandom(seed, 0x43524154);
        var plateCores = new int[16];
        var bestDepth = Enumerable.Repeat(double.NegativeInfinity, 16).ToArray();
        for (int id = 0; id < count; id++)
        {
            double latitudePenalty = Math.Abs(id / width / (double)rows - .5) * 12;
            double score = motion.DistanceToBoundaryByCellId[id] - latitudePenalty;
            if (score > bestDepth[plates[id]]) { bestDepth[plates[id]] = score; plateCores[plates[id]] = id; }
        }
        var hubs = new int[continentCount]; var hubPlates = new byte[continentCount];
        hubPlates[0] = (byte)rng.NextInt(16); hubs[0] = plateCores[hubPlates[0]];
        for (int owner = 1; owner < continentCount; owner++)
        {
            int best = -1; double separation = -1;
            for (int p = 0; p < 16; p++)
            {
                if (hubPlates.AsSpan(0, owner).Contains((byte)p)) continue;
                double candidateSeparation = hubs.Take(owner).Min(hub => topology.Distance(hub, plateCores[p]));
                if (candidateSeparation > separation) { separation = candidateSeparation; best = p; }
            }
            hubPlates[owner] = (byte)best; hubs[owner] = plateCores[best];
        }
        var roughness = new float[count];
        for (int id = 0; id < count; id++)
            roughness[id] = (float)PeriodicNoise.Sample(id % width, id / width, width, rows, 9, seed ^ 0x13A27);

        // Geological ancestry: multi-source travel cost, not a land quota or a plate-as-continent rule.
        var distanceFromRoot = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
        var province = new byte[count]; var parent = Enumerable.Repeat(-1, count).ToArray();
        var queue = new PriorityQueue<int, (double Cost, int Id)>();
        for (byte owner = 0; owner < continentCount; owner++)
        {
            int id = hubs[owner]; distanceFromRoot[id] = 0; province[id] = owner; queue.Enqueue(id, (0, id));
        }
        while (queue.TryDequeue(out int id, out var priority))
        {
            if (priority.Cost != distanceFromRoot[id]) continue;
            foreach (int next in topology.Neighbors(id))
            {
                double candidate = priority.Cost + Cost(id, next);
                if (candidate >= distanceFromRoot[next]) continue;
                distanceFromRoot[next] = candidate; province[next] = province[id]; parent[next] = id;
                queue.Enqueue(next, (candidate, next));
            }
        }
        var clearance = Enumerable.Repeat(-1, count).ToArray();
        var flood = new Queue<int>();
        for (int id = 0; id < count; id++)
            foreach (int next in topology.Neighbors(id))
                if (province[id] != province[next]) { clearance[id] = 0; flood.Enqueue(id); break; }
        while (flood.TryDequeue(out int id))
            foreach (int next in topology.Neighbors(id))
                if (clearance[next] < 0) { clearance[next] = clearance[id] + 1; flood.Enqueue(next); }

        var skeleton = new bool[count];
        for (int owner = 0; owner < continentCount; owner++)
        {
            int hub = hubs[owner]; skeleton[hub] = true;
            double maxReach = Enumerable.Range(0, count).Where(id => province[id] == owner).Max(id => distanceFromRoot[id]) * .62;
            double heading = motion.Motions[hubPlates[owner]].DirectionDegrees * Math.PI / 180;
            // Attached terranes target the real plate contacts in several directions.
            for (int arm = 0; arm < 4; arm++)
            {
                double angle = heading + arm * Math.PI / 2;
                int endpoint = hub; double best = double.NegativeInfinity;
                for (int id = 0; id < count; id++)
                {
                    if (province[id] != owner || clearance[id] < 3 || distanceFromRoot[id] > maxReach) continue;
                    double dx = id % width - hub % width; dx -= width * Math.Round(dx / width);
                    double dy = (id / width - hub / width) * 1.1547;
                    double alignment = dx * Math.Cos(angle) + dy * Math.Sin(angle);
                    double contact = 1 + .025 * motion.CompressionByCellId[id] - .015 * motion.ExtensionByCellId[id];
                    double score = alignment * contact - .30 * Math.Abs(-dx * Math.Sin(angle) + dy * Math.Cos(angle));
                    if (score > best) { best = score; endpoint = id; }
                }
                for (int id = endpoint; id >= 0; id = parent[id])
                {
                    if (clearance[id] >= 2) skeleton[id] = true;
                    if (id == hub) break;
                }
            }
        }
        var distance = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
        for (int id = 0; id < count; id++)
            if (skeleton[id]) { distance[id] = 0; queue.Enqueue(id, (0, id)); }
        while (queue.TryDequeue(out int id, out var priority))
        {
            if (priority.Cost != distance[id]) continue;
            foreach (int next in topology.Neighbors(id))
            {
                if (province[next] != province[id]) continue;
                double candidate = priority.Cost + Cost(id, next);
                if (candidate >= distance[next]) continue;
                distance[next] = candidate; queue.Enqueue(next, (candidate, next));
            }
        }
        double scale = Math.Sqrt(count / (double)continentCount) * .15;
        var affinity = new float[count];
        for (int id = 0; id < count; id++)
            affinity[id] = clearance[id] < 1 ? -100 :
                (float)(-distance[id] / scale + .07 * roughness[id] - .012 * motion.ExtensionByCellId[id]);
        var sorted = affinity.Order().ToArray();
        float datum = sorted[(int)(.45 * (count - 1))];
        var crust = affinity.Select(value => value >= datum ? (byte)CrustKind.Continental : (byte)CrustKind.Oceanic).ToArray();
        for (int i = 0; i < hubs.Length; i++) { crust[hubs[i]] = 1; affinity[hubs[i]] = 1; province[hubs[i]] = (byte)i; }
        return new(crust, affinity, hubs, hubPlates, province,
            motion.CompressionByCellId, motion.ExtensionByCellId);

        double Cost(int from, int to)
        {
            var normal = TectonicMotionGenerator.BoundaryNormal(topology, from, to);
            var direction = motion.Motions[plates[to]].UnitVector;
            double alignment = Math.Abs(normal.X * direction.X + normal.Y * direction.Y);
            return Math.Max(.4, 1.25 - .35 * alignment + .20 * roughness[to] +
                .015 * motion.ExtensionByCellId[to] - .012 * motion.CompressionByCellId[to]);
        }
    }
}
