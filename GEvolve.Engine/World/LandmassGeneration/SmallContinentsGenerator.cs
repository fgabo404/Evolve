using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Regiones locales con radio y cuota propios; corredores de agua entre núcleos.</summary>
public sealed class SmallContinentsGenerator : ILandmassGenerator
{
    public LandmassType Type => LandmassType.SmallContinents;

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
    {
        var profile = configuration.SmallContinents
            ?? throw new ArgumentException("Small-continent parameters are missing.");
        var random = new DeterministicRandom(configuration.Seed, (uint)(0x5C0A + attempt));
        int[] cores = HexRegionUtilities.SeparatedSeeds(topology, profile.CoreCount,
            (int)Math.Ceiling(profile.MaximumRadius * 2 + profile.CorridorWidth), ref random);
        int[] budgets = HexRegionUtilities.Budgets(
            (int)(topology.Count * profile.TargetLandFraction), cores.Length, ref random);
        var owners = new int[topology.Count];
        var seen = new int[topology.Count];
        Array.Fill(owners, -1);
        var queue = new int[topology.Count];

        for (int owner = 0; owner < cores.Length; owner++)
        {
            int core = cores[owner];
            if (owners[core] != -1) continue;
            double radius = profile.MaximumRadius * (.88 + .17 * random.NextDouble());
            int head = 0, tail = 0, owned = 1;
            owners[core] = owner;
            seen[core] = owner + 1;
            queue[tail++] = core;
            while (head < tail && owned < budgets[owner])
            {
                foreach (int neighbor in topology.Neighbors(queue[head++]))
                {
                    if (seen[neighbor] == owner + 1 || owners[neighbor] != -1) continue;
                    seen[neighbor] = owner + 1;
                    int distance = topology.Distance(core, neighbor);
                    int column = neighbor % topology.Columns, row = neighbor / topology.Columns;
                    double noise = PeriodicNoise.Sample(column, row, topology.Columns,
                        topology.Rows, profile.NoiseFrequency,
                        configuration.Seed + attempt * 2203 + owner * 79);
                    if (distance > radius * (1 + profile.CoastRoughness * noise)) continue;
                    bool touchesOther = false;
                    foreach (int adjacent in topology.Neighbors(neighbor))
                        if (owners[adjacent] >= 0 && owners[adjacent] != owner)
                        { touchesOther = true; break; }
                    if (touchesOther) continue;
                    owners[neighbor] = owner;
                    queue[tail++] = neighbor;
                    owned++;
                    if (owned >= budgets[owner]) break;
                }
            }
        }
        var surface = new byte[topology.Count];
        for (int id = 0; id < surface.Length; id++)
            if (owners[id] >= 0) surface[id] = 1;
        HexRegionUtilities.AddSecondaryIslands(topology, surface,
            (int)(topology.Count * profile.SecondaryIslandFraction), ref random);
        return surface;
    }
}
