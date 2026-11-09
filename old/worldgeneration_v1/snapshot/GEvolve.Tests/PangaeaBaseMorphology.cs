using GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>Numerical audit heuristics, never a generation rule or visual approval.</summary>
internal static class PangaeaBaseMorphology
{
    internal sealed record Component(int Id, int Cells, int Representative, int PerimeterEdges,
        int DiameterLowerBound, int MaximumInradius, double MeanWidthProxy, double Compactness,
        double Elongation, int SkeletonCells, int SkeletonEndpoints, int SkeletonJunctionClusters,
        int SkeletonCycleRank, bool ThinningConverged, bool CrossesSeam, bool WindsCylinder, bool TouchesOpenBoundary,
        double MeanPlateDistance, double MeanCoastDistance, double? MeanAxisDistance,
        bool RidgeCandidate, bool CompactCandidate);
    internal sealed record Hole(int ComponentId, int InteriorCells, int Representative, double LandFraction,
        double MeanInteriorElevation, int BoundaryMinimum, int BoundaryMaximum, double Contrast,
        bool Candidate);
    internal sealed record Result(int Threshold, int HighCells, int Components, int[] Sizes,
        int RidgeCandidates, int CompactCandidates, int RingCandidates, Component[] Details, Hole[] Holes);

    public static Result Measure(HexTopology topology, byte[] surface, byte[] heights, int threshold,
        int[] plateDistance, int[] coastDistance, int[] axisDistance, bool detailed)
    {
        int n = topology.Count;
        var mask = Enumerable.Range(0, n).Select(i => surface[i] == 1 && heights[i] >= threshold).ToArray();
        var labels = new int[n]; var components = new List<int[]>();
        for (int id = 0; id < n; id++)
        {
            if (!mask[id] || labels[id] != 0) continue;
            int label = components.Count + 1; var cells = new List<int>(); var queue = new Queue<int>();
            queue.Enqueue(id); labels[id] = label;
            while (queue.TryDequeue(out int current))
            {
                cells.Add(current);
                foreach (int next in topology.Neighbors(current))
                    if (mask[next] && labels[next] == 0) { labels[next] = label; queue.Enqueue(next); }
            }
            components.Add(cells.ToArray());
        }
        var skeleton = (bool[])mask.Clone();
        // Sequential simple-point thinning preserves local foreground connectivity and endpoints.
        // Six directional passes reduce bias, but this remains a diagnostic skeleton, not a physical axis.
        bool changed = true; int passes = 0;
        int[] active = components.SelectMany(c => c).Order().ToArray();
        while (changed && passes++ < 128)
        {
            changed = false;
            for (int direction = 0; direction < 6; direction++)
            foreach (int id in active)
            {
                if (!skeleton[id]) continue;
                var neighbors = topology.Neighbors(id);
                if (neighbors.Length == 6 && skeleton[neighbors[direction]]) continue;
                if (Simple(id)) { skeleton[id] = false; changed = true; }
            }
        }
        bool Simple(int id)
        {
            Span<int> occupied = stackalloc int[6]; int count = 0;
            foreach (int next in topology.Neighbors(id)) if (skeleton[next]) occupied[count++] = next;
            if (count <= 1 || count == 6) return false;
            int bits = 1, previous;
            do
            {
                previous = bits;
                for (int a = 0; a < count; a++) if ((bits & (1 << a)) != 0)
                for (int b = 0; b < count; b++)
                    if (topology.Neighbors(occupied[a]).Contains(occupied[b])) bits |= 1 << b;
            } while (bits != previous);
            return bits == (1 << count) - 1;
        }
        var depth = Enumerable.Repeat(-1, n).ToArray(); var erosion = new Queue<int>();
        foreach (int id in active)
        {
            bool boundary = topology.Neighbors(id).Length < 6;
            foreach (int next in topology.Neighbors(id)) if (!mask[next]) boundary = true;
            if (boundary) { depth[id] = 0; erosion.Enqueue(id); }
        }
        while (erosion.TryDequeue(out int current))
            foreach (int next in topology.Neighbors(current))
                if (mask[next] && depth[next] < 0) { depth[next] = depth[current] + 1; erosion.Enqueue(next); }
        var shape = new List<Component>();
        foreach (int[] cells in components)
        {
            int label = labels[cells[0]], perimeter = 0, endpoints = 0, edges = 0;
            bool seam = false, open = false;
            var sk = cells.Where(i => skeleton[i]).ToHashSet(); var junctions = new HashSet<int>();
            foreach (int id in cells)
            {
                var neighbors = topology.Neighbors(id); perimeter += 6 - neighbors.Length;
                if (neighbors.Length < 6) open = true;
                int degree = 0;
                foreach (int next in neighbors)
                {
                    if (!mask[next]) perimeter++;
                    if (mask[next] && Math.Abs(id % topology.Columns - next % topology.Columns) > topology.Columns / 2) seam = true;
                    if (skeleton[id] && skeleton[next]) degree++;
                }
                if (skeleton[id]) { edges += degree; if (degree <= 1) endpoints++; if (degree >= 3) junctions.Add(id); }
            }
            var first = Sweep(cells[0], i => labels[i] == label); var second = Sweep(first.Cell, i => labels[i] == label);
            int skeletonDiameter = 0;
            if (sk.Count > 0) { var a = Sweep(sk.Min(), i => sk.Contains(i)); skeletonDiameter = Sweep(a.Cell, i => sk.Contains(i)).Distance; }
            int clusters = 0;
            while (junctions.Count > 0)
            {
                clusters++; var q = new Queue<int>(); int start = junctions.Min(); q.Enqueue(start); junctions.Remove(start);
                while (q.TryDequeue(out int id)) foreach (int next in topology.Neighbors(id)) if (junctions.Remove(next)) q.Enqueue(next);
            }
            int inradius = cells.Max(i => depth[i]); double width = 2 * inradius + 1;
            double elongation = Math.Max(second.Distance, skeletonDiameter) / width;
            double compact = perimeter == 0 ? 0 : 4 * Math.PI * cells.Length * (3 * Math.Sqrt(3) / 2) / (perimeter * (double)perimeter);
            shape.Add(new(label, cells.Length, cells.Min(), perimeter, second.Distance, inradius,
                cells.Average(i => 2.0 * depth[i] + 1), compact, elongation, sk.Count, endpoints, clusters,
                Math.Max(0, edges / 2 - sk.Count + 1), !changed, seam, Winding(cells[0], i => labels[i] == label), open,
                cells.Average(i => (double)plateDistance[i]), cells.Average(i => (double)coastDistance[i]), cells.Where(i => axisDistance[i] >= 0).Select(i => (double?)axisDistance[i]).Average(),
                cells.Length >= 8 && second.Distance >= 8 && elongation >= 4,
                cells.Length >= 10 && compact >= .35 && elongation <= 3));
        }
        var seen = new bool[n]; var holes = new List<Hole>();
        for (int start = 0; start < n; start++)
        {
            if (mask[start] || seen[start]) continue;
            var q = new Queue<int>(); var interior = new List<int>(); var bordering = new HashSet<int>();
            bool open = false; var boundaryHeights = new List<int>(); q.Enqueue(start); seen[start] = true;
            while (q.TryDequeue(out int id))
            {
                interior.Add(id); if (topology.Neighbors(id).Length < 6) open = true;
                foreach (int next in topology.Neighbors(id))
                    if (mask[next]) { bordering.Add(labels[next]); boundaryHeights.Add(heights[next]); }
                    else if (!seen[next]) { seen[next] = true; q.Enqueue(next); }
            }
            // A cylindrical band or two separate barriers is not an enclosed disk-like ring.
            if (open || bordering.Count != 1 || Winding(start, i => !mask[i])) continue;
            double landFraction = interior.Count(i => surface[i] == 1) / (double)interior.Count;
            double mean = interior.Average(i => (double)heights[i]); int min = boundaryHeights.Min(), max = boundaryHeights.Max();
            holes.Add(new(bordering.Single(), interior.Count, start, landFraction, mean, min, max, min - mean,
                interior.Count >= 3 && landFraction >= .9 && min - mean >= 5));
        }
        return new(threshold, active.Length, shape.Count, shape.Select(c => c.Cells).OrderDescending().ToArray(),
            shape.Count(c => c.RidgeCandidate), shape.Count(c => c.CompactCandidate), holes.Count(h => h.Candidate),
            shape.OrderByDescending(c => c.Cells).Take(detailed ? 30 : 12).Concat(shape.Where(c => holes.Any(h => h.ComponentId == c.Id))).DistinctBy(c => c.Id).ToArray(),
            holes.ToArray());

        (int Cell, int Distance) Sweep(int start, Func<int, bool> allowed)
        {
            var distances = new Dictionary<int, int> { [start] = 0 }; var q = new Queue<int>(); q.Enqueue(start); int farthest = start;
            while (q.TryDequeue(out int id))
            {
                if (distances[id] > distances[farthest]) farthest = id;
                foreach (int next in topology.Neighbors(id))
                    if (allowed(next) && distances.TryAdd(next, distances[id] + 1)) q.Enqueue(next);
            }
            return (farthest, distances[farthest]);
        }
        bool Winding(int start, Func<int, bool> allowed)
        {
            var xs = new Dictionary<int, int> { [start] = start % topology.Columns }; var q = new Queue<int>(); q.Enqueue(start);
            while (q.TryDequeue(out int id)) foreach (int next in topology.Neighbors(id))
            {
                if (!allowed(next)) continue;
                int dx = next % topology.Columns - id % topology.Columns;
                if (dx > topology.Columns / 2) dx -= topology.Columns;
                if (dx < -topology.Columns / 2) dx += topology.Columns;
                int x = xs[id] + dx;
                if (xs.TryGetValue(next, out int previous)) { if (x != previous) return true; }
                else { xs[next] = x; q.Enqueue(next); }
            }
            return false;
        }
    }
}
