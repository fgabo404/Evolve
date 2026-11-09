using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.Tectonics;

/// <summary>
/// Planet-wide tectonic mosaic, independent of continent type and grid resolution.
/// Weighted hex-region growth creates contiguous, unequal plates with irregular borders.
/// </summary>
public static class TectonicPlateGenerator
{
    public const string Version = "tectonic-plates-2";
    // Controls border complexity without changing the number or connectedness of plates.
    public const double BorderIrregularity = 1.90;
    private const double MinimumResistance = .27;
    private const double MaximumResistance = 3.10;
    public const int NumberOfPlates = 16;
    private const byte Unassigned = byte.MaxValue;

    private readonly record struct PlateGrowthProfile(
        double Cost,
        double BroadX, double BroadY,
        double MediumX, double MediumY,
        double FineX, double FineY);

    public static byte[] Generate(HexTopology topology, int seed)
    {
        ArgumentNullException.ThrowIfNull(topology);
        int count = topology.Count, columns = topology.Columns, rows = topology.Rows;
        if (count < NumberOfPlates)
            throw new ArgumentException("Not enough hexagons for tectonic plates.", nameof(topology));

        var random = new DeterministicRandom(seed, 0x54454331);
        var origins = new int[NumberOfPlates];

        // Farthest-candidate sampling prevents tiny seed clusters, without imposing a grid.
        origins[0] = random.NextInt(count);
        for (int plate = 1; plate < NumberOfPlates; plate++)
        {
            int best = -1, bestSeparation = -1;
            for (int attempt = 0; attempt < 160; attempt++)
            {
                int candidate = random.NextInt(count);
                int distance = int.MaxValue;
                for (int other = 0; other < plate; other++)
                    distance = Math.Min(distance, topology.Distance(candidate, origins[other]));
                if (distance > bestSeparation ||
                    distance == bestSeparation && candidate < best)
                {
                    best = candidate;
                    bestSeparation = distance;
                }
            }
            origins[plate] = best;
        }

        // A few large plates and several smaller plates, with randomized assignment
        // of growth rates so no plate ID has a privileged geographic position.
        double[] costs =
        [
            .58, .64, .72, .79, .88, .97, 1.05, 1.14,
            1.24, 1.35, 1.48, 1.62, 1.79, 1.97, 2.17, 2.40
        ];
        for (int i = costs.Length - 1; i > 0; i--)
        {
            int swap = random.NextInt(i + 1);
            (costs[i], costs[swap]) = (costs[swap], costs[i]);
        }

        // Domain-warped, multi-scale fields: large lobes, medium bays and
        // small bends. All noise is periodic across the east/west seam.
        // Instead of giving every plate the same resistance field, each plate
        // combines these fields differently. The competition between plates
        // creates curved, asymmetric boundaries without pixel-by-pixel noise.
        var fields = new float[6][];
        for (int band = 0; band < fields.Length; band++)
            fields[band] = new float[count];

        for (int cell = 0; cell < count; cell++)
        {
            int x = cell % columns, y = cell / columns;
            double warpX = PeriodicNoise.Sample(x, y, columns, rows, 3,
                seed ^ 0x231A45) * columns * .055;
            double warpY = PeriodicNoise.Sample(x, y, columns, rows, 3,
                seed ^ 0x5E12AB) * rows * .055;
            double wx = x + warpX, wy = y + warpY;

            fields[0][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 3, seed ^ 0x7B3741);
            fields[1][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 5, seed ^ 0x31CC2F);
            fields[2][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 9, seed ^ 0x47DA1E);
            fields[3][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 13, seed ^ 0x178CAB);
            fields[4][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 21, seed ^ 0x70A145);
            fields[5][cell] = (float)PeriodicNoise.Sample(wx, wy, columns, rows, 29, seed ^ 0x2E91F4);
        }

        // Each plate has its own rotation of the noise pairs, rather than
        // sharing the same valleys with all its neighbors.
        var profiles = new PlateGrowthProfile[NumberOfPlates];
        for (int plate = 0; plate < NumberOfPlates; plate++)
        {
            double broadAngle = random.NextDouble() * 2 * Math.PI;
            double mediumAngle = random.NextDouble() * 2 * Math.PI;
            double fineAngle = random.NextDouble() * 2 * Math.PI;
            profiles[plate] = new PlateGrowthProfile(
                costs[plate],
                Math.Cos(broadAngle), Math.Sin(broadAngle),
                Math.Cos(mediumAngle), Math.Sin(mediumAngle),
                Math.Cos(fineAngle), Math.Sin(fineAngle));
        }

        var owner = new byte[count];
        Array.Fill(owner, Unassigned);
        var frontier = new PriorityQueue<(int Cell, byte Plate), double>();
        for (byte plate = 0; plate < NumberOfPlates; plate++)
            frontier.Enqueue((origins[plate], plate), 0);

        // Each claimed cell is reached from an already claimed cell of its plate:
        // every plate remains one connected hex region (including the world seam).
        while (frontier.TryDequeue(out var step, out double priority))
        {
            if (owner[step.Cell] != Unassigned) continue;
            owner[step.Cell] = step.Plate;
            foreach (int neighbor in topology.Neighbors(step.Cell))
            {
                if (owner[neighbor] != Unassigned) continue;
                var profile = profiles[step.Plate];
                double broad = 1.65 * (
                    profile.BroadX * fields[0][neighbor] +
                    profile.BroadY * fields[1][neighbor]);
                double medium = 1.10 * (
                    profile.MediumX * fields[2][neighbor] +
                    profile.MediumY * fields[3][neighbor]);
                double fine = .44 * (
                    profile.FineX * fields[4][neighbor] +
                    profile.FineY * fields[5][neighbor]);

                // Always positive: Dijkstra's increasing priorities mean
                // no detached islands, even at very sinuous boundaries.
                double resistance = Math.Clamp(
                    1 - BorderIrregularity * (broad + medium + fine),
                    MinimumResistance, MaximumResistance);
                double next = priority + profile.Cost * resistance;
                frontier.Enqueue((neighbor, step.Plate), next);
            }
        }

        if (owner.Any(plate => plate == Unassigned))
            throw new InvalidOperationException("Some hexagons received no plate.");
        return owner;
    }
}
