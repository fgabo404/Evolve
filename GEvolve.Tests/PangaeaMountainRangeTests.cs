using System.Diagnostics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaMountainRangeTests
{
    public static void Run()
    {
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
                    Check(convergentLand.Contains(id) && state.RangeByCellId[id] == range.Id &&
                        initial.Surface[id] == 1, "Axis must follow convergent land contacts");
                for (int i = 1; i < range.AxisCellIds.Length; i++)
                    Check(topology.Neighbors(range.AxisCellIds[i - 1]).Contains(range.AxisCellIds[i]),
                        "Axis must follow real hex neighbors, including seams");
            }
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
