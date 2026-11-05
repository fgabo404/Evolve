using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

// Ephemeral diagnostics, never a persisted cortical identity or a new B2 link.
public readonly record struct RegionalEmergenceSource(int LinkIndex, int InteractionIndex,
    int ParentInteractionIndex, int FirstSourceCellId, int SecondSourceCellId, int OceanGapHexes);
public readonly record struct EmergenceBudget(int FragmentId, double PositiveA, double PositiveB,
    double NegativeA, double NegativeB, string PositiveStatus, string NegativeStatus);
public sealed record ContinentsSurfaceComparison(byte[] LocalizedSurface, byte[] RegionalSurface,
    double[] BasePotential, double[] LocalizedUplift, double[] LocalizedSubsidence,
    double[] RegionalUplift, double[] RegionalSubsidence,
    RegionalEmergenceSource[] Sources, EmergenceBudget[] Budgets);

/// <summary>Distributes the existing A budgets; no new stress, noise, crust or links.</summary>
internal static class RegionalEmergenceExperiment
{
    internal sealed record Result(double[] Uplift, double[] Subsidence,
        RegionalEmergenceSource[] Sources, EmergenceBudget[] Budgets);

    internal static Result Distribute(HexTopology topology, byte[] plates, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly, int range, double[] localizedUplift,
        double[] localizedSubsidence, double positiveCap, double negativeCap)
    {
        var cortex = assembly.Model;
        int count = topology.Count, fragments = cortex.Fragments.Length;
        var byCell = new List<int>?[count];
        var unique = new HashSet<(int, int)>();
        for (int index = 0; index < motion.Interactions.Length; index++)
        {
            var edge = motion.Interactions[index];
            if ((uint)edge.FirstCellId >= (uint)count || (uint)edge.SecondCellId >= (uint)count ||
                edge.FirstCellId >= edge.SecondCellId || !unique.Add((edge.FirstCellId, edge.SecondCellId)) ||
                plates[edge.FirstCellId] != edge.FirstPlateId || plates[edge.SecondCellId] != edge.SecondPlateId ||
                edge.FirstPlateId == edge.SecondPlateId ||
                !topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) ||
                !double.IsFinite(edge.Convergence) || !double.IsFinite(edge.Divergence) ||
                !double.IsFinite(edge.Shear) || edge.Convergence < 0 || edge.Divergence < 0 || edge.Shear < 0)
                throw new ArgumentException("Regional sources require unique real official contacts.");
            (byCell[edge.FirstCellId] ??= []).Add(index);
            (byCell[edge.SecondCellId] ??= []).Add(index);
        }

        var queue = new int[count];
        // Same admissible distances as B2, but only the accepted 4r horizon.
        var distances = new int[fragments][];
        for (int f = 0; f < fragments; f++)
        {
            int[] d = distances[f] = new int[count];
            Array.Fill(d, -1);
            int read = 0, write = 0;
            for (int id = 0; id < count; id++)
                if (cortex.FragmentByCellId[id] == f) { d[id] = 0; queue[write++] = id; }
            Flood(f, d, ref read, ref write);
        }
        var positiveSeeds = Enumerable.Range(0, fragments).Select(_ => new HashSet<int>()).ToArray();
        var negativeSeeds = Enumerable.Range(0, fragments).Select(_ => new HashSet<int>()).ToArray();
        var sources = new List<RegionalEmergenceSource>();
        for (int linkIndex = 0; linkIndex < assembly.Evidence.Length; linkIndex++)
        {
            var evidence = assembly.Evidence[linkIndex];
            var representative = motion.Interactions[evidence.InteractionIndex];
            var visited = new HashSet<int> { evidence.InteractionIndex };
            var pending = new Queue<(int Edge, int Parent)>();
            pending.Enqueue((evidence.InteractionIndex, -1));
            while (pending.TryDequeue(out var item))
            {
                var edge = motion.Interactions[item.Edge];
                if (!SamePlates(edge, representative) || !Supported(edge, evidence.Kind)) continue;
                int forward = Gap(edge.FirstCellId, edge.SecondCellId);
                int reverse = Gap(edge.SecondCellId, edge.FirstCellId);
                int gap = Math.Min(forward, reverse);
                if (gap > range) continue;
                int first = forward <= reverse ? edge.FirstCellId : edge.SecondCellId;
                int second = forward <= reverse ? edge.SecondCellId : edge.FirstCellId;
                sources.Add(new(linkIndex, item.Edge, item.Parent, first, second, gap));
                var seeds = evidence.Kind == CorticalAssemblyKind.Accreted ? positiveSeeds : negativeSeeds;
                seeds[evidence.FirstFragmentId].Add(first);
                seeds[evidence.SecondFragmentId].Add(second);
                // Connected admissible segment: neither quiet/opposite edges nor a
                // third fragment can connect otherwise independent source segments.
                foreach (int endpoint in new[] { edge.FirstCellId, edge.SecondCellId })
                    foreach (int next in byCell[endpoint]!)
                        if (visited.Add(next)) pending.Enqueue((next, item.Edge));
            }

            int Gap(int first, int second)
            {
                int a = distances[evidence.FirstFragmentId][first];
                int b = distances[evidence.SecondFragmentId][second];
                return a < 0 || b < 0 ? int.MaxValue : a + b;
            }
        }

        var uplift = new double[count];
        var subsidence = new double[count];
        var budgets = new EmergenceBudget[fragments];
        var work = new int[count];
        for (int f = 0; f < fragments; f++)
        {
            var p = DistributeSign(f, positiveSeeds[f], localizedUplift, uplift, positiveCap);
            var n = DistributeSign(f, negativeSeeds[f], localizedSubsidence, subsidence, negativeCap);
            budgets[f] = new(f, p.Target, p.Actual, n.Target, n.Actual, p.Status, n.Status);
        }
        return new(uplift, subsidence, sources.ToArray(), budgets);

        (double Target, double Actual, string Status) DistributeSign(int fragment, HashSet<int> seeds,
            double[] original, double[] output, double cap)
        {
            Array.Fill(work, -1);
            int read = 0, write = 0;
            foreach (int seed in seeds.Order()) { work[seed] = 0; queue[write++] = seed; }
            Flood(fragment, work, ref read, ref write);
            double target = 0, available = 0;
            for (int id = 0; id < count; id++)
                if (cortex.FragmentByCellId[id] == fragment)
                {
                    target += original[id];
                    if (work[id] >= 0)
                    {
                        output[id] = cap * (1 - work[id] / (range + 1.0));
                        available += output[id];
                    }
                }
            // Representative sources remain in the set: normally available >= target.
            // Never exceed the kernel/cell cap to force a budget equality.
            double scale = available == 0 ? 0 : Math.Min(1, target / available);
            double actual = 0;
            for (int id = 0; id < count; id++)
                if (cortex.FragmentByCellId[id] == fragment)
                { output[id] *= scale; actual += output[id]; }
            string status = target == 0 ? "zero budget" : available == 0 ? "no admissible source" :
                available + 1e-10 * Math.Max(1, target) < target ? "insufficient kernel capacity" : "conserved";
            return (target, actual, status);
        }

        void Flood(int fragment, int[] d, ref int read, ref int write)
        {
            while (read < write)
            {
                int cell = queue[read++];
                if (d[cell] >= range) continue;
                foreach (int neighbor in topology.Neighbors(cell))
                    if (d[neighbor] < 0 && plates[neighbor] == plates[cell] &&
                        (cortex.FragmentByCellId[neighbor] == -1 || cortex.FragmentByCellId[neighbor] == fragment))
                    { d[neighbor] = d[cell] + 1; queue[write++] = neighbor; }
            }
        }
    }

    private static bool SamePlates(PlateInteraction a, PlateInteraction b) =>
        a.FirstPlateId == b.FirstPlateId && a.SecondPlateId == b.SecondPlateId ||
        a.FirstPlateId == b.SecondPlateId && a.SecondPlateId == b.FirstPlateId;
    private static bool Supported(PlateInteraction edge, CorticalAssemblyKind kind) => kind == CorticalAssemblyKind.Accreted
        ? edge.Convergence >= .5 && edge.Convergence > edge.Divergence && edge.Convergence >= .4 * edge.Shear
        : edge.Divergence >= .5 && edge.Divergence > edge.Convergence && edge.Divergence >= .4 * edge.Shear;
}
