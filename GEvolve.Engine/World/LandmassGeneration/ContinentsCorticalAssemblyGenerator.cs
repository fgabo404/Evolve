using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Non-persisted provenance for a direct or regionally inferred tectonic link.</summary>
public readonly record struct CorticalAssemblyEvidence(int FirstFragmentId, int SecondFragmentId,
    CorticalAssemblyKind Kind, int InteractionIndex, int OceanGapHexes, bool DirectContact);

/// <summary>
 /// Diagnostics over unordered fragment pairs. The extended window measures evidence
 /// but NEVER authorizes a link outside AcceptedGapHexes.
 /// </summary>
public readonly record struct CorticalAssemblyDiagnostics(int FragmentPairs,
    int PairsObservedNearOfficialBoundaries, int PairsWithinAcceptedDistance,
    int PairsWithNormalEvidence, int RejectedByDistance,
    int RejectedWithoutNormalEvidence, int UnobservedWithinDiagnosticHorizon,
    int AcceptedGapHexes, int DiagnosticGapHexes,
    int MinimumAcceptedEvidenceGap, int MinimumRejectedEvidenceGap);

/// <summary>Derived accretion system; rift-separated pairs never merge systems.</summary>
public readonly record struct CorticalAssemblySystem(int Id, int[] FragmentIds, byte[] PlateIds);

/// <summary>H05-B2 result: unchanged cortical cells/cores plus evidence-backed links.</summary>
public sealed record ContinentsCorticalAssemblyResult(SharedCorticalModel Model,
    CorticalAssemblyEvidence[] Evidence, CorticalAssemblySystem[] Systems)
{
    public int IsolatedFragments => Systems.Count(system => system.FragmentIds.Length == 1);
    public int AssembledFragments => Model.Fragments.Length - IsolatedFragments;
    public CorticalAssemblyDiagnostics Diagnostics { get; init; } = default!;
}

/// <summary>
/// Infers regional assembly from actual plate interactions near two existing cortical blocks.
/// It never produces crust, a surface or elevations; regional proximity is NOT direct contact.
/// </summary>
public sealed class ContinentsCorticalAssemblyGenerator
{
    public const string Version = "continents-assembly-1";
    private const double NormalThreshold = .5;
    private const double ShearTolerance = .4;

    public ContinentsCorticalAssemblyResult Assemble(HexTopology topology,
        LandmassConfiguration configuration, ReadOnlySpan<byte> plateByCellId,
        TectonicMotionState motion, SharedCorticalModel cortex)
    {
        return AssembleAtRadius(topology, configuration, plateByCellId, motion, cortex, 4);
    }

    /// <summary>
    /// Diagnostic-only alternative. Does not change Assemble() or persist a calibration.
    /// Allowed multipliers are 4 (current), 5, 6 and 8.
    /// </summary>
    public ContinentsCorticalAssemblyResult EvaluateExperimentalRadius(HexTopology topology,
        LandmassConfiguration configuration, ReadOnlySpan<byte> plateByCellId,
        TectonicMotionState motion, SharedCorticalModel cortex, int radiusMultiplier)
    {
        if (radiusMultiplier is not (4 or 5 or 6 or 8))
            throw new ArgumentOutOfRangeException(nameof(radiusMultiplier));
        return AssembleAtRadius(topology, configuration, plateByCellId, motion, cortex,
            radiusMultiplier);
    }

    private ContinentsCorticalAssemblyResult AssembleAtRadius(HexTopology topology,
        LandmassConfiguration configuration, ReadOnlySpan<byte> plateByCellId,
        TectonicMotionState motion, SharedCorticalModel cortex, int radiusMultiplier)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(motion);
        ArgumentNullException.ThrowIfNull(cortex);
        if (configuration.Type != LandmassType.Continents ||
            configuration != LandmassProfiles.Create(configuration.Size,
                configuration.Type, configuration.Seed) ||
            topology.Grid.Definition != HexGridDefinitions.Get(configuration.Size) ||
            motion.Version != TectonicMotionGenerator.Version ||
            motion.Interactions is null || !double.IsFinite(motion.InfluenceRadiusHexes) ||
            motion.InfluenceRadiusHexes <= 0)
            throw new ArgumentException("Valid official Continents tectonics are required.");

        SharedCorticalModelValidator.Validate(topology, cortex, plateByCellId);
        int count = topology.Count, fragments = cortex.Fragments.Length;
        byte[] plates = plateByCellId.ToArray();

        // No independent tectonics: verify every official inter-plate edge exactly.
        var edges = new Dictionary<ulong, int>(motion.Interactions.Length);
        for (int index = 0; index < motion.Interactions.Length; index++)
        {
            var edge = motion.Interactions[index];
            if (edge.FirstCellId < 0 || edge.SecondCellId >= count ||
                edge.FirstCellId >= edge.SecondCellId ||
                plates[edge.FirstCellId] != edge.FirstPlateId ||
                plates[edge.SecondCellId] != edge.SecondPlateId ||
                edge.FirstPlateId == edge.SecondPlateId ||
                !topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) ||
                !double.IsFinite(edge.Convergence) || !double.IsFinite(edge.Divergence) ||
                !double.IsFinite(edge.Shear) ||
                edge.Convergence < 0 || edge.Divergence < 0 || edge.Shear < 0 ||
                !edges.TryAdd(Key(edge.FirstCellId, edge.SecondCellId), index))
                throw new ArgumentException("Invalid official contact.");
        }
        int verified = 0;
        for (int id = 0; id < count; id++)
            foreach (int neighbor in topology.Neighbors(id))
                if (id < neighbor && plates[id] != plates[neighbor])
                {
                    if (!edges.ContainsKey(Key(id, neighbor)))
                        throw new ArgumentException("Missing official inter-plate contact.");
                    verified++;
                }
        if (verified != edges.Count)
            throw new ArgumentException("Official contact count differs from the plate topology.");

        // Multi-source geodesics, restricted to each tectonic plate. A candidate at a
        // boundary must have cortical material on its OWN side of that same boundary.
        // This prevents linking remote fragments merely because they share a plate ID.
        int range = Math.Max(2, (int)Math.Ceiling(motion.InfluenceRadiusHexes * radiusMultiplier));
        // Keep a comparable diagnostic horizon for each alternative; it cannot authorize links.
        int diagnosticRange = checked(range * 2);
        int[][] distances = new int[fragments][];
        int[] queue = new int[count];
        for (int fragment = 0; fragment < fragments; fragment++)
        {
            var distance = new int[count];
            Array.Fill(distance, int.MaxValue);
            int head = 0, tail = 0;
            for (int id = 0; id < count; id++)
                if (cortex.FragmentByCellId[id] == fragment)
                {
                    distance[id] = 0;
                    queue[tail++] = id;
                }
            while (head < tail)
            {
                int id = queue[head++];
                if (distance[id] >= diagnosticRange) continue;
                foreach (int neighbor in topology.Neighbors(id))
                    if (plates[id] == plates[neighbor] &&
                        distance[neighbor] == int.MaxValue &&
                        (cortex.FragmentByCellId[neighbor] == -1 ||
                         cortex.FragmentByCellId[neighbor] == fragment))
                    {
                        distance[neighbor] = distance[id] + 1;
                        queue[tail++] = neighbor;
                    }
            }
            distances[fragment] = distance;
        }

        // Pair-level diagnostic counters are independent of the number of
        // boundary edges: dozens of interactions for one pair count once.
        var closestAny = new int[fragments, fragments];
        var closestNormal = new int[fragments, fragments];
        for (int a = 0; a < fragments; a++)
        for (int b = 0; b < fragments; b++)
            closestAny[a, b] = closestNormal[a, b] = int.MaxValue;

        // Keep the closest justified local interaction per unordered pair.
        // A larger observation horizon never makes a relationship acceptable.
        var best = new SortedDictionary<(int, int), (CorticalAssemblyEvidence Evidence,
            int Gap, double Intensity)>();
        for (int index = 0; index < motion.Interactions.Length; index++)
        {
            var edge = motion.Interactions[index];
            CorticalAssemblyKind? kind = null;
            double strength = 0;
            if (edge.Convergence >= NormalThreshold &&
                edge.Convergence > edge.Divergence &&
                edge.Convergence >= ShearTolerance * edge.Shear)
            {
                kind = CorticalAssemblyKind.Accreted;
                strength = edge.Convergence;
            }
            else if (edge.Divergence >= NormalThreshold &&
                edge.Divergence > edge.Convergence &&
                edge.Divergence >= ShearTolerance * edge.Shear)
            {
                kind = CorticalAssemblyKind.RiftSeparated;
                strength = edge.Divergence;
            }

            ExamineSides(edge.FirstCellId, edge.SecondCellId);
            ExamineSides(edge.SecondCellId, edge.FirstCellId);

            void ExamineSides(int left, int right)
            {
                for (int a = 0; a < fragments; a++)
                {
                    int d1 = distances[a][left];
                    if (d1 > diagnosticRange) continue;
                    for (int b = 0; b < fragments; b++)
                    {
                        if (a == b) continue;
                        int d2 = distances[b][right];
                        if (d2 > diagnosticRange) continue;
                        int gap = d1 + d2;
                        if (gap > diagnosticRange) continue;
                        int first = Math.Min(a, b), second = Math.Max(a, b);
                        if (gap < closestAny[first, second])
                            closestAny[first, second] = gap;
                        if (kind is null) continue;
                        if (gap < closestNormal[first, second])
                            closestNormal[first, second] = gap;
                        if (gap > range) continue;
                        var pair = (first, second);
                        var evidence = new CorticalAssemblyEvidence(first, second,
                            kind.Value, index, gap, gap == 0);
                        if (!best.TryGetValue(pair, out var prior) ||
                            gap < prior.Gap ||
                            (gap == prior.Gap && strength > prior.Intensity))
                            best[pair] = (evidence, gap, strength);
                    }
                }
            }
        }
        int observed = 0, within = 0, withNormal = 0, tooFar = 0, noNormal = 0;
        int closestAccepted = int.MaxValue, closestRejected = int.MaxValue;
        for (int a = 0; a < fragments; a++)
        for (int b = a + 1; b < fragments; b++)
        {
            int any = closestAny[a, b], normal = closestNormal[a, b];
            if (any != int.MaxValue) observed++;
            if (any <= range) within++;
            if (normal != int.MaxValue)
            {
                withNormal++;
                if (normal > range)
                {
                    tooFar++;
                    closestRejected = Math.Min(closestRejected, normal);
                }
                else closestAccepted = Math.Min(closestAccepted, normal);
            }
            if (any <= range && normal > range) noNormal++;
        }
        int pairs = fragments * (fragments - 1) / 2;
        var diagnostics = new CorticalAssemblyDiagnostics(pairs, observed, within,
            withNormal, tooFar, noNormal, pairs - observed, range, diagnosticRange,
            closestAccepted, closestRejected);

        var evidenceArray = best.Values.Select(value => value.Evidence).ToArray();
        var links = evidenceArray.Select(e => new CorticalAssemblyLink(e.FirstFragmentId,
            e.SecondFragmentId, e.Kind)).ToArray();
        var assembled = cortex with { AssemblyLinks = links };
        SharedCorticalModelValidator.Validate(topology, assembled, plates);
        return new ContinentsCorticalAssemblyResult(assembled, evidenceArray,
            BuildSystems(assembled, plates)) { Diagnostics = diagnostics };
    }

    private static CorticalAssemblySystem[] BuildSystems(SharedCorticalModel cortex, byte[] plates)
    {
        int n = cortex.Fragments.Length;
        var parent = Enumerable.Range(0, n).ToArray();
        int Root(int id)
        {
            while (parent[id] != id) id = parent[id] = parent[parent[id]];
            return id;
        }
        foreach (var link in cortex.AssemblyLinks)
            if (link.Kind == CorticalAssemblyKind.Accreted)
            {
                int first = Root(link.FirstFragmentId), second = Root(link.SecondFragmentId);
                if (first != second) parent[Math.Max(first, second)] = Math.Min(first, second);
            }

        var groups = new SortedDictionary<int, List<int>>();
        for (int fragment = 0; fragment < n; fragment++)
        {
            int root = Root(fragment);
            if (!groups.TryGetValue(root, out var members))
                groups[root] = members = [];
            members.Add(fragment);
        }
        var plateSets = new HashSet<byte>[n];
        for (int id = 0; id < n; id++) plateSets[id] = [];
        for (int cell = 0; cell < cortex.FragmentByCellId.Length; cell++)
        {
            int fragment = cortex.FragmentByCellId[cell];
            if (fragment >= 0) plateSets[fragment].Add(plates[cell]);
        }
        return groups.Select((group, index) => new CorticalAssemblySystem(index,
            group.Value.ToArray(),
            group.Value.SelectMany(id => plateSets[id]).Distinct().Order().ToArray())).ToArray();
    }

    private static ulong Key(int first, int second) =>
        ((ulong)(uint)Math.Min(first, second) << 32) | (uint)Math.Max(first, second);
}
