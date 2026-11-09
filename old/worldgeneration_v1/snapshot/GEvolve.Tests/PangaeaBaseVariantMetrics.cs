using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

internal static class PangaeaBaseVariantMetrics
{
    internal static readonly int[] Thresholds = [60, 75, 85, 90, 95, 100];
    internal sealed class Context
    {
        internal HexTopology Topology { get; }
        internal byte[] Surface { get; }
        internal int[] Land { get; }
        internal int[] Coast { get; }
        internal int[] Boundary { get; }
        internal int[] H04 { get; }
        internal int[] Axis { get; }
        internal double[] Compression { get; }
        internal bool Detailed { get; }
        internal Context(HexTopology topology, LandmassState official, bool detailed)
        {
            Topology = topology; Surface = official.SurfaceByCellId; Detailed = detailed;
            Land = Enumerable.Range(0, topology.Count).Where(official.IsLand).ToArray();
            var coast = new List<int>();
            foreach (int id in Land) foreach (int nb in topology.Neighbors(id))
                if (!official.IsLand(nb)) { coast.Add(id); break; }
            Coast = PangaeaBaseAuditTests.Distances(topology, coast);
            var motion = official.TectonicMotion!;
            Boundary = motion.DistanceToBoundaryByCellId.Select(x => (int)x).ToArray();
            H04 = PangaeaBaseAuditTests.Distances(topology, motion.Interactions
                .Where(e => (e.Kind is PlateInteractionKind.Convergent or PlateInteractionKind.Mixed) && e.Convergence >= 1)
                .SelectMany(e => new[] { e.FirstCellId, e.SecondCellId }));
            Axis = PangaeaBaseAuditTests.Distances(topology, official.PangaeaMountainRanges!.Ranges.SelectMany(r => r.AxisCellIds));
            Compression = motion.CompressionByCellId.Select(x => (double)x).ToArray();
        }
    }
    internal static object Measure(Context c, PangaeaBaseVariants.Result result, byte[] v0, byte[] parent)
    {
        var t = c.Topology; var h = result.Heights;
        int[] sorted = c.Land.Select(i => (int)h[i]).Order().ToArray();
        int p90 = sorted[(int)((sorted.Length - 1) * .9)], p99 = sorted[(int)((sorted.Length - 1) * .99)];
        var heights = h.Select(v => (double)v).ToArray();
        var landEdges = new List<double>(); var coastEdges = new List<double>(); var slopes = new double[t.Count];
        foreach (int id in c.Land) foreach (int nb in t.Neighbors(id))
        {
            int step = Math.Abs(h[id] - h[nb]);
            if (c.Surface[nb] == 0) { coastEdges.Add(step); continue; }
            slopes[id] = Math.Max(slopes[id], step);
            if (nb > id) landEdges.Add(step);
        }
        int[] thresholds = Thresholds.Concat(new[] { p90, p99 }).Distinct().Order().ToArray();
        return new
        {
            Distribution = PangaeaBaseAuditTests.Stats(c.Land.Select(i => heights[i])),
            Levels = Thresholds.Select(threshold => new { Threshold = threshold, Cells = c.Land.Count(i => h[i] >= threshold),
                Percent = 100.0 * c.Land.Count(i => h[i] >= threshold) / c.Land.Length }).ToArray(),
            LandSlope = PangaeaBaseAuditTests.Stats(c.Land.Select(i => slopes[i])),
            LandEdgeJumps = PangaeaBaseAuditTests.Stats(landEdges), CoastEdgeJumps = PangaeaBaseAuditTests.Stats(coastEdges),
            JumpEdges10 = landEdges.Count(d => d >= 10), JumpEdges20 = landEdges.Count(d => d >= 20), JumpEdges30 = landEdges.Count(d => d >= 30),
            CoastBands = new[] { (0, 0), (1, 3), (4, 10), (11, int.MaxValue) }.Select(b => new
            { MinSteps = b.Item1, MaxSteps = b.Item2 == int.MaxValue ? (int?)null : b.Item2,
                Heights = PangaeaBaseAuditTests.Stats(c.Land.Where(i => c.Coast[i] >= b.Item1 && c.Coast[i] <= b.Item2).Select(i => heights[i])) }).ToArray(),
            DifferenceV0 = Delta(c.Land, v0, h), DifferenceParent = Delta(c.Land, parent, h),
            BaseVsH04Correlation = PangaeaBaseAuditTests.Correlation(c.Land, heights, c.Compression),
            BaseVsPlateDistanceCorrelation = PangaeaBaseAuditTests.Correlation(c.Land, heights, c.Boundary.Select(x => (double)x).ToArray()),
            BaseVsCoastDistanceCorrelation = PangaeaBaseAuditTests.Correlation(c.Land, heights, c.Coast.Select(x => (double)x).ToArray()),
            Peaks = Peaks(c, h, result.Potential, p90),
            Morphology = thresholds.Select(threshold => new { Threshold = threshold,
                Uses = new[] { Thresholds.Contains(threshold) ? "fixed" : null, threshold == p90 ? "relative_P90" : null, threshold == p99 ? "relative_P99" : null }.Where(s => s is not null).ToArray(),
                Analysis = PangaeaBaseMorphology.Measure(t, c.Surface, h, threshold, c.Boundary, c.Coast, c.Axis, c.Detailed) }).ToArray()
        };
    }
    private static object Delta(int[] land, byte[] before, byte[] after) => new
    {
        Changed = land.Count(i => before[i] != after[i]), Increased = land.Count(i => after[i] > before[i]), Decreased = land.Count(i => after[i] < before[i]),
        MeanDelta = land.Average(i => (double)after[i] - before[i]), MeanAbsoluteDelta = land.Average(i => Math.Abs((double)after[i] - before[i])),
        Thresholds = Thresholds.Select(threshold => new { Threshold = threshold,
            Entered = land.Count(i => before[i] < threshold && after[i] >= threshold), Exited = land.Count(i => before[i] >= threshold && after[i] < threshold),
            Inherited = land.Count(i => before[i] >= threshold && after[i] >= threshold) }).ToArray()
    };
    private static object Peaks(Context c, byte[] h, double[] potential, int p90)
    {
        var t = c.Topology; var seen = new bool[t.Count];
        var peaks = new List<(int Id, int Cells, int H04, int Axis)>(); int strictPotential = 0;
        foreach (int start in c.Land)
        {
            bool noHigher = true, lower = false;
            foreach (int nb in t.Neighbors(start)) if (c.Surface[nb] == 1)
            { if (potential[nb] > potential[start]) noHigher = false; if (potential[nb] < potential[start]) lower = true; }
            if (noHigher && lower) strictPotential++;
            if (seen[start]) continue;
            var plateau = new List<int>(); var q = new Queue<int>(); q.Enqueue(start); seen[start] = true; bool maximum = true;
            while (q.TryDequeue(out int id))
            {
                plateau.Add(id);
                foreach (int nb in t.Neighbors(id))
                {
                    if (c.Surface[nb] == 0) continue;
                    if (h[nb] > h[start]) maximum = false;
                    if (h[nb] == h[start] && !seen[nb]) { seen[nb] = true; q.Enqueue(nb); }
                }
            }
            if (maximum) peaks.Add((start, plateau.Count, plateau.Min(i => c.H04[i]), plateau.Min(i => c.Axis[i])));
        }
        return new { QuantizedPlateauMaxima = peaks.Count, Maxima85 = peaks.Count(p => h[p.Id] >= 85), MaximaAtOrAboveP90 = peaks.Count(p => h[p.Id] >= p90),
            StrictPotentialMaxima = strictPotential, HighMaximaFarther10 = peaks.Count(p => h[p.Id] >= 85 && p.H04 > 10),
            Details = peaks.OrderByDescending(p => h[p.Id]).ThenByDescending(p => p.Cells).ThenBy(p => p.Id).Take(c.Detailed ? 20 : 6)
                .Select(p => new { CellId = p.Id, Column = p.Id % t.Columns, Row = p.Id / t.Columns, Height = h[p.Id], PlateauCells = p.Cells,
                    H04MinimumDistance = p.H04, AxisMinimumDistance = p.Axis, RepresentativeCoastDistance = c.Coast[p.Id] }).ToArray() };
    }
}
