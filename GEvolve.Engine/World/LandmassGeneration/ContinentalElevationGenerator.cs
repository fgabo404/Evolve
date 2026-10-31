using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Regional tectonic systems over the unchanged approved continental surface.</summary>
public sealed class ContinentalElevationGenerator
{
    public const string Version = "continental-elevation-2";

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
        TectonicReliefSystem[] systems = TectonicReliefSystemAnalyzer.Analyze(topology, motion, contacts);

        var collision = new float[count];
        var collisionPlateau = new float[count];
        var coastalArc = new float[count];
        var islandArc = new float[count];
        var rift = new float[count];
        var riftFlank = new float[count];
        var fault = new float[count];
        var ridge = new float[count];
        var trench = new float[count];
        var seen = new int[count];
        var distances = new ushort[count];
        var queue = new int[count];
        int stamp = 0;

        foreach (var system in systems)
        {
            double strength = Math.Sqrt(Math.Clamp(system.Intensity /
                ContinentalGeologySettings.ReferenceSpeedCmPerYear, 0, 1));
            if (strength <= 0) continue;
            switch (system.Kind)
            {
                case GeologicalContactKind.ContinentalCollision:
                    SpreadSystem(system, false, (id, distance) =>
                    {
                        double fold = Bell(distance, system.WidthHexes * .52);
                        collision[id] = Math.Max(collision[id], (float)(strength * fold));
                        collisionPlateau[id] = Math.Max(collisionPlateau[id],
                            (float)(strength * Bell(distance, system.WidthHexes * 1.3)));
                    });
                    break;
                case GeologicalContactKind.OceanContinentSubduction:
                    SpreadSystem(system, false, (id, distance) =>
                        coastalArc[id] = Math.Max(coastalArc[id],
                            (float)(strength * Bell(distance - 2, system.WidthHexes * .53))));
                    SpreadSystem(system, true, (id, distance) =>
                        trench[id] = Math.Max(trench[id],
                            (float)(strength * Bell(distance, system.WidthHexes * .42))));
                    break;
                case GeologicalContactKind.OceanOceanSubduction:
                    SpreadSystem(system, false, (id, distance) =>
                        islandArc[id] = Math.Max(islandArc[id],
                            (float)(strength * Bell(distance - 2, system.WidthHexes * .48))));
                    SpreadSystem(system, true, (id, distance) =>
                        trench[id] = Math.Max(trench[id],
                            (float)(strength * Bell(distance, system.WidthHexes * .42))));
                    break;
                case GeologicalContactKind.ContinentalRift:
                    SpreadSystem(system, false, (id, distance) =>
                    {
                        rift[id] = Math.Max(rift[id], (float)(strength * Bell(distance,
                            system.WidthHexes * .36)));
                        riftFlank[id] = Math.Max(riftFlank[id], (float)(strength *
                            Bell(distance - system.WidthHexes * .65, system.WidthHexes * .30)));
                    });
                    break;
                case GeologicalContactKind.OceanRidge:
                    SpreadSystem(system, false, (id, distance) =>
                        ridge[id] = Math.Max(ridge[id], (float)(strength *
                            Bell(distance, system.WidthHexes * .55))));
                    break;
                case GeologicalContactKind.Transform:
                    SpreadSystem(system, false, (id, distance) =>
                        fault[id] = Math.Max(fault[id], (float)(strength *
                            Bell(distance, system.WidthHexes * .30))));
                    break;
            }
        }

        byte[] coastDistance = DistanceToCoast(topology, terrain.SurfaceByCellId);
        var height = new byte[count];
        var depth = new byte[count];
        var landform = new byte[count];
        var uplift = new byte[count];
        var subsidence = new byte[count];
        for (int id = 0; id < count; id++)
        {
            int x = id % topology.Columns, y = id / topology.Columns;
            double broad = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 9,
                terrain.Configuration.Seed ^ 0x704A53);
            double fine = PeriodicNoise.Sample(x, y, topology.Columns, topology.Rows, 23,
                terrain.Configuration.Seed ^ 0x32521B);
            // Small along-belt corrugation creates branches and intermontane lows;
            // it cannot produce a range where there is no classified tectonic system.
            double corrugation = Math.Clamp(1 + .18 * broad + .10 * fine, .7, 1.3);
            double regionalUplift = 72 * collision[id] * corrugation +
                16 * collisionPlateau[id] + 7 * collisionPlateau[id] * Math.Max(0, fine) +
                54 * coastalArc[id] * corrugation + 44 * islandArc[id] * corrugation +
                8 * riftFlank[id];
            double regionalDown = 24 * rift[id] + 9 * fault[id] +
                4 * collision[id] * Math.Max(0, -fine);
            uplift[id] = (byte)Math.Clamp((int)Math.Round(regionalUplift), 0, 100);
            subsidence[id] = (byte)Math.Clamp((int)Math.Round(regionalDown), 0, 100);
            if (terrain.SurfaceByCellId[id] == 0)
            {
                depth[id] = (byte)Math.Clamp((int)Math.Round(58 + 3 * broad +
                    25 * trench[id] - 23 * ridge[id]), 1, 100);
                continue;
            }

            double interior = 11 * (1 - Math.Exp(-coastDistance[id] / 7.0));
            double stable = Math.Min(4, Math.Max(0,
                motion.DistanceToBoundaryByCellId[id] - 3) * .12);
            double level = 8 + interior + stable + 3.5 * broad + 1.4 * fine +
                regionalUplift - regionalDown;
            height[id] = (byte)Math.Clamp((int)Math.Round(level), 1, 100);
            landform[id] = (byte)(height[id] >= 75 && collision[id] >= .45
                ? ContinentalReliefKind.HighRange :
                height[id] >= 44 && (collision[id] >= .20 || coastalArc[id] >= .24 ||
                    islandArc[id] >= .24) ? ContinentalReliefKind.Mountain :
                rift[id] >= .25 ? ContinentalReliefKind.Rift :
                fault[id] >= .30 ? ContinentalReliefKind.Fault :
                height[id] >= 30 || collisionPlateau[id] >= .30 ? ContinentalReliefKind.Plateau :
                height[id] >= 18 ? ContinentalReliefKind.Hills :
                coastDistance[id] <= 3 ? ContinentalReliefKind.CoastalPlain :
                ContinentalReliefKind.InlandPlain);
        }
        return terrain with
        {
            ElevationByCellId = height,
            ContinentalElevation = new ContinentalElevationState(Version, contacts, landform, depth)
            {
                Systems = systems, UpliftByCellId = uplift, SubsidenceByCellId = subsidence
            }
        };

        void SpreadSystem(TectonicReliefSystem system, bool trenchSide, Action<int, double> apply)
        {
            ++stamp;
            int head = 0, tail = 0;
            foreach (int index in system.InteractionIndices)
            {
                var edge = motion.Interactions[index];
                int first = edge.FirstCellId, second = edge.SecondCellId;
                if (system.Kind is GeologicalContactKind.OceanContinentSubduction or
                    GeologicalContactKind.OceanOceanSubduction)
                {
                    int upper = edge.FirstPlateId == system.OverridingPlateId ? first : second;
                    Start(trenchSide ? (upper == first ? second : first) : upper);
                }
                else { Start(first); Start(second); }
            }
            int limit = (int)Math.Ceiling(system.WidthHexes * 1.7);
            while (head < tail)
            {
                int id = queue[head++], distance = distances[id];
                apply(id, distance);
                if (distance >= limit) continue;
                foreach (int next in topology.Neighbors(id))
                    if (seen[next] != stamp && Eligible(next))
                    {
                        seen[next] = stamp;
                        distances[next] = (ushort)(distance + 1);
                        queue[tail++] = next;
                    }
            }
            void Start(int id)
            {
                if (seen[id] == stamp || !Eligible(id)) return;
                seen[id] = stamp;
                distances[id] = 0;
                queue[tail++] = id;
            }
            bool Eligible(int id) => system.Kind switch
            {
                GeologicalContactKind.ContinentalCollision or GeologicalContactKind.ContinentalRift =>
                    terrain.SurfaceByCellId[id] == 1 &&
                    surface.CrustByCellId[id] == (byte)CrustKind.Continental,
                GeologicalContactKind.OceanContinentSubduction => trenchSide
                    ? terrain.SurfaceByCellId[id] == 0 &&
                        plates[id] == (system.FirstPlateId == system.OverridingPlateId
                            ? system.SecondPlateId : system.FirstPlateId)
                    : terrain.SurfaceByCellId[id] == 1 && plates[id] == system.OverridingPlateId,
                GeologicalContactKind.OceanOceanSubduction => trenchSide
                    ? terrain.SurfaceByCellId[id] == 0 &&
                        plates[id] == (system.FirstPlateId == system.OverridingPlateId
                            ? system.SecondPlateId : system.FirstPlateId)
                    : plates[id] == system.OverridingPlateId,
                GeologicalContactKind.OceanRidge => terrain.SurfaceByCellId[id] == 0,
                _ => true
            };
        }
    }

    private static double Bell(double distance, double width)
        => Math.Exp(-.5 * distance * distance / (width * width));

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
}
