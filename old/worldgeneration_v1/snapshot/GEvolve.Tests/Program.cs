using GEvolve.Engine;
using GEvolve.Engine.World;
using GEvolve.Engine.World.Climate;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

if (args.Contains("--continental-geology")) { ContinentalGeologyTests.Run(); return 0; }
if (args.Contains("--tectonic-motion")) { TectonicMotionTests.Run(); return 0; }
if (args.Contains("--mountain-ranges")) { PangaeaMountainRangeTests.Run(); return 0; }
if (args.Contains("--pangaea-elevations")) { PangaeaElevationTests.Run(); return 0; }
if (args.Contains("--pangaea-tectonic-audit")) { PangaeaTectonicAuditTests.Run(); return 0; }
if (args.Contains("--pangaea-uplift")) { PangaeaUpliftTests.Run(); return 0; }
if (args.Contains("--pangaea-staged")) { PangaeaStagedGenerationTests.Run(); return 0; }
if (args.Contains("--pangaea-laboratory")) { PangaeaLaboratoryTests.Run(); return 0; }
if (args.Contains("--pangaea-ecology")) { PangaeaEcologyTests.Run(); return 0; }
if (args.Contains("--pangaea-base-reference")) { PangaeaBaseAuditTests.CaptureReference(); return 0; }
if (args.Contains("--pangaea-base-audit")) { PangaeaBaseAuditTests.Run(); return 0; }
if (args.Contains("--pangaea-base-variants-controls")) { PangaeaBaseVariantControls.Run(); return 0; }
if (args.Contains("--pangaea-base-variants")) { PangaeaBaseVariantTests.Run(); return 0; }
if (args.Contains("--pangaea-lowland-variants")) { PangaeaBaseVariantTests.RunLowlands(); return 0; }
if (args.Contains("--pangaea-lowland-controls")) { PangaeaBaseVariantControls.Run(); PangaeaRegionalLowlandVariants.Controls(); return 0; }
if (args.Contains("--pangaea-base-comparator")) { PangaeaBaseComparatorTests.Run(); return 0; }
if (args.Contains("--all-suites"))
{
    (string Name, Action Run)[] suites = [
        ("World", () => { if (WorldTests.Run() != 0) throw new Exception("World tests failed"); }),
        ("Grid", GridTests.Run), ("Landmass", LandmassTests.Run), ("Biomes", BiomeTests.Run),
        ("Slope", SlopeTests.Run), ("Temperature", TemperatureTests.Run),
        ("Precipitation", PrecipitationTests.Run), ("PlateBoundary", PlateBoundaryTests.Run),
        ("TectonicPlate", TectonicPlateTests.Run), ("StagedLandmass", StagedLandmassTests.Run),
        ("Game", GameTests.Run), ("TectonicMotion", TectonicMotionTests.Run), ("ContinentalGeology", ContinentalGeologyTests.Run),
        ("PangaeaMountainRanges", PangaeaMountainRangeTests.Run), ("PangaeaElevations", PangaeaElevationTests.Run),
        ("PangaeaUplift", () => PangaeaUpliftTests.Run()), ("PangaeaStaged", PangaeaStagedGenerationTests.Run),
        ("PangaeaLaboratory", PangaeaLaboratoryTests.Run), ("PangaeaEcology", PangaeaEcologyTests.Run)];
    int failures = 0;
    string? requestedSuite = args.FirstOrDefault(arg => arg.StartsWith("--suite=", StringComparison.Ordinal))?[8..];
    if (requestedSuite is not null && !suites.Any(suite => suite.Name == requestedSuite))
        throw new ArgumentException($"Unknown suite: {requestedSuite}");
    foreach (var suite in suites.Where(suite => requestedSuite is null || suite.Name == requestedSuite))
    {
        try { suite.Run(); Console.WriteLine($"SUITE PASS: {suite.Name}"); }
        catch (Exception error) { failures++; Console.WriteLine($"SUITE FAIL: {suite.Name}: {error}"); }
    }
    return failures == 0 ? 0 : 1;
}

// World generation is external to simulation; environmental generators remain independent.
var generated = new WorldGenerationService().Generate(GridSize.Small, LandmassType.Pangaea, 42);
var topology = new HexTopology(generated.Grid);
var terrain = new TemperatureGenerator().Apply(topology, generated.Landmass);
var state = new GameState
{
    WorldSettings = new WorldSettings(Ancho: 20, Alto: 20, Semilla: 42),
    HexGrid = new HexGridState(generated.Grid.Definition),
    Landmass = terrain
};
var originalSurface = (byte[])terrain.SurfaceByCellId.Clone();
var originalElevation = (byte[])terrain.ElevationByCellId!.Clone();
var originalPlates = (byte[])terrain.PlateByCellId!.Clone();
var originalClimate = (short[])terrain.Climate!.BaseTemperatureTenthsByCellId.Clone();

try { _ = new SimulationEngine(null!); throw new Exception("Null GameState unexpectedly accepted."); }
catch (ArgumentNullException) { }

var simulation = new SimulationEngine(state);
if (!ReferenceEquals(simulation.State, state) ||
    !ReferenceEquals(state.Landmass, terrain))
    throw new Exception("Simulation constructor must preserve supplied state and geography.");
simulation.AdvanceTurns(3);
if (state.Turn != 3) throw new Exception("Turn advancement failed.");
try { simulation.AdvanceTurns(-1); throw new Exception("Negative turn count was accepted."); }
catch (ArgumentOutOfRangeException) { }
if (state.Turn != 3) throw new Exception("Rejected turn count changed state.");
simulation.AdvanceTurn();
if (state.Turn != 4) throw new Exception("Single turn advancement failed.");
if (!ReferenceEquals(state.Landmass, terrain) ||
    !terrain.SurfaceByCellId.SequenceEqual(originalSurface) ||
    !terrain.ElevationByCellId!.SequenceEqual(originalElevation) ||
    !terrain.PlateByCellId!.SequenceEqual(originalPlates) ||
    !terrain.Climate!.BaseTemperatureTenthsByCellId.SequenceEqual(originalClimate))
    throw new Exception("Simulation construction or turns modified world geography or climate.");
string path = Path.Combine(Path.GetTempPath(), $"evolve-test-{Guid.NewGuid():N}.json");
try
{
    GameSaveStore.Save(path, state);
    var loaded = GameSaveStore.Load(path);
    if (loaded.Turn != 4 || loaded.WorldSettings.Semilla != 42 ||
        loaded.HexGrid?.Definition != generated.Grid.Definition ||
        loaded.Landmass?.Configuration != terrain.Configuration ||
        !loaded.Landmass.SurfaceByCellId.SequenceEqual(originalSurface) ||
        !loaded.Landmass.ElevationByCellId!.SequenceEqual(originalElevation) ||
        !loaded.Landmass.PlateByCellId!.SequenceEqual(originalPlates) ||
        !loaded.Landmass.Climate!.BaseTemperatureTenthsByCellId.SequenceEqual(originalClimate))
        throw new Exception("Save/load must preserve turns, WorldSettings and official world layers.");
    var restoredSimulation = new SimulationEngine(loaded);
    if (!ReferenceEquals(restoredSimulation.State, loaded) || loaded.Turn != 4)
        throw new Exception("Restoring the simulation changed world state.");
    Console.WriteLine("PASS: simulation uses external official world; turns, unchanged layers and save/load");
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
ContinentalGeologyTests.Run();
PangaeaMountainRangeTests.Run();
PangaeaElevationTests.Run();
return 0;
