using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Game.Graphics;
using GEvolve.Game;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaElevationTests
{
    private sealed record FieldStatistics(int Count, int Min, int Max, double Mean, double Median,
        int[] Percentiles, int[] Bands, int Zeros, int Above75, int Above85, int Above95,
        int Exactly100, int[] HighComponents, int InheritedComponents, int CreatedComponents,
        int MixedComponents, string LargestComponentDetails, double MeanLandStep, int MaximumLandStep);

    public static void Run()
    {
        VerifyStatistics();
        using var baselineStream = typeof(PangaeaElevationTests).Assembly
            .GetManifestResourceStream("ApprovedNonContinentalBaseline.txt")!;
        using var baselineReader = new StreamReader(baselineStream);
        var baselines = new Dictionary<(GridSize, int), string[]>();
        while (baselineReader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            if (parts[1] == "Pangaea") baselines.Add((Enum.Parse<GridSize>(parts[0]), int.Parse(parts[2])), parts);
        }
        var cases = Enum.GetValues<GridSize>().SelectMany(size => new[] { 2026, 42, -731 }
            .Select(seed => (Size: size, Seed: seed))).Concat(new[]
        {
            (GridSize.Enormous, 1324918567), (GridSize.Enormous, 418579367),
            (GridSize.Enormous, 1697481406), (GridSize.Enormous, 1242668747),
            (GridSize.Large, -234020843), (GridSize.Large, -690013300),
            (GridSize.Large, 689804754), (GridSize.Large, 1195510322),
            (GridSize.Large, 1443340172), (GridSize.Large, -185224545)
        });
        bool calibrationChangedHeights = false;
        bool regionalRecoveryChangedHeights = false;
        foreach (var (size, seed) in cases)
        {
            var timer = Stopwatch.StartNew();
            var world = new WorldGenerationService().Generate(size, LandmassType.Pangaea, seed);
            timer.Stop();
            var fields = world.PangaeaElevations ?? throw new Exception("Missing elevation diagnostics");
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            byte[] initial = fields.BaseByCellId.ToArray(), delta = fields.ContributionByCellId.ToArray();
            byte[] final = fields.FinalByCellId.ToArray();
            byte[] previousBase = fields.PreviousBaseByCellId.ToArray();
            byte[] previousFinal = fields.PreviousFinalByCellId.ToArray();
            byte[] priorCalibrationBase = fields.PriorCalibrationBaseByCellId.ToArray();
            byte[] priorCalibrationFinal = fields.PriorCalibrationFinalByCellId.ToArray();
            Check(previousBase.Length == topology.Count && previousFinal.Length == topology.Count,
                "Before/after fields are captured in the same world generation");
            Check(priorCalibrationBase.Length == topology.Count && priorCalibrationFinal.Length == topology.Count,
                "P03-A.3 comparison fields are captured without a second world");
            calibrationChangedHeights |= !initial.SequenceEqual(previousBase);
            regionalRecoveryChangedHeights |= !initial.SequenceEqual(priorCalibrationBase);
            if (baselines.TryGetValue((size, seed), out var baseline))
            {
                string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
                Check(terrain.DeterministicAttempt == int.Parse(baseline[3]) &&
                    Hash(terrain.SurfaceByCellId) == baseline[4] && Hash(previousBase) == baseline[5] &&
                    Hash(terrain.PlateByCellId!) == baseline[6],
                    "Official surface, plates and attempt are unchanged; the previous base retains its historical hash");
            }
            Check(final.SequenceEqual(terrain.ElevationByCellId!) && initial.Length == topology.Count &&
                delta.Length == topology.Count, "Diagnostic fields align with the generated world");
            for (int id = 0; id < topology.Count; id++)
            {
                Check(terrain.IsLand(id)
                    ? initial[id] is >= 1 and <= 100 && final[id] is >= 1 and <= 100 &&
                      initial[id] + delta[id] == final[id]
                    : initial[id] == 0 && delta[id] == 0 && final[id] == 0,
                    "Base plus contribution equals final; ocean remains zero");
                if (previousBase[id] <= 60)
                    Check(initial[id] == previousBase[id], "Low and middle regional relief stays unchanged");
                Check(initial[id] >= priorCalibrationBase[id] && final[id] >= priorCalibrationFinal[id],
                    "Recovery never removes existing structured relief or lowers a mountain crest");
                Check((initial[id] >= 85) == (priorCalibrationBase[id] >= 85) &&
                    (initial[id] < 85 || initial[id] == priorCalibrationBase[id]),
                    "P03-A.3 extreme base cells and their heights remain exactly unchanged");
                if (previousBase[id] <= 75)
                    Check(initial[id] >= previousBase[id], "Moderate regional height lost in P03-A.3 is recovered");
                if (final[id] >= 85 && priorCalibrationFinal[id] < 85)
                    Check(terrain.PangaeaMountainRanges!.RangeByCellId[id] != 0,
                        "New extreme final heights stay within the unchanged mountain influence");
            }
            Check(initial.Max() == 100 && final.Max() == 100,
                "Calibrated base and final retain height-100 summits");

            // Reapply only the mountain phase to the captured base: no second world
            // or reconstruction of the original surface is needed for attribution.
            var baseTerrain = terrain with { ElevationByCellId = initial, PangaeaMountainRanges = null };
            var repeated = new PangaeaMountainRangeGenerator().Apply(topology, baseTerrain);
            Check(ReferenceEquals(repeated.SurfaceByCellId, terrain.SurfaceByCellId) &&
                ReferenceEquals(repeated.PlateByCellId, terrain.PlateByCellId) &&
                ReferenceEquals(repeated.TectonicMotion, terrain.TectonicMotion),
                "Diagnostic capture preserves surface, plates and official H04 state");
            Check(repeated.ElevationByCellId!.SequenceEqual(final) &&
                repeated.PangaeaMountainRanges!.RangeByCellId.SequenceEqual(terrain.PangaeaMountainRanges!.RangeByCellId) &&
                repeated.PangaeaMountainRanges.Ranges.Zip(terrain.PangaeaMountainRanges.Ranges).All(pair =>
                    pair.First.Id == pair.Second.Id && pair.First.AxisCellIds.SequenceEqual(pair.Second.AxisCellIds) &&
                    pair.First.WidthByAxisCell.SequenceEqual(pair.Second.WidthByAxisCell)),
                "Diagnostic capture preserves heights, range identity, axes and widths");

            PangaeaElevationDiagnostics? repeatedPrevious = null, repeatedPrior = null;
            var priorTerrain = new PangaeaMountainRangeGenerator().Apply(topology,
                baseTerrain with { ElevationByCellId = previousBase }, value => repeatedPrevious = value);
            Check(repeatedPrevious!.AdditiveFinalByCellId.Span.SequenceEqual(previousFinal),
                "The captured previous final uses exactly the unchanged mountain contribution and composition");
            var priorRanges = priorTerrain.PangaeaMountainRanges!;
            var currentRanges = terrain.PangaeaMountainRanges!;
            Check(SameRangeGeometry(priorRanges, currentRanges),
                "Base calibration preserves mountain IDs, axes, widths, classification and intensity");
            var priorCalibrationTerrain = new PangaeaMountainRangeGenerator().Apply(topology,
                baseTerrain with { ElevationByCellId = priorCalibrationBase }, value => repeatedPrior = value);
            Check(repeatedPrior!.AdditiveFinalByCellId.Span.SequenceEqual(priorCalibrationFinal) &&
                SameRangeGeometry(priorCalibrationTerrain.PangaeaMountainRanges!, currentRanges),
                "P03-A.2, P03-A.3 and P03-A.4 have identical tectonic systems and geometry");

            Console.WriteLine($"P03-A.6 {size}/{seed}: generation={timer.Elapsed.TotalMilliseconds:F3} ms");
            Console.WriteLine($"  recovered base cells={initial.Zip(priorCalibrationBase).Count(p => p.First > p.Second)}, " +
                $"final cells crossing85 vs P03-A.3={final.Zip(priorCalibrationFinal).Count(p => p.First >= 85 && p.Second < 85)}");
            var previousBaseStats = Measure(topology, terrain.SurfaceByCellId, previousBase);
            var previousFinalStats = Measure(topology, terrain.SurfaceByCellId, previousFinal, previousBase);
            Print("BASE ANTERIOR P03-A.2", previousBaseStats);
            Print("FINAL ANTERIOR P03-A.2", previousFinalStats);
            byte[] previousDelta = previousFinal.Zip(previousBase).Select(pair => (byte)(pair.First - pair.Second)).ToArray();
            Print("APORTE ANTERIOR P03-A.2", Measure(topology, terrain.SurfaceByCellId, previousDelta));
            VerifyReportedPreviousResults(size, seed, previousBaseStats, previousFinalStats);
            Print("BASE P03-A.3", Measure(topology, terrain.SurfaceByCellId, priorCalibrationBase));
            Print("FINAL P03-A.3", Measure(topology, terrain.SurfaceByCellId, priorCalibrationFinal, priorCalibrationBase));
            byte[] priorCalibrationDelta = priorCalibrationFinal.Zip(priorCalibrationBase)
                .Select(pair => (byte)(pair.First - pair.Second)).ToArray();
            Print("APORTE P03-A.3", Measure(topology, terrain.SurfaceByCellId, priorCalibrationDelta));
            Print("BASE", Measure(topology, terrain.SurfaceByCellId, initial));
            Print("APORTE", Measure(topology, terrain.SurfaceByCellId, delta));
            Print("FINAL", Measure(topology, terrain.SurfaceByCellId, final, initial));
            int raised = delta.Count(value => value > 0), total = delta.Sum(value => (int)value);
            int land = terrain.SurfaceByCellId.Count(value => value == 1);
            int limited = fields.CompositionLimitedByCellId.ToArray().Count(value => value);
            bool[] limitedCells = fields.CompositionLimitedByCellId.ToArray();
            int reached94 = Enumerable.Range(0, topology.Count)
                .Count(id => limitedCells[id] && initial[id] < 94 && final[id] == 94);
            int inheritedHigh = Enumerable.Range(0, topology.Count).Count(id => initial[id] >= 85);
            int createdHigh = Enumerable.Range(0, topology.Count).Count(id => initial[id] < 85 && final[id] >= 85);
            Console.WriteLine($"  raised={raised}/{land} ({raised * 100.0 / land:F3}%), " +
                $"gain affected mean={(raised == 0 ? 0 : (double)total / raised):F3}, max={delta.Max()}, " +
                $"composition limited={limited}, reached94 by clipping={reached94}, " +
                $"extreme inherited={inheritedHigh}, created={createdHigh}");
            int previousRaised = previousDelta.Count(value => value > 0);
            Console.WriteLine($"  PREVIOUS raised={previousRaised}/{land} ({previousRaised * 100.0 / land:F3}%), " +
                $"gain affected mean={(previousRaised == 0 ? 0 : previousDelta.Sum(value => (int)value) / (double)previousRaised):F3}, " +
                $"max={previousDelta.Max()}, composition limited={fields.PreviousCompositionLimitedCells}");
            var sources = fields.Sources ?? throw new Exception("Missing base-source diagnostics");
            Console.WriteLine($"  REGIONAL envelope anchors={string.Join(",", sources.PotentialAnchors.Select(v => v.ToString("F5")))}, " +
                $"quantiles={string.Join(",", sources.Quantiles)}, levels={string.Join(",", sources.ElevationAnchors)}");
            Console.WriteLine($"  BASE correlations: approximate tectonics={sources.ApproximationCorrelation:F4}, " +
                $"regional noise={sources.NoiseCorrelation:F4}; >=85 means approx/noise=" +
                $"{sources.MeanApproximationHigh:F4}/{sources.MeanNoiseHigh:F4}; " +
                $"other={sources.MeanApproximationOther:F4}/{sources.MeanNoiseOther:F4}");
            var previousSources = sources.Previous ?? throw new Exception("Missing previous source statistics");
            var priorCalibrationSources = sources.PriorCalibration ?? throw new Exception("Missing P03-A.3 source statistics");
            Console.WriteLine($"  PREVIOUS BASE correlations: approximate tectonics={previousSources.ApproximationCorrelation:F4}, " +
                $"regional noise={previousSources.NoiseCorrelation:F4}; calibration={sources.CalibrationMilliseconds:F3} ms; " +
                $"version={PangaeaFromPlatesGenerator.Version}");
            Console.WriteLine($"  P03-A.3 BASE correlations: approximate tectonics={priorCalibrationSources.ApproximationCorrelation:F4}, " +
                $"regional noise={priorCalibrationSources.NoiseCorrelation:F4}; composition limited=" +
                $"{fields.PriorCalibrationCompositionLimitedCells}");
            Console.WriteLine($"  SHA256 base={Convert.ToHexString(SHA256.HashData(initial))}, " +
                $"final={Convert.ToHexString(SHA256.HashData(final))}");
            Console.WriteLine($"  SHA256 previous base={Convert.ToHexString(SHA256.HashData(previousBase))}, " +
                $"previous final={Convert.ToHexString(SHA256.HashData(previousFinal))}");
            Console.WriteLine($"  SHA256 P03-A.3 base={Convert.ToHexString(SHA256.HashData(priorCalibrationBase))}, " +
                $"P03-A.3 final={Convert.ToHexString(SHA256.HashData(priorCalibrationFinal))}");
            var convergence = ConvergenceNeighborhood(topology, terrain);
            PrintSystems("P03-A.2", priorRanges, land);
            PrintSystems("P03-A.3", priorCalibrationTerrain.PangaeaMountainRanges!, land);
            PrintSystems("P03-A.6", currentRanges, land);
            PrintMountainMetrics("P03-A.2 BASE", topology, terrain, previousBase, priorRanges, convergence);
            PrintMountainMetrics("P03-A.2 FINAL", topology, terrain, previousFinal, priorRanges, convergence);
            PrintMountainMetrics("P03-A.3 BASE", topology, terrain, priorCalibrationBase,
                priorCalibrationTerrain.PangaeaMountainRanges!, convergence);
            PrintMountainMetrics("P03-A.3 FINAL", topology, terrain, priorCalibrationFinal,
                priorCalibrationTerrain.PangaeaMountainRanges!, convergence);
            PrintMountainMetrics("P03-A.4 BASE", topology, terrain, initial, currentRanges, convergence);
            PrintMountainMetrics("P03-A.4 FINAL", topology, terrain, fields.AdditiveFinalByCellId.ToArray(), currentRanges, convergence);
            PrintMountainMetrics("P03-A.6 FINAL", topology, terrain, final, currentRanges, convergence);
            VerifyViews(terrain, fields);
            if (size == GridSize.Small && seed == 2026)
            {
                VerifySave(world);
                VerifySession(world);
            }
        }
        Check(calibrationChangedHeights, "The calibration changes upper relief in the regression matrix");
        Check(regionalRecoveryChangedHeights, "Regional relief is recovered in the regression matrix");
        foreach (var type in new[] { LandmassType.Archipelagos, LandmassType.SmallContinents, LandmassType.Continents })
            Check(new WorldGenerationService().Generate(GridSize.Small, type, 2026).PangaeaElevations is null,
                "Pangaea diagnostics are absent for other world types");
    }

    private static bool SameRangeGeometry(PangaeaMountainRangeState first, PangaeaMountainRangeState second) =>
        first.Version == second.Version && first.Ranges.Length == second.Ranges.Length &&
        first.RangeByCellId.SequenceEqual(second.RangeByCellId) &&
        first.Ranges.Zip(second.Ranges).All(pair =>
            pair.First.Id == pair.Second.Id && pair.First.AxisCellIds.SequenceEqual(pair.Second.AxisCellIds) &&
            pair.First.WidthByAxisCell.SequenceEqual(pair.Second.WidthByAxisCell) &&
            pair.First.FirstPlateId == pair.Second.FirstPlateId && pair.First.SecondPlateId == pair.Second.SecondPlateId &&
            pair.First.PeakConvergence == pair.Second.PeakConvergence && pair.First.MeanConvergence == pair.Second.MeanConvergence &&
            pair.First.Elongation == pair.Second.Elongation && pair.First.ContactEndpoints == pair.Second.ContactEndpoints &&
            pair.First.ContactCycles == pair.Second.ContactCycles && pair.First.BranchingCells == pair.Second.BranchingCells);

    private static void PrintSystems(string label, PangaeaMountainRangeState ranges, int landCells)
    {
        var restored = System.Text.Json.JsonSerializer.Deserialize<PangaeaMountainRangeState>(
            System.Text.Json.JsonSerializer.Serialize(ranges)) ?? throw new Exception("Range serialization failed");
        Check(SameRangeGeometry(ranges, restored) && ranges.Ranges.Zip(restored.Ranges)
            .All(pair => pair.First.AffectedCells == pair.Second.AffectedCells),
            "Detected mountain systems, geometry and affected-cell metadata survive serialization");
        int influence = ranges.RangeByCellId.Count(id => id != 0);
        Console.WriteLine($"  SYSTEMS {label}: detected={ranges.Ranges.Length}, persisted={restored.Ranges.Length}, " +
            $"axis cells={ranges.Ranges.Sum(range => range.AxisCellIds.Length)}, " +
            $"influence cells={influence} ({influence * 100.0 / landCells:F3}% land), " +
            $"final raised cells metadata={ranges.Ranges.Sum(range => range.AffectedCells)}");
    }

    private static bool[] ConvergenceNeighborhood(HexTopology topology, LandmassState terrain)
    {
        var motion = terrain.TectonicMotion!;
        int radius = Math.Max(1, (int)Math.Ceiling(motion.InfluenceRadiusHexes));
        var distance = Enumerable.Repeat(-1, topology.Count).ToArray();
        var queue = new Queue<int>();
        foreach (var edge in motion.Interactions)
        {
            if (edge.Convergence < 1 || edge.Kind is not (PlateInteractionKind.Convergent or PlateInteractionKind.Mixed)) continue;
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
                if (distance[id] < 0) { distance[id] = 0; queue.Enqueue(id); }
        }
        while (queue.TryDequeue(out int id))
        {
            if (distance[id] == radius) continue;
            foreach (int next in topology.Neighbors(id))
                if (distance[next] < 0) { distance[next] = distance[id] + 1; queue.Enqueue(next); }
        }
        return distance.Select(value => value >= 0).ToArray();
    }

    private static void PrintMountainMetrics(string label, HexTopology topology, LandmassState terrain,
        byte[] heights, PangaeaMountainRangeState ranges, bool[] nearConvergence)
    {
        var slopes = TerrainSlope.Calculate(topology, terrain with { ElevationByCellId = heights })!;
        int[] bands = new int[6], corridorBands = new int[6];
        var boundarySum = new double[ranges.Ranges.Length];
        var boundaryLinks = new int[ranges.Ranges.Length];
        int corridorCells = 0, highCells = 0, lowCells = 0, highNearConvergence = 0, highInRanges = 0;
        double corridorSlope = 0, highCompression = 0, lowCompression = 0;
        for (int id = 0; id < topology.Count; id++)
        {
            if (!terrain.IsLand(id)) continue;
            int band = heights[id] < 40 ? 0 : heights[id] <= 60 ? 1 : heights[id] <= 75 ? 2 :
                heights[id] <= 84 ? 3 : heights[id] <= 94 ? 4 : 5;
            bands[band]++;
            int owner = ranges.RangeByCellId[id];
            if (owner != 0)
            {
                corridorCells++; corridorSlope += slopes[id]; corridorBands[band]++;
                foreach (int next in topology.Neighbors(id))
                    if (terrain.IsLand(next) && ranges.RangeByCellId[next] != owner)
                    { boundarySum[owner - 1] += heights[next]; boundaryLinks[owner - 1]++; }
            }
            double compression = terrain.TectonicMotion!.CompressionByCellId[id];
            if (heights[id] >= 61)
            {
                highCells++; highCompression += compression;
                if (nearConvergence[id]) highNearConvergence++;
                if (owner != 0) highInRanges++;
            }
            else { lowCells++; lowCompression += compression; }
        }
        double axisSum = 0, localProminence = 0, boundaryProminence = 0;
        int axisCells = 0, boundaryAxisCells = 0, axisMinimum = 100, axisMaximum = 0;
        foreach (var range in ranges.Ranges)
        {
            double rangeAxisSum = 0;
            foreach (int id in range.AxisCellIds)
            {
                int neighborSum = 0, neighbors = 0;
                foreach (int next in topology.Neighbors(id))
                    if (terrain.IsLand(next)) { neighbors++; neighborSum += heights[next]; }
                axisCells++; axisSum += heights[id]; rangeAxisSum += heights[id];
                axisMinimum = Math.Min(axisMinimum, heights[id]); axisMaximum = Math.Max(axisMaximum, heights[id]);
                if (neighbors > 0) localProminence += heights[id] - (double)neighborSum / neighbors;
            }
            int links = boundaryLinks[range.Id - 1];
            if (links > 0)
            {
                boundaryProminence += rangeAxisSum - range.AxisCellIds.Length * boundarySum[range.Id - 1] / links;
                boundaryAxisCells += range.AxisCellIds.Length;
            }
        }
        string Bands(int[] counts) => string.Join("/", counts);
        Console.WriteLine($"  MOUNTAIN {label}: bands <40/40-60/61-75/76-84/85-94/95-100={Bands(bands)}; " +
            $"corridor={Bands(corridorBands)}; outside={Bands(bands.Zip(corridorBands).Select(p => p.First - p.Second).ToArray())}");
        Console.WriteLine($"    axis min/mean/max={(axisCells == 0 ? 0 : axisMinimum)}/" +
            $"{(axisCells == 0 ? 0 : axisSum / axisCells):F3}/{axisMaximum}; " +
            $"prominence vs six land neighbors={(axisCells == 0 ? 0 : localProminence / axisCells):F3}; " +
            $"vs outside corridor={(boundaryAxisCells == 0 ? "n/a" : (boundaryProminence / boundaryAxisCells).ToString("F3"))}; " +
            $"corridor mean slope={(corridorCells == 0 ? 0 : corridorSlope / corridorCells):F3} levels/hex");
        Console.WriteLine($"    >=61 land={highCells}, near convergent contacts={highNearConvergence}, " +
            $"in range influence={highInRanges}; H04 mean compression high/other=" +
            $"{(highCells == 0 ? 0 : highCompression / highCells):F4}/{(lowCells == 0 ? 0 : lowCompression / lowCells):F4}; " +
            $"near-contact radius={Math.Max(1, (int)Math.Ceiling(terrain.TectonicMotion!.InfluenceRadiusHexes))} hex");
        foreach (byte threshold in new byte[] { 61, 75 })
        {
            var components = ElevatedComponents(topology, terrain.SurfaceByCellId, heights, threshold);
            Console.WriteLine($"    >={threshold} components={components.Count}, largest={components.Largest}");
        }
    }

    private static (int Count, int Largest) ElevatedComponents(HexTopology topology, byte[] surface,
        byte[] heights, byte threshold)
    {
        var elevated = new byte[topology.Count];
        for (int id = 0; id < topology.Count; id++)
            if (surface[id] == 1 && heights[id] >= threshold) elevated[id] = 1;
        var statistics = LandmassAnalysis.Analyze(topology, elevated);
        return (statistics.ComponentCount, statistics.DominantMassSize);
    }

    private static void VerifyReportedPreviousResults(GridSize size, int seed,
        FieldStatistics previousBase, FieldStatistics previousFinal)
    {
        if (size != GridSize.Enormous) return;
        // User-supplied P03-A.2 measurements, not new elevation acceptance quotas.
        (int Base, int Final, int? LargestBase, int? LargestFinal)? reported = seed switch
        {
            1324918567 => (876, 988, 568, 568),
            418579367 => (764, 947, 282, 319),
            1242668747 => (472, 712, 472, 571),
            1697481406 => (674, 1257, null, null),
            _ => null
        };
        if (reported is not { } reference) return;
        Check(previousBase.Above85 == reference.Base && previousFinal.Above85 == reference.Final &&
            (reference.LargestBase is null || previousBase.HighComponents[0] == reference.LargestBase) &&
            (reference.LargestFinal is null || previousFinal.HighComponents[0] == reference.LargestFinal),
            "The comparison baseline reproduces the user's measured P03-A.2 concentrations");
    }

    private static FieldStatistics Measure(HexTopology topology, byte[] surface, byte[] values,
        byte[]? original = null)
    {
        var histogram = new int[101];
        int count = 0, total = 0;
        int landEdges = 0, summedSteps = 0, maximumStep = 0;
        for (int id = 0; id < topology.Count; id++)
        {
            if (surface[id] != 1) continue;
            histogram[values[id]]++; count++; total += values[id];
            foreach (int next in topology.Neighbors(id))
                if (next > id && surface[next] == 1)
                {
                    int difference = Math.Abs(values[id] - values[next]);
                    landEdges++; summedSteps += difference;
                    maximumStep = Math.Max(maximumStep, difference);
                }
        }
        int AtRank(int rank)
        {
            int cumulative = 0;
            for (int level = 0; level <= 100; level++)
            { cumulative += histogram[level]; if (cumulative > rank) return level; }
            throw new InvalidOperationException("An elevation field must contain land");
        }
        int Quantile(double probability) => AtRank((int)Math.Round(probability * (count - 1)));
        var bands = new int[5];
        for (int level = 1; level <= 100; level++) bands[(level - 1) / 20] += histogram[level];
        var seen = new bool[topology.Count];
        var queue = new Queue<int>();
        var sizes = new List<int>();
        int inherited = 0, created = 0, mixed = 0;
        int largest = 0;
        string largestDetails = "none";
        for (int id = 0; id < topology.Count; id++)
        {
            if (seen[id] || surface[id] == 0 || values[id] < 85) continue;
            int size = 0, priorHigh = 0, sum = 0, minimum = 100, maximum = 0, perimeter = 0;
            seen[id] = true; queue.Enqueue(id);
            while (queue.TryDequeue(out int current))
            {
                size++;
                sum += values[current];
                minimum = Math.Min(minimum, values[current]);
                maximum = Math.Max(maximum, values[current]);
                if (original is not null && original[current] >= 85) priorHigh++;
                perimeter += 6 - topology.Neighbors(current).Length;
                foreach (int next in topology.Neighbors(current))
                {
                    if (surface[next] == 0 || values[next] < 85) { perimeter++; continue; }
                    if (!seen[next])
                    { seen[next] = true; queue.Enqueue(next); }
                }
            }
            sizes.Add(size);
            if (size > largest)
            {
                largest = size;
                largestDetails = $"size={size}, min/max/mean={minimum}/{maximum}/{(double)sum / size:F3}, " +
                    $"perimeter={perimeter}, perimeter/sqrt(area)={perimeter / Math.Sqrt(size):F3}" +
                    (original is null ? "" : $", inherited={priorHigh}, created={size - priorHigh}");
            }
            if (original is not null)
            {
                if (priorHigh == size) inherited++;
                else if (priorHigh == 0) created++;
                else mixed++;
            }
        }
        sizes.Sort((a, b) => b.CompareTo(a));
        return new(count, Quantile(0), Quantile(1), (double)total / count,
            (AtRank((count - 1) / 2) + AtRank(count / 2)) / 2.0,
            new[] { .10, .25, .50, .75, .90, .95, .99 }.Select(Quantile).ToArray(),
            bands, histogram[0], histogram.Skip(75).Sum(), histogram.Skip(85).Sum(),
            histogram.Skip(95).Sum(), histogram[100], sizes.ToArray(), inherited, created, mixed, largestDetails,
            landEdges == 0 ? 0 : (double)summedSteps / landEdges, maximumStep);
    }

    private static void Print(string name, FieldStatistics stats)
    {
        Console.WriteLine($"  {name}: min={stats.Min}, max={stats.Max}, mean={stats.Mean:F3}, " +
            $"median={stats.Median:F3}, P10/25/50/75/90/95/99={string.Join("/", stats.Percentiles)}");
        Console.WriteLine($"    land bands 1-20/21-40/41-60/61-80/81-100=" +
            string.Join("/", stats.Bands.Select(n => $"{n * 100.0 / stats.Count:F3}%")) +
            $", zero={stats.Zeros}, >=75/85/95={stats.Above75}/{stats.Above85}/{stats.Above95}, 100={stats.Exactly100}");
        if (!name.StartsWith("APORTE", StringComparison.Ordinal))
            Console.WriteLine($"    traditional white (>=85)={stats.Above85}; white below100=" +
                $"{stats.Above85 - stats.Exactly100}; largest white component: {stats.LargestComponentDetails}");
        Console.WriteLine($"    >=85 components={stats.HighComponents.Length}, " +
            $"sizes=[{string.Join(",", stats.HighComponents)}], " +
            $"inherited/created/mixed={stats.InheritedComponents}/{stats.CreatedComponents}/{stats.MixedComponents}");
        Console.WriteLine($"    real land-neighbor height difference: mean={stats.MeanLandStep:F3}, max={stats.MaximumLandStep}");
    }

    private static void VerifyStatistics()
    {
        var topology = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        var surface = new byte[topology.Count];
        var values = new byte[topology.Count];
        int a = topology.Id(0, 66);
        int b = topology.Neighbors(a).ToArray().First(id => id % topology.Columns == topology.Columns - 1);
        int c = topology.Id(100, 66);
        int[] ids = [a, b, c, topology.Id(50, 50), topology.Id(50, 51)];
        byte[] levels = [85, 95, 100, 10, 20];
        for (int i = 0; i < ids.Length; i++) { surface[ids[i]] = 1; values[ids[i]] = levels[i]; }
        var stats = Measure(topology, surface, values);
        Check(stats.Min == 10 && stats.Max == 100 && stats.Mean == 62 && stats.Median == 85 && stats.Percentiles[2] == 85 &&
            stats.HighComponents.SequenceEqual(new[] { 2, 1 }) && stats.Above85 == 3 && stats.Exactly100 == 1,
            "Histogram and components use known levels and the east/west seam");
        Check(ElevatedComponents(topology, surface, values, 85) == (2, 2),
            "Mountain connectivity metrics reproduce the controlled wrapped components");
        int north = topology.Id(20, 0), south = topology.Id(20, topology.Rows - 1);
        Check(!topology.Neighbors(north).Contains(south), "Extreme components cannot wrap north/south");
    }

    private static void VerifyViews(LandmassState terrain, PangaeaElevationDiagnostics fields)
    {
        var before = (byte[])terrain.ElevationByCellId!.Clone();
        var mode = PangaeaElevationViews.Next(MapViewMode.Biomes, true);
        Check(mode == MapViewMode.ElevationBase && PangaeaElevationViews.Next(mode, true) == MapViewMode.ElevationContribution &&
            PangaeaElevationViews.Next(MapViewMode.ElevationContribution, true) == MapViewMode.ElevationFinal,
            "The same height control cycles through base, contribution and final");
        int land = Array.FindIndex(terrain.SurfaceByCellId, value => value == 1);
        Check(MapViewColors.ForCell(terrain, land, MapViewMode.ElevationBase, fields) ==
            new Vector3(fields.BaseByCellId.Span[land] / 100f), "Base view uses the actual pre-range height");
        for (int id = 0; id < terrain.SurfaceByCellId.Length; id++)
        {
            var color = MapViewColors.ForCell(terrain, id, MapViewMode.ElevationFinal, fields);
            if (terrain.IsLand(id)) Check(color == new Vector3(before[id] / 100f),
                "Final diagnostic retains numeric differences within the summit range");
            Check(BiomeClassifier.GetCell(terrain, id).Elevation == before[id],
                "The surface environment consumes the final world height");
            if (terrain.IsImpassableSummit(id))
                Check(MapViewColors.ForCell(terrain, id, MapViewMode.Biomes) == Vector3.One,
                    "The traditional surface view uses the final summit classification");
        }
        Check(before.SequenceEqual(terrain.ElevationByCellId!), "View queries never mutate geography");
    }

    private static void VerifySave(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-elevations-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path, new GameState { HexGrid = new HexGridState(world.Grid.Definition), Landmass = world.Landmass });
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(!File.ReadAllText(path).Contains("PangaeaElevations", StringComparison.Ordinal) &&
                loaded.ElevationByCellId!.SequenceEqual(world.Landmass.ElevationByCellId!),
                "Diagnostic arrays are transient and saved final heights remain compatible");
            Check(SameRangeGeometry(world.Landmass.PangaeaMountainRanges!, loaded.PangaeaMountainRanges!),
                "The full game save retains mountain count, IDs, axes and widths");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void VerifySession(GeneratedWorld world)
    {
        var session = new GameSession();
        session.OpenCreateWorld();
        session.ChooseSize(world.Grid.Definition.Size);
        session.ChooseType(LandmassType.Pangaea);
        session.SetSeedText(world.Landmass.Configuration.Seed.ToString());
        session.BeginGeneration();
        session.CompleteGeneration(world);
        var selected = session.SelectAt(FlatHexGeometry.Center(new GridAddress(20, 20)));
        Check(selected is not null && ReferenceEquals(session.PangaeaElevations, world.PangaeaElevations),
            "A session consumes diagnostics from the same generated world");
        var selectedCell = selected ?? throw new InvalidOperationException("The session did not select a grid cell");
        foreach (var mode in new[] { MapViewMode.ElevationBase, MapViewMode.ElevationContribution, MapViewMode.ElevationFinal })
            _ = MapViewColors.ForCell(world.Landmass, selectedCell.Id, mode, session.PangaeaElevations);
        Check(session.SelectedCell == selected && ReferenceEquals(session.State.Landmass, world.Landmass),
            "Diagnostic views preserve selection and world state");
        session.ReturnToMenu();
        Check(session.PangaeaElevations is null, "Diagnostic arrays are released with the session world");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
