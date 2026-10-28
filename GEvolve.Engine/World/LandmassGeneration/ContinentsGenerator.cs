using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Núcleos distantes compiten por expansión irregular con cuotas distintas.</summary>
public sealed class ContinentsGenerator : IPlateAwareLandmassGenerator
{
    private readonly record struct Frontier(int Id, int Owner, double Cost);
    public LandmassType Type => LandmassType.Continents;

    // Keep the legacy path callable, while the staged world passes existing plates.
    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
        => Generate(topology, configuration, attempt, ReadOnlySpan<byte>.Empty);

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration,
        int attempt, ReadOnlySpan<byte> plates)
    {
        var tectonics = new PlateSurfaceInfluence(topology, plates, configuration.Seed);
        var profile = configuration.Continents
            ?? throw new ArgumentException("Continents parameters are missing.");
        var random = new DeterministicRandom(configuration.Seed, (uint)(0xC011 + attempt));
        int[] cores = HexRegionUtilities.SeparatedSeeds(topology, profile.CoreCount,
            profile.MinimumCoreDistance, ref random);
        int islands = (int)(topology.Count * profile.IslandFraction);
        int[] budgets = HexRegionUtilities.Budgets(
            (int)(topology.Count * profile.TargetLandFraction) - islands,
            cores.Length, ref random);
        var owners = new int[topology.Count];
        Array.Fill(owners, -1);
        var owned = new int[cores.Length];
        var frontier = new PriorityQueue<Frontier, double>();
        for (int owner = 0; owner < cores.Length; owner++)
        {
            owners[cores[owner]] = owner;
            owned[owner] = 1;
            EnqueueNeighbors(cores[owner], owner, 0);
        }
        int land = cores.Length;
        int target = budgets.Sum();
        while (frontier.TryDequeue(out var item, out _) && land < target)
        {
            if (owners[item.Id] != -1 || owned[item.Owner] >= budgets[item.Owner]) continue;
            bool otherMassTouches = false;
            foreach (int neighbor in topology.Neighbors(item.Id))
                if (owners[neighbor] >= 0 && owners[neighbor] != item.Owner)
                { otherMassTouches = true; break; }
            if (otherMassTouches) continue;
            owners[item.Id] = item.Owner;
            owned[item.Owner]++;
            land++;
            EnqueueNeighbors(item.Id, item.Owner, item.Cost);
        }
        var surface = new byte[topology.Count];
        for (int id = 0; id < surface.Length; id++)
            if (owners[id] >= 0) surface[id] = 1;
        HexRegionUtilities.AddSecondaryIslands(topology, surface, islands, ref random);
        return surface;

        void EnqueueNeighbors(int from, int owner, double cost)
        {
            foreach (int neighbor in topology.Neighbors(from))
            {
                if (owners[neighbor] != -1) continue;
                int column = neighbor % topology.Columns, row = neighbor / topology.Columns;
                double noise = PeriodicNoise.Sample(column, row, topology.Columns,
                    topology.Rows, profile.NoiseFrequency, configuration.Seed + attempt * 1543);
                double next = cost + Math.Max(.18,
                    1 + profile.CoastRoughness * noise +
                    tectonics.ExpansionCost(from, neighbor));
                frontier.Enqueue(new Frontier(neighbor, owner, next), next + neighbor * 1e-9);
            }
        }
    }
}
