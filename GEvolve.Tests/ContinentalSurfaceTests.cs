using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

internal static class ContinentalSurfaceTests
{
    public static void Run(bool export = false)
    {
        var service = new WorldGenerationService();
        int multiPlate = 0, mixedPlate = 0;
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var world = service.Generate(config);
            var terrain = world.Landmass;
            var state = terrain.ContinentalSurface ?? throw new Exception("Missing plate-derived surface state");
            var plates = terrain.PlateByCellId!;
            var topology = new HexTopology(world.Grid);
            var groups = state.GroupByPlateId;
            int count = groups.Where(group => group != byte.MaxValue).Distinct().Count();
            double weakestMainShare = 1;
            int smallestGroupLand = int.MaxValue;
            Check(count is >= 2 and <= 6 && world.Statistics?.LandFraction is >= .33 and <= .37 &&
                terrain.HasSurface && !terrain.SurfaceGenerationPending && !terrain.TectonicPreviewOnly &&
                terrain.ElevationByCellId is null && terrain.ContinentalGeology is null &&
                terrain.Climate is null && terrain.Biomes is null,
                "Continents have only a plate-derived surface, with later stages pending");
            for (byte group = 0; group < count; group++)
            {
                var memberPlates = Enumerable.Range(0, 16).Where(plate => groups[plate] == group).ToArray();
                Check(memberPlates.Length > 0, "Empty tectonic continent");
                var reached = new HashSet<int> { memberPlates[0] };
                bool changed;
                do
                {
                    changed = false;
                    for (int id = 0; id < topology.Count; id++)
                        if (reached.Contains(plates[id]))
                            foreach (int next in topology.Neighbors(id))
                                if (groups[plates[next]] == group && reached.Add(plates[next])) changed = true;
                } while (changed);
                Check(memberPlates.All(reached.Contains), "A continent's plate set is disconnected");
                int landForGroup = state.ContinentByCellId.Count(identity => identity == group);
                Check(landForGroup > 0, "A selected continental plate group never emerges");
                var ownLand = new byte[topology.Count];
                for (int id = 0; id < ownLand.Length; id++)
                    if (state.ContinentByCellId[id] == group) ownLand[id] = 1;
                var components = LandmassAnalysis.Analyze(topology, ownLand);
                Check(landForGroup >= topology.Count * .01 && components.DominantLandShare >= .50,
                    "Each selected plate group needs a recognizable connected main landmass");
                weakestMainShare = Math.Min(weakestMainShare, components.DominantLandShare);
                smallestGroupLand = Math.Min(smallestGroupLand, landForGroup);
                if (memberPlates.Length > 1) multiPlate++;
            }
            for (int plate = 0; plate < 16; plate++)
            {
                var values = Enumerable.Range(0, topology.Count).Where(id => plates[id] == plate)
                    .Select(id => terrain.SurfaceByCellId[id]).Distinct().ToArray();
                if (values.Length == 2) mixedPlate++;
            }
            for (int id = 0; id < topology.Count; id++)
                Check(terrain.SurfaceByCellId[id] == 1
                    ? state.CrustByCellId[id] == 1 && state.ContinentByCellId[id] == groups[plates[id]]
                    : state.ContinentByCellId[id] == byte.MaxValue,
                    "Land must inherit its identity and crust from its tectonic plate group");
            var second = service.Generate(config).Landmass;
            Check(second.SurfaceByCellId.SequenceEqual(terrain.SurfaceByCellId) &&
                JsonSerializer.Serialize(second.ContinentalSurface) == JsonSerializer.Serialize(state),
                "Plate-derived surface is deterministic");
            if (size == GridSize.Small && seed == 2026)
            {
                VerifyPersistence(world);
                var alteredPlates = TectonicPlateGenerator.Generate(topology, seed + 17);
                var alteredMotion = new TectonicMotionGenerator().Generate(topology,
                    new LandmassState(config, [], 0) { PlateByCellId = alteredPlates,
                        TectonicPreviewOnly = true });
                var counterfactual = new PlateGroupSurfaceGenerator().Generate(topology,
                    alteredPlates, alteredMotion, seed);
                Check(terrain.SurfaceByCellId.Where((value, id) => value != counterfactual.Surface[id]).Count()
                    > topology.Count * .15,
                    "Changing plate geometry with the same surface seed must change the continents");
            }
            if (export && (size, seed) is (GridSize.Small, 2026) or (GridSize.Medium, 42) or
                (GridSize.Large, 2026) or (GridSize.Large, -731))
            {
                string path = $"continental-diagnostic-{size.ToString().ToLowerInvariant()}-{seed}.svg";
                ContinentalSurfaceDiagnostics.Export(path, world);
                ContinentalSurfaceDiagnostics.ExportBitmap(Path.ChangeExtension(path, ".bmp"), world);
                Console.WriteLine($"CAPTURE: {path}");
            }
            Console.WriteLine($"CONTINENTS {size}/{seed}: {count} groups, {world.Statistics!.LandFraction:P1} land, " +
                $"{world.Statistics.ComponentCount} land components, weakest main {weakestMainShare:P0}, " +
                $"smallest group {smallestGroupLand} cells");
        }
        Check(multiPlate > 0 && mixedPlate > 0,
            "The map must include multi-plate continents and plates containing both land and ocean");
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"continental-surface-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(loaded.SurfaceByCellId.SequenceEqual(world.Landmass.SurfaceByCellId) &&
                JsonSerializer.Serialize(loaded.ContinentalSurface) ==
                    JsonSerializer.Serialize(world.Landmass.ContinentalSurface),
                "Plate-derived crust and continent identity survive save/load");
            state.Landmass = world.Landmass with
            {
                ContinentalSurface = world.Landmass.ContinentalSurface! with
                    { ContinentByCellId = new byte[world.Grid.Cells.Count] }
            };
            try { GameSaveStore.Save(path, state); throw new Exception("Invalid continent identity was saved"); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
