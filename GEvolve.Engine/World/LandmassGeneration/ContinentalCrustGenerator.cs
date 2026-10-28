using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Synthetic geological assembly: a continental craton and several attached
/// terranes per mainland, inherited plate boundaries, accretion and rift
/// signals. Continental crust may be submerged and may cross plate boundaries.
/// This is a tectonically guided reconstruction, not a time simulation.
/// </summary>
public sealed record ContinentalCrustMap(
    byte[] CrustByCellId,
    float[] EmergenceAffinityByCellId,
    int[] MainContinentCoreCells,
    byte[] MainContinentCorePlates,
    byte[] ProvinceByCellId,
    float[] CollisionByCellId,
    float[] RiftByCellId);

public sealed class ContinentalCrustGenerator
{
    public const string Version = "continental-assembly-1";
    private const int PlateCount = TectonicPlateGenerator.NumberOfPlates;

    private readonly record struct Fragment(
        int Owner, double X, double Y, double Cos, double Sin,
        double MajorRadius, double MinorRadius, double Strength);

    public ContinentalCrustMap Generate(
        HexTopology topology, ReadOnlySpan<byte> plateIds, int continentCount, int seed)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (continentCount is < 2 or > 6)
            throw new ArgumentOutOfRangeException(nameof(continentCount));
        int count = topology.Count, width = topology.Columns, height = topology.Rows;
        if (plateIds.Length != count)
            throw new ArgumentException("Stage 01 plate IDs must match the grid.", nameof(plateIds));
        var plates = plateIds.ToArray();
        int[] platePopulation = new int[PlateCount];
        var plateBorders = new List<int>[PlateCount];
        for (int p = 0; p < PlateCount; p++) plateBorders[p] = [];
        var distanceFromBorder = new int[count];
        Array.Fill(distanceFromBorder, -1);
        var borderQueue = new Queue<int>();

        for (int cell = 0; cell < count; cell++)
        {
            int plate = plates[cell];
            if (plate >= PlateCount)
                throw new ArgumentException("Unknown tectonic plate.", nameof(plateIds));
            platePopulation[plate]++;
            foreach (int other in topology.Neighbors(cell))
                if (plates[other] != plate)
                {
                    if (distanceFromBorder[cell] < 0)
                    {
                        distanceFromBorder[cell] = 0;
                        borderQueue.Enqueue(cell);
                        plateBorders[plate].Add(cell);
                    }
                    break;
                }
        }
        if (platePopulation.Any(n => n == 0))
            throw new ArgumentException("All sixteen plates must be present.", nameof(plateIds));

        while (borderQueue.TryDequeue(out int cell))
            foreach (int next in topology.Neighbors(cell))
                if (plates[next] == plates[cell] && distanceFromBorder[next] < 0)
                {
                    distanceFromBorder[next] = distanceFromBorder[cell] + 1;
                    borderQueue.Enqueue(next);
                }

        // The ancient cratons begin in deep interiors of ACTUAL plates.
        var plateCores = new int[PlateCount];
        var coreDepth = new int[PlateCount];
        Array.Fill(coreDepth, -1);
        for (int cell = 0; cell < count; cell++)
        {
            int p = plates[cell], row = cell / width;
            int depth = distanceFromBorder[cell] - (row < height / 12 ||
                row > height * 11 / 12 ? 4 : 0);
            if (depth <= coreDepth[p]) continue;
            coreDepth[p] = depth;
            plateCores[p] = cell;
        }

        var rng = new DeterministicRandom(seed, 0x43524154);
        var hubs = new int[continentCount];
        var hubPlates = new byte[continentCount];
        int first = 0;
        double ticket = rng.NextDouble() * platePopulation.Sum(n => Math.Sqrt(n));
        for (int p = 0; p < PlateCount; p++)
        {
            ticket -= Math.Sqrt(platePopulation[p]);
            if (ticket <= 0) { first = p; break; }
        }
        hubs[0] = plateCores[first];
        hubPlates[0] = (byte)first;

        // Farthest tectonic interiors become distinct continental ancestors.
        for (int owner = 1; owner < continentCount; owner++)
        {
            int bestPlate = -1;
            double bestScore = double.NegativeInfinity;
            for (int p = 0; p < PlateCount; p++)
            {
                if (hubPlates.AsSpan(0, owner).Contains((byte)p)) continue;
                int separation = int.MaxValue;
                for (int prior = 0; prior < owner; prior++)
                    separation = Math.Min(separation,
                        topology.Distance(plateCores[p], hubs[prior]));
                double score = separation + .16 * coreDepth[p] +
                    .7 * Math.Sqrt(platePopulation[p]);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlate = p;
                }
            }
            if (bestPlate < 0) throw new InvalidOperationException("No continental root plate.");
            hubs[owner] = plateCores[bestPlate];
            hubPlates[owner] = (byte)bestPlate;
        }

        // Seeded pseudo-velocities let inherited plate contacts act as
        // convergent sutures or divergent rifts. They are NOT actual motions.
        var vx = new double[PlateCount];
        var vy = new double[PlateCount];
        var motions = new DeterministicRandom(seed, 0x4D4F544E);
        for (int p = 0; p < PlateCount; p++)
        {
            double angle = motions.NextDouble() * Math.PI * 2;
            vx[p] = Math.Cos(angle);
            vy[p] = Math.Sin(angle);
        }
        var collisions = new float[count];
        var rifts = new float[count];
        for (int cell = 0; cell < count; cell++)
        {
            int p = plates[cell], x = cell % width, y = cell / width;
            foreach (int next in topology.Neighbors(cell))
            {
                int q = plates[next];
                if (p == q) continue;
                double dx = next % width - x;
                dx -= width * Math.Round(dx / width);
                double dy = next / width - y;
                double magnitude = Math.Sqrt(dx * dx + dy * dy);
                if (magnitude == 0) continue;
                // Positive relative approach is convergent; negative splits.
                double approach = ((vx[p] - vx[q]) * dx +
                    (vy[p] - vy[q]) * dy) / magnitude;
                if (approach > 0)
                    collisions[cell] = Math.Max(collisions[cell],
                        (float)Math.Min(1, approach / 2));
                else
                    rifts[cell] = Math.Max(rifts[cell],
                        (float)Math.Min(1, -approach / 2));
            }
        }

        // Diffusion gives each event a finite geological zone, not a 1-hex
        // artifact exactly on the plate border.
        for (int pass = 0; pass < 6; pass++)
        {
            var nextCollision = new float[count];
            var nextRift = new float[count];
            for (int cell = 0; cell < count; cell++)
            {
                double c = collisions[cell] * 3.3;
                double r = rifts[cell] * 3.3;
                double weight = 3.3;
                foreach (int nb in topology.Neighbors(cell))
                {
                    c += collisions[nb]; r += rifts[nb]; weight++;
                }
                nextCollision[cell] = (float)(c / weight);
                nextRift[cell] = (float)(r / weight);
            }
            collisions = nextCollision;
            rifts = nextRift;
        }

        // Geological fragments are anisotropic, attached and curved; the shape
        // is NOT a distance field around a single circular continent seed.
        var fragments = new List<Fragment>(continentCount * 23);
        double averageRadius = Math.Sqrt(count * .35 /
            (continentCount * Math.PI));
        for (int owner = 0; owner < continentCount; owner++)
        {
            int hub = hubs[owner], rootPlate = hubPlates[owner];
            double rootX = hub % width, rootY = hub / width;
            double scale = .85 + .29 * rng.NextDouble();
            double radius = averageRadius * scale;
            double rootAngle = rng.NextDouble() * Math.PI * 2;

            Add(owner, rootX, rootY, rootAngle,
                radius * .90, radius * .52, 1.85);

            // Craton margins acquire terranes along different plate-facing
            // directions. Some arcs continue onto adjacent plates, like the
            // composite margins of real continents.
            int arms = 4 + rng.NextInt(3);
            for (int arm = 0; arm < arms; arm++)
            {
                double heading = rootAngle + 2 * Math.PI * arm / arms +
                    (rng.NextDouble() - .5) * .8;
                double endpointX = rootX + Math.Cos(heading) * radius;
                double endpointY = rootY + Math.Sin(heading) * radius;
                double nearest = double.PositiveInfinity;

                // Favor existing contact zones with neighboring plates. Only
                // the real stage-01 plate geometry supplies these contacts.
                foreach (int cell in plateBorders[rootPlate])
                {
                    double bx = cell % width - rootX;
                    bx -= width * Math.Round(bx / width);
                    double by = cell / width - rootY;
                    double headingError = Math.Abs(Math.Atan2(
                        Math.Sin(Math.Atan2(by, bx) - heading),
                        Math.Cos(Math.Atan2(by, bx) - heading)));
                    double cost = headingError * radius * .55 +
                        .22 * Math.Abs(Math.Sqrt(bx * bx + by * by) - radius);
                    if (cost >= nearest) continue;
                    nearest = cost;
                    endpointX = rootX + bx;
                    endpointY = rootY + by;
                }

                double extension = .78 + .62 * rng.NextDouble();
                double dx = endpointX - rootX;
                double dy = endpointY - rootY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < 1) { dx = Math.Cos(heading); dy = Math.Sin(heading); dist = 1; }
                dx /= dist; dy /= dist;
                double bend = (rng.NextDouble() - .5) * radius * .7;
                // Three consecutive attached terranes make a bent geological
                // backbone, rather than free-floating noise circles.
                for (int step = 1; step <= 3; step++)
                {
                    double t = step / 3.0;
                    double length = radius * extension * t;
                    double cx = rootX + dx * length +
                        (-dy) * bend * t * t;
                    double cy = rootY + dy * length +
                        dx * bend * t * t;
                    double tangent = Math.Atan2(dy, dx) + bend / (2 * radius) * t;
                    double major = radius * (.46 + .12 * rng.NextDouble());
                    double minor = radius * (.22 + .12 * rng.NextDouble());
                    Add(owner, cx, cy, tangent, major, minor,
                        1.47 - .10 * step);
                }
            }
        }

        // Evaluate the tectonic assembly from attached cratons and terranes.
        // Max of fragment profiles preserves the inherited lobes and peninsulas.
        var affinity = new float[count];
        var province = new byte[count];
        for (int cell = 0; cell < count; cell++)
        {
            int x = cell % width, y = cell / width;
            double warpX = PeriodicNoise.Sample(x, y, width, height, 3,
                seed ^ 0x2A6137) * width * .026;
            double warpY = PeriodicNoise.Sample(x, y, width, height, 3,
                seed ^ 0x491B23) * height * .026;
            double wx = x + warpX, wy = y + warpY;
            double coast = .20 * PeriodicNoise.Sample(wx, wy,
                width, height, 7, seed ^ 0x5985A1) +
                .08 * PeriodicNoise.Sample(wx, wy,
                    width, height, 17, seed ^ 0x1AF28C);
            double best = double.NegativeInfinity;
            int bestOwner = 0;
            foreach (var fragment in fragments)
            {
                double dx = x - fragment.X;
                dx -= width * Math.Round(dx / width);
                double dy = y - fragment.Y;
                double longitudinal = dx * fragment.Cos + dy * fragment.Sin;
                double transverse = -dx * fragment.Sin + dy * fragment.Cos;
                double ellipsoid = Math.Sqrt(
                    Math.Pow(longitudinal / fragment.MajorRadius, 2) +
                    Math.Pow(transverse / fragment.MinorRadius, 2));
                double candidate = fragment.Strength - 1.23 * ellipsoid;
                if (candidate <= best) continue;
                best = candidate;
                bestOwner = fragment.Owner;
            }
            // Collision uplifts ancient contacts; rifting lowers sea floors.
            affinity[cell] = (float)(best + coast +
                .16 * collisions[cell] - .30 * rifts[cell]);
            province[cell] = (byte)bestOwner;
        }

        // Crust is broader than dry land: its outer shelves stay underwater.
        // The global threshold regulates crustal abundance, NOT a land quota
        // for any continent. The next layer separately chooses sea level.
        var sorted = affinity.ToArray();
        Array.Sort(sorted);
        float crustDatum = sorted[(int)(.54 * (count - 1))];
        var crust = new byte[count];
        for (int cell = 0; cell < count; cell++)
            crust[cell] = affinity[cell] >= crustDatum ? (byte)1 : (byte)0;
        for (int owner = 0; owner < continentCount; owner++)
        {
            crust[hubs[owner]] = 1;
            province[hubs[owner]] = (byte)owner;
            affinity[hubs[owner]] = Math.Max(affinity[hubs[owner]], 1.8f);
        }

        return new ContinentalCrustMap(crust, affinity, hubs,
            hubPlates, province, collisions, rifts);

        void Add(int owner, double x, double y, double angle,
            double major, double minor, double strength)
        {
            fragments.Add(new Fragment(owner, x, y,
                Math.Cos(angle), Math.Sin(angle),
                Math.Max(2, major), Math.Max(2, minor), strength));
        }
    }
}
