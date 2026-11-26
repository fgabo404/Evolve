// Exercises frozen R/M/Influence data, independent geometry, CORE lifecycle and private E2/E3 calibration.
// Compares complete payloads and dispatches isolated calibration tools without changing production execution.
using System.Reflection;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicCheckEntryPoint
{
    private static int Main(string[] args)
    {
        if (args.SequenceEqual(new[] { "--calibration" })) { TectonicCalibrationChecks.Run(); return 0; }
        if (args.Length == 2 && args[0] == "--influence-calibration") { TectonicInfluenceCalibration.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--export-influence-vectors") { TectonicInfluenceVectors.Export(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--influence-final-performance") { TectonicInfluenceFinalPerformance.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--mantle-calibration") { MantleCalibrationRunner.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--mantle-spatial-calibration") { MantleSpatialRunner.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--export-mantle-candidate-vectors") { MantleCandidateVectors.Export(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--mantle-final-performance") { MantleFinalPerformance.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--compare-motion") { MotionProfileComparison.Run(args[1]); return 0; }
        if (args.Length == 2 && args[0] == "--export-motion-vectors") { TectonicMotionChecks.Export(args[1]); return 0; }
        if (args.SequenceEqual(new[] { "--motion-performance" })) { TectonicMotionChecks.Performance(); return 0; }
        if (args.SequenceEqual(new[] { "--final-performance" })) { TectonicFinalVerification.Measure(); return 0; }
        if (args.Length == 2 && args[0] == "--export-vectors") { TectonicFrozenVectors.Export(args[1]); return 0; }
        if (args.Length != 0) throw new ArgumentException("Use no arguments for checks; see readme.md for supported measurement and explicit fixture-export switches.");
        (string Name, Action Check)[] checks =
        [
            ("T01-T03 exhaustive owners, independent connectivity and seam/pole coverage", Coverage),
            ("T04 complete deterministic outputs across all world types and signed seeds", Replay),
            ("T05 fixed numeric vectors, periodic warp and distinct-seed fallback", Numeric),
            ("T04-T05 frozen R full buffers and equivalence with all CAL01 R cases", TectonicFrozenVectors.Verify),
            ("T06 malformed packed schemas and disconnected data are rejected", Malformed),
            ("T06 immutable metadata and defensive source ownership", Immutable),
            ("T10 real CORE ordering, rebuilding and historical results", Lifecycle),
            ("T10 failures and invalid Grid/candidates stay atomic and retryable", Failures),
            ("T10 competing and reentrant operations cannot publish partial data", Concurrent),
            ("T11 graphical independence and disabled influence scope", Scope),
            ("T08 frozen M precision, projections and category ties", TectonicMotionChecks.Numeric),
            ("T07 complete independent boundary topology and projections", TectonicMotionChecks.Boundaries),
            ("T06/T10 C schema safety, immutability and historical rebuilding", TectonicMotionChecks.Schema),
            ("T08 fifteen complete M compatibility buffers", TectonicMotionChecks.Frozen),
            ("T08 exact agreement with all 75 approved CAL01 M cases", MotionProfileComparison.VerifyApprovedM),
            ("D independent category predicates and exact binary midpoint rounding", TectonicFinalVerification.Check),
            ("D-FIX01 conventional graphics timing median", TectonicFinalVerification.CheckTimingStatistics),
            ("E2-CAL01 stored M endpoint sources and ownership", TectonicInfluenceChecks.Sources),
            ("E2-CAL01 checked numeric inputs, reach and canonical zeros", TectonicInfluenceChecks.Numeric),
            ("E2-CAL01 independent complete Gaussian and distance oracles", TectonicInfluenceChecks.Propagation),
            ("E2-CAL01 replay, all sizes/types/signed seeds and unchanged publication", TectonicInfluenceChecks.Compatibility),
            ("E2-FINAL exact policy, dense oracle and candidate integrity", TectonicInfluenceFinalChecks.PolicyAndDense),
            ("E2-FINAL all-target independent real source/distance/envelope oracles", TectonicInfluenceFinalChecks.CompleteOracles),
            ("E2-FINAL complete Influence vectors and original R/M bindings", TectonicInfluenceVectors.Verify),
            ("E3-CAL01 parameters, periodic signals and isolated seed policy", MantleCalibrationChecks.ParametersAndSignals),
            ("E3-CAL01 controlled potentials and independent edge oracle", MantleCalibrationChecks.ControlledAndEdges),
            ("E3-CAL01 complete candidate replay and unchanged R/M/Influence", MantleCalibrationChecks.ReplayAndIntegrity),
            ("E3-CAL02 controlled geometric operators and continuous resolution field", MantleSpatialChecks.Controlled),
            ("E3-CAL02 geometry, octave sampling and shifted seam probes", MantleSpatialChecks.SamplingAndGeometry),
            ("E3-CAL02 complete alternative oracles and archived controls", MantleSpatialChecks.RealMaps),
            ("E3-FINAL strict candidate identity and immutable output", MantleFinalChecks.Identity),
            ("E3-FINAL complete independent A0 noise, potential and R0 oracles", MantleFinalChecks.FullOracles),
            ("E3-FINAL complete candidate vectors and historical bindings", MantleCandidateVectors.Verify)
        ];
        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try { check(); Console.WriteLine($"PASS WG-01-B/C {name}"); }
            catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL WG-01-B/C {name}: {exception}"); }
        }
        Console.WriteLine($"WG-01-B/C: {checks.Length - failures}/{checks.Length} groups passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void Coverage()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var (grid, packed) = Generate(size);
            var view = TectonicData.FromLayerData(packed, grid);
            CheckRegions(grid, view);
            Require(view.CellCount == grid.CellCount && view.PlateCount == 14 && view.Size == size &&
                view.SchemaVersion == 1 && view.PartitionRevision == 2 && !view.HasMotion && !view.HasBoundaries &&
                !view.HasInfluence && view.BoundaryCount == 0 && view.MotionRevision == 0 && view.InfluenceRevision == 0, "Schema metadata");
            Require(packed.Count * 4 == 576 + grid.CellCount, "Compact ownership payload size");
            Console.WriteLine($"COVERAGE {size}: {grid.CellCount} cells; 14 independently connected regions; payload={packed.Count * 4} B");
        }
    }

    private static void Replay()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            uint[] expected = Generate(size, seed).Data.ToArray();
            // Includes fresh Pangea reconstruction, not just cross-type comparison to a single run.
            foreach (var type in Enum.GetValues<WorldType>())
            {
                var (grid, actual) = Generate(size, seed, type);
                Require(expected.SequenceEqual(actual), $"Complete word mismatch: {size}/{seed}/{type}");
                CheckRegions(grid, TectonicData.FromLayerData(actual, grid));
            }
        }
        Require(!Generate(MapSize.Small, 0).Data.SequenceEqual(Generate(MapSize.Small, 1).Data), "Selected seeds produced no observed variation");
    }

    private static void Numeric()
    {
        // CORE's known stream vector is independent of partition logic.
        var random = new DeterministicStageRandom(0);
        Require(random.NextUInt64() == 0xE220A8397B1DCDAFUL &&
            random.NextUInt64() == 0x6E789E6AA1B965F4UL && random.NextUInt64() == 0x06C45D188009454FUL, "CORE stream vector");
        Require(PeriodicTectonicNoise.Hash(0, 0, 0) == -1, "Zero lattice hash vector");
        Require(BitConverter.DoubleToUInt64Bits(PeriodicTectonicNoise.Hash(13, -7, 0x123456789ABCDEF0)) == 0x3FDA6049D98CADA4UL,
            "Frozen 64-bit lattice hash vector");
        Require(BitConverter.DoubleToUInt64Bits(PeriodicTectonicNoise.Sample(12.125, 15.75, 200, 100, 9, 0x123456789ABCDEF0)) == 0xBFA27206DA58A447UL,
            "Frozen octave interpolation vector");
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var grid = new HexGridData(size);
            int[] fallback = TectonicPartitionGenerator.SelectSeeds(grid, new(0), 0);
            Require(fallback.Distinct().Count() == 14 && fallback.All(cell => cell >= 0 && cell < grid.CellCount), "Fallback seeds must be distinct");
            foreach (double x in new[] { -402.25, -1.75, 0, 12.125, 399.5 })
            foreach (double y in new[] { -3.25, 0, 15.75, 200.25 })
            foreach (int frequency in TectonicGenerationProfile.Approved.Frequencies)
            {
                double first = PeriodicTectonicNoise.Sample(x, y, grid.Columns, grid.Rows, frequency, 0x123456789ABCDEF0);
                double second = PeriodicTectonicNoise.Sample(x + grid.Columns, y, grid.Columns, grid.Rows, frequency, 0x123456789ABCDEF0);
                Require(Math.Abs(first - second) < 1e-12 && first >= -1 && first <= 1, "Periodic normalized field");
                var a = TectonicPartitionGenerator.Warp(x, y, grid.Columns, grid.Rows, 11, 17, TectonicGenerationProfile.Approved);
                var b = TectonicPartitionGenerator.Warp(x + grid.Columns, y, grid.Columns, grid.Rows, 11, 17, TectonicGenerationProfile.Approved);
                Require(Math.Abs(a.X + grid.Columns - b.X) < 1e-12 && Math.Abs(a.Y - b.Y) < 1e-12, "Periodic domain warp");
            }
        }
        Expect<ArgumentOutOfRangeException>(() => PeriodicTectonicNoise.Sample(0, 0, 0, 100, 3, 0));
        Expect<ArgumentOutOfRangeException>(() => PeriodicTectonicNoise.Sample(double.NaN, 0, 200, 100, 3, 0));
        Expect<ArgumentOutOfRangeException>(() => PeriodicTectonicNoise.Sample(0, double.PositiveInfinity, 200, 100, 3, 0));
        Expect<ArgumentOutOfRangeException>(() => PeriodicTectonicNoise.Sample(double.MaxValue, 0, 200, 100, 3, 0));
        Expect<ArgumentOutOfRangeException>(() => TectonicPartitionGenerator.NextInt(new(0), 0));
    }

    private static void Malformed()
    {
        var (grid, data) = Generate(MapSize.Small);
        uint[] source = data.ToArray();
        Expect<ArgumentNullException>(() => TectonicData.FromLayerData(null!, grid));
        Expect<ArgumentNullException>(() => TectonicData.FromLayerData(data, null!));
        Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<int>(GenerationStage.Tectonics, [1]), grid));
        Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Grid, source), grid));
        Expect<ArgumentException>(() => TectonicData.FromLayerData(data, new HexGridData(MapSize.Large)));
        foreach (int length in new[] { 0, 1, 31, 143, source.Length - 1, source.Length + 1 })
            Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, new uint[length]), grid));
        int cases = 0;
        for (int field = 0; field < 32; field++) Reject(field, source[field] ^ uint.MaxValue);
        Reject(16, 1); // P0's historical revision must never be decoded as the approved R profile.
        Reject(16, 0xCA010003); // The CAL01 experimental tag is not a final production revision.
        for (int plate = 0; plate < 14; plate++)
        {
            int start = 32 + plate * 8;
            Reject(start, uint.MaxValue); Reject(start + 1, 0); Reject(start + 1, uint.MaxValue);
            foreach (float cost in new[] { float.NaN, float.PositiveInfinity, 0, -1 }) Reject(start + 2, BitConverter.SingleToUInt32Bits(cost));
            for (int field = 3; field < 8; field++) Reject(start + field, 1);
        }
        Reject(144, (source[144] & 0xFFFFFF00) | 14);
        Reject(144, (source[144] & 0xFFFFFF00) | 255);
        var view = TectonicData.FromLayerData(data, grid);
        Expect<ArgumentOutOfRangeException>(() => view.GetPlateId(-1));
        Expect<ArgumentOutOfRangeException>(() => view.GetPlateId(grid.CellCount));
        Expect<ArgumentOutOfRangeException>(() => view.GetPlate(14));
        Expect<ArgumentOutOfRangeException>(() => view.GetPlate(255));
        // Swap two deep interior cells: counts/seed ownership remain valid, but each new owner is isolated.
        var origins = Enumerable.Range(0, 14).Select(id => view.GetPlate((byte)id).SeedCellId).ToHashSet();
        int first = -1, second = -1;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (origins.Contains(cell) || !Neighbors(cell, grid.Columns, grid.Rows).All(next => view.GetPlateId(next) == view.GetPlateId(cell))) continue;
            if (first < 0) first = cell;
            else if (view.GetPlateId(first) != view.GetPlateId(cell)) { second = cell; break; }
        }
        Require(second >= 0, "No interior fixture available");
        uint[] disconnected = source.ToArray();
        SetOwner(disconnected, first, view.GetPlateId(second)); SetOwner(disconnected, second, view.GetPlateId(first));
        Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, disconnected), grid));
        Console.WriteLine($"MALFORMED {cases} field corruptions plus type/length/index/connectivity fixtures rejected");

        void Reject(int index, uint value)
        {
            uint[] bad = source.ToArray(); bad[index] = value;
            Expect<ArgumentException>(() => TectonicData.FromLayerData(new LayerData<uint>(GenerationStage.Tectonics, bad), grid));
            cases++;
        }
    }

    private static void SetOwner(uint[] words, int cell, byte owner)
    {
        int word = 144 + cell / 4, shift = 8 * (cell % 4);
        words[word] = (words[word] & ~(255u << shift)) | ((uint)owner << shift);
    }

    private static void Immutable()
    {
        var (grid, generated) = Generate(MapSize.Small);
        uint[] mutable = generated.ToArray();
        var snapshot = new LayerData<uint>(GenerationStage.Tectonics, mutable);
        var view = TectonicData.FromLayerData(snapshot, grid);
        Array.Fill(mutable, uint.MaxValue);
        Require(generated.SequenceEqual(snapshot) && view.GetPlateId(0) < 14, "Mutable input altered published data");
        Require(snapshot.Values is not uint[] && snapshot.Values is not IList<uint>, "Mutable publication escaped");
        Require(typeof(TectonicData).GetProperties().All(property => property.SetMethod is null) &&
            typeof(TectonicPlate).GetProperties().All(property => property.SetMethod is null), "External metadata mutation");
        Require(typeof(TectonicData).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).All(field => field.IsInitOnly &&
            (field.FieldType == typeof(LayerData<uint>) || field.FieldType == typeof(HexGridData))), "View retains mutable scratch or context");
    }

    private static void Lifecycle()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var orchestrator = Orchestrator();
            var context = orchestrator.CreateGeneration(Request(size, -7));
            Require(context.GetProgress().NextStage == GenerationStage.Grid && context.GetLayerData(GenerationStage.Tectonics) is null, "Prerequisites missing");
            orchestrator.RunNext(context);
            var gridData = context.GetLayerData(GenerationStage.Grid);
            Require(context.GetProgress().NextStage == GenerationStage.Tectonics, "Grid not first");
            orchestrator.RunNext(context);
            uint[] expected = Published(context).ToArray();
            var oldView = TectonicData.FromLayerData(Published(context), new HexGridData(size));
            orchestrator.RunAll(context);
            var historical = orchestrator.FinalizeWorld(context);
            orchestrator.RebuildFrom(context, GenerationStage.Tectonics);
            Require(ReferenceEquals(gridData, context.GetLayerData(GenerationStage.Grid)), "Tectonics rebuild changed Grid");
            foreach (var stage in Enum.GetValues<GenerationStage>().Skip(1))
                Require(context.GetLayerData(stage) is null && context.GetProgress().GetStatus(stage) == GenerationStageStatus.Invalidated, "Suffix invalidation");
            orchestrator.RunUntil(context, GenerationStage.Tectonics);
            Require(expected.SequenceEqual(Published(context)) && ReferenceEquals(gridData, context.GetLayerData(GenerationStage.Grid)), "Replay or Grid mutation");
            Require(expected.SequenceEqual((LayerData<uint>)historical.GetLayerData(GenerationStage.Tectonics)) && oldView.GetPlateId(0) == TectonicData.FromLayerData(Published(context), new(size)).GetPlateId(0), "Historical result changed");
            orchestrator.RebuildFrom(context, GenerationStage.Grid);
            Require(context.GetLayerData(GenerationStage.Grid) is null && context.GetLayerData(GenerationStage.Tectonics) is null, "Grid rebuild kept Tectonics");
            orchestrator.RunAll(context);
            Require(expected.SequenceEqual(Published(context)), "Full replay differs");
        }
    }

    private static void Failures()
    {
        Expect<ArgumentNullException>(() => new TectonicPlatesLayer().Execute(null!));
        foreach (int mode in Enumerable.Range(0, 4))
        {
            int attempts = 0;
            var probe = new TestLayer(GenerationStage.Tectonics, inputs =>
            {
                var candidate = new TectonicPlatesLayer().Execute(inputs);
                if (attempts++ > 0) return candidate;
                return mode switch
                {
                    0 => LayerExecutionResult.Failed("Injected failure after candidate."),
                    1 => throw new InvalidOperationException("Injected exception after candidate."),
                    2 => LayerExecutionResult.Success(new LayerData<uint>(GenerationStage.Climate, [1])),
                    _ => LayerExecutionResult.Success(TectonicData.CreateCandidate(new(MapSize.Small),
                        new(new byte[20000], new TectonicPlate[14], default), TectonicGenerationProfile.Approved))
                };
            });
            var orchestrator = Orchestrator(probe);
            var context = orchestrator.CreateGeneration(Request(MapSize.Small));
            orchestrator.RunAll(context);
            var grid = context.GetLayerData(GenerationStage.Grid);
            Require(context.GetProgress().GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Failed &&
                context.GetLayerData(GenerationStage.Tectonics) is null && context.GetProgress().GetStatus(GenerationStage.Landmasses) == GenerationStageStatus.Pending, "Failure exposed partial data or advanced");
            orchestrator.RunNext(context);
            Require(ReferenceEquals(grid, context.GetLayerData(GenerationStage.Grid)) &&
                OwnershipOnly(Published(context)).SequenceEqual(Generate(MapSize.Small).Data), "Retry changed payload or Grid");
        }
        foreach (int invalid in Enumerable.Range(0, 4))
        {
            var badGrid = new TestLayer(GenerationStage.Grid, inputs =>
            {
                Expect<ArgumentException>(() => new TectonicPlatesLayer().Execute(inputs));
                return invalid switch
                {
                    0 => LayerExecutionResult.Success(new LayerData<uint>(GenerationStage.Grid, [1])),
                    1 => LayerExecutionResult.Success(new LayerData<HexGridDescriptor>(GenerationStage.Grid, [])),
                    2 => LayerExecutionResult.Success(new LayerData<HexGridDescriptor>(GenerationStage.Grid, [new(MapSize.Large)])),
                    _ => LayerExecutionResult.Success(new LayerData<HexGridDescriptor>(GenerationStage.Grid, [new(MapSize.Small), new(MapSize.Small)]))
                };
            });
            var orchestrator = Orchestrator(grid: badGrid);
            var context = orchestrator.CreateGeneration(Request(MapSize.Small));
            orchestrator.RunUntil(context, GenerationStage.Tectonics);
            Require(context.GetProgress().GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Failed &&
                context.GetLayerData(GenerationStage.Tectonics) is null, "Malformed Grid accepted");
        }
    }

    private static void Concurrent()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var orchestrator = Orchestrator(new TestLayer(GenerationStage.Tectonics, inputs =>
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Test barrier expired.");
            return new TectonicPlatesLayer().Execute(inputs);
        }));
        var context = orchestrator.CreateGeneration(Request(MapSize.Small));
        orchestrator.RunNext(context);
        var task = Task.Run(() => orchestrator.RunNext(context));
        try
        {
            Require(entered.Wait(TimeSpan.FromSeconds(10)), "Worker not entered");
            Require(context.GetLayerData(GenerationStage.Tectonics) is null && context.GetLayerData(GenerationStage.Grid) is not null, "Running data visible");
            Expect<InvalidOperationException>(() => Orchestrator().RunNext(context));
            Expect<InvalidOperationException>(() => orchestrator.RebuildFrom(context, GenerationStage.Tectonics));
        }
        finally { release.Set(); Require(task.Wait(TimeSpan.FromSeconds(20)), "Worker did not finish"); }
        Require(context.GetProgress().GetStatus(GenerationStage.Tectonics) == GenerationStageStatus.Completed, "Contention damaged result");
        WorldGenerationOrchestrator? reentrant = null;
        GenerationContext? same = null;
        reentrant = Orchestrator(new TestLayer(GenerationStage.Tectonics, inputs =>
        {
            Expect<InvalidOperationException>(() => reentrant!.RunAll(same!));
            return new TectonicPlatesLayer().Execute(inputs);
        }));
        same = reentrant.CreateGeneration(Request(MapSize.Small));
        reentrant.RunUntil(same, GenerationStage.Tectonics);
        Require(same.GetProgress().LastCompletedStage == GenerationStage.Tectonics, "Reentrant rejection damaged context");
    }

    private static void Scope()
    {
        var probe = Orchestrator(new TestLayer(GenerationStage.Tectonics, inputs =>
        {
            var expected = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(inputs.Request.Seed, GenerationStage.Tectonics));
            for (int draw = 0; draw < 3; draw++) _ = expected.NextUInt64();
            var result = new TectonicPlatesLayer().Execute(inputs);
            Require(inputs.Random.NextUInt64() == expected.NextUInt64(), "Tectonics must reserve exactly three stage substream seeds");
            return result;
        }));
        var draft = probe.CreateGeneration(Request(MapSize.Small, long.MinValue));
        probe.RunUntil(draft, GenerationStage.Tectonics);
        Require(draft.GetProgress().LastCompletedStage == GenerationStage.Tectonics, "Substream probe failed");
        var assembly = typeof(TectonicPlatesLayer).Assembly;
        Require(!AppDomain.CurrentDomain.GetAssemblies().Any(item => item.GetName().Name?.StartsWith("OpenTK") == true), "Graphics loaded");
        Require(assembly.GetReferencedAssemblies().All(item => item.Name?.StartsWith("System") == true ||
            item.Name == typeof(MapSize).Assembly.GetName().Name), "Unexpected Engine dependency");
        Require(!assembly.GetTypes().Any(type => type.Namespace?.StartsWith("Evolve.Engine.WorldGeneration.Layers.Tectonics") == true &&
            type.Name.Contains("InfluenceBuilder")), "Optional influences anticipated");
    }
}
