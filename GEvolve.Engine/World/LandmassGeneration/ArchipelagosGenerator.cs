using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Trayectorias curvas que depositan islas de tamaño y silueta variables.</summary>
public sealed class ArchipelagosGenerator : ILandmassGenerator
{
    public LandmassType Type => LandmassType.Archipelagos;

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
    {
        var profile = configuration.Archipelagos
            ?? throw new ArgumentException("Archipelago parameters are missing.");
        var random = new DeterministicRandom(configuration.Seed, (uint)(0xA7C4 + attempt));
        var surface = new byte[topology.Count];
        var islandOwner = new int[topology.Count];
        Array.Fill(islandOwner, -1);
        int land = 0;
        int target = (int)(profile.TargetLandFraction * topology.Count);
        for (int chain = 0; chain < profile.ChainCount && land < target; chain++)
        {
            double x = random.NextInt(topology.Columns);
            double y = topology.Rows * (.1 + .8 * random.NextDouble());
            double heading = random.NextDouble() * Math.PI * 2;
            double curve = (random.NextDouble() - .5) * .18;
            for (int step = 0; step < profile.StepsPerChain && land < target; step++)
            {
                int column = Wrap((int)Math.Round(x), topology.Columns);
                int row = Math.Clamp((int)Math.Round(y), 0, topology.Rows - 1);
                int center = topology.Id(column, row);
                int island = chain * profile.StepsPerChain + step;
                double radius = profile.MinimumIslandRadius + random.NextDouble() *
                    (profile.MaximumIslandRadius - profile.MinimumIslandRadius);
                Stamp(center, radius, chain, island);
                if (step % 5 == 0 && land < target)
                {
                    int extensionColumn = Wrap(column + (int)Math.Round(Math.Cos(heading)),
                        topology.Columns);
                    int extensionRow = Math.Clamp(row + (int)Math.Round(Math.Sin(heading)),
                        0, topology.Rows - 1);
                    Stamp(topology.Id(extensionColumn, extensionRow), radius * .65,
                        chain, island);
                }
                double bend = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows,
                    profile.NoiseFrequency, configuration.Seed + attempt * 2663 + chain);
                heading += curve + bend * .14;
                double stride = 3.5 + 2 * random.NextDouble();
                x = (x + Math.Cos(heading) * stride + topology.Columns) % topology.Columns;
                y += Math.Sin(heading) * stride;
                if (y < 3 || y > topology.Rows - 4)
                {
                    y = Math.Clamp(y, 3, topology.Rows - 4);
                    heading = -heading;
                }
            }
        }
        return surface;

        void Stamp(int center, double radius, int chainIndex, int island)
        {
            int centerColumn = center % topology.Columns, centerRow = center / topology.Columns;
            int bound = (int)Math.Ceiling(radius * 2.5);
            for (int row = Math.Max(0, centerRow - bound);
                 row <= Math.Min(topology.Rows - 1, centerRow + bound) && land < target; row++)
            for (int column = centerColumn - bound;
                 column <= centerColumn + bound && land < target; column++)
            {
                int id = topology.Id(column, row);
                if (surface[id] != 0) continue;
                bool touchesOtherIsland = false;
                foreach (int neighbor in topology.Neighbors(id))
                    if (islandOwner[neighbor] >= 0 && islandOwner[neighbor] != island)
                    { touchesOtherIsland = true; break; }
                if (touchesOtherIsland) continue;
                int distance = topology.Distance(center, id);
                if (distance > radius + 1) continue;
                double noise = PeriodicNoise.Sample(column, row, topology.Columns,
                    topology.Rows, profile.NoiseFrequency,
                    configuration.Seed + attempt * 3011 + chainIndex * 41);
                if (distance > radius * (1 + profile.CoastRoughness * noise)) continue;
                surface[id] = 1;
                islandOwner[id] = island;
                land++;
            }
        }
    }

    private static int Wrap(int value, int width) => (value % width + width) % width;
}
