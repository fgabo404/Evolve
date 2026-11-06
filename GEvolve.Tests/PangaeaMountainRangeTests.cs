using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaMountainRangeTests
{
    public static void Run()
    {
        VerifyControlledContactShapes();
        var service = new WorldGenerationService();
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var timer = Stopwatch.StartNew();
            var world = service.Generate(size, LandmassType.Pangaea, seed);
            timer.Stop();
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            var state = terrain.PangaeaMountainRanges ?? throw new Exception("Pangaea ranges missing");
            var initial = new PangaeaFromPlatesGenerator().Generate(topology,
                terrain.Configuration, terrain.PlateByCellId!, terrain.DeterministicAttempt);
            var phaseTimer = Stopwatch.StartNew();
            var phase = new PangaeaMountainRangeGenerator().Apply(topology,
                terrain with { ElevationByCellId = initial.Elevation, PangaeaMountainRanges = null });
            phaseTimer.Stop();
            Check(phase.ElevationByCellId!.SequenceEqual(terrain.ElevationByCellId!) &&
                phase.PangaeaMountainRanges!.RangeByCellId.SequenceEqual(
                    terrain.PangaeaMountainRanges!.RangeByCellId),
                "Independent mountain phase reproduces orchestrated result");
            Check(terrain.SurfaceByCellId.SequenceEqual(initial.Surface), "Pangaea surface changed");
            Check(state.Version == PangaeaMountainRangeGenerator.Version && state.Ranges.Length > 0,
                "Pangaea range systems exist");
            Check(state.RangeByCellId.Length == topology.Count &&
                state.Ranges.Select(range => range.Id).SequenceEqual(
                    Enumerable.Range(1, state.Ranges.Length)), "Stable range IDs and aligned ownership");
            var motion = terrain.TectonicMotion!;
            var convergentLand = new HashSet<int>(motion.Interactions
                .Where(edge => (edge.Kind is PlateInteractionKind.Convergent or PlateInteractionKind.Mixed) &&
                    edge.Convergence >= 1 && edge.Convergence >= edge.Shear * .65 &&
                    initial.Surface[edge.FirstCellId] == 1 && initial.Surface[edge.SecondCellId] == 1)
                .SelectMany(edge => new[] { edge.FirstCellId, edge.SecondCellId }));
            foreach (var range in state.Ranges)
            {
                Check(range.AxisCellIds.Length >= Math.Max(7, topology.Rows / 20) &&
                    range.WidthByAxisCell.Length == range.AxisCellIds.Length &&
                    range.AxisCellIds.Distinct().Count() == range.AxisCellIds.Length,
                    "Regional axis length, width and uniqueness");
                foreach (int id in range.AxisCellIds)
                    Check((convergentLand.Contains(id) ||
                        topology.Neighbors(id).ToArray().Any(convergentLand.Contains)) &&
                        state.RangeByCellId[id] == range.Id &&
                        initial.Surface[id] == 1, "Axis must follow convergent land contacts");
                for (int i = 1; i < range.AxisCellIds.Length; i++)
                    Check(topology.Neighbors(range.AxisCellIds[i - 1]).Contains(range.AxisCellIds[i]),
                        "Axis must follow real hex neighbors, including seams");
                Check(range.FirstPlateId < range.SecondPlateId &&
                    range.MeanConvergence >= 1 && range.Elongation >= 1 &&
                    !(range.AxisCellIds.Length < topology.Rows * .12 && range.Elongation < 2) &&
                    !(range.AxisCellIds.Length < topology.Rows * .20 &&
                      range.Elongation < 1.35 && range.ContactEndpoints <= 2),
                    "Compact contact networks do not become mountain ranges");
            }
            PrintDiagnostics($"{size}/{seed}", topology, initial.Surface,
                initial.Elevation, terrain.ElevationByCellId!, state);
            for (int id = 0; id < topology.Count; id++)
                Check(initial.Surface[id] == 0
                    ? terrain.ElevationByCellId![id] == 0 && state.RangeByCellId[id] == 0
                    : terrain.ElevationByCellId![id] is >= 1 and <= 100 &&
                      terrain.ElevationByCellId[id] >= initial.Elevation[id] &&
                      (state.RangeByCellId[id] != 0 || terrain.ElevationByCellId[id] == initial.Elevation[id]),
                    "Only local land elevation may rise");
            var repeated = service.Generate(size, LandmassType.Pangaea, seed).Landmass;
            Check(terrain.SurfaceByCellId.SequenceEqual(repeated.SurfaceByCellId) &&
                terrain.ElevationByCellId!.SequenceEqual(repeated.ElevationByCellId!) &&
                terrain.PlateByCellId!.SequenceEqual(repeated.PlateByCellId!) &&
                state.RangeByCellId.SequenceEqual(repeated.PangaeaMountainRanges!.RangeByCellId) &&
                state.Ranges.Zip(repeated.PangaeaMountainRanges.Ranges)
                    .All(pair => pair.First.Id == pair.Second.Id &&
                        pair.First.AxisCellIds.SequenceEqual(pair.Second.AxisCellIds) &&
                        pair.First.WidthByAxisCell.SequenceEqual(pair.Second.WidthByAxisCell)),
                "Deterministic geography and mountain identities");
            Check(motion.Motions.SequenceEqual(repeated.TectonicMotion!.Motions) &&
                motion.Interactions.SequenceEqual(repeated.TectonicMotion.Interactions) &&
                motion.CompressionByCellId.SequenceEqual(repeated.TectonicMotion.CompressionByCellId) &&
                motion.ExtensionByCellId.SequenceEqual(repeated.TectonicMotion.ExtensionByCellId) &&
                motion.ShearByCellId.SequenceEqual(repeated.TectonicMotion.ShearByCellId),
                "Official H04 motion and stress remain deterministic");
            Console.WriteLine($"P03-A {size}/{seed}: {state.Ranges.Length} ranges, " +
                $"{state.Ranges.Sum(range => range.AxisCellIds.Length)} axis cells, " +
                $"{phaseTimer.ElapsedMilliseconds} ms P03-A, {timer.ElapsedMilliseconds} ms total");
            if (size == GridSize.Small && seed == 2026) VerifyPersistence(world);
        }
        foreach (var type in new[] { LandmassType.Archipelagos, LandmassType.SmallContinents })
        {
            var world = service.Generate(GridSize.Small, type, 2026);
            Check(world.Landmass.PangaeaMountainRanges is null, "Other types have no Pangaea ranges");
        }
        foreach (int seed in new[] { 1510325998, -251942972, 1697481406,
            1567286827, 418579367 })
        {
            var world = service.Generate(GridSize.Enormous, LandmassType.Pangaea, seed);
            var topology = new HexTopology(world.Grid);
            var terrain = world.Landmass;
            var initial = new PangaeaFromPlatesGenerator().Generate(topology,
                terrain.Configuration, terrain.PlateByCellId!, terrain.DeterministicAttempt);
            Check(initial.Surface.SequenceEqual(terrain.SurfaceByCellId),
                "Visual regression seed changed the Pangaea surface");
            PrintDiagnostics($"Enormous/{seed}", topology, initial.Surface,
                initial.Elevation, terrain.ElevationByCellId!, terrain.PangaeaMountainRanges!);
        }
    }

    private static void VerifyControlledContactShapes()
    {
        var configuration = LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 2026);
        var topology = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        int center = topology.Id(100, 66);
        int[] Ring(int radius)
        {
            var cells = Enumerable.Range(0, topology.Count)
                .Where(id => topology.Distance(center, id) == radius).ToHashSet();
            var ordered = new List<int>();
            int first = cells.Min(), previous = -1, current = first;
            do
            {
                ordered.Add(current);
                int next = topology.Neighbors(current).ToArray()
                    .Where(cells.Contains).Where(id => id != previous)
                    .Order().First();
                previous = current; current = next;
            } while (current != first);
            Check(ordered.Count == cells.Count, "Controlled ring follows one hex cycle");
            return ordered.ToArray();
        }
        LandmassState Terrain(int[] path, bool close)
        {
            var compression = new float[topology.Count];
            var distance = Enumerable.Repeat((ushort)1, topology.Count).ToArray();
            var edges = new List<PlateInteraction>();
            for (int i = 1; i < path.Length + (close ? 1 : 0); i++)
            {
                int a = path[i - 1], b = path[i % path.Length];
                Check(topology.Neighbors(a).Contains(b), "Controlled contact is adjacent");
                compression[a] = compression[b] = 3;
                distance[a] = distance[b] = 0;
                edges.Add(new(Math.Min(a, b), Math.Max(a, b), 0, 1,
                    new TectonicVector(1, 0), 3, 0, 0, PlateInteractionKind.Convergent));
            }
            var motion = new TectonicMotionState("controlled", [], edges.ToArray(),
                compression, new float[topology.Count], new float[topology.Count],
                distance, [], 2);
            return new LandmassState(configuration, Enumerable.Repeat((byte)1, topology.Count).ToArray(), 0)
            {
                PlateByCellId = new byte[topology.Count], TectonicMotion = motion,
                ElevationByCellId = Enumerable.Repeat((byte)10, topology.Count).ToArray()
            };
        }
        var generator = new PangaeaMountainRangeGenerator();
        var compact = generator.Apply(topology, Terrain(Ring(3), true));
        Check(compact.PangaeaMountainRanges!.Ranges.Length == 0,
            "Compact closed contact does not become a circular cordillera");
        int[] openArc = Ring(18).Take(80).ToArray();
        var broad = generator.Apply(topology, Terrain(openArc, false));
        Check(broad.PangaeaMountainRanges!.Ranges.Length > 0,
            "Long open tectonic arc remains eligible");
    }

    private static void PrintDiagnostics(string label, HexTopology topology, byte[] surface,
        byte[] initial, byte[] final, PangaeaMountainRangeState state)
    {
        int land = 0, raised = 0, totalGain = 0, maxGain = 0, saturated = 0;
        int[] before = new int[3], after = new int[3], crossed = new int[3];
        int[] levels = [75, 85, 95];
        for (int id = 0; id < topology.Count; id++)
        {
            if (surface[id] == 0) continue;
            land++;
            int gain = final[id] - initial[id];
            if (gain > 0) { raised++; totalGain += gain; maxGain = Math.Max(maxGain, gain); }
            if (initial[id] < 100 && final[id] == 100) saturated++;
            for (int i = 0; i < levels.Length; i++)
            {
                if (initial[id] >= levels[i]) before[i]++;
                if (final[id] >= levels[i]) after[i]++;
                if (initial[id] < levels[i] && final[id] >= levels[i]) crossed[i]++;
            }
        }
        int inherited100 = Enumerable.Range(0, topology.Count)
            .Count(id => surface[id] == 1 && initial[id] == 100);
        Console.WriteLine($"P03-A.1 {label}: land={land}, raised={raised} ({raised * 100.0 / land:F2}%), " +
            $"gain mean={(raised == 0 ? 0 : (double)totalGain / raised):F2}, max={maxGain}, " +
            $"largest >=85={LargestHighComponent(topology, surface, initial, 85)}->" +
            $"{LargestHighComponent(topology, surface, final, 85)}");
        Console.WriteLine($"  >=75 {before[0]}->{after[0]} (+{crossed[0]}), " +
            $">=85 {before[1]}->{after[1]} (+{crossed[1]}), " +
            $">=95 {before[2]}->{after[2]} (+{crossed[2]}), " +
            $"100 inherited={inherited100}, reached by P03-A={saturated}");
        foreach (var range in state.Ranges)
            Console.WriteLine($"  range {range.Id} P{range.FirstPlateId}/P{range.SecondPlateId}: " +
                $"axis={range.AxisCellIds.Length}, affected={range.AffectedCells}, " +
                $"width={range.WidthByAxisCell.Min()}/{range.WidthByAxisCell.Average(v => (double)v):F1}/{range.WidthByAxisCell.Max()}, " +
                $"convergence={range.MeanConvergence:F1}/{range.PeakConvergence:F1}, " +
                $"elongation={range.Elongation:F2}, endpoints={range.ContactEndpoints}, " +
                $"cycles={range.ContactCycles}, branches={range.BranchingCells}");
    }

    private static int LargestHighComponent(HexTopology topology, byte[] surface, byte[] elevation,
        byte threshold)
    {
        var seen = new bool[topology.Count];
        var queue = new Queue<int>();
        int largest = 0;
        for (int id = 0; id < topology.Count; id++)
        {
            if (seen[id] || surface[id] == 0 || elevation[id] < threshold) continue;
            int size = 0;
            seen[id] = true; queue.Enqueue(id);
            while (queue.TryDequeue(out int current))
            {
                size++;
                foreach (int next in topology.Neighbors(current))
                    if (!seen[next] && surface[next] == 1 && elevation[next] >= threshold)
                    { seen[next] = true; queue.Enqueue(next); }
            }
            largest = Math.Max(largest, size);
        }
        return largest;
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-p03a-{Guid.NewGuid():N}.json");
        try
        {
            var game = new GameState { HexGrid = new HexGridState(world.Grid.Definition),
                Landmass = world.Landmass };
            GameSaveStore.Save(path, game);
            var loaded = GameSaveStore.Load(path).Landmass!.PangaeaMountainRanges!;
            var original = world.Landmass.PangaeaMountainRanges!;
            Check(loaded.Version == original.Version &&
                loaded.RangeByCellId.SequenceEqual(original.RangeByCellId) &&
                loaded.Ranges.Length == original.Ranges.Length &&
                loaded.Ranges.Zip(original.Ranges).All(pair =>
                    pair.First.AxisCellIds.SequenceEqual(pair.Second.AxisCellIds)),
                "Mountain geometry survives save/load");
            game.Landmass = world.Landmass with { PangaeaMountainRanges = null };
            GameSaveStore.Save(path, game);
            Check(GameSaveStore.Load(path).Landmass!.PangaeaMountainRanges is null,
                "Older saves without ranges remain readable");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
