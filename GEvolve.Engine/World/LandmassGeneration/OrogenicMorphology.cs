using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Landward expression of an existing convergent margin, in real hex adjacency.</summary>
internal sealed class OrogenicMorphology
{
    private readonly HexTopology topology;
    private readonly LandmassState terrain;
    private readonly int[] distance, origin, ridgeDistance, queue, shore, plateShore;
    private readonly double[] broad, fine;
    private readonly double scale;

    public OrogenicMorphology(HexTopology topology, LandmassState terrain)
    {
        this.topology = topology;
        this.terrain = terrain;
        scale = ContinentalOrogenySettings.ResolutionScale(topology.Rows);
        distance = new int[topology.Count]; origin = new int[topology.Count];
        ridgeDistance = new int[topology.Count]; queue = new int[topology.Count];
        shore = ShoreDistance(false); plateShore = ShoreDistance(true);
        broad = new double[topology.Count]; fine = new double[topology.Count];
        for (int id = 0; id < topology.Count; id++)
        {
            broad[id] = PeriodicNoise.Sample(id % topology.Columns, id / topology.Columns,
                topology.Columns, topology.Rows, 7, terrain.Configuration.Seed ^ 0x74315A);
            fine[id] = PeriodicNoise.Sample(id % topology.Columns, id / topology.Columns,
                topology.Columns, topology.Rows, 27, terrain.Configuration.Seed ^ 0x135279);
        }
    }

    public (int[] Crest, int[] Sources, int[] Flanks, double Width) Apply(TectonicReliefSystem system,
        Action<int, double, double> apply)
    {
        bool collision = system.Kind == GeologicalContactKind.ContinentalCollision;
        double width = Math.Clamp(system.WidthHexes, 3 * scale, 9 * scale);
        int reach = (int)Math.Ceiling(width * 2.8);
        Array.Fill(distance, -1);
        Array.Fill(ridgeDistance, -1);
        int head = 0, tail = 0;
        foreach (int id in system.AxisCellIds)
            if (Eligible(id) && distance[id] < 0)
            {
                distance[id] = 0; origin[id] = id; queue[tail++] = id;
            }
        while (head < tail)
        {
            int id = queue[head++];
            if (distance[id] >= reach) continue;
            foreach (int next in topology.Neighbors(id))
                if (distance[next] < 0 && Eligible(next))
                {
                    distance[next] = distance[id] + 1;
                    origin[next] = origin[id]; queue[tail++] = next;
                }
        }

        // A source can transmit deformation through submerged continental crust, but only
        // within its finite regional reach. This never projects across an oceanic basin.
        var crest = new HashSet<int>();
        int[] landDistance = collision ? shore : plateShore;
        for (int i = 0; i < tail; i++)
        {
            int id = queue[i], source = origin[id];
            if (terrain.SurfaceByCellId[id] == 0 || landDistance[source] > width * 1.5) continue;
            double offset = landDistance[source] + (collision ? 0 : 2 * scale);
            // Bend the contour by less than a hex at the base resolution; location is
            // controlled by the tectonic distance field, never by independent noise.
            offset += .65 * scale * broad[id];
            if (Math.Abs(distance[id] - offset) <= .8) crest.Add(id);
        }
        // Close only a single land hex between contour neighbours in the same deformation
        // domain. Disconnected land and different tectonic processes remain separate.
        var bridges = new List<int>();
        foreach (int id in crest.Order())
            foreach (int next in topology.Neighbors(id))
                if (!crest.Contains(next) && terrain.SurfaceByCellId[next] == 1 &&
                    distance[next] >= 0 && Eligible(next))
                {
                    int[] adjacent = topology.Neighbors(next).ToArray().Where(crest.Contains).ToArray();
                    if (adjacent.Length < 2) continue;
                    var connected = new HashSet<int> { adjacent[0] };
                    for (int pass = 0; pass < adjacent.Length; pass++)
                        foreach (int across in adjacent)
                            if (topology.Neighbors(across).ToArray().Any(connected.Contains)) connected.Add(across);
                    // Two already connected neighbours describe the side of a crest, not a
                    // missing link. Filling those cells would dilate every ridge by a hex.
                    if (connected.Count < adjacent.Length) bridges.Add(next);
                }
        crest.UnionWith(bridges);
        head = 0; tail = 0;
        foreach (int id in crest.Order())
        {
            ridgeDistance[id] = 0; queue[tail++] = id;
        }
        var flanks = new List<int>();
        int flankLimit = (int)Math.Ceiling(width * (collision ? 2.0 : 1.6));
        var sourceConvergence = new Dictionary<int, (double Sum, int Count)>();
        foreach (int index in system.InteractionIndices)
        {
            var edge = terrain.TectonicMotion!.Interactions[index];
            Accumulate(edge.FirstCellId, edge.Convergence);
            Accumulate(edge.SecondCellId, edge.Convergence);
        }
        double strength = Strength(system.Intensity);
        while (head < tail)
        {
            int id = queue[head++], d = ridgeDistance[id];
            double localWidth = width * (1 + .16 * broad[id]);
            double main = Bell(d, localWidth * .20);
            double secondary = Bell(d - localWidth * (.72 + .12 * broad[id]), localWidth * .14) *
                (.75 + .25 * broad[id]);
            double foothill = Bell(d, localWidth * .95);
            double axialVariation = Math.Clamp(1 + .20 * broad[id] + .12 * fine[id], .72, 1.28);
            double localStrength = sourceConvergence.TryGetValue(origin[id], out var force)
                ? .75 * strength + .25 * Strength(force.Sum / force.Count) : strength;
            double signal = localStrength * axialVariation *
                (.85 * main + .40 * secondary + .13 * foothill);
            double plateau = collision ? localStrength * Bell(d, localWidth * 1.35) : 0;
            apply(id, signal, plateau);
            if (d >= width * 1.25) flanks.Add(id);
            if (d >= flankLimit) continue;
            foreach (int next in topology.Neighbors(id))
                if (ridgeDistance[next] < 0 && distance[next] >= 0 &&
                    terrain.SurfaceByCellId[next] == 1 && Eligible(next))
                {
                    ridgeDistance[next] = d + 1; queue[tail++] = next;
                }
        }
        int[] crestCells = crest.Order().ToArray();
        return (crestCells, crestCells.Select(id => origin[id]).ToArray(), flanks.ToArray(), width);

        void Accumulate(int id, double force)
        {
            var previous = sourceConvergence.GetValueOrDefault(id);
            sourceConvergence[id] = (previous.Sum + force, previous.Count + 1);
        }

        bool Eligible(int id) => terrain.ContinentalSurface!.CrustByCellId[id] ==
            (byte)CrustKind.Continental && (collision
                ? system.ParticipatingPlateIds.Contains(terrain.PlateByCellId![id])
                : terrain.PlateByCellId![id] == system.OverridingPlateId);
    }

    private int[] ShoreDistance(bool samePlate)
    {
        var result = new int[topology.Count];
        Array.Fill(result, int.MaxValue);
        int head = 0, tail = 0;
        for (int id = 0; id < topology.Count; id++)
            if (terrain.SurfaceByCellId[id] == 1)
            { result[id] = 0; queue[tail++] = id; }
        while (head < tail)
        {
            int id = queue[head++];
            foreach (int next in topology.Neighbors(id))
                if (result[next] == int.MaxValue && terrain.ContinentalSurface!.CrustByCellId[next] ==
                    (byte)CrustKind.Continental && (!samePlate ||
                        terrain.PlateByCellId![next] == terrain.PlateByCellId[id]))
                { result[next] = result[id] + 1; queue[tail++] = next; }
        }
        return result;
    }

    private static double Bell(double d, double width) => Math.Exp(-.5 * d * d / (width * width));
    private static double Strength(double force) => Math.Sqrt(Math.Clamp(force /
        ContinentalGeologySettings.ReferenceSpeedCmPerYear, 0, 1));
}
