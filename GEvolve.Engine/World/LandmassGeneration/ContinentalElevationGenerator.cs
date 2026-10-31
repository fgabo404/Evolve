using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Static tectonic relief over the already approved continental surface.</summary>
public sealed class ContinentalElevationGenerator
{
    public const string Version = "continental-elevation-1";

    public LandmassState Generate(HexTopology topology, LandmassState terrain)
    {
        if (terrain.Configuration.Type != LandmassType.Continents || !terrain.HasSurface ||
            terrain.ContinentalSurface is not { } surface || terrain.PlateByCellId is not { } plates ||
            terrain.TectonicMotion is not { } motion || terrain.SurfaceByCellId.Length != topology.Count)
            throw new ArgumentException("An approved continental surface and original tectonic motion are required.");

        int count = topology.Count;
        var ageRandom = new DeterministicRandom(terrain.Configuration.Seed, 0x41474531);
        var ages = Enumerable.Range(0, 16).Select(_ => (ushort)(20 + ageRandom.NextInt(161))).ToArray();
        GeologicalContact[] contacts = ContinentalReliefGenerator.ClassifyContacts(
            motion, surface.CrustByCellId, ages);

        var collision = new float[count];
        var coastalSubduction = new float[count];
        var islandArc = new float[count];
        var rift = new float[count];
        var fault = new float[count];
        var ridge = new float[count];
        var trench = new float[count];
        foreach (var contact in contacts)
        {
            var edge = motion.Interactions[contact.InteractionIndex];
            int a = edge.FirstCellId, b = edge.SecondCellId;
            switch (contact.Kind)
            {
                case GeologicalContactKind.ContinentalCollision:
                    Seed(collision, a, edge.Convergence);
                    Seed(collision, b, edge.Convergence);
                    break;
                case GeologicalContactKind.OceanContinentSubduction:
                case GeologicalContactKind.OceanOceanSubduction:
                    int upper = edge.FirstPlateId == contact.OverridingPlateId ? a : b;
                    int lower = upper == a ? b : a;
                    Seed(trench, lower, edge.Convergence);
                    // Elevation belongs to the overriding plate; oceanic arcs only
                    // affect a cell if that plate already has land from the surface stage.
                    if (surface.CrustByCellId[upper] == (byte)CrustKind.Continental ||
                        terrain.SurfaceByCellId[upper] == 1)
                        Seed(contact.Kind == GeologicalContactKind.OceanContinentSubduction
                            ? coastalSubduction : islandArc, upper, edge.Convergence);
                    break;
                case GeologicalContactKind.ContinentalRift:
                    if (surface.CrustByCellId[a] == (byte)CrustKind.Continental)
                        Seed(rift, a, edge.Divergence);
                    if (surface.CrustByCellId[b] == (byte)CrustKind.Continental)
                        Seed(rift, b, edge.Divergence);
                    break;
                case GeologicalContactKind.OceanRidge:
                    Seed(ridge, a, edge.Divergence);
                    Seed(ridge, b, edge.Divergence);
                    break;
                case GeologicalContactKind.Transform:
                    Seed(fault, a, edge.Shear);
                    Seed(fault, b, edge.Shear);
                    break;
            }
        }

        double radius = motion.InfluenceRadiusHexes;
        var uplift = TectonicMotionGenerator.Spread(topology, collision, coastalSubduction,
            islandArc, radius * 1.35);
        var deformation = TectonicMotionGenerator.Spread(topology, rift, fault, ridge,
            Math.Max(1, radius * .65));
        var oceanTrench = TectonicMotionGenerator.Spread(topology, trench,
            new float[count], new float[count], Math.Max(1, radius * .7));
        byte[] coastDistance = DistanceToCoast(topology, terrain.SurfaceByCellId);
        var height = new byte[count];
        var depth = new byte[count];
        var landform = new byte[count];
        for (int id = 0; id < count; id++)
        {
            int x = id % topology.Columns, y = id / topology.Columns;
            double broad = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 9,
                terrain.Configuration.Seed ^ 0x704A53);
            double fine = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 23,
                terrain.Configuration.Seed ^ 0x32521B);
            if (terrain.SurfaceByCellId[id] == 0)
            {
                depth[id] = (byte)Math.Clamp((int)Math.Round(58 + 3 * broad +
                    24 * Response(oceanTrench.Compression[id]) -
                    21 * Response(deformation.Shear[id])), 1, 100);
                continue;
            }

            double continentInterior = 11 * (1 - Math.Exp(-coastDistance[id] / 7.0));
            double stablePlateau = Math.Min(4,
                Math.Max(0, motion.DistanceToBoundaryByCellId[id] - 3) * .12);
            double fold = Response(uplift.Compression[id]);
            double coastalArc = Response(uplift.Extension[id]);
            double island = Response(uplift.Shear[id]);
            double extension = Response(deformation.Compression[id]);
            double shear = Response(deformation.Extension[id]);
            double level = 8 + continentInterior + stablePlateau + 3.5 * broad + 1.4 * fine +
                72 * fold + 49 * coastalArc + 43 * island - 22 * extension - 10 * shear;
            height[id] = (byte)Math.Clamp((int)Math.Round(level), 1, 100);
            landform[id] = (byte)(height[id] >= 75 && fold >= .45 ? ContinentalReliefKind.HighRange :
                height[id] >= 44 && (fold >= .22 || coastalArc >= .25 || island >= .25)
                    ? ContinentalReliefKind.Mountain :
                extension >= .25 ? ContinentalReliefKind.Rift :
                shear >= .30 ? ContinentalReliefKind.Fault :
                height[id] >= 30 ? ContinentalReliefKind.Plateau :
                height[id] >= 18 ? ContinentalReliefKind.Hills :
                coastDistance[id] <= 3 ? ContinentalReliefKind.CoastalPlain :
                ContinentalReliefKind.InlandPlain);
        }
        return terrain with { ElevationByCellId = height,
            ContinentalElevation = new ContinentalElevationState(Version, contacts, landform, depth) };
    }

    private static byte[] DistanceToCoast(HexTopology topology, byte[] surface)
    {
        var distance = new byte[topology.Count];
        Array.Fill(distance, byte.MaxValue);
        var queue = new int[topology.Count];
        int head = 0, tail = 0;
        for (int id = 0; id < topology.Count; id++)
            if (surface[id] == 1)
                foreach (int neighbor in topology.Neighbors(id))
                    if (surface[neighbor] == 0)
                    {
                        distance[id] = 0;
                        queue[tail++] = id;
                        break;
                    }
        while (head < tail)
        {
            int id = queue[head++];
            foreach (int neighbor in topology.Neighbors(id))
                if (surface[neighbor] == 1 && distance[neighbor] == byte.MaxValue)
                {
                    distance[neighbor] = (byte)Math.Min(254, distance[id] + 1);
                    queue[tail++] = neighbor;
                }
        }
        return distance;
    }

    private static double Response(float value) => Math.Sqrt(Math.Clamp(
        value / ContinentalGeologySettings.ReferenceSpeedCmPerYear, 0, 1));
    private static void Seed(float[] field, int id, double value)
        => field[id] = Math.Max(field[id], (float)value);
}
