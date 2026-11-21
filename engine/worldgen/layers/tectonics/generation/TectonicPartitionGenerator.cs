// Builds fourteen connected plate regions using real Grid neighbors and isolated deterministic streams.
// Keeps seeds, noise, competitive growth and calibration metrics private until exhaustive candidate validation.
using System.Diagnostics;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal static class TectonicPartitionGenerator
{
    internal static TectonicDraft Generate(HexGridData grid, DeterministicStageRandom partitionRandom,
        DeterministicStageRandom noiseRandom, TectonicGenerationProfile profile)
    {
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        int[] origins = SelectSeeds(grid, partitionRandom, profile.CandidateSamples);
        double seedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        float[] costs = Enumerable.Range(0, TectonicGenerationProfile.PlateCount).Select(profile.GetCost).ToArray();
        for (int index = costs.Length - 1; index > 0; index--)
        {
            int other = NextInt(partitionRandom, index + 1);
            (costs[index], costs[other]) = (costs[other], costs[index]);
        }
        double[] rotations = new double[costs.Length * 6];
        for (int plate = 0; plate < costs.Length; plate++)
        for (int pair = 0; pair < 3; pair++)
        {
            double angle = NextUnit(partitionRandom) * (2 * Math.PI);
            rotations[plate * 6 + pair * 2] = Math.Cos(angle);
            rotations[plate * 6 + pair * 2 + 1] = Math.Sin(angle);
        }
        start = Stopwatch.GetTimestamp();
        float[][] fields = BuildFields(grid, noiseRandom, profile);
        double noiseMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        start = Stopwatch.GetTimestamp();

        byte[] owners = new byte[grid.CellCount];
        Array.Fill(owners, byte.MaxValue);
        int[] counts = new int[costs.Length];
        var frontier = new PriorityQueue<(int Cell, byte Plate), (double Cost, byte Plate, int Cell, long Sequence)>();
        long sequence = 0;
        int peak = 0;
        // Reserve every origin before competition so no fast plate can erase a slow plate's seed.
        for (byte plate = 0; plate < origins.Length; plate++) { owners[origins[plate]] = plate; counts[plate] = 1; }
        for (byte plate = 0; plate < origins.Length; plate++) Expand(origins[plate], plate, 0);
        while (frontier.TryDequeue(out var entry, out var priority))
        {
            if (owners[entry.Cell] != byte.MaxValue) continue;
            owners[entry.Cell] = entry.Plate;
            counts[entry.Plate]++;
            Expand(entry.Cell, entry.Plate, priority.Cost);
        }
        var plates = new TectonicPlate[origins.Length];
        for (byte plate = 0; plate < plates.Length; plate++)
            plates[plate] = new(plate, origins[plate], counts[plate], costs[plate]);
        double growthMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return new TectonicDraft(owners, plates, new(seedMs, noiseMs, growthMs, peak, allocated));

        // Candidates originate only at owned cells; accepting any candidate preserves same-plate connectivity.
        void Expand(int cell, byte plate, double cost)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                if (!grid.TryGetNeighbor(cell, (HexDirection)direction, out int next) || owners[next] != byte.MaxValue) continue;
                int rotation = plate * 6;
                double broad = profile.BroadWeight * (rotations[rotation] * fields[0][next] + rotations[rotation + 1] * fields[1][next]);
                double medium = profile.MediumWeight * (rotations[rotation + 2] * fields[2][next] + rotations[rotation + 3] * fields[3][next]);
                double fine = profile.FineWeight * (rotations[rotation + 4] * fields[4][next] + rotations[rotation + 5] * fields[5][next]);
                double resistance = Math.Clamp(1 - profile.Irregularity * (broad + medium + fine),
                    profile.MinimumResistance, profile.MaximumResistance);
                double arrival = cost + costs[plate] * resistance;
                if (!double.IsFinite(arrival) || arrival <= cost) throw new InvalidOperationException("Invalid growth priority.");
                frontier.Enqueue((next, plate), (arrival, plate, next, sequence));
                sequence = checked(sequence + 1);
                peak = Math.Max(peak, frontier.Count);
            }
        }
    }

    internal static int[] SelectSeeds(HexGridData grid, DeterministicStageRandom random, int samples)
    {
        if (samples < 0) throw new ArgumentOutOfRangeException(nameof(samples));
        int[] origins = new int[TectonicGenerationProfile.PlateCount];
        origins[0] = NextInt(random, grid.CellCount);
        for (int plate = 1; plate < origins.Length; plate++)
        {
            int best = -1, separation = -1;
            for (int attempt = 0; attempt < samples; attempt++) Consider(NextInt(random, grid.CellCount));
            // Even an all-duplicate sample cannot produce duplicate seed identities.
            if (best < 0) for (int cell = 0; cell < grid.CellCount; cell++) Consider(cell);
            if (best < 0) throw new InvalidOperationException("Not enough distinct Grid cells.");
            origins[plate] = best;
            void Consider(int candidate)
            {
                int distance = int.MaxValue;
                for (int previous = 0; previous < plate; previous++)
                {
                    if (origins[previous] == candidate) return;
                    distance = Math.Min(distance, grid.GetDistance(candidate, origins[previous]));
                }
                if (distance > separation || (distance == separation && candidate < best))
                { best = candidate; separation = distance; }
            }
        }
        return origins;
    }

    private static float[][] BuildFields(HexGridData grid, DeterministicStageRandom random, TectonicGenerationProfile profile)
    {
        ulong warpXSeed = random.NextUInt64(), warpYSeed = random.NextUInt64();
        ulong[] seeds = new ulong[6];
        float[][] fields = new float[6][];
        for (int band = 0; band < fields.Length; band++) { seeds[band] = random.NextUInt64(); fields[band] = new float[grid.CellCount]; }
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var offset = grid.GetOffset(cell);
            var warped = Warp(offset.Column, offset.Row, grid.Columns, grid.Rows, warpXSeed, warpYSeed, profile);
            for (int band = 0; band < fields.Length; band++)
                fields[band][cell] = (float)PeriodicTectonicNoise.Sample(warped.X, warped.Y, grid.Columns, grid.Rows,
                    profile.Frequencies[band], seeds[band]);
        }
        return fields;
    }

    internal static (double X, double Y) Warp(double x, double y, int columns, int rows, ulong xSeed, ulong ySeed,
        TectonicGenerationProfile profile) =>
        (x + PeriodicTectonicNoise.Sample(x, y, columns, rows, 3, xSeed) * columns * profile.WarpFraction,
         y + PeriodicTectonicNoise.Sample(x, y, columns, rows, 3, ySeed) * rows * profile.WarpFraction);

    // Rejection sampling avoids modulo bias, including for arbitrary supported Grid cell counts.
    internal static int NextInt(DeterministicStageRandom random, int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        ulong bound = (ulong)count, threshold = unchecked(0UL - bound) % bound;
        ulong draw;
        do { draw = random.NextUInt64(); } while (draw < threshold);
        return (int)(draw % bound);
    }

    internal static double NextUnit(DeterministicStageRandom random) => (random.NextUInt64() >> 11) * (1d / 9007199254740992d);
}
