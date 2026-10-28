using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Una sola frontera conectada; los frentes direccionales esculpen la costa.</summary>
public sealed class PangaeaGenerator : ILandmassGenerator
{
    private readonly record struct Frontier(int Id, double Cost);
    public LandmassType Type => LandmassType.Pangaea;

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
    {
        var profile = configuration.Pangaea
            ?? throw new ArgumentException("Pangaea parameters are missing.");
        var random = new DeterministicRandom(configuration.Seed, (uint)(0xA9EA + attempt));
        int centerColumn = random.NextInt(topology.Columns);
        int centerRow = (int)(topology.Rows * (.35 + .3 * random.NextDouble()));
        int core = topology.Id(centerColumn, centerRow);
        double phase = random.NextDouble() * Math.PI * 2;
        int islands = (int)(profile.IslandFraction * topology.Count);
        int target = (int)(profile.TargetLandFraction * topology.Count) - islands;
        var surface = new byte[topology.Count];
        surface[core] = 1;
        int land = 1;
        var frontier = new PriorityQueue<Frontier, double>();
        EnqueueNeighbors(core, 0);
        while (frontier.TryDequeue(out var item, out _) && land < target)
        {
            if (surface[item.Id] != 0) continue;
            surface[item.Id] = 1;
            land++;
            EnqueueNeighbors(item.Id, item.Cost);
        }
        HexRegionUtilities.AddSecondaryIslands(topology, surface, islands, ref random);
        return surface;

        void EnqueueNeighbors(int from, double cost)
        {
            foreach (int neighbor in topology.Neighbors(from))
            {
                if (surface[neighbor] != 0) continue;
                int col = neighbor % topology.Columns, row = neighbor / topology.Columns;
                double dx = col - centerColumn;
                if (dx > topology.Columns / 2.0) dx -= topology.Columns;
                if (dx < -topology.Columns / 2.0) dx += topology.Columns;
                double angle = Math.Atan2(row - centerRow, dx);
                double directional = Math.Sin(3 * angle + phase);
                double noise = PeriodicNoise.Sample(col, row, topology.Columns,
                    topology.Rows, profile.NoiseFrequency, configuration.Seed + attempt * 1877);
                double next = cost + Math.Max(.15,
                    1 + profile.CoastRoughness * noise + profile.DirectionalVariation * directional);
                frontier.Enqueue(new Frontier(neighbor, next), next + neighbor * 1e-9);
            }
        }
    }
}
