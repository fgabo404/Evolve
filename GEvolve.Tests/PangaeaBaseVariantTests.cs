using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaBaseVariantTests
{
    private static readonly int[] Seeds = [-1387338765, -132790047, 2026];
    private const string Historical = "GEvolve.Tests/ValidationReports/P03-A74_20261108_222619723Z_ca4e68da/base-audit.log";
    public static void Run() => Run(false);
    public static void RunLowlands() => Run(true);
    private static void Run(bool lowlands)
    {
        PangaeaBaseVariantControls.Run();
        if (lowlands) PangaeaRegionalLowlandVariants.Controls();
        var history = ReadHistory(); int comparisons = 0;
        var definitions = lowlands ? PangaeaRegionalLowlandVariants.Definitions : PangaeaRegionalLowlandVariants.Legacy;
        var oldResults = lowlands ? ReadPreviousVariants() : new Dictionary<string, JsonElement>();
        int[] seeds = lowlands ? [.. Seeds, 42] : Seeds;
        Console.WriteLine("BASE_VARIANT_ENV " + JsonSerializer.Serialize(new { Worlds = seeds.Length * 4, PrimaryWorlds = 12, HoldoutWorlds = lowlands ? 4 : 0, Variants = definitions.Length,
            Definitions = definitions,
            Mode = "Tests only, official surface/fields, no experimental FINAL/save", KernelRepeats = 3,
            PangaeaBaseVariants.NoiseAmplitude, PangaeaBaseVariants.Shoulder, PangaeaBaseVariants.LandMinimum, PangaeaBaseVariants.LandMaximum }));
        foreach (var size in Enum.GetValues<GridSize>()) foreach (int seed in seeds)
        {
            Compare(size, seed, history, oldResults, lowlands); comparisons += definitions.Length;
        }
        Console.WriteLine($"BASE_VARIANT_PASS: {comparisons} comparisons = {seeds.Length * 4} configurations, controlled variants; exact V0 F/G/H; old audit morphology; immutable official geology+climate+biomes; deterministic variants; historical/current persistence; no adoption or visual approval");
    }
    private static void Compare(GridSize size, int seed, Dictionary<string, JsonElement> history, Dictionary<string, JsonElement> oldResults, bool lowlands)
    {
        var config = LandmassProfiles.Create(size, LandmassType.Pangaea, seed); var service = new WorldGenerationService();
        long normalBytes = GC.GetAllocatedBytesForCurrentThread(); var normalTimer = Stopwatch.StartNew();
        var official = service.Generate(config); normalTimer.Stop();
        normalBytes = GC.GetAllocatedBytesForCurrentThread() - normalBytes;
        long normalBeforeBytes = normalBytes;
        string beforeHash = Hash(official.Landmass);
        long captureBytes = GC.GetAllocatedBytesForCurrentThread(); var captureTimer = Stopwatch.StartNew();
        var session = service.CreatePangaeaSession(config, captureBaseFields: true);
        foreach (var stage in PangaeaGenerationSession.GeologicalStages) session.Execute(stage);
        var snapshot = session.GetSnapshot();
        var input = PangaeaBaseVariants.Input.From(snapshot); string inputHash = Hash(input);
        Check(Hash(Geology(official.Landmass)) == Hash(session.GetCompletedWorld().Landmass) &&
            official.PangaeaElevations!.BaseByCellId.Span.SequenceEqual(input.Official), "Captured fixture must use exact official surface/geology/BASE");
        foreach (var stage in new[] { PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes }) session.Execute(stage);
        var manual = session.GetCompletedWorld();
        Check(beforeHash == Hash(manual.Landmass), "Complete automatic/manual geology, climate and biomes must remain exact");
        captureTimer.Stop(); captureBytes = GC.GetAllocatedBytesForCurrentThread() - captureBytes;
        var topo = new HexTopology(official.Grid);
        bool detailed = size == GridSize.Small && seed == Seeds[0] || size == GridSize.Enormous && seed == Seeds[1];
        var context = new PangaeaBaseVariantMetrics.Context(topo, official.Landmass, detailed);
        var results = new Dictionary<string, PangaeaBaseVariants.Result>();
        foreach (var definition in lowlands ? PangaeaRegionalLowlandVariants.Definitions : PangaeaRegionalLowlandVariants.Legacy)
        {
            _ = PangaeaRegionalLowlandVariants.Generate(input, definition.Id, context); // warm this kernel, excluded from recorded samples
            var times = new List<double>(); var bytes = new List<long>(); PangaeaBaseVariants.Result result = null!; string? previous = null;
            for (int repeat = 0; repeat < 3; repeat++)
            {
                long allocated = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
                result = PangaeaRegionalLowlandVariants.Generate(input, definition.Id, context); timer.Stop();
                bytes.Add(GC.GetAllocatedBytesForCurrentThread() - allocated); times.Add(timer.Elapsed.TotalMilliseconds);
                string hash = Hash(result.Heights);
                Check(previous is null || previous == hash, "Variant output must be deterministic"); previous = hash;
            }
            results.Add(definition.Id, result);
            Check(Enumerable.Range(0, context.Surface.Length).All(i => context.Surface[i] == 0 ? result.Heights[i] == 0 : result.Heights[i] is >= 1 and <= 100),
                "Every variant retains official land/ocean mask and height contract");
            var parent = definition.Id == "V0" ? result : results[definition.Parent];
            var v0 = results["V0"];
            long metricBytes = GC.GetAllocatedBytesForCurrentThread(); var metricTimer = Stopwatch.StartNew();
            object metrics = PangaeaBaseVariantMetrics.Measure(context, result, v0.Heights, parent.Heights);
            metricTimer.Stop(); metricBytes = GC.GetAllocatedBytesForCurrentThread() - metricBytes;
            using var measured = JsonDocument.Parse(JsonSerializer.Serialize(metrics));
            if (definition.Id == "V0")
            {
                Check(result.Potential.AsSpan().SequenceEqual(input.Fields.Potential.Span), "V0 reproduces captured E exactly");
                if (history.TryGetValue($"world/{size}/{seed}", out var prior))
                    Check(prior.GetProperty("Base").GetString() == Hash(result.Heights) && prior.GetProperty("State").GetString() == Hash(Geology(official.Landmass)),
                        "V0 preserves historical P03-A.7.4 state/BASE hashes");
                foreach (var morph in measured.RootElement.GetProperty("Morphology").EnumerateArray())
                {
                    int threshold = morph.GetProperty("Threshold").GetInt32();
                    if (history.TryGetValue($"morph/{size}/{seed}/{threshold}", out var old))
                        Check(old.GetRawText() == morph.GetProperty("Analysis").GetRawText(), "V0 reproduces historical morphology exactly at every measured common threshold");
                }
            }
            if (definition.Id is "V1T" or "V2" or "V3")
                Check(result.Potential.SequenceEqual(parent.Potential), "Only the declared factor changes relative to immediate parent");
            if (definition.Id == "V1T") Check(result.Normalized.SequenceEqual(parent.Normalized), "Bridge changes summit treatment only");
            if (definition.Id == "V3")
                Check(result.Normalized.SequenceEqual(parent.Normalized) && Enumerable.Range(0, context.Surface.Length)
                    .All(i => parent.Heights[i] > 60 || parent.Heights[i] == result.Heights[i]), "V3 changes only upper treatment, preserving low/middle heights");
            long coastBytes = GC.GetAllocatedBytesForCurrentThread(); var coastTimer = Stopwatch.StartNew();
            object? coastMetrics = lowlands ? PangaeaLowlandMetrics.Measure(context, result.Heights, parent.Heights) : null;
            coastTimer.Stop(); coastBytes = GC.GetAllocatedBytesForCurrentThread() - coastBytes;
            Emit("BASE_VARIANT_RESULT", new { Size = size.ToString(), Seed = seed, Holdout = lowlands && seed == 42, Variant = definition.Id.ToString(), definition.Parent, definition.Hypothesis,
                Lowlands = coastMetrics, CoastalDiagnostics = new { Milliseconds = coastTimer.Elapsed.TotalMilliseconds, AllocatedBytes = coastBytes },
                HeightsHash = Hash(result.Heights), PotentialHash = Hash(result.Potential), result.Anchors,
                Kernel = new { SamplesMs = times, AllocatedBytes = bytes, MedianMs = times.Order().ElementAt(1), MedianAllocatedBytes = bytes.Order().ElementAt(1),
                    OutputPayloadBytes = result.Potential.Length * sizeof(double) + result.Anchors.Length * sizeof(double) + result.Normalized.Length + result.Heights.Length },
                Diagnostics = new { Milliseconds = metricTimer.Elapsed.TotalMilliseconds, AllocatedBytes = metricBytes }, Metrics = measured.RootElement });
            if (lowlands && oldResults.TryGetValue($"{size}/{seed}/{definition.Id}", out var oldResult))
                Check(oldResult.GetProperty("HeightsHash").GetString() == Hash(result.Heights) &&
                    oldResult.GetProperty("PotentialHash").GetString() == Hash(result.Potential) &&
                    oldResult.GetProperty("Metrics").GetRawText() == measured.RootElement.GetRawText(), "The five previous variants retain exact hashes AND numerical results");
            Console.WriteLine($"BASE_VARIANT_CASE_PASS {size}/{seed}/{definition.Id}: deterministic isolated BASE, immutable official surface/inputs, controlled parent comparison");
        }
        if (lowlands) ValidateLowlands(input, context, results);
        Check(inputHash == Hash(input) && beforeHash == Hash(official.Landmass) && beforeHash == Hash(session.GetCompletedWorld().Landmass),
            "All official inputs and completed session remain untouched after every variant");
        Persist(official.Landmass, official); Persist(Geology(official.Landmass), official);
        normalBytes = GC.GetAllocatedBytesForCurrentThread(); var afterTimer = Stopwatch.StartNew();
        var after = service.Generate(config); afterTimer.Stop(); normalBytes = GC.GetAllocatedBytesForCurrentThread() - normalBytes;
        Check(beforeHash == Hash(after.Landmass) && Hash(official.PangaeaElevations!.BaseByCellId.ToArray()) == Hash(after.PangaeaElevations!.BaseByCellId.ToArray()) &&
            Hash(official.PangaeaElevations.FinalByCellId.ToArray()) == Hash(after.PangaeaElevations.FinalByCellId.ToArray()), "Normal official generation must not adopt any experimental value");
        Emit("BASE_VARIANT_WORLD", new { Size = size.ToString(), Seed = seed, Holdout = lowlands && seed == 42, Columns = topo.Columns, Rows = topo.Rows, LandCells = context.Land.Length, Detailed = detailed,
            Official = new { State = beforeHash, Geology = Hash(Geology(official.Landmass)), Base = Hash(input.Official), Final = Hash(official.Landmass.ElevationByCellId!),
                Surface = Hash(official.Landmass.SurfaceByCellId), Plates = Hash(official.Landmass.PlateByCellId!), H04 = Hash(official.Landmass.TectonicMotion),
                Systems = Hash(official.Landmass.PangaeaMountainRanges), Climate = Hash(official.Landmass.Climate), Biomes = Hash(official.Landmass.Biomes),
                Slopes = Hash(official.Landmass.SlopeByCellId!), HistoricalAuditAvailable = history.ContainsKey($"world/{size}/{seed}") },
            CaptureSession = new { Milliseconds = captureTimer.Elapsed.TotalMilliseconds, AllocatedBytes = captureBytes },
            NormalGenerateBeforeMs = normalTimer.Elapsed.TotalMilliseconds, NormalGenerateAfterMs = afterTimer.Elapsed.TotalMilliseconds,
            NormalGenerateBeforeAllocatedBytes = normalBeforeBytes, NormalGenerateAfterAllocatedBytes = normalBytes,
            OfficialUnchanged = true, CurrentAndHistoricalSaveRoundtrip = true });
        Console.WriteLine($"BASE_VARIANT_WORLD_PASS {size}/{seed}: official automatic/manual hashes, climate/biomes, persistence and re-entry unchanged; eight laboratory actions remain original");
    }
    private static Dictionary<string, JsonElement> ReadPreviousVariants()
    {
        var values = new Dictionary<string, JsonElement>();
        const string prefix = "BASE_VARIANT_RESULT ";
        foreach (string line in File.ReadLines("GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/base-variants.log"))
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) continue;
            using var doc = JsonDocument.Parse(line[prefix.Length..]); var item = doc.RootElement;
            values.Add(item.GetProperty("Size").GetString() + "/" + item.GetProperty("Seed").GetInt32() + "/" + item.GetProperty("Variant").GetString(), item.Clone());
        }
        if (values.Count != 60) throw new InvalidOperationException("The five historical variants require all 60 original records.");
        return values;
    }
    private static void ValidateLowlands(PangaeaBaseVariants.Input input, PangaeaBaseVariantMetrics.Context c, Dictionary<string, PangaeaBaseVariants.Result> r)
    {
        Check(PangaeaBaseVariants.GeneratePressureControl(input, true, true).Heights.SequenceEqual(r["V0"].Heights) &&
            PangaeaBaseVariants.GeneratePressureControl(input, false, false).Heights.SequenceEqual(r["V1"].Heights), "Pressure factorial endpoints equal their historical controls");
        Check(r["VP"].Potential.SequenceEqual(r["V0"].Potential) && r["VP"].Normalized.SequenceEqual(r["V0"].Normalized) &&
            r["VS"].Potential.SequenceEqual(r["V1"].Potential) && r["VS"].Normalized.SequenceEqual(r["V1"].Normalized), "Support-only contrasts hold potential AND normalization constant");
        foreach (string id in new[] { "V4O", "V4S", "V4U", "V4C" })
            Check(r[id].Potential.SequenceEqual(r["V2"].Potential), "Regional scale/coast contrasts cannot change the noise field");
        Check(r["V4U"].Normalized.SequenceEqual(r["V4O"].Normalized) && r["V4C"].Normalized.SequenceEqual(r["V4U"].Heights), "Upper/coast parents remain fixed");
        foreach (int id in c.Land)
        {
            if (r["V4O"].Heights[id] <= 50) Check(r["V4U"].Heights[id] == r["V4O"].Heights[id], "Upper curve preserves low/middle heights");
            Check(r["V4C"].Heights[id] <= r["V4U"].Heights[id], "Coastal transition cannot raise the continent");
            if (c.Coast[id] >= PangaeaRegionalLowlandVariants.Radius(c.Topology) || r["V4U"].Heights[id] >= 50)
                Check(r["V4C"].Heights[id] == r["V4U"].Heights[id], "High coast and outside-transition values remain exactly unchanged");
        }
    }
    private static Dictionary<string, JsonElement> ReadHistory()
    {
        var result = new Dictionary<string, JsonElement>();
        foreach (string line in File.ReadLines(Historical))
        {
            bool world = line.StartsWith("BASE_WORLD ", StringComparison.Ordinal), morph = line.StartsWith("BASE_MORPH ", StringComparison.Ordinal);
            if (!world && !morph) continue;
            using var doc = JsonDocument.Parse(line[11..]); var item = doc.RootElement;
            string key = item.GetProperty("Size").GetString() + "/" + item.GetProperty("Seed").GetInt32();
            if (world) result.Add("world/" + key, item.Clone());
            else if (item.GetProperty("Stage").GetString() == "H")
            { var m = item.GetProperty("Morphology"); result.Add("morph/" + key + "/" + m.GetProperty("Threshold").GetInt32(), m.Clone()); }
        }
        if (result.Count != 8 + 8 * 6) throw new InvalidOperationException("Original P03-A.7.4 evidence is incomplete.");
        return result;
    }
    private static LandmassState Geology(LandmassState source) => source with { SlopeByCellId = null, Climate = null, Biomes = null };
    private static void Persist(LandmassState terrain, GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), "evolve-regional-base-" + Guid.NewGuid().ToString("N") + ".json");
        try { GameSaveStore.Save(path, new GameState { HexGrid = new(world.Grid.Definition), Landmass = terrain });
            Check(Hash(GameSaveStore.Load(path).Landmass) == Hash(terrain), "Official historical/current save must roundtrip without experimental fields"); }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void Emit(string tag, object value) => Console.WriteLine(tag + " " + JsonSerializer.Serialize(value));
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
