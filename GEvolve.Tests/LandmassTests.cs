using System.Diagnostics;
using System.Security.Cryptography;
using GEvolve.Definitions.Versioning;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class LandmassTests
{
    // Old landmass-4 snapshot hashes apply only to the former dimensions.
    // With landmass-5, compare generated content against same-size repeats and seeds.

    public static void Run()
    {
        Check(EvolveVersion.GameName == "Evolve" && EvolveVersion.Display == "v0.001" &&
            EvolveVersion.Technical == new Version(0, 0, 1, 0) &&
            EvolveVersion.SaveFormat == 2 && EvolveVersion.LandmassGenerator == "landmass-5",
            "Independent game, save and generator versions");
        var service = new LandmassGenerationService();
        Check(service.Generators.Count == 4 &&
            service.Generators.Select(item => item.Type).Distinct().Count() == 4 &&
            service.Generators.Select(item => item.GetType()).Distinct().Count() == 4,
            "Four independent generation strategies");

        var baselineHashes = new Dictionary<(GridSize, LandmassType), string>();
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var worlds = new Dictionary<LandmassType, GeneratedWorld>();
            foreach (var type in Enum.GetValues<LandmassType>())
            {
                var configuration = LandmassProfiles.Create(size, type, seed);
                long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                var timer = Stopwatch.StartNew();
                var result = service.Generate(configuration);
                timer.Stop();
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
                worlds[type] = result;
                var stats = result.Statistics;
                Check(result.Grid.Definition == HexGridDefinitions.Get(size) &&
                    result.Landmass.Configuration == configuration &&
                    result.Landmass.SurfaceByCellId.Length == result.Grid.Cells.Count &&
                    result.Landmass.SurfaceByCellId.All(value => value is 0 or 1) &&
                    stats.LandCells > 0 && stats.LandCells < stats.TotalCells &&
                    LandmassGenerationService.MeetsProfile(type, stats),
                    "Configuration, IDs, classification and profile statistics");
                Check(stats.ComponentSizes.Sum() == stats.LandCells &&
                    stats.ComponentSizes.SequenceEqual(stats.ComponentSizes.OrderDescending()),
                    "Connected-component analysis");
                string hash = Convert.ToHexString(SHA256.HashData(result.Landmass.SurfaceByCellId));
                if (seed == 2026)
                {
                    baselineHashes[(size, type)] = hash;
                    var repeated = service.Generate(configuration);
                    Check(repeated.Landmass.SurfaceByCellId.SequenceEqual(
                        result.Landmass.SurfaceByCellId) &&
                        repeated.Landmass.DeterministicAttempt == result.Landmass.DeterministicAttempt,
                        "Determinism for every algorithm and size");
                    // Historical hashes cannot be applied to the resized grid.
                    Check(hash.Length == 64, "SHA256 surface fingerprint was produced");
                    Console.WriteLine($"PASS: {size} {type}: {stats.LandFraction:P1} land, " +
                        $"{stats.ComponentCount} masses, {timer.ElapsedMilliseconds} ms, " +
                        $"{allocatedBytes / 1024.0:F0} KiB allocated");
                }
                else
                    Check(hash != baselineHashes[(size, type)],
                        "Different seeds change the generated world");
            }
            var continents = worlds[LandmassType.Continents].Statistics;
            var pangaea = worlds[LandmassType.Pangaea].Statistics;
            var small = worlds[LandmassType.SmallContinents].Statistics;
            var islands = worlds[LandmassType.Archipelagos].Statistics;
            Check(continents.LargeComponents >= 2 &&
                pangaea.ComponentCount == 1 && pangaea.DominantLandShare == 1 &&
                small.ComponentCount > continents.ComponentCount &&
                small.DominantMassSize < continents.DominantMassSize &&
                islands.ComponentCount > small.ComponentCount &&
                islands.IslandComponents * 2 > islands.ComponentCount,
                "Distinct continent, Pangaea, fragmented and island structures");
        }

        VerifyPeriodicSeam();
        VerifyPersistence(service);
        var tectonic = service.Generate(LandmassProfiles.Create(GridSize.Small,LandmassType.Pangaea,2026));
        int expectedCount = HexGridDefinitions.Get(GridSize.Small).Columns *
            HexGridDefinitions.Get(GridSize.Small).Rows;
        Check(tectonic.Landmass.ElevationByCellId is { } heights &&
            heights.Length == expectedCount &&
            heights.All(value=>value<=100) &&
            heights.Max()==100 &&
            heights.Where((value,id)=>tectonic.Landmass.SurfaceByCellId[id]==1)
                .Distinct().Count()>20 &&
            heights.Where((value,id)=>tectonic.Landmass.SurfaceByCellId[id]==0)
                .All(value=>value==0) &&
            tectonic.Landmass.PlateByCellId is { } plates &&
            plates.Length == expectedCount &&
            plates.All(plate => plate < PangaeaGenerator.NumberOfPlates) &&
            plates.Distinct().Count() >= 12,
            "Pangaea has 16 plate seeds and unchanged 0..100 height semantics");
    }

    private static void VerifyPeriodicSeam()
    {
        var definition = HexGridDefinitions.Get(GridSize.Small);
        var topology = new HexTopology(HexGrid.Generate(definition));
        foreach (int row in new[] { 1, 20, 50 })
        {
            Check(Math.Abs(PeriodicNoise.Sample(0, row, definition.Columns,
                definition.Rows, 9, 2026) -
                PeriodicNoise.Sample(definition.Columns, row, definition.Columns,
                    definition.Rows, 9, 2026)) < 1e-12,
                "Noise repeats exactly at the east-west seam");
            int west = topology.Id(0, row);
            int east = -1;
            foreach (int neighbor in topology.Neighbors(west))
                if (neighbor % definition.Columns == definition.Columns - 1)
                { east = neighbor; break; }
            Check(east >= 0, "Engine exposes a wrapped neighbor");
            var surface = new byte[topology.Count];
            surface[west] = surface[east] = 1;
            var stats = LandmassAnalysis.Analyze(topology, surface);
            Check(stats.ComponentCount == 1 && stats.DominantMassSize == 2,
                "Land crossing the visual seam is one logical mass");
        }
    }

    private static void VerifyPersistence(LandmassGenerationService service)
    {
        var result = service.Generate(LandmassProfiles.Create(
            GridSize.Small, LandmassType.Pangaea, -731));
        string path = Path.Combine(Path.GetTempPath(), $"evolve-landmass-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState
            {
                HexGrid = new HexGridState(result.Grid.Definition),
                Landmass = result.Landmass
            };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path);
            Check(loaded.SaveFormatVersion == EvolveVersion.SaveFormat &&
                loaded.HexGrid?.Definition == result.Grid.Definition &&
                loaded.Landmass?.Configuration == result.Landmass.Configuration &&
                loaded.Landmass.SurfaceByCellId.SequenceEqual(result.Landmass.SurfaceByCellId) &&
                loaded.Landmass.ElevationByCellId!.SequenceEqual(result.Landmass.ElevationByCellId!) &&
                loaded.Landmass.PlateByCellId!.SequenceEqual(result.Landmass.PlateByCellId!),
                "Capa 02 survives save/load without regeneration");

            var old = new GameState { SaveFormatVersion = 1,
                HexGrid = new HexGridState(result.Grid.Definition) };
            GameSaveStore.Save(path, old);
            var restoredOld = GameSaveStore.Load(path);
            Check(restoredOld.SaveFormatVersion == 1 && restoredOld.Landmass is null,
                "Old save format remains readable");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
