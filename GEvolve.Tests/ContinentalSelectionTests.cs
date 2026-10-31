using System.Diagnostics;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

internal static class ContinentalSelectionTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        int worlds = 0, retried = 0, multipleLarge = 0;
        long enormousMilliseconds = 0;
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731, 7, 99, 12345, 314159,
            -2026, 8675309, 0, 1, 123456789 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var timer = Stopwatch.StartNew();
            var world = service.Generate(config);
            timer.Stop();
            var terrain = world.Landmass;
            var surface = terrain.ContinentalSurface!;
            var relief = terrain.ContinentalElevation!;
            var selection = surface.Selection!;
            var topology = new HexTopology(world.Grid);
            int principal = relief.Systems.Count(system =>
                system.MountainTier == TectonicMountainTier.Principal);
            int secondary = relief.Systems.Count(system =>
                system.MountainTier == TectonicMountainTier.Secondary);
            Check(principal >= 1 && principal <= 3 &&
                selection.Attempts[selection.SelectedAttempt].PrincipalSystems >= 1 &&
                selection.FinalPrincipalSystems == principal &&
                selection.FinalSecondarySystems == secondary &&
                selection.EvaluatedGroupings is >= 1 and <= WorldGenerationService.MaxContinentalSelectionAttempts &&
                selection.Attempts.Length == selection.EvaluatedGroupings &&
                selection.Attempts.Select((entry, index) => entry.Attempt == index).All(value => value),
                "Each successful continental world has documented principal tectonic systems");
            Check(world.Statistics!.LandFraction is >= .33 and <= .37 &&
                surface.GroupByPlateId.Where(group => group != byte.MaxValue).Distinct().Count() is >= 2 and <= 6,
                "Selection retains continental land fraction and group count");
            int principalMountainCells = Enumerable.Range(0, topology.Count).Count(id =>
                terrain.SurfaceByCellId[id] == 1 && terrain.ElevationByCellId![id] >= 44 &&
                relief.MountainTierByCellId[id] == (byte)TectonicMountainTier.Principal);
            Check(principalMountainCells >= 12 &&
                relief.Systems.Where(system => system.MountainTier == TectonicMountainTier.Principal)
                    .All(system => system.LengthHexEdges >= 8 &&
                        system.EffectiveMountainCells >= 24 &&
                        system.Kind is GeologicalContactKind.ContinentalCollision or
                            GeologicalContactKind.OceanContinentSubduction or
                            GeologicalContactKind.OceanOceanSubduction),
                "Principal mountain systems are extended land relief on convergent margins");
            int groupCount = surface.GroupByPlateId.Where(group => group != byte.MaxValue).Distinct().Count();
            for (byte group = 0; group < groupCount; group++)
            {
                var land = new byte[topology.Count];
                for (int id = 0; id < land.Length; id++)
                    if (surface.ContinentByCellId[id] == group) land[id] = 1;
                var quality = LandmassAnalysis.Analyze(topology, land);
                Check(quality.LandCells >= topology.Count * .01 && quality.DominantLandShare >= .5,
                    "Selected continental group retains a connected main landmass");
            }
            if (seed == 2026 || selection.SelectedAttempt > 0)
            {
                var preview = service.GeneratePlatePreview(config).Landmass;
                Check(terrain.PlateByCellId!.SequenceEqual(preview.PlateByCellId!) &&
                    JsonSerializer.Serialize(terrain.TectonicMotion) ==
                        JsonSerializer.Serialize(preview.TectonicMotion),
                    "Retries preserve all 16 plates and original motion fields");
                var repeated = service.Generate(config).Landmass;
                Check(repeated.SurfaceByCellId.SequenceEqual(terrain.SurfaceByCellId) &&
                    JsonSerializer.Serialize(repeated.ContinentalSurface) ==
                        JsonSerializer.Serialize(surface) &&
                    repeated.ElevationByCellId!.SequenceEqual(terrain.ElevationByCellId!),
                    "Selection attempt and resulting world are deterministic");
            }
            if (size == GridSize.Small && seed == 2026) VerifyPersistence(world);
            worlds++;
            if (selection.SelectedAttempt > 0) retried++;
            if (size is GridSize.Large or GridSize.Enormous && principal >= 2) multipleLarge++;
            if (size == GridSize.Enormous) enormousMilliseconds += timer.ElapsedMilliseconds;
            Console.WriteLine($"SELECTION {size}/{seed}: attempt {selection.SelectedAttempt}, " +
                $"evaluated {selection.EvaluatedGroupings}, principal {principal}, secondary {secondary}, " +
                $"principal mountain cells {principalMountainCells}, {timer.ElapsedMilliseconds} ms");
            foreach (var system in relief.Systems.Where(system =>
                system.MountainTier == TectonicMountainTier.Principal))
                Console.WriteLine($"  RANGE {system.Id}: {system.Kind}, axis {system.LengthHexEdges} edges, " +
                    $"land axis {system.LandMountainAxisHexes} hexes, mountain belt " +
                    $"{system.EffectiveMountainCells} land hexes, max {system.MaximumLandElevation}, " +
                    $"mean {system.MeanLandElevation:F1}");
            foreach (var entry in selection.Attempts.Where(entry => entry.Attempt < selection.SelectedAttempt))
                Console.WriteLine($"  REJECTED {entry.Attempt}: {entry.Outcome}");
        }
        Console.WriteLine($"PASS: {worlds} continental selections, {retried} needed retries, " +
            $"{multipleLarge}/24 large or enormous worlds have multiple principal systems; " +
            $"Enormous mean {enormousMilliseconds / 12} ms");
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"continental-selection-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(JsonSerializer.Serialize(loaded.ContinentalSurface!.Selection) ==
                JsonSerializer.Serialize(world.Landmass.ContinentalSurface!.Selection),
                "Selection diagnostics survive save/load");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
