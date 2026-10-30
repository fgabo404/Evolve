using GEvolve.Engine;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

if (args.Contains("--tectonic-motion")) { TectonicMotionTests.Run(); return 0; }
if (args.Contains("--all-suites"))
{
    (string Name, Action Run)[] suites = [
        ("World", () => { if (WorldTests.Run() != 0) throw new Exception("World tests failed"); }),
        ("Grid", GridTests.Run), ("Landmass", LandmassTests.Run), ("Biomes", BiomeTests.Run),
        ("Slope", SlopeTests.Run), ("Temperature", TemperatureTests.Run),
        ("Precipitation", PrecipitationTests.Run), ("PlateBoundary", PlateBoundaryTests.Run),
        ("TectonicPlate", TectonicPlateTests.Run), ("StagedLandmass", StagedLandmassTests.Run),
        ("Game", GameTests.Run), ("TectonicMotion", TectonicMotionTests.Run)];
    int failures = 0;
    foreach (var suite in suites)
    {
        try { suite.Run(); Console.WriteLine($"SUITE PASS: {suite.Name}"); }
        catch (Exception error) { failures++; Console.WriteLine($"SUITE FAIL: {suite.Name}: {error}"); }
    }
    return failures == 0 ? 0 : 1;
}

var state = new GameState { WorldSettings = new WorldSettings(Ancho: 20, Alto: 20, Semilla: 42) };
var simulation = new SimulationEngine(state);
simulation.AdvanceTurns(3);
if (state.Turn != 3) throw new Exception("Turn advancement failed.");
string path = Path.Combine(Path.GetTempPath(), $"evolve-test-{Guid.NewGuid():N}.json");
try
{
    GameSaveStore.Save(path, state);
    var loaded = GameSaveStore.Load(path);
    if (loaded.Turn != 3 || loaded.WorldSettings.Semilla != 42)
        throw new Exception("Save/load test failed.");
    var otherWorld = new SimulationEngine(loaded).World;
    if (!simulation.World.Celdas.SequenceEqual(otherWorld.Celdas))
        throw new Exception("World generation is not deterministic.");
    Console.WriteLine("PASS: turns, save/load, deterministic world");
}
finally { if (File.Exists(path)) File.Delete(path); }

if (WorldTests.Run() != 0) return 1;
GridTests.Run();
LandmassTests.Run();
BiomeTests.Run();
SlopeTests.Run();
TemperatureTests.Run();
PrecipitationTests.Run();
PlateBoundaryTests.Run();
TectonicPlateTests.Run();
StagedLandmassTests.Run();
GameTests.Run();
TectonicMotionTests.Run();
return 0;
