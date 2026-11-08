internal static class PangaeaLowlandMetrics
{
    internal static object Measure(PangaeaBaseVariantMetrics.Context c, byte[] heights, byte[] parent)
    {
        var t = c.Topology;
        var bands = new[] { (1, 1), (1, 3), (4, 8), (9, int.MaxValue) }; //first cell deliberately overlaps 1..3
        var edges = new List<(int Depth, int Jump)>();
        int sameDepth = 0, equalBefore = 0, equalAfter = 0, newEqual = 0;
        foreach (int id in c.Land) foreach (int nb in t.Neighbors(id))
        {
            if (c.Surface[nb] == 0 || nb <= id) continue;
            int depth = Math.Min(c.Coast[id], c.Coast[nb]) + 1;
            if (depth <= 8) edges.Add((depth, Math.Abs(heights[id] - heights[nb])));
            if (c.Coast[id] == c.Coast[nb] && c.Coast[id] < 8)
            {
                sameDepth++; if (parent[id] == parent[nb]) equalBefore++;
                if (heights[id] == heights[nb]) { equalAfter++; if (parent[id] != parent[nb]) newEqual++; }
            }
        }
        object HeightGroup(IEnumerable<int> ids)
        {
            int[] cells = ids.ToArray();
            return new { Heights = PangaeaBaseAuditTests.Stats(cells.Select(i => (double)heights[i])),
                ParentHeights = PangaeaBaseAuditTests.Stats(cells.Select(i => (double)parent[i])),
                Changed = cells.Count(i => heights[i] != parent[i]), Raised = cells.Count(i => heights[i] > parent[i]),
                AtOne = cells.Count(i => heights[i] == 1), DistinctLevels = cells.Select(i => heights[i]).Distinct().Count(),
                DominantLevelPercent = cells.Length == 0 ? (double?)null : 100.0 * cells.GroupBy(i => heights[i]).Max(g => g.Count()) / cells.Length };
        }
        return new
        {
            DistanceConvention = "ocean=0, first land=1; first land row also included in aggregate 1..3",
            OceanBands = bands.Select(b => new { Min = b.Item1, Max = b.Item2 == int.MaxValue ? (int?)null : b.Item2,
                Data = HeightGroup(c.Land.Where(i => c.Coast[i] + 1 >= b.Item1 && c.Coast[i] + 1 <= b.Item2)) }).ToArray(),
            ExactDepths = Enumerable.Range(1, 8).Select(d => new { OceanDistance = d,
                Data = HeightGroup(c.Land.Where(i => c.Coast[i] + 1 == d)) }).ToArray(),
            NearCoastLandEdges = PangaeaBaseAuditTests.Stats(edges.Select(e => (double)e.Jump)),
            NearCoastJump20 = edges.Count(e => e.Jump >= 20), NearCoastJump10 = edges.Count(e => e.Jump >= 10),
            LandEdgeBands = new[] { (1, 3), (4, 8) }.Select(b => new { Min = b.Item1, Max = b.Item2,
                Jump = PangaeaBaseAuditTests.Stats(edges.Where(e => e.Depth >= b.Item1 && e.Depth <= b.Item2).Select(e => (double)e.Jump)) }).ToArray(),
            SameDepthLandEdges = sameDepth, EqualAlongshoreBefore = equalBefore, EqualAlongshoreAfter = equalAfter, NewlyEqualAlongshore = newEqual,
            HeightOne = c.Land.Count(i => heights[i] == 1),
            ElevationBands = new[] { (1, 15), (16, 50), (51, 60), (61, 75), (76, 84), (85, 94), (95, 100) }.Select(b => new
            { Min = b.Item1, Max = b.Item2, Cells = c.Land.Count(i => heights[i] >= b.Item1 && heights[i] <= b.Item2),
                Percent = 100.0 * c.Land.Count(i => heights[i] >= b.Item1 && heights[i] <= b.Item2) / c.Land.Length }).ToArray(),
            ParallelRunProxy = FlatRuns(c, heights), ParentParallelRunProxy = FlatRuns(c, parent)
        };
    }
    private static object FlatRuns(PangaeaBaseVariantMetrics.Context c, byte[] heights)
    {
        var seen = new bool[c.Topology.Count]; var runs = new List<(int Id, int Cells, int Span)>();
        foreach (int start in c.Land)
        {
            if (c.Coast[start] >= 8 || seen[start]) continue;
            var ids = new HashSet<int>(); var q = new Queue<int>(); q.Enqueue(start); seen[start] = true;
            while (q.TryDequeue(out int id))
            {
                ids.Add(id);
                foreach (int nb in c.Topology.Neighbors(id))
                    if (c.Surface[nb] == 1 && !seen[nb] && c.Coast[nb] == c.Coast[start] && heights[nb] == heights[start])
                    { seen[nb] = true; q.Enqueue(nb); }
            }
            if (ids.Count < 6) continue;
            var first = Sweep(start, ids); var second = Sweep(first.Id, ids);
            if (second.Distance >= 5) runs.Add((start, ids.Count, second.Distance));
        }
        return new { CandidateRuns = runs.Count, LargestCells = runs.Count == 0 ? 0 : runs.Max(r => r.Cells),
            LongestLowerBound = runs.Count == 0 ? 0 : runs.Max(r => r.Span),
            Examples = runs.OrderByDescending(r => r.Span).Take(5).Select(r => new { CellId = r.Id, r.Cells, r.Span,
                Height = heights[r.Id], OceanDistance = c.Coast[r.Id] + 1 }).ToArray() };
        (int Id, int Distance) Sweep(int source, HashSet<int> allowed)
        {
            var distances = new Dictionary<int, int> { [source] = 0 }; var q = new Queue<int>(); q.Enqueue(source); int far = source;
            while (q.TryDequeue(out int id)) { if (distances[id] > distances[far]) far = id;
                foreach (int nb in c.Topology.Neighbors(id)) if (allowed.Contains(nb) && distances.TryAdd(nb, distances[id] + 1)) q.Enqueue(nb); }
            return (far, distances[far]);
        }
    }
}
