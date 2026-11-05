using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

public static class ContinentsCorrelatedNucleusExperimentTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        var b1 = new ContinentsCorticalFragmentGenerator();
        var b2 = new ContinentsCorticalAssemblyGenerator();
        foreach (GridSize size in Enum.GetValues<GridSize>())
        {
            var signatures = new HashSet<string>();
            foreach (int seed in new[] { 2026, 42, -731 })
            {
                var configuration = LandmassProfiles.Create(size, LandmassType.Continents, seed);
                var official = service.Generate(configuration);
                var topology = new HexTopology(official.Grid);
                var plates = official.Landmass.PlateByCellId!;
                var motion = official.Landmass.TectonicMotion!;
                byte[] platesBefore = (byte[])plates.Clone();
                string motionBefore = JsonSerializer.Serialize(motion);
                var original = b1.GenerateOriginalGeometry(topology, configuration, plates, motion);
                var originalRepeat = b1.GenerateOriginalGeometry(
                    topology, configuration, plates, motion);
                Check(Identical(original, originalRepeat),
                    "Historical B1 geometry remains deterministic");
                Check(Identical(original, SharedCorticalPersistence.FromState(
                    SharedCorticalPersistence.ToState(original))),
                    "Historical B1 geometry retains exact v1 persistence");

                long startAllocated = GC.GetAllocatedBytesForCurrentThread();
                var fragmentTimer = Stopwatch.StartNew();
                var correlated = b1.GenerateExperimentalCorrelated(
                    topology, configuration, plates, motion);
                fragmentTimer.Stop();
                long fragmentAllocated = GC.GetAllocatedBytesForCurrentThread() - startAllocated;
                SharedCorticalModelValidator.Validate(topology, correlated, plates);
                Check(correlated.Fragments.Length == original.Fragments.Length &&
                    correlated.Provinces.Length == original.Provinces.Length &&
                    correlated.CrustByCellId.Length == topology.Count,
                    "Correlated geometry preserves the shared cortex contract");
                Check(!correlated.FragmentByCellId.SequenceEqual(original.FragmentByCellId),
                    "Correlated nucleus geometry differs from historical B1");
                var correlatedRepeat = b1.GenerateExperimentalCorrelated(
                    topology, configuration, plates, motion);
                Check(Identical(correlated, correlatedRepeat),
                    "Correlated geometry remains deterministically reproducible");
                Check(Identical(b1.Generate(topology, configuration, plates, motion), correlated),
                    "Default Generate exactly matches the correlated strategy");
                var save = SharedCorticalPersistence.ToState(correlated);
                Check(Identical(correlated, SharedCorticalPersistence.FromState(save)),
                    "Correlated cortex has exact v1 persistence representation");

                // Each correlated paired nucleus is traceable to at least one actual
                // convergent edge: the selection never uses mere plate-ID similarity.
                for (int pair = 0; pair < 2; pair++)
                {
                    int first = correlated.Fragments[pair * 2].CoreCellId;
                    int second = correlated.Fragments[pair * 2 + 1].CoreCellId;
                    Check(motion.Interactions.Any(edge =>
                        edge.Convergence >= .5 && edge.Convergence > edge.Divergence &&
                        edge.Convergence >= .4 * edge.Shear &&
                        ((plates[first] == edge.FirstPlateId &&
                            plates[second] == edge.SecondPlateId &&
                            topology.Distance(first, edge.FirstCellId) <= 3 &&
                            topology.Distance(second, edge.SecondCellId) <= 3) ||
                         (plates[first] == edge.SecondPlateId &&
                            plates[second] == edge.FirstPlateId &&
                            topology.Distance(first, edge.SecondCellId) <= 3 &&
                            topology.Distance(second, edge.FirstCellId) <= 3))),
                        "Paired correlated nuclei require real nearby convergence");
                }

                var centerDistances = new List<int>();
                for (int a = 0; a < correlated.Fragments.Length; a++)
                    for (int b = a + 1; b < correlated.Fragments.Length; b++)
                        centerDistances.Add(topology.Distance(
                            correlated.Fragments[a].CoreCellId,
                            correlated.Fragments[b].CoreCellId));
                var originalDistances = new List<int>();
                for (int a = 0; a < original.Fragments.Length; a++)
                    for (int b = a + 1; b < original.Fragments.Length; b++)
                        originalDistances.Add(topology.Distance(
                            original.Fragments[a].CoreCellId,
                            original.Fragments[b].CoreCellId));
                // Report distribution rather than hiding a cluster by looking only at
                // the number of regional assembly links. These are core/area diagnostics,
                // not invented morphological acceptance thresholds.
                int[] coreSectors = new int[8], cellSectors = new int[8];
                foreach (var core in correlated.Fragments)
                    coreSectors[Sector(core.CoreCellId, topology)]++;
                for (int cell = 0; cell < topology.Count; cell++)
                    if (correlated.FragmentByCellId[cell] >= 0)
                        cellSectors[Sector(cell, topology)]++;
                Check(coreSectors.Count(c => c > 0) >= 3,
                    "The experiment retains regional independence instead of one cortical cluster");
                Check(coreSectors.Sum() == correlated.Fragments.Length &&
                    cellSectors.Sum() == correlated.CrustByCellId.Count(value => value == 1),
                    "Spatial coverage diagnostics account for all cores and cortical cells");

                for (int multiplierIndex = 0; multiplierIndex < 2; multiplierIndex++)
                {
                    int radius = multiplierIndex == 0 ? 4 : 6;
                    var oldTimer = Stopwatch.StartNew();
                    long oldStart = GC.GetAllocatedBytesForCurrentThread();
                    var baseline = b2.EvaluateExperimentalRadius(topology, configuration,
                        plates, motion, original, radius);
                    long oldAllocated = GC.GetAllocatedBytesForCurrentThread() - oldStart;
                    oldTimer.Stop();

                    if (radius == 4)
                    {
                        var operational = b2.Assemble(topology, configuration,
                            plates, motion, original);
                        Check(baseline.Model.AssemblyLinks.SequenceEqual(
                                operational.Model.AssemblyLinks) &&
                            baseline.Evidence.SequenceEqual(operational.Evidence) &&
                            baseline.Diagnostics == operational.Diagnostics,
                            "Approved B2 still uses exactly the operational 4r behavior");
                    }
                    var newTimer = Stopwatch.StartNew();
                    long newStart = GC.GetAllocatedBytesForCurrentThread();
                    var alternative = b2.EvaluateExperimentalRadius(topology, configuration,
                        plates, motion, correlated, radius);
                    long newAllocated = GC.GetAllocatedBytesForCurrentThread() - newStart;
                    newTimer.Stop();
                    Check(oldTimer.ElapsedMilliseconds < 120000 &&
                        newTimer.ElapsedMilliseconds < 120000 &&
                        oldAllocated < 500_000_000 && newAllocated < 500_000_000,
                        "Both nucleus geometries remain within the test resource budget");

                    Check(baseline.Model.CrustByCellId.SequenceEqual(original.CrustByCellId) &&
                        alternative.Model.CrustByCellId.SequenceEqual(correlated.CrustByCellId) &&
                        alternative.Model.FragmentByCellId.SequenceEqual(correlated.FragmentByCellId) &&
                        alternative.Model.ProvinceByCellId.SequenceEqual(correlated.ProvinceByCellId),
                        "B2 never modifies either cortical geometry");
                    SharedCorticalModelValidator.Validate(topology, alternative.Model, plates);
                    Check(Identical(alternative.Model,
                            SharedCorticalPersistence.FromState(
                                SharedCorticalPersistence.ToState(alternative.Model))),
                        "Experiment links survive exact cortical persistence");
                    foreach (var evidence in alternative.Evidence)
                    {
                        var edge = motion.Interactions[evidence.InteractionIndex];
                        int a1 = DistanceOnPlate(topology, correlated, plates,
                            evidence.FirstFragmentId, edge.FirstCellId);
                        int b1Distance = DistanceOnPlate(topology, correlated, plates,
                            evidence.SecondFragmentId, edge.SecondCellId);
                        int a2 = DistanceOnPlate(topology, correlated, plates,
                            evidence.FirstFragmentId, edge.SecondCellId);
                        int b2Distance = DistanceOnPlate(topology, correlated, plates,
                            evidence.SecondFragmentId, edge.FirstCellId);
                        int gap = Math.Min(Sum(a1, b1Distance), Sum(a2, b2Distance));
                        Check(gap == evidence.OceanGapHexes &&
                            gap <= alternative.Diagnostics.AcceptedGapHexes &&
                            plates[edge.FirstCellId] != plates[edge.SecondCellId] &&
                            topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId),
                            "Every link has a genuine plate contact and a corridor avoiding third fragments");
                        Check(evidence.Kind == CorticalAssemblyKind.Accreted
                            ? edge.Convergence >= .5 && edge.Convergence > edge.Divergence &&
                              edge.Convergence >= .4 * edge.Shear
                            : evidence.Kind == CorticalAssemblyKind.RiftSeparated &&
                              edge.Divergence >= .5 && edge.Divergence > edge.Convergence &&
                              edge.Divergence >= .4 * edge.Shear,
                            "Every link is supported by normal tectonic interaction");
                    }
                    var repeat = b2.EvaluateExperimentalRadius(topology, configuration,
                        plates, motion, correlated, radius);
                    Check(alternative.Evidence.SequenceEqual(repeat.Evidence) &&
                        alternative.Model.AssemblyLinks.SequenceEqual(repeat.Model.AssemblyLinks) &&
                        alternative.Diagnostics == repeat.Diagnostics &&
                        JsonSerializer.Serialize(alternative.Systems) ==
                            JsonSerializer.Serialize(repeat.Systems),
                        "Experimental assembly is deterministic");
                    PrintRow(size, seed, radius, "original", original, baseline,
                        originalDistances, oldTimer.ElapsedMilliseconds,
                        oldAllocated, topology);
                    PrintRow(size, seed, radius, "correlated", correlated, alternative,
                        centerDistances, newTimer.ElapsedMilliseconds,
                        newAllocated, topology);
                }
                Check(plates.SequenceEqual(platesBefore) &&
                    JsonSerializer.Serialize(motion) == motionBefore &&
                    official.Landmass.SurfaceGenerationPending &&
                    official.Landmass.SurfaceByCellId.Length == 0 &&
                    official.Landmass.ElevationByCellId is null &&
                    official.Landmass.SharedCortex is null,
                    "No tectonic motion, plate, official generation or surface was changed");
                Check(fragmentTimer.ElapsedMilliseconds < 120000 &&
                    fragmentAllocated < 500_000_000,
                    "Correlated cortex memory and time remain bounded");
                Console.WriteLine($"H05-B2 DISTRIBUTION {size}/{seed}: " +
                    $"originalMinCoreGap={originalDistances.Min()}, " +
                    $"correlatedMinCoreGap={centerDistances.Min()}, " +
                    $"originalMeanCoreGap={originalDistances.Average():F1}, " +
                    $"correlatedMeanCoreGap={centerDistances.Average():F1}, " +
                    $"coreSectors=[{string.Join(",", coreSectors)}], " +
                    $"corticalSectors=[{string.Join(",", cellSectors)}], " +
                    $"correlatedB1Ms={fragmentTimer.ElapsedMilliseconds}, " +
                    $"correlatedB1MiB={fragmentAllocated / 1048576.0:F1}");
                signatures.Add(Convert.ToHexString(SHA256.HashData(correlated.CrustByCellId)));
            }
            Check(signatures.Count == 3, "Seed variation remains present for each size");
        }
        Console.WriteLine("PASS: H05-B2 default correlated geometry and historical B1 comparison");
    }

    private static int Sector(int id, HexTopology topology)
    {
        int x = id % topology.Columns, y = id / topology.Columns;
        return Math.Min(3, x * 4 / topology.Columns) +
            4 * Math.Min(1, y * 2 / topology.Rows);
    }

    private static void PrintRow(GridSize size, int seed, int radius, string label,
        SharedCorticalModel cortex, ContinentsCorticalAssemblyResult result,
        List<int> distances, long milliseconds, long allocated,
        HexTopology topology)
    {
        int accr = result.Evidence.Count(e => e.Kind == CorticalAssemblyKind.Accreted);
        int rift = result.Evidence.Count(e => e.Kind == CorticalAssemblyKind.RiftSeparated);
        string gaps = result.Evidence.Length == 0 ? "none" :
            string.Join(",", result.Evidence.Select(e => e.OceanGapHexes).Order());
        Console.WriteLine($"H05-B2 GEOMETRY {size}/{seed} {label} {radius}r: " +
            $"pairs={result.Diagnostics.FragmentPairs}, accepted={result.Evidence.Length}, " +
            $"accretion={accr}, separation={rift}, systems={result.Systems.Length}, " +
            $"isolated={result.IsolatedFragments}, " +
            $"maxSystem={result.Systems.Max(s => s.FragmentIds.Length)}, " +
            $"multiPlateSystems={result.Systems.Count(s => s.PlateIds.Length > 1)}, " +
            $"minCoreGap={distances.Min()}, meanCoreGap={distances.Average():F1}, " +
            $"cortexPercent={100.0 * cortex.CrustByCellId.Count(c => c == 1) / topology.Count:F1}, " +
            $"gaps=[{gaps}], rejectedDistance={result.Diagnostics.RejectedByDistance}, " +
            $"timeMs={milliseconds}, allocatedMiB={allocated / 1048576.0:F1}");
    }

    private static int DistanceOnPlate(HexTopology topology, SharedCorticalModel model,
        byte[] plates, int fragment, int target)
    {
        var distance = new int[topology.Count];
        Array.Fill(distance, -1);
        int[] queue = new int[topology.Count];
        int head = 0, tail = 1;
        queue[0] = target; distance[target] = 0;
        byte plate = plates[target];
        while (head < tail)
        {
            int cell = queue[head++];
            if (model.FragmentByCellId[cell] == fragment) return distance[cell];
            foreach (int neighbor in topology.Neighbors(cell))
                if (distance[neighbor] < 0 && plates[neighbor] == plate &&
                    (model.FragmentByCellId[neighbor] < 0 ||
                     model.FragmentByCellId[neighbor] == fragment))
                {
                    distance[neighbor] = distance[cell] + 1;
                    queue[tail++] = neighbor;
                }
        }
        return int.MaxValue;
    }

    private static int Sum(int a, int b) =>
        a == int.MaxValue || b == int.MaxValue ? int.MaxValue : a + b;

    private static bool Identical(SharedCorticalModel a, SharedCorticalModel b) =>
        a.Version == b.Version &&
        a.CrustByCellId.SequenceEqual(b.CrustByCellId) &&
        a.FragmentByCellId.SequenceEqual(b.FragmentByCellId) &&
        a.ProvinceByCellId.SequenceEqual(b.ProvinceByCellId) &&
        a.EmergenceAffinityByCellId.SequenceEqual(b.EmergenceAffinityByCellId) &&
        a.Fragments.SequenceEqual(b.Fragments) &&
        a.Provinces.SequenceEqual(b.Provinces) &&
        a.AssemblyLinks.SequenceEqual(b.AssemblyLinks);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
