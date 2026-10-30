using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Tectonics;

public sealed class TectonicMotionGenerator
{
    public const string Version = "tectonic-motion-1";
    private readonly TectonicMotionSettings _settings;
    public TectonicMotionGenerator(TectonicMotionSettings? settings = null)
    {
        _settings = settings ?? new();
        _settings.Validate();
    }

    /// <summary>Normal to the shared edge, A to B, in the local flat-top plane.</summary>
    public static TectonicVector BoundaryNormal(HexTopology topology, int first, int second)
    {
        if (!topology.Neighbors(first).Contains(second))
            throw new ArgumentException("Boundary cells must be real hex neighbors.");
        int a = first % topology.Columns, b = second % topology.Columns;
        int dx = b - a;
        if (dx > topology.Columns / 2) dx -= topology.Columns;
        if (dx < -topology.Columns / 2) dx += topology.Columns;
        double x = 1.5 * dx;
        double y = Math.Sqrt(3) * (second / topology.Columns - first / topology.Columns +
            .5 * ((b & 1) - (a & 1)));
        double length = Math.Sqrt(x * x + y * y);
        return new(x / length, y / length);
    }

    public PlateInteraction Project(int first, int second, PlateMotion a, PlateMotion b, TectonicVector normal)
    {
        foreach (var plate in new[] { a, b })
            if (!double.IsFinite(plate.DirectionDegrees) || plate.DirectionDegrees is < 0 or >= 360 ||
                !double.IsFinite(plate.SpeedCmPerYear) || plate.SpeedCmPerYear <= 0)
                throw new ArgumentException("Motion direction and speed must be finite and valid.");
        double length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
        if (!double.IsFinite(length) || Math.Abs(length - 1) > 1e-8)
            throw new ArgumentException("A finite unit normal is required.", nameof(normal));
        var va = a.Velocity; var vb = b.Velocity;
        double x = vb.X - va.X, y = vb.Y - va.Y;
        double signedNormal = x * normal.X + y * normal.Y;
        double convergence = Math.Max(0, -signedNormal), divergence = Math.Max(0, signedNormal);
        double shear = Math.Abs(-x * normal.Y + y * normal.X);
        double axial = Math.Abs(signedNormal), maximum = Math.Max(axial, shear);
        PlateInteractionKind kind = maximum <= _settings.ActivityThresholdCmPerYear
            ? PlateInteractionKind.Quiet
            : Math.Min(axial, shear) >= _settings.ActivityThresholdCmPerYear &&
                Math.Min(axial, shear) >= maximum * _settings.MixedComponentRatio
                ? PlateInteractionKind.Mixed
                : shear > axial ? PlateInteractionKind.Transform
                    : signedNormal < 0 ? PlateInteractionKind.Convergent : PlateInteractionKind.Divergent;
        return new(first, second, a.PlateId, b.PlateId, normal, convergence, divergence, shear, kind);
    }

    public TectonicMotionState Generate(HexTopology topology, LandmassState terrain)
    {
        var plates = terrain.PlateByCellId ?? throw new ArgumentException("Plates are required.");
        if (plates.Any(id => id >= TectonicPlateGenerator.NumberOfPlates) ||
            plates.Distinct().Count() != TectonicPlateGenerator.NumberOfPlates)
            throw new ArgumentException("Exactly 16 existing plates are required.");
        var boundary = PlateBoundaryMap.Analyze(topology, terrain)!;
        var motions = PlateMotionGenerator.Generate(terrain.Configuration.Seed, _settings);
        var interactions = boundary.Edges.Select(edge => Project(edge.FirstCellId, edge.SecondCellId,
            motions[plates[edge.FirstCellId]], motions[plates[edge.SecondCellId]],
            BoundaryNormal(topology, edge.FirstCellId, edge.SecondCellId))).ToArray();
        var c = new float[topology.Count]; var d = new float[topology.Count]; var s = new float[topology.Count];
        foreach (var edge in interactions)
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
            {
                c[id] = Math.Max(c[id], (float)edge.Convergence);
                d[id] = Math.Max(d[id], (float)edge.Divergence);
                s[id] = Math.Max(s[id], (float)edge.Shear);
            }
        double radius = Math.Max(1, Math.Min(topology.Columns, topology.Rows) * _settings.InfluenceRadiusFraction);
        var fields = Spread(topology, c, d, s, radius, _settings.CutoffRadii);
        // Deepest interior cell per plate, with stable ID tie-break. No centroid outside a concave plate.
        var anchors = Enumerable.Repeat(-1, motions.Length).ToArray();
        for (int id = 0; id < topology.Count; id++)
            if (anchors[plates[id]] < 0 || boundary.DistanceToBoundaryByCellId[id] >
                boundary.DistanceToBoundaryByCellId[anchors[plates[id]]]) anchors[plates[id]] = id;
        return new(Version, motions, interactions, fields.Compression, fields.Extension, fields.Shear,
            boundary.DistanceToBoundaryByCellId, anchors, radius);
    }

    /// <summary>Maximum Gaussian envelope per channel, not a sum dependent on boundary density.</summary>
    public static (float[] Compression, float[] Extension, float[] Shear) Spread(HexTopology topology,
        float[] compression, float[] extension, float[] shear, double radius, double cutoffRadii = 3)
    {
        if (!double.IsFinite(radius) || radius <= 0 || !double.IsFinite(cutoffRadii) || cutoffRadii is < 1 or > 5 ||
            new[] { compression, extension, shear }.Any(field => field.Length != topology.Count ||
                field.Any(value => !float.IsFinite(value) || value < 0)))
            throw new ArgumentException("Invalid stress propagation input.");
        var c = new float[topology.Count]; var d = new float[topology.Count]; var s = new float[topology.Count];
        int limit = Math.Min(topology.Count, (int)Math.Ceiling(radius * cutoffRadii));
        var weights = Enumerable.Range(0, limit + 1).Select(distance =>
            (float)Math.Exp(-Math.Pow(distance / radius, 2))).ToArray();
        var visited = new int[topology.Count]; var distances = new int[topology.Count];
        var queue = new int[topology.Count];
        for (int source = 0; source < topology.Count; source++)
        {
            if (compression[source] == 0 && extension[source] == 0 && shear[source] == 0) continue;
            int head = 0, tail = 1, stamp = source + 1;
            queue[0] = source; visited[source] = stamp; distances[source] = 0;
            while (head < tail)
            {
                int id = queue[head++], distance = distances[id];
                float weight = weights[distance];
                c[id] = Math.Max(c[id], compression[source] * weight);
                d[id] = Math.Max(d[id], extension[source] * weight);
                s[id] = Math.Max(s[id], shear[source] * weight);
                if (distance == limit) continue;
                foreach (int next in topology.Neighbors(id))
                    if (visited[next] != stamp)
                    {
                        visited[next] = stamp; distances[next] = distance + 1; queue[tail++] = next;
                    }
            }
        }
        return (c, d, s);
    }
}
