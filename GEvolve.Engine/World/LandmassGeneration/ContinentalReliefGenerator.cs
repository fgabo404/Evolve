using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

public sealed class ContinentalReliefGenerator
{
    public LandmassState Generate(HexTopology topology, LandmassState terrain, ContinentalCrustMap crust, float seaLevel)
    {
        var motion = terrain.TectonicMotion ?? throw new ArgumentException("Official plate motion required.");
        var plates = terrain.PlateByCellId!;
        int count = topology.Count;
        var ageRandom = new DeterministicRandom(terrain.Configuration.Seed, 0x41474531);
        // Idealized inherited oceanic lithosphere ages, fixed per plate, not random per edge.
        var ages = Enumerable.Range(0, 16).Select(_ => (ushort)(20 + ageRandom.NextInt(161))).ToArray();
        var contacts = ClassifyContacts(motion, crust.CrustByCellId, ages);
        var collision = new float[count]; var arc = new float[count]; var trench = new float[count];
        var rift = new float[count]; var shear = new float[count]; var ridge = new float[count];
        var candidates = new Dictionary<int, (int Edge, byte Upper, byte Lower, VolcanoKind Type)>();
        int arcOffset = Math.Max(2, (int)Math.Round(motion.InfluenceRadiusHexes * .8));
        foreach (var contact in contacts)
        {
            var edge = motion.Interactions[contact.InteractionIndex];
            int a = edge.FirstCellId, b = edge.SecondCellId;
            switch (contact.Kind)
            {
                case GeologicalContactKind.ContinentalCollision:
                    Seed(collision, a, edge.Convergence); Seed(collision, b, edge.Convergence); break;
                case GeologicalContactKind.ContinentalRift:
                    Seed(rift, a, edge.Divergence); Seed(rift, b, edge.Divergence); break;
                case GeologicalContactKind.OceanRidge:
                    Seed(ridge, a, edge.Divergence); Seed(ridge, b, edge.Divergence); break;
                case GeologicalContactKind.Transform:
                    Seed(shear, a, edge.Shear); Seed(shear, b, edge.Shear); break;
                case GeologicalContactKind.OceanContinentSubduction:
                case GeologicalContactKind.OceanOceanSubduction:
                    int upper = edge.FirstPlateId == contact.OverridingPlateId ? a : b;
                    int lower = upper == a ? b : a;
                    Seed(trench, lower, edge.Convergence);
                    // Local upper-plate arc, offset from the trench. Normal alignment avoids forearc volcanoes.
                    var inward = edge.FirstPlateId == contact.OverridingPlateId
                        ? new TectonicVector(-edge.Normal.X, -edge.Normal.Y) : edge.Normal;
                    var queue = new Queue<(int Id, int Distance)>(); var visited = new HashSet<int> { upper };
                    queue.Enqueue((upper, 0));
                    while (queue.TryDequeue(out var current))
                    {
                        if (current.Distance == arcOffset)
                        {
                            int dx = current.Id % topology.Columns - upper % topology.Columns;
                            if (dx > topology.Columns / 2) dx -= topology.Columns;
                            if (dx < -topology.Columns / 2) dx += topology.Columns;
                            double dy = Math.Sqrt(3) * (current.Id / topology.Columns - upper / topology.Columns +
                                .5 * ((current.Id % topology.Columns & 1) - (upper % topology.Columns & 1)));
                            if (dx * 1.5 * inward.X + dy * inward.Y < arcOffset * .7) continue;
                            Seed(arc, current.Id, edge.Convergence);
                            bool continental = crust.CrustByCellId[current.Id] == (byte)CrustKind.Continental;
                            if (continental && !terrain.IsLand(current.Id)) continue;
                            if (!candidates.ContainsKey(current.Id))
                                candidates[current.Id] = (contact.InteractionIndex, contact.OverridingPlateId,
                                    contact.SubductingPlateId, continental ? VolcanoKind.ContinentalArc : VolcanoKind.IslandArc);
                            continue;
                        }
                        foreach (int next in topology.Neighbors(current.Id))
                            if (plates[next] == contact.OverridingPlateId && visited.Add(next))
                                queue.Enqueue((next, current.Distance + 1));
                    }
                    break;
            }
        }
        double radius = motion.InfluenceRadiusHexes;
        var orogeny = TectonicMotionGenerator.Spread(topology, collision, arc, trench, radius * 1.3);
        var deformation = TectonicMotionGenerator.Spread(topology, rift, shear, ridge, Math.Max(1, radius * .5));
        var heights = new byte[count]; var depth = new byte[count]; var features = new byte[count];
        for (int id = 0; id < count; id++)
        {
            double detail = PeriodicNoise.Sample(id % topology.Columns, id / topology.Columns,
                topology.Columns, topology.Rows, 19, terrain.Configuration.Seed ^ 0x46494E45) * 2;
            double c = Response(orogeny.Compression[id]), a = Response(orogeny.Extension[id]);
            double e = Response(deformation.Compression[id]), s = Response(deformation.Extension[id]);
            if (terrain.IsLand(id))
            {
                // Low cratonic platform plus motion-derived uplift. No per-map relief normalization.
                double platform = 8 + Math.Clamp((crust.EmergenceAffinityByCellId[id] - seaLevel) * 22, 0, 20);
                heights[id] = (byte)Math.Clamp((int)Math.Round(platform + detail +
                    ContinentalGeologySettings.OrogenicUpliftLevels * c + ContinentalGeologySettings.ArcUpliftLevels * a -
                    ContinentalGeologySettings.RiftSubsidenceLevels * e - ContinentalGeologySettings.FaultSubsidenceLevels * s), 1, 100);
                features[id] = (byte)(c >= .2 && c >= a ? ContinentalLandform.MountainBelt : a >= .2 ? ContinentalLandform.VolcanicArc :
                    e >= .2 ? ContinentalLandform.Rift : s >= .2 ? ContinentalLandform.Fault : ContinentalLandform.None);
            }
            else
            {
                depth[id] = (byte)Math.Clamp((int)Math.Round(55 + detail + 27 * Response(orogeny.Shear[id]) -
                    25 * Response(deformation.Shear[id])), 1, 100);
                features[id] = (byte)(orogeny.Shear[id] >= 1 ? ContinentalLandform.Trench : deformation.Shear[id] >= 1 ?
                    ContinentalLandform.Ridge : deformation.Extension[id] >= 1 ? ContinentalLandform.Fault : ContinentalLandform.None);
            }
        }
        var volcanoes = new List<Volcano>();
        int spacing = Math.Max(3, (int)(Math.Min(topology.Columns, topology.Rows) * ContinentalGeologySettings.VolcanicSpacingMapFraction));
        var surface = terrain.SurfaceByCellId.ToArray();
        foreach (var (id, candidate) in candidates.OrderBy(pair => Rank(pair.Key, terrain.Configuration.Seed)).ThenBy(pair => pair.Key))
        {
            if (volcanoes.Any(volcano => topology.Distance(id, volcano.CellId) < spacing)) continue;
            // Small oceanic arcs may emerge, but never bridge two pre-existing continental provinces.
            if (surface[id] == 0 && topology.Neighbors(id).ToArray().Any(next => surface[next] != 0)) continue;
            if (surface[id] == 0) { surface[id] = 1; depth[id] = 0; }
            heights[id] = (byte)Math.Clamp(Math.Max(heights[id], 35 + (int)(25 * Response(arc[id]))), 1, 100);
            features[id] = (byte)ContinentalLandform.VolcanicArc;
            volcanoes.Add(new(id, candidate.Type, candidate.Lower, candidate.Upper, candidate.Edge));
        }
        return terrain with { SurfaceByCellId = surface, ElevationByCellId = heights,
            ContinentalGeology = new(ContinentsGenerator.Version, crust.CrustByCellId, crust.ProvinceByCellId,
                crust.MainContinentCoreCells, ages, contacts, volcanoes.ToArray(), features, depth, seaLevel) };
    }

    public static GeologicalContact[] ClassifyContacts(TectonicMotionState motion, byte[] crust, ushort[] ages)
        => motion.Interactions.Select((edge, index) => GeologicalContactClassifier.Classify(index, edge,
            (CrustKind)crust[edge.FirstCellId], (CrustKind)crust[edge.SecondCellId], ages)).ToArray();
    private static double Response(float value) => Math.Sqrt(Math.Clamp(value / ContinentalGeologySettings.ReferenceSpeedCmPerYear, 0, 1));
    private static void Seed(float[] field, int id, double value) => field[id] = Math.Max(field[id], (float)value);
    private static uint Rank(int cell, int seed)
    {
        uint value = unchecked((uint)(cell ^ seed)); value ^= value >> 16;
        value = unchecked(value * 0x7FEB352D); return value ^ (value >> 15);
    }
}
