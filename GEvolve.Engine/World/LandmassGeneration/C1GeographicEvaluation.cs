using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Read-only frozen classification. Never repairs, paints or relabels a surface to pass.</summary>
public static class C1GeographicEvaluation
{
    public static C1GeographicReport Evaluate(HexTopology topology, LandmassState terrain,
        C1FormationCandidate formation, ContinentsCorticalAssemblyResult assembly)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(formation);
        ArgumentNullException.ThrowIfNull(assembly);
        var model = assembly.Model; var surface = terrain.SurfaceByCellId; int n = topology.Count;
        SharedCorticalModelValidator.Validate(topology, model, terrain.PlateByCellId!, surface);
        var coast = CoastMetrics.Measure(topology, surface);
        for (int id = 0; id < n; id++)
            if (surface[id] == 1 && model.CrustByCellId[id] == 0)
                throw new ArgumentException("C1 cannot emerge land on oceanic crust.");
        var areas = new int[coast.Areas.Length];
        for (int id = 0; id < n; id++) if (coast.Labels[id] >= 0) areas[coast.Labels[id]]++;
        var mainLabels = Enumerable.Range(0, areas.Length).Where(label => areas[label] * 100 >= n * 3).ToArray();
        var (main, ambiguous, secondary) = ClassifyAreas(n, areas);
        int land = areas.Sum(), mainLand = main.Sum(); double dominance = land == 0 ? 0 : (double)mainLand / land;
        var (_, oceanAreas) = Components(topology, id => surface[id] == 0);
        var (corticalLabels, _) = Components(topology, id => model.CrustByCellId[id] == 1);
        int oceanTotal = oceanAreas.Sum(); double largestOcean = oceanTotal == 0 ? 0 : (double)oceanAreas.Max() / oceanTotal;
        var fragmentAreas = new int[model.Fragments.Length]; var provinceAreas = new int[model.Provinces.Length];
        int submerged = 0;
        for (int id = 0; id < n; id++) if (model.CrustByCellId[id] == 1)
        { fragmentAreas[model.FragmentByCellId[id]]++; provinceAreas[model.ProvinceByCellId[id]]++; if (surface[id] == 0) submerged++; }

        var distance = new int[n]; Array.Fill(distance, -1); var queue = new Queue<int>();
        for (int id = 0; id < n; id++) if (surface[id] == 0) { distance[id] = 0; queue.Enqueue(id); }
        // Outside N/S is a clipping boundary for width diagnostics, never a wrapped ocean.
        for (int id = 0; id < n; id++) if (surface[id] == 1 && topology.Neighbors(id).Length < 6)
        { distance[id] = 1; queue.Enqueue(id); }
        while (queue.TryDequeue(out int cell)) foreach (int next in topology.Neighbors(cell))
            if (distance[next] < 0) { distance[next] = distance[cell] + 1; queue.Enqueue(next); }
        int width = Math.Max(1, (int)Math.Round(.005 * Math.Sqrt(n)));
        var (erodedLabels, erodedAreas) = Components(topology, id => surface[id] == 1 && distance[id] > width);
        var largeCores = new HashSet<int>[areas.Length];
        var crustSets = new HashSet<int>[areas.Length]; var fragmentSets = new HashSet<int>[areas.Length];
        for (int label = 0; label < areas.Length; label++)
        { largeCores[label] = []; crustSets[label] = []; fragmentSets[label] = []; }
        var clippedMains = new HashSet<int>();
        for (int id = 0; id < n; id++) if (surface[id] == 1)
        {
            int label = coast.Labels[id]; crustSets[label].Add(corticalLabels[id]); fragmentSets[label].Add(model.FragmentByCellId[id]);
            if (erodedLabels[id] >= 0 && erodedAreas[erodedLabels[id]] * 1000 >= n * 3) largeCores[label].Add(erodedLabels[id]);
            if (areas[label] * 100 >= n * 3 && topology.Neighbors(id).Length < 6) clippedMains.Add(label);
        }
        int narrow = mainLabels.Count(label => largeCores[label].Count > 1), shelfPairs = 0;
        for (int a = 0; a < mainLabels.Length; a++) for (int b = a + 1; b < mainLabels.Length; b++)
            if (crustSets[mainLabels[a]].Overlaps(crustSets[mainLabels[b]])) shelfPairs++;
        var directGraph = model.Fragments.Select(_ => new HashSet<int>()).ToArray();
        int direct = 0, regional = 0, rifts = 0, emerged = 0;
        foreach (var evidence in assembly.Evidence)
        {
            if (evidence.Kind == CorticalAssemblyKind.RiftSeparated) { rifts++; continue; }
            if (!evidence.DirectContact) { regional++; continue; }
            direct++; directGraph[evidence.FirstFragmentId].Add(evidence.SecondFragmentId); directGraph[evidence.SecondFragmentId].Add(evidence.FirstFragmentId);
            var edge = terrain.TectonicMotion!.Interactions[evidence.InteractionIndex];
            if (surface[edge.FirstCellId] == 1 && surface[edge.SecondCellId] == 1) emerged++;
        }
        int unsupported = 0;
        foreach (int label in mainLabels)
        {
            var members = fragmentSets[label]; var reached = new HashSet<int>();
            int first = members.Min(); reached.Add(first); queue.Enqueue(first);
            while (queue.TryDequeue(out int fragment)) foreach (int next in directGraph[fragment])
                if (members.Contains(next) && reached.Add(next)) queue.Enqueue(next);
            if (reached.Count != members.Count) unsupported++;
        }
        var reasons = new List<string>(); bool rejected = false;
        if (formation.Deficits.Sum() != 0 || formation.CorticalBudget != fragmentAreas.Sum())
        { rejected = true; reasons.Add("incomplete cortical budget"); }
        if (formation.Solvers.Length != 8 || formation.Solvers.Any(s => !s.Converged))
        { rejected = true; reasons.Add("numerical failure"); }
        if (main.Length is < 2 or > 4) { rejected = true; reasons.Add($"main count {main.Length}, expected 2..4"); }
        if (land == 0 || mainLand * 5 < land * 3) { rejected = true; reasons.Add("main dominance below 60%"); }
        if (ambiguous.Length > 0) reasons.Add($"ambiguous 1..3% components: {ambiguous.Length}");
        if (narrow > 0) reasons.Add($"narrow main connections: {narrow}");
        if (shelfPairs > 0) reasons.Add($"main pairs share continental support: {shelfPairs}");
        if (unsupported > 0) reasons.Add($"composites without direct accretion graph: {unsupported}");
        if (clippedMains.Count > 0) reasons.Add($"main components touch N/S clipping: {clippedMains.Count}");
        return new(rejected ? C1GeographicStatus.Rejected : reasons.Count > 0 ? C1GeographicStatus.ReviewRequired : C1GeographicStatus.Accepted,
            reasons.ToArray(), main, ambiguous, secondary, dominance, land, fragmentAreas.Sum(), submerged, fragmentAreas, provinceAreas,
            oceanAreas.Length, largestOcean, narrow, shelfPairs, unsupported, direct, regional, rifts, emerged, coast);
    }

    public static (int[] Main, int[] Ambiguous, int[] Secondary) ClassifyAreas(int cellCount, IReadOnlyList<int> areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        if (cellCount <= 0 || areas.Any(area => area <= 0) || areas.Sum(area => (long)area) > cellCount)
            throw new ArgumentException("Classification requires real component areas within the map.");
        return (areas.Where(area => (long)area * 100 >= (long)cellCount * 3).OrderDescending().ToArray(),
            areas.Where(area => (long)area * 100 >= cellCount && (long)area * 100 < (long)cellCount * 3).OrderDescending().ToArray(),
            areas.Where(area => (long)area * 100 < cellCount).OrderDescending().ToArray());
    }

    private static (int[] Labels, int[] Areas) Components(HexTopology topology, Func<int, bool> includes)
    {
        var labels = new int[topology.Count]; Array.Fill(labels, -1); var sizes = new List<int>(); var queue = new Queue<int>();
        for (int start = 0; start < topology.Count; start++)
        {
            if (labels[start] >= 0 || !includes(start)) continue;
            int label = sizes.Count, area = 0; labels[start] = label; queue.Enqueue(start);
            while (queue.TryDequeue(out int cell))
            {
                area++;
                foreach (int next in topology.Neighbors(cell)) if (labels[next] < 0 && includes(next))
                { labels[next] = label; queue.Enqueue(next); }
            }
            sizes.Add(area);
        }
        return (labels, sizes.ToArray());
    }
}
