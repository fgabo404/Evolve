using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.Climate;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaEcologyTests
{
    private static readonly int[] Seeds = [2026, -1387338765, -2024691550, -174327693];
    public static void Run()
    {
        var catalog = BiomeCatalog.Default;
        Check(catalog.Rules.Count == 22 && catalog.Rules.Select(r => r.BiomeId).Distinct().Count() == 8, "Original catalog coverage");
        Console.WriteLine("ECOLOGY_ENV " + JsonSerializer.Serialize(new { CatalogHash = catalog.ContentHash,
            Temperature = TemperatureGenerator.Version, Precipitation = PrecipitationGenerator.Version, Classifier = BiomeClassifier.Version,
            CatalogRules = catalog.Rules.Count, Worlds = 16, MoistureUnit = "index 0..100, not relative atmospheric humidity" }));
        int finalSensitivity = 0;
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in Seeds)
        {
            var service = new WorldGenerationService(); var config = LandmassProfiles.Create(size, LandmassType.Pangaea, seed);
            var geology = service.GenerateGeology(config); var world = service.Generate(config);
            var t = world.Landmass; var topo = new HexTopology(world.Grid);
            string geoHash = Hash(geology.Landmass);
            Check(geoHash == Hash(Geology(t)) && Hash(geology.PangaeaElevations!.FinalByCellId.ToArray()) == Hash(world.PangaeaElevations!.FinalByCellId.ToArray()),
                "Ecology cannot alter any geological state or FINAL diagnostics");
            var slopes = TerrainSlope.Calculate(topo, geology.Landmass)!;
            var temperature = new TemperatureGenerator().Apply(topo, geology.Landmass with { SlopeByCellId = slopes });
            var rain = new PrecipitationGenerator().Apply(topo, temperature);
            var expected = new BiomeClassifier().Apply(rain, topo);
            Check(Hash(t) == Hash(expected), "Official automatic integration reuses exactly original slope/climate/classifier outputs");
            Check(Hash(t) == Hash(service.Generate(config).Landmass), "Deterministic ecological state");
            var baseClimate = new TemperatureGenerator().Apply(topo, geology.Landmass with { ElevationByCellId = world.PangaeaElevations!.BaseByCellId.ToArray() });
            if (!temperature.Climate!.BaseTemperatureTenthsByCellId.SequenceEqual(baseClimate.Climate!.BaseTemperatureTenthsByCellId)) finalSensitivity++;
            Validate(t, catalog);
            Persist(world); Persist(geology);
            if (seed == 2026) Manual(config, world);
            Console.WriteLine("ECOLOGY_WORLD " + JsonSerializer.Serialize(Distribution(t, geoHash, catalog)));
            Console.WriteLine($"ECOLOGY_CASE_PASS {size}/{seed}: unchanged complete geology; FINAL climate; original catalog eligibility; exact automatic/manual composition; determinism; persistence");
        }
        Check(finalSensitivity > 0, "Evidence must distinguish FINAL-driven climate from BASE-driven climate");
        Console.WriteLine($"ECOLOGY_PASS: 16 worlds, four sizes, {finalSensitivity} FINAL/BASE climate-sensitive worlds; no quotas or calibration; geological equivalence and historical save contracts preserved");
    }
    private static void Manual(LandmassConfiguration config, GeneratedWorld automatic)
    {
        var session = new WorldGenerationService().CreatePangaeaSession(config);
        foreach (var stage in PangaeaGenerationSession.GeologicalStages) session.Execute(stage);
        var geo = session.GetSnapshot();
        Check(geo.Slopes is null && geo.Climate is null && geo.Biomes is null, "FINAL snapshot contains no fabricated ecology");
        Expect<InvalidOperationException>(() => session.Execute(PangaeaGenerationStage.Biomes));
        session.Execute(PangaeaGenerationStage.Temperature);
        var temp = session.GetSnapshot();
        Check(temp.Slopes is not null && temp.Climate is not null && temp.Climate.BasePrecipitationMmByCellId is null && temp.Biomes is null, "Temperature includes real slopes only");
        Expect<InvalidOperationException>(() => session.Execute(PangaeaGenerationStage.Biomes));
        session.Execute(PangaeaGenerationStage.Precipitation);
        var rain = session.GetSnapshot();
        Check(rain.Climate?.BaseMoistureByCellId is not null && rain.Biomes is null, "Rain creates paired real moisture/rain fields");
        session.Execute(PangaeaGenerationStage.Biomes);
        Check(Hash(session.GetCompletedWorld().Landmass) == Hash(automatic.Landmass), "Same session produces byte-exact automatic ecology");
        var detached = session.GetSnapshot(); detached.Slopes![0] = 99; detached.Climate!.BaseTemperatureTenthsByCellId[0] = 999;
        detached.Climate.BasePrecipitationMmByCellId![0] = ushort.MaxValue; detached.Biomes!.AssignmentByCellId[0] = 255;
        Check(Hash(session.GetCompletedWorld().Landmass) == Hash(automatic.Landmass), "Ecological snapshots are detached from cached inputs");
        session.Execute(PangaeaGenerationStage.Precipitation);
        var repeatedRain = session.GetSnapshot();
        Check(repeatedRain.Biomes is null && repeatedRain.Climate is not null && repeatedRain.Slopes is not null, "Rain repeat invalidates biomes, preserving slopes/temperature");
        session.Execute(PangaeaGenerationStage.Biomes);
        session.Execute(PangaeaGenerationStage.Temperature);
        var repeatedTemp = session.GetSnapshot();
        Check(repeatedTemp.Biomes is null && repeatedTemp.Climate!.BaseMoistureByCellId is null, "Temperature repeat removes old rain/moisture/biomes");
        session.Execute(PangaeaGenerationStage.Precipitation); session.Execute(PangaeaGenerationStage.Biomes);
        session.Execute(PangaeaGenerationStage.Final);
        var reset = session.GetSnapshot();
        Check(reset.Slopes is null && reset.Climate is null && reset.Biomes is null &&
            reset.Stages.Where(s => (int)s.Stage > 7).All(s => s.Status == PangaeaStageStatus.Invalidated), "FINAL repeat clears all ecological descendants immediately");
        foreach (var stage in new[] { PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes }) session.Execute(stage);
        Check(Hash(session.GetCompletedWorld().Landmass) == Hash(automatic.Landmass), "Regeneration preserves ecological determinism");
        session.Reset(config with { Seed = config.Seed + 1 });
        Check(session.GetSnapshot().Climate is null && session.GetSnapshot().Biomes is null, "Configuration reset cannot expose old ecology");
        Laboratory(config, automatic);
    }
    private static void Laboratory(LandmassConfiguration config, GeneratedWorld automatic)
    {
        using var lab = new PangaeaLaboratory(); lab.ChooseSize(config.Size); lab.SetSeedText(config.Seed.ToString()); lab.RequestRestart(); Wait(lab);
        Check(PangaeaLaboratoryLayout.VisibleStages.Length == 8 && !lab.CanExecute(PangaeaGenerationStage.Temperature), "Five geology controls plus three ecology controls with real dependency gates");
        lab.ExecutePlatesAndMotion(); Wait(lab);
        foreach (var stage in new[] { PangaeaGenerationStage.Surface, PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final })
        { lab.Execute(stage); Wait(lab); }
        Check(!lab.ChooseView(MapViewMode.Biomes) && lab.Map!.Snapshot.Climate is null, "Biome view unavailable before actual climate");
        foreach (var stage in new[] { PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes })
        { Check(lab.CanExecute(stage), "Ecological UI dependency available"); lab.Execute(stage); Wait(lab); Check(lab.Error is null, "Ecological controller completed"); }
        var map = lab.Map!;
        Check(lab.View == MapViewMode.Biomes && map.Snapshot.Biomes is not null && Hash(map.Snapshot.Biomes) == Hash(automatic.Landmass.Biomes), "BIOMAS view reads same automatic state");
        lab.SelectAt(map.Snapshot.Grid!.Cells[0].Center);
        foreach (string field in new[] { "BIOMA:", "SUBBIOMA:", "FINAL:", "PENDIENTE:", "LATITUD:", "TEMPERATURA:", "PRECIPITACIÓN:", "HUMEDAD:", "CLASIFICACIÓN:" })
            Check(lab.Inspect().Any(line => line.StartsWith(field, StringComparison.Ordinal)), "Cell inspector contains actual ecology field: " + field);
        foreach (var view in map.AvailableViews) { Check(lab.ChooseView(view), "Ecological layer selector"); _ = map.Color(0, view); }
        PangaeaHeightLabelTests.Run(map);
        lab.Execute(PangaeaGenerationStage.Precipitation);
        Check(lab.Map!.Snapshot.Biomes is null && lab.Map.Snapshot.Climate!.BasePrecipitationMmByCellId is null && !lab.Map.AvailableViews.Contains(MapViewMode.Biomes),
            "Controller prunes old rain and biome views before worker finishes");
        Wait(lab); lab.Execute(PangaeaGenerationStage.Biomes); Wait(lab);
        lab.Execute(PangaeaGenerationStage.Final);
        Check(lab.Map!.Snapshot.Climate is null && lab.Map.Snapshot.Slopes is null && lab.Map.Snapshot.Biomes is null, "FINAL UI repeat hides every stale ecological field");
        Wait(lab);
        Console.WriteLine($"ECOLOGY_SESSION_PASS {config.Size}: stage dependencies, isolated snapshots, downstream invalidation, eight buttons, BIOMAS/inspector, exact automatic state, original readable labels");
    }
    private static void Validate(LandmassState t, BiomeCatalog catalog)
    {
        int n = t.SurfaceByCellId.Length; var def = HexGridDefinitions.Get(t.Configuration.Size);
        Check(t.SlopeByCellId?.Length == n && t.Climate?.BaseMoistureByCellId?.Length == n && t.Biomes?.AssignmentByCellId.Length == n &&
            t.Biomes.CatalogHash == catalog.ContentHash && t.Biomes.ClassifierVersion == BiomeClassifier.Version, "Complete original catalog climate and slope state");
        var rules = catalog.Rules.ToDictionary(r => r.Subbiome.Id, StringComparer.Ordinal);
        for (int id = 0; id < n; id++)
        {
            var biome = t.Biomes!.Get(id); double latitude = BiomeClassifier.Latitude(id / def.Columns, def.Rows);
            Check(biome.Status is BiomeClassificationStatus.Assigned or BiomeClassificationStatus.NoCatalogMatch, "Explicit valid or no-match status");
            if (biome.Status == BiomeClassificationStatus.Assigned)
                Check(rules.TryGetValue(biome.SubbiomeId!, out var rule) && rule.BiomeId == biome.BiomeId &&
                    BiomeClassifier.Matches(rule, t.IsLand(id), t.ElevationByCellId![id], latitude), "IDs and JSON elevation/latitude/surface eligibility");
            else Check(!catalog.Rules.Any(r => BiomeClassifier.Matches(r, t.IsLand(id), t.ElevationByCellId![id], latitude)), "No-match is justified by absence of eligible catalog entry");
            Check(t.IsLand(id) ? biome.BiomeId != "marine" : biome.BiomeId == "marine", "Correct ocean/land classification");
        }
    }
    private static object Distribution(LandmassState t, string geologyHash, BiomeCatalog catalog)
    {
        var def = HexGridDefinitions.Get(t.Configuration.Size); int n = t.SurfaceByCellId.Length;
        var land = Enumerable.Range(0, n).Where(t.IsLand).ToArray();
        double Pct(int count, int denominator) => denominator == 0 ? 0 : 100.0 * count / denominator;
        string Bio(int id) => t.Biomes!.Get(id).BiomeId ?? "NO_MATCH";
        string Sub(int id) => t.Biomes!.Get(id).SubbiomeId ?? "NO_MATCH";
        object Groups(IEnumerable<int> ids, Func<int, string> key) => ids.GroupBy(key).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new {
            Id = g.Key, Cells = g.Count(), PctWithinGroup = Pct(g.Count(), ids.Count()), PctLand = Pct(g.Count(i => t.IsLand(i)), land.Length), PctWorld = Pct(g.Count(), n),
            MeanElevation = g.Average(i => t.ElevationByCellId![i]), MeanSlope = g.Average(i => t.SlopeByCellId![i]),
            MeanTemperatureC = g.Average(i => t.Climate!.BaseCelsius(i)), MeanRainMm = g.Average(i => t.Climate!.BasePrecipitationMmByCellId![i]),
            MeanMoisture = g.Average(i => t.Climate!.BaseMoistureByCellId![i]) }).ToArray();
        var rain = land.Select(i => (int)t.Climate!.BasePrecipitationMmByCellId![i]).Order().ToArray();
        bool Arid(int i) => t.Climate!.BasePrecipitationMmByCellId![i] < 250 && t.Climate.BaseMoistureByCellId![i] <= 25;
        var flags = new Dictionary<string, int>();
        var samples = new Dictionary<string, List<object>>();
        void Flag(string name, int id, BiomeRule[] eligible)
        {
            flags[name] = flags.GetValueOrDefault(name) + 1;
            if (!samples.TryGetValue(name, out var examples)) samples[name] = examples = [];
            if (examples.Count < 5) examples.Add(new { CellId = id, Column = id % def.Columns, Row = id / def.Columns,
                Biome = Bio(id), Subbiome = Sub(id), Elevation = t.ElevationByCellId![id], Slope = t.SlopeByCellId![id],
                Latitude = BiomeClassifier.Latitude(id / def.Columns, def.Rows), TemperatureC = t.Climate!.BaseCelsius(id),
                RainMm = t.Climate.BasePrecipitationMmByCellId![id], Moisture = t.Climate.BaseMoistureByCellId![id],
                EligibleSubbiomes = eligible.Select(r => r.Subbiome.Id).ToArray() });
        }
        foreach (int id in land)
        {
            double lat = BiomeClassifier.Latitude(id / def.Columns, def.Rows);
            var eligible = catalog.Rules.Where(r => BiomeClassifier.Matches(r, true, t.ElevationByCellId![id], lat)).ToArray();
            if (Arid(id) && !eligible.Any(r => r.BiomeId == "desert")) Flag("AridWithoutCatalogEligibleDesert", id, eligible);
            if (Arid(id) && Bio(id) == "forest") Flag("AridForestPriorityFallback", id, eligible);
            if (Bio(id) == "mountain" && eligible.Any(r => r.BiomeId is "forest" or "plains")) Flag("ReliefPriorityOverEligibleVegetation", id, eligible);
            if (Bio(id) == "desert" && (!Arid(id) || t.Climate!.BaseCelsius(id) < -4)) Flag("DesertOutsidePreferredAridWarmConditions", id, eligible);
            if (Sub(id) == "forest_tropical" && !(t.Climate!.BaseCelsius(id) >= 20 && t.Climate.BasePrecipitationMmByCellId![id] >= 1800 && t.Climate.BaseMoistureByCellId![id] >= 58))
                Flag("TropicalOutsidePreferredClimate", id, eligible);
            if (t.Climate!.BaseCelsius(id) >= 20 && t.Climate.BasePrecipitationMmByCellId![id] >= 1800 && t.Climate.BaseMoistureByCellId![id] >= 58 && Bio(id) != "forest")
                Flag(eligible.Any(r => r.Subbiome.Id == "forest_tropical") ? "WarmWetNonForestPriority" : "WarmWetNoTropicalCatalogEligibility", id, eligible);
        }
        return new { Size = t.Configuration.Size.ToString(), t.Configuration.Seed, GeologyHash = geologyHash, Cells = n, LandCells = land.Length,
            LandBiomes = Groups(land, Bio), Subbiomes = Groups(Enumerable.Range(0, n), Sub),
            LatitudeBands = land.GroupBy(i => Math.Min(5, (int)(Math.Abs(BiomeClassifier.Latitude(i / def.Columns, def.Rows)) / 15)))
                .OrderBy(g => g.Key).Select(g => new { AbsoluteLatitudeMin = g.Key * 15, Max = g.Key * 15 + 15, Distribution = Groups(g, Bio) }).ToArray(),
            ElevationBands = land.GroupBy(i => Math.Min(4, (t.ElevationByCellId![i] - 1) / 20)).OrderBy(g => g.Key)
                .Select(g => new { Min = g.Key * 20 + 1, Max = g.Key * 20 + 20, Distribution = Groups(g, Bio) }).ToArray(),
            AridCells = land.Count(Arid), AridPctLand = Pct(land.Count(Arid), land.Length), RainBelow500PctLand = Pct(rain.Count(r => r < 500), land.Length),
            RainMin = rain[0], RainP10 = rain[(int)((rain.Length - 1) * .1)], RainMedian = rain[(rain.Length - 1) / 2], RainP90 = rain[(int)((rain.Length - 1) * .9)], RainMax = rain[^1],
            Unclassified = Enumerable.Range(0, n).Count(i => t.Biomes!.Get(i).Status != BiomeClassificationStatus.Assigned),
            IncorrectOcean = Enumerable.Range(0, n).Count(i => !t.IsLand(i) && Bio(i) != "marine"), PriorityObservations = flags, PrioritySamples = samples };
    }
    private static void Persist(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-ecology-{Guid.NewGuid():N}.json");
        try { GameSaveStore.Save(path, new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass });
            Check(Hash(GameSaveStore.Load(path).Landmass) == Hash(world.Landmass), "Historical and ecological states persist exactly without migration"); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static LandmassState Geology(LandmassState t) => t with { SlopeByCellId = null, Climate = null, Biomes = null };
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static void Wait(PangaeaLaboratory lab) { var limit = System.Diagnostics.Stopwatch.StartNew(); while (lab.Busy) {
        lab.Update(); if (limit.Elapsed.TotalSeconds > 90) throw new TimeoutException("Ecology controller timeout"); Thread.Sleep(1); } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
