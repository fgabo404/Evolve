using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

public static class ContinentsCorticalAssemblyTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        var fragments = new ContinentsCorticalFragmentGenerator();
        var assembly = new ContinentsCorticalAssemblyGenerator();
        foreach (var size in Enum.GetValues<GridSize>())
        {
            var fingerprints = new HashSet<string>();
            foreach (int seed in new[] { 2026, 42, -731 })
            {
                var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
                var world = service.Generate(config);
                var topology = new HexTopology(world.Grid);
                byte[] plates = world.Landmass.PlateByCellId!;
                var motion = world.Landmass.TectonicMotion!;
                byte[] beforePlates = (byte[])plates.Clone();
                string beforeMotion = JsonSerializer.Serialize(motion);
                int top = topology.Id(5, 0), bottom = topology.Id(5, topology.Rows - 1);
                Check(!topology.Neighbors(top).Contains(bottom),
                    "The assembly topology never wraps north/south");
                foreach (var seam in motion.Interactions.Where(edge =>
                    Math.Abs(edge.FirstCellId % topology.Columns -
                        edge.SecondCellId % topology.Columns) == topology.Columns - 1))
                    Check(topology.Neighbors(seam.FirstCellId).Contains(seam.SecondCellId),
                        "Official east/west seam contacts remain real hexagonal neighbors");
                var cortex = fragments.Generate(topology, config, plates, motion);
                var crustBefore = (byte[])cortex.CrustByCellId.Clone();
                var fragmentBefore = (int[])cortex.FragmentByCellId.Clone();
                var provinceBefore = (int[])cortex.ProvinceByCellId.Clone();
                var affinityBefore = (float[])cortex.EmergenceAffinityByCellId.Clone();
                var stopwatch = Stopwatch.StartNew();
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var result = assembly.Assemble(topology, config, plates, motion, cortex);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                stopwatch.Stop();
                SharedCorticalModelValidator.Validate(topology, result.Model, plates);
                Check(result.Model.CrustByCellId.SequenceEqual(crustBefore) &&
                    result.Model.FragmentByCellId.SequenceEqual(fragmentBefore) &&
                    result.Model.ProvinceByCellId.SequenceEqual(provinceBefore) &&
                    result.Model.EmergenceAffinityByCellId.SequenceEqual(affinityBefore) &&
                    result.Model.Fragments.SequenceEqual(cortex.Fragments) &&
                    result.Model.Provinces.SequenceEqual(cortex.Provinces),
                    "B2 preserves every B1 cell, province and cortical core");
                Check(plates.SequenceEqual(beforePlates) && plates.Distinct().Count() == 16 &&
                    JsonSerializer.Serialize(motion) == beforeMotion &&
                    world.Landmass.SurfaceGenerationPending &&
                    world.Landmass.SurfaceByCellId.Length == 0 &&
                    world.Landmass.ElevationByCellId is null,
                    "Official tectonics and surface-pending state remain unchanged");
                var audit = result.Diagnostics;
                Check(audit.FragmentPairs == cortex.Fragments.Length *
                        (cortex.Fragments.Length - 1) / 2 &&
                    audit.DiagnosticGapHexes == 2 * audit.AcceptedGapHexes &&
                    audit.PairsObservedNearOfficialBoundaries +
                        audit.UnobservedWithinDiagnosticHorizon == audit.FragmentPairs &&
                    audit.PairsWithNormalEvidence == result.Evidence.Length +
                        audit.RejectedByDistance &&
                    audit.PairsWithinAcceptedDistance <= audit.PairsObservedNearOfficialBoundaries &&
                    audit.RejectedWithoutNormalEvidence <= audit.PairsWithinAcceptedDistance &&
                    (result.Evidence.Length == 0 ||
                        audit.MinimumAcceptedEvidenceGap <= audit.AcceptedGapHexes) &&
                    (audit.RejectedByDistance == 0 ||
                        audit.MinimumRejectedEvidenceGap > audit.AcceptedGapHexes),
                    "Pair-level rejection diagnostics preserve the acceptance boundary");
                Check(result.Evidence.Length == result.Model.AssemblyLinks.Length &&
                    result.Evidence.Select(e => (e.FirstFragmentId, e.SecondFragmentId)).Distinct().Count()
                        == result.Evidence.Length &&
                    result.Evidence.SequenceEqual(result.Evidence.OrderBy(e => e.FirstFragmentId)
                        .ThenBy(e => e.SecondFragmentId)),
                    "Canonical unique evidence corresponds one-to-one to cortical links");

                int accretions = 0, rifts = 0;
                var convergentUsed = new HashSet<int>();
                var divergentUsed = new HashSet<int>();
                foreach (var evidence in result.Evidence)
                {
                    var link = result.Model.AssemblyLinks[accretions + rifts];
                    Check(link.FirstFragmentId == evidence.FirstFragmentId &&
                        link.SecondFragmentId == evidence.SecondFragmentId &&
                        link.Kind == evidence.Kind &&
                        evidence.FirstFragmentId >= 0 &&
                        evidence.FirstFragmentId < evidence.SecondFragmentId &&
                        evidence.SecondFragmentId < result.Model.Fragments.Length &&
                        (uint)evidence.InteractionIndex < (uint)motion.Interactions.Length,
                        "Evidence and cortical link preserve canonical references");
                    var edge = motion.Interactions[evidence.InteractionIndex];
                    Check(plates[edge.FirstCellId] != plates[edge.SecondCellId] &&
                        topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId),
                        "Every inferred relationship is grounded in an official adjacent contact");

                    int firstToA = DistanceOnPlate(topology, result.Model, plates,
                        evidence.FirstFragmentId, edge.FirstCellId);
                    int secondToB = DistanceOnPlate(topology, result.Model, plates,
                        evidence.SecondFragmentId, edge.SecondCellId);
                    int firstToB = DistanceOnPlate(topology, result.Model, plates,
                        evidence.FirstFragmentId, edge.SecondCellId);
                    int secondToA = DistanceOnPlate(topology, result.Model, plates,
                        evidence.SecondFragmentId, edge.FirstCellId);
                    int actual = Math.Min(Sum(firstToA, secondToB), Sum(firstToB, secondToA));
                    Check(actual == evidence.OceanGapHexes &&
                        evidence.DirectContact == (actual == 0),
                        "Reported gap is a real within-plate distance to the official contact");
                    if (evidence.Kind == CorticalAssemblyKind.Accreted)
                    {
                        Check(edge.Convergence >= .5 && edge.Convergence > edge.Divergence &&
                            edge.Convergence >= .4 * edge.Shear, "Accretion requires dominant convergence");
                        accretions++; convergentUsed.Add(evidence.InteractionIndex);
                    }
                    else
                    {
                        Check(evidence.Kind == CorticalAssemblyKind.RiftSeparated &&
                            edge.Divergence >= .5 && edge.Divergence > edge.Convergence &&
                            edge.Divergence >= .4 * edge.Shear, "Rifting requires dominant divergence");
                        rifts++; divergentUsed.Add(evidence.InteractionIndex);
                    }
                }

                Check(result.Systems.Sum(system => system.FragmentIds.Length) == cortex.Fragments.Length &&
                    result.Systems.SelectMany(system => system.FragmentIds).Distinct().Count() ==
                        cortex.Fragments.Length &&
                    result.IsolatedFragments + result.AssembledFragments == cortex.Fragments.Length,
                    "Accreted systems partition fragment IDs with isolated blocks allowed");
                // A rift link must never by itself merge the systems containing its endpoints.
                foreach (var link in result.Model.AssemblyLinks)
                    if (link.Kind == CorticalAssemblyKind.RiftSeparated &&
                        !result.Model.AssemblyLinks.Any(other =>
                            other.Kind == CorticalAssemblyKind.Accreted))
                        Check(result.Systems.Single(s => s.FragmentIds.Contains(link.FirstFragmentId)).Id !=
                            result.Systems.Single(s => s.FragmentIds.Contains(link.SecondFragmentId)).Id,
                            "Rifting cannot assemble regional systems");
                Check(result.Systems.All(system =>
                    system.PlateIds.SequenceEqual(system.PlateIds.Order()) &&
                    system.FragmentIds.Length >= 1),
                    "Derived system labels have stable sorted plate provenance");
                var repeat = assembly.Assemble(topology, config, plates, motion, cortex);
                Check(result.Model.AssemblyLinks.SequenceEqual(repeat.Model.AssemblyLinks) &&
                    result.Evidence.SequenceEqual(repeat.Evidence) &&
                    result.Diagnostics == repeat.Diagnostics &&
                    JsonSerializer.Serialize(result.Systems) == JsonSerializer.Serialize(repeat.Systems),
                    "B2 evidence, links and systems are exactly deterministic");
                var persisted = SharedCorticalPersistence.ToState(result.Model);
                var restored = SharedCorticalPersistence.FromState(persisted);
                Check(restored.AssemblyLinks.SequenceEqual(result.Model.AssemblyLinks) &&
                    restored.CrustByCellId.SequenceEqual(result.Model.CrustByCellId),
                    "B2 links persist losslessly under shared-cortex-1");

                // Experimental 4r, 5r, 6r, 8r use the SAME B1 cortex and official motion.
                // No multiplier is persisted or adopted by the default API.
                var variants = new List<(int Multiplier, ContinentsCorticalAssemblyResult Result)>();
                foreach (int multiplier in new[] { 4, 5, 6, 8 })
                {
                    var experimentTimer = Stopwatch.StartNew();
                    long experimentAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                    var candidate = assembly.EvaluateExperimentalRadius(
                        topology, config, plates, motion, cortex, multiplier);
                    long experimentAllocated = GC.GetAllocatedBytesForCurrentThread() -
                        experimentAllocationStart;
                    experimentTimer.Stop();
                    SharedCorticalModelValidator.Validate(topology, candidate.Model, plates);
                    Check(candidate.Model.CrustByCellId.SequenceEqual(crustBefore) &&
                        candidate.Model.FragmentByCellId.SequenceEqual(fragmentBefore) &&
                        candidate.Model.ProvinceByCellId.SequenceEqual(provinceBefore) &&
                        candidate.Model.EmergenceAffinityByCellId.SequenceEqual(affinityBefore) &&
                        candidate.Model.Fragments.SequenceEqual(cortex.Fragments) &&
                        candidate.Model.Provinces.SequenceEqual(cortex.Provinces),
                        "All radius experiments preserve identical B1 cortex and provenance");
                    Check(candidate.Evidence.Length == candidate.Model.AssemblyLinks.Length &&
                        candidate.Diagnostics.PairsWithNormalEvidence ==
                            candidate.Evidence.Length + candidate.Diagnostics.RejectedByDistance,
                        "Experimental diagnostics remain internally consistent");
                    foreach (var evidence in candidate.Evidence)
                    {
                        var edge = motion.Interactions[evidence.InteractionIndex];
                        int aToFirst = DistanceOnPlate(topology, candidate.Model, plates,
                            evidence.FirstFragmentId, edge.FirstCellId);
                        int bToSecond = DistanceOnPlate(topology, candidate.Model, plates,
                            evidence.SecondFragmentId, edge.SecondCellId);
                        int aToSecond = DistanceOnPlate(topology, candidate.Model, plates,
                            evidence.FirstFragmentId, edge.SecondCellId);
                        int bToFirst = DistanceOnPlate(topology, candidate.Model, plates,
                            evidence.SecondFragmentId, edge.FirstCellId);
                        int gap = Math.Min(Sum(aToFirst, bToSecond),
                            Sum(aToSecond, bToFirst));
                        Check(gap == evidence.OceanGapHexes &&
                            gap <= candidate.Diagnostics.AcceptedGapHexes &&
                            evidence.DirectContact == (gap == 0),
                            "Experimental links follow real plate-local paths without third fragments");
                        Check(evidence.Kind == CorticalAssemblyKind.Accreted
                                ? edge.Convergence >= .5 &&
                                    edge.Convergence > edge.Divergence &&
                                    edge.Convergence >= .4 * edge.Shear
                                : evidence.Kind == CorticalAssemblyKind.RiftSeparated &&
                                    edge.Divergence >= .5 &&
                                    edge.Divergence > edge.Convergence &&
                                    edge.Divergence >= .4 * edge.Shear,
                            "Every experimental link retains actual tectonic evidence");
                    }
                    variants.Add((multiplier, candidate));
                    int acc = candidate.Evidence.Count(e => e.Kind == CorticalAssemblyKind.Accreted);
                    int experimentalRifts = candidate.Evidence.Count(e => e.Kind == CorticalAssemblyKind.RiftSeparated);
                    string gaps = candidate.Evidence.Length == 0 ? "none" :
                        string.Join(",", candidate.Evidence.Select(e => e.OceanGapHexes).Order());
                    Console.WriteLine($"H05-B2 RADIUS {size}/{seed} {multiplier}r: " +
                        $"acceptedPairs={candidate.Evidence.Length}, accretions={acc}, " +
                        $"separations={experimentalRifts}, systems={candidate.Systems.Length}, " +
                        $"isolated={candidate.IsolatedFragments}, " +
                        $"largestSystem={candidate.Systems.Max(x => x.FragmentIds.Length)}, " +
                        $"gaps=[{gaps}], withinRadius={candidate.Diagnostics.PairsWithinAcceptedDistance}, " +
                        $"rejectedDistance={candidate.Diagnostics.RejectedByDistance}, " +
                        $"ms={experimentTimer.ElapsedMilliseconds}, " +
                        $"MiB={experimentAllocated / 1048576.0:F1}");
                }
                Check(variants[0].Result.Model.AssemblyLinks.SequenceEqual(result.Model.AssemblyLinks) &&
                    variants[0].Result.Evidence.SequenceEqual(result.Evidence) &&
                    variants[0].Result.Diagnostics == result.Diagnostics,
                    "Experimental 4r is bit-for-bit equivalent to the operational 4r");
                for (int index = 1; index < variants.Count; index++)
                {
                    var earlier = variants[index - 1].Result;
                    var later = variants[index].Result;
                    Check(earlier.Model.AssemblyLinks.All(link =>
                        later.Model.AssemblyLinks.Any(next =>
                            next.FirstFragmentId == link.FirstFragmentId &&
                            next.SecondFragmentId == link.SecondFragmentId)),
                        "Increasing the radius cannot erase an eligible fragment pair");
                }

                fingerprints.Add(Convert.ToHexString(SHA256.HashData(
                    result.Model.CrustByCellId.Concat(result.Model.AssemblyLinks.SelectMany(l =>
                        new byte[] { (byte)l.FirstFragmentId, (byte)l.SecondFragmentId, (byte)l.Kind }))
                    .ToArray())));
                Check(stopwatch.ElapsedMilliseconds < 120000 && allocated < 500_000_000,
                    "B2 runtime and allocated bytes stay bounded");
                Console.WriteLine($"H05-B2 {size}/{seed}: fragments={cortex.Fragments.Length}, " +
                    $"provinces={cortex.Provinces.Length}, systems={result.Systems.Length}, " +
                    $"isolated={result.IsolatedFragments}, assembled={result.AssembledFragments}, " +
                    $"accreted={accretions}, rift={rifts}, convergentContacts={convergentUsed.Count}, " +
                    $"divergentContacts={divergentUsed.Count}, multiPlateSystems=" +
                    $"{result.Systems.Count(s => s.PlateIds.Length > 1)}, " +
                    $"cortex={cortex.CrustByCellId.Count(c => c == 1) * 100.0 / topology.Count:F1}%, " +
                    $"pairs={audit.FragmentPairs}, observed={audit.PairsObservedNearOfficialBoundaries}, " +
                    $"within={audit.PairsWithinAcceptedDistance}, tectonicCandidates={audit.PairsWithNormalEvidence}, " +
                    $"rejectedDistance={audit.RejectedByDistance}, " +
                    $"rejectedNoNormal={audit.RejectedWithoutNormalEvidence}, " +
                    $"outsideHorizon={audit.UnobservedWithinDiagnosticHorizon}, " +
                    $"radius={audit.AcceptedGapHexes}/{audit.DiagnosticGapHexes}, " +
                    $"minAcceptedGap={audit.MinimumAcceptedEvidenceGap}, " +
                    $"minRejectedGap={audit.MinimumRejectedEvidenceGap}, " +
                    $"largestSystem={result.Systems.Max(system => system.FragmentIds.Length)}, " +
                    $"ms={stopwatch.ElapsedMilliseconds}, MiB={allocated / 1048576.0:F1}");
            }
            Check(fingerprints.Count == 3, "Different seeds must produce distinct cortical configurations");
        }
        VerifyControlledRelationships(service, assembly);
        Console.WriteLine("PASS: H05-B2 real-contact provenance, deterministic assembly and systems");
    }

    private static void VerifyControlledRelationships(WorldGenerationService service,
        ContinentsCorticalAssemblyGenerator assembly)
    {
        var config = LandmassProfiles.Create(GridSize.Small, LandmassType.Continents, 2026);
        var world = service.Generate(config);
        var topology = new HexTopology(world.Grid);
        byte[] plates = world.Landmass.PlateByCellId!;
        var motion = world.Landmass.TectonicMotion!;
        foreach (var kind in new[] { CorticalAssemblyKind.Accreted, CorticalAssemblyKind.RiftSeparated })
        {
            var edge = motion.Interactions.First(e => kind == CorticalAssemblyKind.Accreted
                ? e.Convergence >= .5 && e.Convergence > e.Divergence &&
                    e.Convergence >= .4 * e.Shear
                : e.Divergence >= .5 && e.Divergence > e.Convergence &&
                    e.Divergence >= .4 * e.Shear);
            var direct = MakeTwoFragments(topology.Count, edge.FirstCellId, edge.SecondCellId);
            var observed = assembly.Assemble(topology, config, plates, motion, direct);
            Check(observed.Evidence.Any(e => e.Kind == kind && e.DirectContact &&
                e.OceanGapHexes == 0), "Real official direct contact produces supported link");
            if (kind == CorticalAssemblyKind.Accreted)
                Check(observed.Systems.Length == 1 &&
                    observed.Systems[0].FragmentIds.Length == 2 &&
                    observed.Systems[0].PlateIds.Length == 2,
                    "Accretion assembles two blocks across genuine plates");
            else
                Check(observed.Systems.Length == 2,
                    "Divergent evidence does not assemble separate blocks");
        }
        // Look for a real convergent edge with a one-cell submerged corridor.
        // The evidence must describe the gap, never claim a fabricated direct contact.
        bool foundRegional = false;
        foreach (var convergent in motion.Interactions.Where(e => e.Convergence >= .5 &&
            e.Convergence > e.Divergence && e.Convergence >= .4 * e.Shear))
        {
            int first = convergent.FirstCellId;
            foreach (int second in topology.Neighbors(convergent.SecondCellId))
            {
                if (plates[second] != plates[convergent.SecondCellId] ||
                    topology.Neighbors(first).Contains(second)) continue;
                var regional = assembly.Assemble(topology, config, plates, motion,
                    MakeTwoFragments(topology.Count, first, second));
                if (!regional.Evidence.Any(e => e.Kind == CorticalAssemblyKind.Accreted &&
                    !e.DirectContact && e.OceanGapHexes > 0)) continue;
                foundRegional = true;
                break;
            }
            if (foundRegional) break;
        }
        Check(foundRegional,
            "Existing convergence supports a regional relation across a genuine oceanic gap");
    }

    private static SharedCorticalModel MakeTwoFragments(int count, int first, int second)
    {
        var crust = new byte[count];
        var fragments = Enumerable.Repeat(-1, count).ToArray();
        var provinces = Enumerable.Repeat(-1, count).ToArray();
        crust[first] = crust[second] = 1;
        fragments[first] = provinces[first] = 0;
        fragments[second] = provinces[second] = 1;
        return new SharedCorticalModel(SharedCorticalModel.CurrentVersion,
            crust, fragments, provinces, new float[count],
            [new(0, first), new(1, second)],
            [new(0, 0, first), new(1, 1, second)], []);
    }

    private static int DistanceOnPlate(HexTopology topology, SharedCorticalModel model,
        byte[] plates, int fragment, int target)
    {
        var visited = new bool[topology.Count];
        var queue = new int[topology.Count];
        int head = 0, tail = 1;
        queue[0] = target; visited[target] = true;
        byte plate = plates[target];
        int distance = 0;
        while (head < tail)
        {
            int end = tail;
            while (head < end)
            {
                int id = queue[head++];
                if (model.FragmentByCellId[id] == fragment) return distance;
                foreach (int neighbor in topology.Neighbors(id))
                    if (!visited[neighbor] && plates[neighbor] == plate &&
                        (model.FragmentByCellId[neighbor] == -1 ||
                         model.FragmentByCellId[neighbor] == fragment))
                    {
                        visited[neighbor] = true;
                        queue[tail++] = neighbor;
                    }
            }
            distance++;
        }
        return int.MaxValue;
    }

    private static int Sum(int a, int b) =>
        a == int.MaxValue || b == int.MaxValue ? int.MaxValue : a + b;

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
