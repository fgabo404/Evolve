using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Stage 02 topography for continents, small continents and archipelagos.
/// Consumes an existing land mask and the immutable universal plate IDs.
/// The type chooses a different balance of continental interior and tectonic relief.
/// No climate, precipitation or biome classification is performed here.
/// </summary>
public sealed class PlateAwareElevationGenerator
{
    public const string Version = "plate-relief-1";

    public byte[] Generate(
        HexTopology topology, ReadOnlySpan<byte> surface, ReadOnlySpan<byte> plates,
        LandmassType type, int seed, int attempt)
    {
        if (type is LandmassType.Pangaea)
            throw new ArgumentException("Pangaea already provides its own elevation model.", nameof(type));
        if (surface.Length != topology.Count || plates.Length != topology.Count ||
            surface.IndexOfAnyExcept((byte)0, (byte)1) >= 0 ||
            plates.ToArray().Any(plate => plate >= TectonicPlateGenerator.NumberOfPlates))
            throw new ArgumentException("Land or plate arrays differ from the hex topology.");

        int count = topology.Count, width = topology.Columns, height = topology.Rows;
        var random = new DeterministicRandom(seed, 0x52454C46);
        var vx = new double[TectonicPlateGenerator.NumberOfPlates];
        var vy = new double[TectonicPlateGenerator.NumberOfPlates];
        for (int i = 0; i < vx.Length; i++)
        {
            vx[i] = random.NextDouble() * 2 - 1;
            vy[i] = random.NextDouble() * 2 - 1;
        }

        // A seeded relative-motion proxy. At this stage these are NOT claims
        // about actual subduction, divergence or a simulated plate history.
        var uplift = new double[count];
        for (int id = 0; id < count; id++)
        {
            if (surface[id] == 0) continue;
            int plate = plates[id], x = id % width, y = id / width;
            foreach (int next in topology.Neighbors(id))
            {
                int other = plates[next];
                if (other == plate) continue;
                int dx = next % width - x;
                if (dx > width / 2) dx -= width;
                if (dx < -width / 2) dx += width;
                int dy = next / width - y;
                double length = Math.Sqrt(dx * dx + dy * dy) + 1e-9;
                double convergence = ((vx[plate] - vx[other]) * dx +
                    (vy[plate] - vy[other]) * dy) / length;
                uplift[id] = Math.Max(uplift[id], Math.Max(0, convergence));
            }
        }

        // Spread the pressure signal inland instead of raising only border cells.
        for (int pass = 0; pass < 6; pass++)
        {
            var next = new double[count];
            for (int id = 0; id < count; id++)
            {
                if (surface[id] == 0) continue;
                double total = uplift[id] * 4;
                int weight = 4;
                foreach (int neighbor in topology.Neighbors(id))
                {
                    if (surface[neighbor] == 0) continue;
                    total += uplift[neighbor];
                    weight++;
                }
                next[id] = total / weight;
            }
            uplift = next;
        }

        // Flood fill exclusively through land. A short distance to the coast
        // encourages low plains; a long distance supports continental interiors.
        var inland = new int[count];
        Array.Fill(inland, -1);
        var queue = new Queue<int>();
        for (int id = 0; id < count; id++)
        {
            if (surface[id] == 0) continue;
            foreach (int nb in topology.Neighbors(id))
            {
                if (surface[nb] != 0) continue;
                inland[id] = 0;
                queue.Enqueue(id);
                break;
            }
        }
        while (queue.TryDequeue(out int id))
        {
            foreach (int nb in topology.Neighbors(id))
            {
                if (surface[nb] == 0 || inland[nb] >= 0) continue;
                inland[nb] = inland[id] + 1;
                queue.Enqueue(nb);
            }
        }

        // Each world type has its own topographic signature; the climatic and
        // biome pipeline can later consume all four as the same 0..100 array.
        var (interiorWeight, mountainWeight, broadWeight, mediumWeight, fineWeight) =
            type switch
            {
                LandmassType.Continents => (1.30, 2.8, 1.15, .58, .24),
                LandmassType.SmallContinents => (.68, 2.6, .94, .75, .34),
                LandmassType.Archipelagos => (.26, 3.5, .76, .72, .42),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        var potential = new double[count];
        var sorted = new List<double>(count / 2);
        for (int id = 0; id < count; id++)
        {
            if (surface[id] == 0) continue;
            int x = id % width, y = id / width;
            double broad = PeriodicNoise.Sample(x, y, width, height, 4,
                seed ^ (int)0x3A176E2B ^ (attempt * 233));
            double medium = PeriodicNoise.Sample(x, y, width, height, 12,
                seed ^ (int)0x2C051AEF ^ (attempt * 443));
            double fine = PeriodicNoise.Sample(x, y, width, height, 27,
                seed ^ (int)0x73F2109A ^ (attempt * 619));
            double continental = Math.Log(1 + Math.Max(0, inland[id]));
            double value = interiorWeight * continental +
                mountainWeight * uplift[id] +
                broadWeight * broad + mediumWeight * medium + fineWeight * fine;
            potential[id] = value;
            sorted.Add(value);
        }
        if (sorted.Count == 0) throw new ArgumentException("Land mask has no land.", nameof(surface));
        sorted.Sort();
        double[] quantiles = [0, .34, .78, .955, 1];
        double[] levels = [1, 16, 50, 80, 100];
        var anchors = new double[quantiles.Length];
        for (int i = 0; i < anchors.Length; i++)
            anchors[i] = sorted[(int)Math.Round(quantiles[i] * (sorted.Count - 1))];

        var elevation = new byte[count];
        for (int id = 0; id < count; id++)
        {
            if (surface[id] == 0) continue;
            double value = potential[id];
            int section = 0;
            while (section < anchors.Length - 2 && value > anchors[section + 1])
                section++;
            double span = anchors[section + 1] - anchors[section];
            double fraction = span > 1e-12
                ? Math.Clamp((value - anchors[section]) / span, 0, 1) : 0;
            elevation[id] = (byte)Math.Clamp((int)Math.Round(
                levels[section] + fraction * (levels[section + 1] - levels[section])), 1, 100);
        }
        return elevation;
    }
}
