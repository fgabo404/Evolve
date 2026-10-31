using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Derives continental crust from connected sets of the existing tectonic plates.</summary>
public sealed class PlateGroupSurfaceGenerator
{
    public const string Version = "plate-groups-surface-2";
    public const byte NoContinent = byte.MaxValue;

    public sealed record Result(byte[] Surface, ContinentalSurfaceState State,
        float[] EmergenceAffinityByCellId, byte[] PlateGroupsByCellId);

    public Result Generate(HexTopology topology, byte[] plates, TectonicMotionState motion, int seed,
        int attempt = 0, OrogenicOpportunityAnalyzer.Result? opportunities = null)
    {
        if (plates.Length != topology.Count || plates.Any(plate => plate >= 16) ||
            motion.ArrowCellByPlateId.Length != 16 || motion.CompressionByCellId.Length != topology.Count ||
            motion.ExtensionByCellId.Length != topology.Count)
            throw new ArgumentException("The original 16 plates and motion fields are required.");

        if (attempt < 0) throw new ArgumentOutOfRangeException(nameof(attempt));
        opportunities ??= OrogenicOpportunityAnalyzer.Analyze(topology, motion);
        var random = new DeterministicRandom(unchecked(seed ^ (attempt * 0x6D2B79F5)), 0x434F4E54);
        int groupCount = 2 + random.NextInt(5);
        var groupByPlate = Enumerable.Repeat(NoContinent, 16).ToArray();
        var plateArea = new int[16];
        var adjacency = new int[16, 16];
        var convergence = new double[16, 16];
        var divergence = new double[16, 16];
        for (int id = 0; id < topology.Count; id++)
        {
            byte owner = plates[id];
            plateArea[owner]++;
            foreach (int neighbor in topology.Neighbors(id))
                if (plates[neighbor] != owner) adjacency[owner, plates[neighbor]]++;
        }
        foreach (var edge in motion.Interactions)
        {
            convergence[edge.FirstPlateId, edge.SecondPlateId] += edge.Convergence;
            convergence[edge.SecondPlateId, edge.FirstPlateId] += edge.Convergence;
            divergence[edge.FirstPlateId, edge.SecondPlateId] += edge.Divergence;
            divergence[edge.SecondPlateId, edge.FirstPlateId] += edge.Divergence;
        }
        int minimumSeedArea = plateArea.Count(area => area >= topology.Count * .025)
            >= groupCount ? (int)(topology.Count * .025) : 0;

        // Seed whole tectonic regions, separated by the actual plate geometry.
        var seeds = new int[groupCount];
        for (int group = 0; group < groupCount; group++)
        {
            double best = double.NegativeInfinity;
            int chosen = -1;
            for (int plate = 0; plate < 16; plate++)
            {
                if (groupByPlate[plate] != NoContinent || plateArea[plate] < minimumSeedArea) continue;
                int separation = group == 0 ? 1 : seeds.Take(group).Min(previous =>
                    topology.Distance(motion.ArrowCellByPlateId[plate], motion.ArrowCellByPlateId[previous]));
                double score = 1.4 * separation / Math.Max(topology.Columns, topology.Rows) +
                    .7 * opportunities.PlateScore[plate] +
                    .12 * plateArea[plate] / topology.Count + .22 * random.NextDouble();
                if (score > best) { best = score; chosen = plate; }
            }
            seeds[group] = chosen;
            groupByPlate[chosen] = (byte)group;
        }

        int selectedArea = seeds.Sum(plate => plateArea[plate]);
        var weights = Enumerable.Range(0, groupCount).Select(_ => .55 + 1.25 * random.NextDouble()).ToArray();
        var groupArea = seeds.Select(plate => plateArea[plate]).ToArray();
        // Add neighboring plates to the groups. Plate adjacency, size and existing
        // convergence/extension decide accretion; no free cell or core growth occurs.
        while (selectedArea < topology.Count * .57)
        {
            double best = double.NegativeInfinity;
            int chosenPlate = -1, chosenGroup = -1;
            for (int plate = 0; plate < 16; plate++)
            {
                if (groupByPlate[plate] != NoContinent) continue;
                for (int group = 0; group < groupCount; group++)
                {
                    int shared = 0;
                    double tectonicLink = 0, orogenicLink = 0;
                    for (int other = 0; other < 16; other++)
                        if (groupByPlate[other] == group)
                        {
                            shared += adjacency[plate, other];
                            tectonicLink += convergence[plate, other] - divergence[plate, other];
                            orogenicLink = Math.Max(orogenicLink,
                                opportunities.PairScore[plate, other]);
                        }
                    if (shared == 0) continue;
                    double desired = topology.Count * .57 * weights[group] / weights.Sum();
                    double score = Math.Log(1 + shared) * .35 +
                        2.0 * (desired - groupArea[group]) / desired +
                        .18 * Math.Clamp(tectonicLink / shared, -4, 4) +
                        .55 * orogenicLink +
                        .25 * random.NextDouble();
                    if (score > best) { best = score; chosenPlate = plate; chosenGroup = group; }
                }
            }
            if (chosenPlate < 0) break;
            groupByPlate[chosenPlate] = (byte)chosenGroup;
            groupArea[chosenGroup] += plateArea[chosenPlate];
            selectedArea += plateArea[chosenPlate];
        }

        var groups = new byte[topology.Count];
        for (int id = 0; id < groups.Length; id++) groups[id] = groupByPlate[plates[id]];
        var distance = new ushort[topology.Count];
        Array.Fill(distance, ushort.MaxValue);
        var queue = new int[topology.Count];
        int head = 0, tail = 0;
        for (int id = 0; id < topology.Count; id++)
        {
            if (groups[id] == NoContinent) continue;
            foreach (int neighbor in topology.Neighbors(id))
                if (groups[neighbor] != groups[id])
                {
                    distance[id] = 0;
                    queue[tail++] = id;
                    break;
                }
        }
        while (head < tail)
        {
            int id = queue[head++];
            foreach (int neighbor in topology.Neighbors(id))
                if (groups[neighbor] == groups[id] && distance[neighbor] == ushort.MaxValue)
                {
                    distance[neighbor] = (ushort)(distance[id] + 1);
                    queue[tail++] = neighbor;
                }
        }

        var maximumInterior = new ushort[groupCount];
        for (int id = 0; id < topology.Count; id++)
            if (groups[id] != NoContinent && distance[id] != ushort.MaxValue)
                maximumInterior[groups[id]] = Math.Max(maximumInterior[groups[id]], distance[id]);

        var affinity = new float[topology.Count];
        var crust = new byte[topology.Count];
        for (int id = 0; id < topology.Count; id++)
        {
            if (groups[id] == NoContinent) continue;
            int x = id % topology.Columns, y = id / topology.Columns;
            double broad = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 5, seed ^ 0x591E20);
            double detail = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 17, seed ^ 0x12C4A1);
            double interior = distance[id] / (double)Math.Max(1, (int)maximumInterior[groups[id]]);
            double stress = Math.Clamp(motion.CompressionByCellId[id] - motion.ExtensionByCellId[id], -5, 5);
            affinity[id] = (float)(.51 + .24 * interior + .085 * broad + .025 * detail + .005 * stress);
            if (affinity[id] >= .46) crust[id] = (byte)CrustKind.Continental;
        }

        // One global sea level, determined from all continental crust together.
        // Individual plate groups receive no land quota.
        var eligible = affinity.Where((value, id) => crust[id] == (byte)CrustKind.Continental)
            .OrderDescending().ToArray();
        if (eligible.Length < topology.Count * .30)
            throw new InvalidOperationException("The selected plate groups contain too little continental crust.");
        int rank = Math.Min(eligible.Length - 1, (int)Math.Round(topology.Count * .35));
        float seaLevel = eligible[rank];
        var surface = new byte[topology.Count];
        var continent = Enumerable.Repeat(NoContinent, topology.Count).ToArray();
        for (int id = 0; id < topology.Count; id++)
            if (crust[id] == (byte)CrustKind.Continental && affinity[id] >= seaLevel)
            {
                surface[id] = 1;
                continent[id] = groups[id];
            }
        return new(surface, new ContinentalSurfaceState(Version, groupByPlate, crust, continent, seaLevel),
            affinity, groups);
    }
}
