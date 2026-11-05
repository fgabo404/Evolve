using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Experimental C1 only. No surface generation, transport history or modification of B1.</summary>
public sealed class ContinentsMultiscaleCorticalGenerator
{
    public IEnumerable<C1FormationCandidate> GenerateCandidates(HexTopology topology,
        LandmassConfiguration configuration, byte[] plates, TectonicMotionState motion, SharedCorticalModel referenceA)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.Type != LandmassType.Continents ||
            configuration != LandmassProfiles.Create(configuration.Size, configuration.Type, configuration.Seed) ||
            topology.Grid.Definition != HexGridDefinitions.Get(configuration.Size))
            throw new ArgumentException("C1 requires an official Continents configuration.");
        SharedCorticalModelValidator.Validate(topology, referenceA, plates);
        int budget = referenceA.CrustByCellId.Count(c => c == 1);
        if (budget < 8 || budget >= topology.Count) throw new ArgumentException("C1 requires a nontrivial reference cortex.");
        var field = new C1GrowthField(topology, plates, motion);
        // The transferable residual is restricted to the approved B1 attempt-0
        // formula. Unknown future cortical properties must not be reinterpreted.
        for (int id = 0; id < topology.Count; id++)
        {
            double compression = Math.Tanh(motion.CompressionByCellId[id] / 5.0);
            double extension = Math.Tanh(motion.ExtensionByCellId[id] / 5.0);
            double detail = PeriodicNoise.Sample(id % topology.Columns, id / topology.Columns,
                topology.Columns, topology.Rows, 5, configuration.Seed ^ 0x434F5245);
            float expected = (float)((referenceA.CrustByCellId[id] == 1 ? .35 : -.35) + .30 * compression - .35 * extension + .10 * detail);
            if (referenceA.EmergenceAffinityByCellId[id] != expected)
                throw new ArgumentException("C1 affinity transfer requires the unchanged B1 attempt-0 reference.");
        }
        for (int k = 2; k <= 4; k++)
        for (int rank = 0; rank < C1Profile.CandidatesPerK; rank++)
        {
            int ordinal = (k - 2) * C1Profile.CandidatesPerK + rank;
            yield return Form(topology, configuration.Seed, plates, motion, referenceA, field, k, ordinal, budget);
        }
    }

    private static C1FormationCandidate Form(HexTopology topology, int seed, byte[] plates, TectonicMotionState motion,
        SharedCorticalModel reference, C1GrowthField graph, int k, int ordinal, int budget)
    {
        int n = topology.Count, range = Math.Max(2, (int)Math.Ceiling(motion.InfluenceRadiusHexes * 4));
        var pool = new List<int>();
        foreach (int cell in Enumerable.Range(0, n)
            .Where(id => graph.Opportunity[id] > .6 * graph.Stability[id] + 1e-12)
            .OrderByDescending(id => graph.Opportunity[id]).ThenBy(id => Tie(seed, ordinal, id, 0)))
        {
            if (pool.All(other => PhysicalDistance(topology, cell, other) >= Math.Max(2, motion.InfluenceRadiusHexes))) pool.Add(cell);
            if (pool.Count == 64) break;
        }
        if (pool.Count < k) return Failure("insufficient tectonically supported principal anchors");
        var anchors = new List<int>(); var domains = new int[8]; var evidence = new int[8]; Array.Fill(evidence, -1);
        double domainLength = Math.Sqrt(budget * 3 * Math.Sqrt(3) / (2 * Math.PI * k));
        for (int origin = 0; origin < k; origin++)
        {
            var ranked = pool.Where(id => !anchors.Contains(id)).OrderByDescending(id =>
                graph.Opportunity[id] + (anchors.Count == 0 ? 0 : .4 * Math.Min(1,
                    anchors.Min(a => PhysicalDistance(topology, id, a)) / (2 * domainLength))))
                .ThenBy(id => Tie(seed, ordinal, id, origin)).ToArray();
            int chosen = ranked[origin == 0 ? (ordinal % C1Profile.CandidatesPerK) % ranked.Length : 0];
            anchors.Add(chosen); domains[origin] = origin;
        }
        var rootDistances = anchors.Select(root => PlateDistances(topology, plates, root, range)).ToArray();
        for (int origin = k; origin < 8; origin++)
        {
            int domain = (origin - k) % k, best = -1, bestInteraction = -1; double bestScore = double.NegativeInfinity;
            for (int index = 0; index < motion.Interactions.Length; index++)
            {
                var edge = motion.Interactions[index]; if (!C1GrowthField.Convergent(edge)) continue;
                Examine(edge.FirstCellId, edge.SecondCellId); Examine(edge.SecondCellId, edge.FirstCellId);
                void Examine(int ownSide, int otherSide)
                {
                    int distance = rootDistances[domain][ownSide];
                    if (distance < 0 || anchors.Contains(otherSide)) return;
                    double separation = anchors.Min(a => PhysicalDistance(topology, otherSide, a));
                    double score = graph.Opportunity[otherSide] + Math.Tanh(edge.Convergence / 5) -
                        (double)distance / (range + 1) + .2 * Math.Min(1, separation / Math.Max(1, motion.InfluenceRadiusHexes));
                    if (score > bestScore || score == bestScore && Tie(seed, ordinal, otherSide, origin) < Tie(seed, ordinal, best, origin))
                    { bestScore = score; best = otherSide; bestInteraction = index; }
                }
            }
            if (best < 0)
            {
                best = pool.Where(id => !anchors.Contains(id)).OrderByDescending(id =>
                    anchors.Min(a => PhysicalDistance(topology, id, a))).ThenBy(id => Tie(seed, ordinal, id, origin)).FirstOrDefault(-1);
                domain = -1;
            }
            if (best < 0) return Failure("insufficient distinct secondary anchors");
            anchors.Add(best); domains[origin] = domain; evidence[origin] = bestInteraction;
        }
        var budgets = new int[8]; int primary = (int)Math.Round(budget * C1Profile.PrimaryBudgetFraction, MidpointRounding.AwayFromZero);
        if (primary < k || budget - primary < 8 - k) return Failure("budget cannot fund eight origins");
        Allocate(0, k, primary); Allocate(k, 8, budget - primary);
        var origins = anchors.Select((cell, id) => new C1Origin(id, id < k ? C1OriginRole.Principal : C1OriginRole.Secondary,
            domains[id], cell, evidence[id], budgets[id], graph.Opportunity[cell])).ToArray();
        var domainModels = Enumerable.Range(0, k).Select(d => new C1Domain(d, d,
            Enumerable.Range(0, 8).Where(o => domains[o] == d).ToArray())).ToArray();
        var fields = new double[8][]; var reports = new C1SolverReport[8];
        for (int origin = 0; origin < 8; origin++)
        {
            var solved = graph.Solve(origin, anchors[origin], budgets[origin]); fields[origin] = solved.Values; reports[origin] = solved.Report;
        }
        if (reports.Any(report => !report.Converged))
            return new(ordinal, k, budget, origins, domainModels, reports, (int[])budgets.Clone(), null, [], [], null, "numerical convergence failure");

        var owner = new int[n]; var parent = new int[n]; var round = new int[n]; var crossed = new int[n];
        Array.Fill(owner, -1); Array.Fill(parent, -1); Array.Fill(round, -1); Array.Fill(crossed, -1);
        var remaining = (int[])budgets.Clone();
        var frontier = Enumerable.Range(0, 8).Select(_ => new HashSet<int>()).ToArray();
        for (int origin = 0; origin < 8; origin++) { owner[anchors[origin]] = origin; round[anchors[origin]] = 0; remaining[origin]--; }
        for (int origin = 0; origin < 8; origin++) AddNeighbors(anchors[origin], origin);
        for (int epoch = 1; epoch <= 2 * Math.Max(topology.Columns, topology.Rows) && remaining.Sum() > 0; epoch++)
        {
            var offers = new Dictionary<int, (int Origin, int Parent, double Score, uint Tie)>();
            for (int origin = 0; origin < 8; origin++)
            {
                if (remaining[origin] == 0) continue;
                int quota = Math.Min(remaining[origin], Math.Max(1, (budgets[origin] + 63) / 64)), proposed = 0;
                foreach (int cell in frontier[origin].Where(id => owner[id] < 0).OrderByDescending(id => fields[origin][id])
                    .ThenBy(id => Tie(seed, ordinal, id, origin)))
                {
                    double normalized = Math.Max(0, fields[origin][cell]) / reports[origin].Maximum;
                    if (normalized <= 1e-12) continue;
                    int predecessor = -1; double strongest = double.NegativeInfinity;
                    bool valid = true;
                    foreach (int adjacent in topology.Neighbors(cell))
                    {
                        if (owner[adjacent] != origin) continue;
                        if (!Allowed(cell, adjacent)) { valid = false; break; }
                        double strength = fields[origin][adjacent];
                        if (strength > strongest || strength == strongest && Tie(seed, ordinal, adjacent, origin) < Tie(seed, ordinal, predecessor, origin))
                        { strongest = strength; predecessor = adjacent; }
                    }
                    if (!valid || predecessor < 0) continue;
                    double score = normalized * remaining[origin] / budgets[origin]; uint tie = Tie(seed, ordinal, cell, origin);
                    if (!offers.TryGetValue(cell, out var previous) || score > previous.Score || score == previous.Score && tie < previous.Tie)
                        offers[cell] = (origin, predecessor, score, tie);
                    if (++proposed == quota) break;
                }
            }
            // Reject both sides of a jointly forbidden interface, independent of commit order.
            var invalid = new HashSet<int>();
            foreach (var offer in offers)
                foreach (int adjacent in topology.Neighbors(offer.Key))
                {
                    int other = offers.TryGetValue(adjacent, out var next) ? next.Origin : owner[adjacent];
                    if (other == offer.Value.Origin && !Allowed(offer.Key, adjacent))
                    { invalid.Add(offer.Key); if (offers.ContainsKey(adjacent)) invalid.Add(adjacent); }
                }
            int incorporated = 0;
            foreach (var offer in offers.OrderBy(pair => pair.Key))
            {
                if (invalid.Contains(offer.Key)) continue;
                int cell = offer.Key, origin = offer.Value.Origin, predecessor = offer.Value.Parent;
                owner[cell] = origin; parent[cell] = predecessor; round[cell] = epoch; remaining[origin]--; incorporated++;
                foreach (var pendingFrontier in frontier) pendingFrontier.Remove(cell);
                if (plates[cell] != plates[predecessor]) crossed[cell] = graph.InteractionIndices[C1GrowthField.Key(cell, predecessor)];
            }
            foreach (var offer in offers)
                if (owner[offer.Key] >= 0) AddNeighbors(offer.Key, owner[offer.Key]);
            if (incorporated == 0) break;
        }
        var crust = owner.Select(o => (byte)(o < 0 ? 0 : 1)).ToArray();
        var fragmentByCell = new int[n]; var provinceByCell = new int[n]; Array.Fill(fragmentByCell, -1); Array.Fill(provinceByCell, -1);
        var fragments = new List<CorticalFragment>(); var ancestry = new List<int>(); var provinces = new List<CorticalProvince>();
        var provinceRoles = new List<C1ProvinceRole>(); var queue = new Queue<int>();
        for (int start = 0; start < n; start++)
        {
            if (owner[start] < 0 || fragmentByCell[start] >= 0) continue;
            int origin = owner[start], fragment = fragments.Count, core = start;
            fragmentByCell[start] = fragment; queue.Enqueue(start);
            while (queue.TryDequeue(out int cell))
            {
                if (cell == anchors[origin]) core = cell;
                foreach (int adjacent in topology.Neighbors(cell))
                    if (owner[adjacent] == origin && fragmentByCell[adjacent] < 0)
                    { fragmentByCell[adjacent] = fragment; queue.Enqueue(adjacent); }
            }
            fragments.Add(new(fragment, core)); ancestry.Add(origin);
        }
        for (int start = 0; start < n; start++)
        {
            if (owner[start] < 0 || provinceByCell[start] >= 0) continue;
            int province = provinces.Count, fragment = fragmentByCell[start]; C1ProvinceRole role = Role(start);
            provinceByCell[start] = province; queue.Enqueue(start);
            while (queue.TryDequeue(out int cell))
                foreach (int adjacent in topology.Neighbors(cell))
                    if (fragmentByCell[adjacent] == fragment && plates[adjacent] == plates[start] && Role(adjacent) == role && provinceByCell[adjacent] < 0)
                    { provinceByCell[adjacent] = province; queue.Enqueue(adjacent); }
            provinces.Add(new(province, fragment, start)); provinceRoles.Add(role);
        }
        var affinity = new float[n];
        for (int id = 0; id < n; id++) affinity[id] = crust[id] == reference.CrustByCellId[id]
            ? reference.EmergenceAffinityByCellId[id]
            : (float)((double)reference.EmergenceAffinityByCellId[id] + .70 * (crust[id] - reference.CrustByCellId[id]));
        var model = new SharedCorticalModel(SharedCorticalModel.CurrentVersion, crust, fragmentByCell, provinceByCell, affinity,
            fragments.ToArray(), provinces.ToArray(), []);
        SharedCorticalModelValidator.Validate(topology, model, plates);
        return new(ordinal, k, budget, origins, domainModels, reports, remaining, model, ancestry.ToArray(), provinceRoles.ToArray(),
            new(owner, parent, round, crossed), remaining.Sum() == 0 ? "" : "incomplete admissible cortical budget");

        C1ProvinceRole Role(int cell)
        {
            if (owner[cell] < 0) return C1ProvinceRole.Margin;
            if (motion.ExtensionByCellId[cell] > motion.CompressionByCellId[cell] + .15) return C1ProvinceRole.ExtensionalMargin;
            if (motion.DistanceToBoundaryByCellId[cell] <= motion.InfluenceRadiusHexes && motion.CompressionByCellId[cell] > motion.ExtensionByCellId[cell]) return C1ProvinceRole.ContactMargin;
            return owner[cell] >= k ? C1ProvinceRole.Terrane : graph.Stability[cell] >= .75 ? C1ProvinceRole.CratonicInterior : C1ProvinceRole.Margin;
        }
        void AddNeighbors(int cell, int origin)
        { foreach (int adjacent in topology.Neighbors(cell)) if (owner[adjacent] < 0 && Allowed(cell, adjacent)) frontier[origin].Add(adjacent); }
        bool Allowed(int a, int b) => plates[a] == plates[b] || graph.InteractionIndices.TryGetValue(C1GrowthField.Key(a, b), out int index) && C1GrowthField.Convergent(motion.Interactions[index]);
        void Allocate(int first, int last, int total)
        {
            double sum = Enumerable.Range(first, last - first).Sum(id => 1 + 3 * graph.Opportunity[anchors[id]]);
            var fractions = new List<(int Origin, double Fraction)>(); int used = 0;
            for (int id = first; id < last; id++)
            {
                double exact = (total - (last - first)) * (1 + 3 * graph.Opportunity[anchors[id]]) / sum;
                budgets[id] = 1 + (int)Math.Floor(exact); used += budgets[id]; fractions.Add((id, exact - Math.Floor(exact)));
            }
            foreach (var remainder in fractions.OrderByDescending(x => x.Fraction).ThenBy(x => Tie(seed, ordinal, anchors[x.Origin], x.Origin)).Take(total - used)) budgets[remainder.Origin]++;
        }
        C1FormationCandidate Failure(string reason) => new(ordinal, k, budget, [], [], [], [], null, [], [], null, reason);
    }

    private static int[] PlateDistances(HexTopology topology, byte[] plates, int source, int range)
    {
        var distances = new int[topology.Count]; Array.Fill(distances, -1); distances[source] = 0;
        var queue = new Queue<int>(); queue.Enqueue(source);
        while (queue.TryDequeue(out int cell))
        {
            if (distances[cell] == range) continue;
            foreach (int next in topology.Neighbors(cell)) if (plates[next] == plates[source] && distances[next] < 0)
            { distances[next] = distances[cell] + 1; queue.Enqueue(next); }
        }
        return distances;
    }
    private static double PhysicalDistance(HexTopology topology, int a, int b)
    {
        var x = FlatHexGeometry.Center(new GridAddress(a % topology.Columns, a / topology.Columns));
        var y = FlatHexGeometry.Center(new GridAddress(b % topology.Columns, b / topology.Columns));
        double dx = Math.Abs(x.X - y.X); dx = Math.Min(dx, FlatHexGeometry.PeriodWidth(topology.Columns) - dx);
        return Math.Sqrt(dx * dx + (double)(x.Y - y.Y) * (x.Y - y.Y));
    }
    private static uint Tie(int seed, int ordinal, int cell, int origin)
    {
        unchecked { uint x = (uint)seed ^ (uint)ordinal * 0x9E3779B9u ^ (uint)cell * 0x85EBCA6Bu ^ (uint)origin * 0xC2B2AE35u;
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; return x ^ (x >> 16); }
    }
}
