using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.Climate;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class TemperatureTests
{
    public static void Run()
    {
        var generator = new TemperatureGenerator();
        Check(generator.CalculateCelsius(0, 0, true, -1, 0) == 27 &&
            generator.CalculateCelsius(90, 0, true, -1, 0) == -23 &&
            generator.CalculateCelsius(-90, 0, true, -1, 0) == -23, "Latitude endpoints and negative values");
        Check(Math.Abs(generator.CalculateCelsius(30, 0, true, 3, 0) -
            generator.CalculateCelsius(30, 100, true, 3, 0) - 26) < 1e-10, "Lapse is 26 C per 4000 m");
        Check(generator.CalculateCelsius(0, 0, true, 1, 0) < generator.CalculateCelsius(0, 0, true, 40, 0) &&
            generator.CalculateCelsius(80, 0, true, 1, 0) > generator.CalculateCelsius(80, 0, true, 40, 0),
            "Ocean cools warm coasts and warms cold coasts");
        for (int latitude = -90; latitude <= 90; latitude++)
        for (int distance = 1; distance < 100; distance++)
            Check(Math.Abs(generator.CalculateCelsius(latitude, 0, true, distance, 0) -
                generator.CalculateCelsius(latitude, 0, true, distance + 1, 0)) < .5,
                "Ocean moderation decays without abrupt inland jumps");

        var definition = HexGridDefinitions.Get(GridSize.Small);
        var topology = new HexTopology(HexGrid.Generate(definition));
        var configuration = LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 42);
        var surface = Enumerable.Repeat((byte)1, topology.Count).ToArray();
        var flat = new LandmassState(configuration, surface, 0)
            { ElevationByCellId = new byte[topology.Count] };
        Check(TemperatureGenerator.OceanDistances(topology, flat).All(value => value == -1),
            "All-land world has no ocean influence");
        int origin = topology.Id(0, definition.Rows / 2);
        surface[origin] = 0;
        var distances = TemperatureGenerator.OceanDistances(topology, flat);
        Check(distances[origin] == 0 && topology.Neighbors(origin).ToArray().All(id => distances[id] == 1),
            "Ocean BFS respects all six neighbors including horizontal seam");
        for (int id = 0; id < topology.Count; id++)
            foreach (int neighbor in topology.Neighbors(id))
                Check(Math.Abs(distances[id] - distances[neighbor]) <= 1, "BFS distances are locally continuous");
        Array.Clear(surface);
        Check(TemperatureGenerator.OceanDistances(topology, flat).All(value => value == 0), "All-ocean distances");
        var flatClimate = generator.Apply(topology, flat).Climate!;
        for (int row = 0; row < definition.Rows; row++)
        {
            double first = PeriodicNoise.Sample(0, row, definition.Columns, definition.Rows, 2, 42);
            double wrapped = PeriodicNoise.Sample(definition.Columns, row, definition.Columns, definition.Rows, 2, 42);
            Check(Math.Abs(first - wrapped) < 1e-12, "Regional noise is exactly periodic");
            Check(Math.Abs(flatClimate.BaseCelsius(row * definition.Columns) -
                flatClimate.BaseCelsius((row + 1) * definition.Columns - 1)) <= .3,
                "Flat ocean temperatures are continuous across map seam");
        }
        Check(ReferenceEquals(generator.Apply(topology, flat with { ElevationByCellId = null }).Climate, null),
            "Other generators without heights retain missing climate");
        Check(!flatClimate.BaseTemperatureTenthsByCellId.SequenceEqual(generator.Apply(topology,
            flat with { Configuration = configuration with { Seed = 43 } }).Climate!.BaseTemperatureTenthsByCellId),
            "Regional temperature varies with seed");
        VerifyPalette();

        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var generated = new LandmassGenerationService().Generate(LandmassProfiles.Create(size, LandmassType.Pangaea, seed));
            var terrain = generated.Landmass;
            var climate = terrain.Climate!;
            var repeated = generator.Apply(new HexTopology(generated.Grid), terrain);
            Check(climate.BaseTemperatureTenthsByCellId.Length == terrain.SurfaceByCellId.Length &&
                climate.BaseTemperatureTenthsByCellId.SequenceEqual(repeated.Climate!.BaseTemperatureTenthsByCellId),
                "Complete deterministic temperature coverage");
            Check(ReferenceEquals(terrain.SurfaceByCellId, repeated.SurfaceByCellId) &&
                ReferenceEquals(terrain.ElevationByCellId, repeated.ElevationByCellId) &&
                ReferenceEquals(terrain.PlateByCellId, repeated.PlateByCellId) &&
                ReferenceEquals(terrain.SlopeByCellId, repeated.SlopeByCellId) &&
                ReferenceEquals(terrain.Biomes, repeated.Biomes), "Climate leaves terrain, slopes and biomes intact");
            for (int id = 0; id < terrain.SurfaceByCellId.Length; id++)
            {
                Check(climate.CurrentCelsius(id) == climate.BaseCelsius(id), "Initial current equals baseline");
                foreach (var mode in Enum.GetValues<MapViewMode>())
                {
                    var color = MapViewColors.ForCell(terrain, id, mode);
                    if (mode == MapViewMode.Infrared)
                        Check(color == ThermalPalette.ForCelsius(climate.CurrentCelsius(id)), "Thermal palette includes oceans and summits");
                    else if (terrain.IsImpassableSummit(id))
                        Check(color == Vector3.One, "Summits stay white outside infrared");
                }
            }
            Check(climate.BaseTemperatureTenthsByCellId.SequenceEqual(repeated.Climate!.BaseTemperatureTenthsByCellId),
                "View color queries never mutate climate");
            if (seed == 2026) VerifyPersistence(generated);
            var values = climate.BaseTemperatureTenthsByCellId;
            Console.WriteLine(FormattableString.Invariant($"TEMPERATURE {size}/{seed}: min {values.Min()/10.0:F1}, max {values.Max()/10.0:F1}, mean {values.Average(v => (double)v)/10:F2} C"));
        }
        Console.WriteLine("PASS: climate determinism, latitude, lapse, oceans, seam, persistence, view colors and palette");
    }

    private static void VerifyPalette()
    {
        (double Temperature, int Hex)[] stops = [(-40, 0x281A59), (-20, 0x3457A4),
            (0, 0x54C9E8), (10, 0x63BA82), (20, 0xE8D66D), (30, 0xE87836), (40, 0xC72D32)];
        foreach (var (temperature, hex) in stops)
            Check(Vector3.Distance(ThermalPalette.ForCelsius(temperature),
                new Vector3((hex >> 16 & 255)/255f, (hex >> 8 & 255)/255f, (hex & 255)/255f)) < 1e-6,
                "Exact thermal palette anchors");
        for (int i = 1; i < stops.Length; i++)
            Check(Vector3.Distance(ThermalPalette.ForCelsius((stops[i-1].Temperature + stops[i].Temperature)/2),
                (ThermalPalette.ForCelsius(stops[i-1].Temperature) + ThermalPalette.ForCelsius(stops[i].Temperature))/2) < 1e-6,
                "Continuous linear interpolation");
        Check(ThermalPalette.ForCelsius(-100) == ThermalPalette.ForCelsius(-40) &&
            ThermalPalette.ForCelsius(100) == ThermalPalette.ForCelsius(40), "Palette clamps only colors");
    }

    private static void VerifyPersistence(GeneratedWorld generated)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-temperature-{Guid.NewGuid():N}.json");
        try
        {
            var baseline = generated.Landmass.Climate!;
            var modified = baseline with { GlobalOffsetTenths = 12, RegionalOffsetsTenthsByCellId = new() { [0] = -7 } };
            Check(Math.Abs(modified.CurrentCelsius(0) - baseline.BaseCelsius(0) - .5) < 1e-10 &&
                Math.Abs(modified.CurrentCelsius(1) - baseline.BaseCelsius(1) - 1.2) < 1e-10,
                "Global and sparse regional modifiers compose without changing baseline");
            var state = new GameState { HexGrid = new(generated.Grid.Definition),
                Landmass = generated.Landmass with { Climate = modified } };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!.Climate!;
            Check(loaded.BaseTemperatureTenthsByCellId.SequenceEqual(baseline.BaseTemperatureTenthsByCellId) &&
                loaded.CurrentCelsius(0) == modified.CurrentCelsius(0), "Climate and modifiers round-trip");
            state.Landmass = generated.Landmass with { Climate = null };
            GameSaveStore.Save(path, state);
            // Emulate an older JSON which does not contain the optional property at all.
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            json["Landmass"]!.AsObject().Remove("Climate");
            File.WriteAllText(path, json.ToJsonString());
            Check(GameSaveStore.Load(path).Landmass!.Climate is null, "Old saves without climate remain readable");
            state.Landmass = generated.Landmass with { Climate = new("bad", [1]) };
            try { GameSaveStore.Save(path, state); throw new Exception("Malformed climate accepted"); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
