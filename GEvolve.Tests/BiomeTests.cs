using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class BiomeTests
{
    // Measured before biomes-2 on the unchanged terrain of commit 29bc6aa.
    private static readonly Dictionary<(GridSize, int), int> MountainBaseline = new()
    {
        [(GridSize.Small, 2026)] = 716, [(GridSize.Small, 42)] = 909, [(GridSize.Small, -731)] = 939,
        [(GridSize.Medium, 2026)] = 1799, [(GridSize.Medium, 42)] = 2001, [(GridSize.Medium, -731)] = 2136,
        [(GridSize.Large, 2026)] = 5736, [(GridSize.Large, 42)] = 7417, [(GridSize.Large, -731)] = 5134
    };
    public static void Run()
    {
        // Captured from 7ebfe5b before adding slope or changing catalog ranges.
        using var distributionStream = typeof(BiomeTests).Assembly.GetManifestResourceStream("BiomeDistributionBaseline.json")!;
        var distributionBaseline = JsonSerializer.Deserialize<DistributionBaseline[]>(distributionStream)!;
        var previousTotals = new Dictionary<string, int>();
        var currentTotals = new Dictionary<string, int>();
        int totalLand = 0, totalCells = 0;
        var classifier = new BiomeClassifier();
        var catalog = BiomeCatalog.Default;
        Check(catalog.Rules.Select(rule => rule.BiomeId).Distinct().Count() == 8,
            "Eight real catalog biomes");
        foreach (var definition in HexGridDefinitions.All)
        {
            Check(BiomeClassifier.Latitude(0, definition.Rows) == 90 &&
                BiomeClassifier.Latitude((definition.Rows - 1) / 2.0, definition.Rows) == 0 &&
                BiomeClassifier.Latitude(definition.Rows - 1, definition.Rows) == -90,
                "Latitude at north, equator and south");
            Check(Math.Abs(BiomeClassifier.Latitude(1, definition.Rows) +
                BiomeClassifier.Latitude(definition.Rows - 2, definition.Rows)) < 1e-12,
                "Symmetric signed latitude");
        }
        Check(BiomeClassifier.Latitude(0, 1) == 0, "Single-row equator");
        Check(classifier.Classify(false, 0, 0).SubbiomeId == "marine_open_ocean" &&
            classifier.Classify(false, 0, 90).BiomeId == "marine" &&
            classifier.Classify(false, 0, -90).BiomeId == "marine" &&
            classifier.Classify(true, 0, 90).BiomeId == "polar_north" &&
            classifier.Classify(true, 0, -90).BiomeId == "polar_south",
            "Polar oceans stay marine, polar land respects hemispheres");
        VerifySummitBoundary();
        Check(classifier.Classify(true, null, 0).Status == BiomeClassificationStatus.MissingElevation,
            "Missing elevation is never fabricated");
        Check(classifier.Classify(false, 1, 0).Status == BiomeClassificationStatus.NoCatalogMatch,
            "Explicit fallback never assigns an out-of-range marine subbiome");
        for (int latitude = -90; latitude <= 90; latitude++)
        {
            Check(classifier.Classify(false, 0, latitude).BiomeId == "marine",
                "Catalog covers ocean at every latitude");
            Check(classifier.Classify(false, null, latitude).BiomeId == "marine",
                "Ocean remains marine in generators without elevation arrays");
            for (int elevation = 0; elevation <= 100; elevation++)
                Check(classifier.Classify(true, (byte)elevation, latitude).Status == BiomeClassificationStatus.Assigned,
                    $"Catalog covers all land elevations and latitudes: {elevation}/{latitude}");
        }
        foreach (var rule in catalog.Rules)
        {
            double latitude = rule.Subbiome.LatitudeDegrees.Min *
                (rule.Subbiome.LatitudeDegrees.Hemisphere == "south" ? -1 : 1);
            bool land = rule.BiomeId != "marine";
            Check(BiomeClassifier.Matches(rule, land, (byte)rule.Subbiome.ElevationLevel.Min, latitude),
                "Catalog minimum boundaries are inclusive");
            latitude = rule.Subbiome.LatitudeDegrees.Max *
                (rule.Subbiome.LatitudeDegrees.Hemisphere == "south" ? -1 : 1);
            Check(BiomeClassifier.Matches(rule, land, (byte)rule.Subbiome.ElevationLevel.Max, latitude),
                "Catalog maximum boundaries are inclusive");
            if (rule.Subbiome.LatitudeDegrees.Hemisphere != "either")
                Check(!BiomeClassifier.Matches(rule, land, 0, -latitude), "Wrong hemisphere rejected");
            _ = BiomePalette.ForBiome(rule.BiomeId);
        }

        // Captured from master 95af296 BEFORE integrating classification.
        // 3 sizes x 4 generators x 3 seeds, including surface, elevation, plates and attempt.
        using var stream = typeof(BiomeTests).Assembly.GetManifestResourceStream("TerrainBaseline.txt")!;
        using var reader = new StreamReader(stream);
        int worlds = 0, assigned = 0, gaps = 0;
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var size = Enum.Parse<GridSize>(parts[0]);
            var type = Enum.Parse<LandmassType>(parts[1]);
            int seed = int.Parse(parts[2]);
            var generated = new LandmassGenerationService().Generate(LandmassProfiles.Create(size, type, seed));
            var terrain = generated.Landmass;
            if (type == LandmassType.Pangaea)
            {
                int mountains = Enumerable.Range(0, terrain.SurfaceByCellId.Length)
                    .Count(id => terrain.Biomes!.Get(id).BiomeId == "mountain");
                int before = MountainBaseline[(size, seed)];
                Check(mountains < before, $"Mountain coverage decreases for {size}/{seed}");
                Console.WriteLine($"PASS: mountain cells {size}/{seed}: {before} -> {mountains} " +
                    $"({100.0 * (before - mountains) / before:F1}% reduction)");
                var baseline = distributionBaseline.Single(item => item.Size == size.ToString() && item.Seed == seed);
                int landCount = terrain.SurfaceByCellId.Count(value => value == 1);
                Check(landCount == baseline.LandCells, "Distribution comparison uses identical land area");
                var currentCounts = Enumerable.Range(0, terrain.SurfaceByCellId.Length)
                    .GroupBy(id => terrain.Biomes!.Get(id).BiomeId!)
                    .ToDictionary(group => group.Key, group => group.Count());
                foreach (string biome in catalog.Rules.Select(rule => rule.BiomeId).Distinct())
                {
                    previousTotals[biome] = previousTotals.GetValueOrDefault(biome) + baseline.Counts.GetValueOrDefault(biome);
                    currentTotals[biome] = currentTotals.GetValueOrDefault(biome) + currentCounts.GetValueOrDefault(biome);
                }
                totalLand += landCount;
                totalCells += terrain.SurfaceByCellId.Length;
                Console.WriteLine(FormattableString.Invariant($"DISTRIBUTION {size}/{seed}: mountain {100.0 * baseline.Counts.GetValueOrDefault("mountain") / landCount:F2}% -> {100.0 * mountains / landCount:F2}% of land ({baseline.Counts.GetValueOrDefault("mountain")} -> {mountains})"));
            }
            Check(terrain.DeterministicAttempt == int.Parse(parts[3]) &&
                Hash(terrain.SurfaceByCellId) == parts[4] &&
                Hash(terrain.ElevationByCellId) == parts[5] &&
                Hash(terrain.PlateByCellId) == parts[6],
                $"Original terrain preserved: {size}/{type}/{seed}");
            var timer = Stopwatch.StartNew();
            var repeated = classifier.Apply(terrain);
            timer.Stop();
            Check(ReferenceEquals(terrain.SurfaceByCellId, repeated.SurfaceByCellId) &&
                ReferenceEquals(terrain.ElevationByCellId, repeated.ElevationByCellId) &&
                ReferenceEquals(terrain.PlateByCellId, repeated.PlateByCellId),
                "Classifier neither changes nor duplicates terrain arrays");
            var biomes = terrain.Biomes!;
            Check(terrain.SlopeByCellId is null ? repeated.SlopeByCellId is null :
                terrain.SlopeByCellId.SequenceEqual(repeated.SlopeByCellId!), "Deterministic slope array");
            Check(biomes.ClassifierVersion == BiomeClassifier.Version &&
                biomes.CatalogHash == catalog.ContentHash &&
                biomes.Assignments.SequenceEqual(repeated.Biomes!.Assignments) &&
                biomes.AssignmentByCellId.SequenceEqual(repeated.Biomes.AssignmentByCellId),
                "Deterministic classification and catalog provenance");
            for (int id = 0; id < terrain.SurfaceByCellId.Length; id++)
            {
                var cell = BiomeClassifier.GetCell(terrain, id);
                Check(cell.Slope == terrain.SlopeByCellId?[id] &&
                    (cell.Elevation is null ? cell.Slope is null : cell.Slope is >= 0 and <= 100),
                    "Slope query is aligned by ID and absent without elevation");
                bool summit = terrain.IsLand(id) && cell.Elevation is >= 85 and <= 100;
                Check(terrain.IsImpassableSummit(id) == summit && cell.IsImpassableSummit == summit,
                    "Only land at elevation 85..100 is impassable");
                Check(cell.Latitude is >= -90 and <= 90 && cell.Elevation is null or <= 100,
                    "Cell query returns original elevation and valid latitude");
                byte? catalogElevation = cell.Elevation ?? (terrain.IsLand(id) ? null : (byte?)0);
                var compatible = catalog.Rules.Where(rule => catalogElevation is { } elevation &&
                    BiomeClassifier.Matches(rule, terrain.IsLand(id), elevation, cell.Latitude)).ToArray();
                if (cell.Biome.Status == BiomeClassificationStatus.Assigned)
                {
                    Check(compatible.Any(rule => rule.BiomeId == cell.Biome.BiomeId &&
                        rule.Subbiome.Id == cell.Biome.SubbiomeId),
                        "Every assigned ID belongs to its biome and respects all catalog ranges");
                    Check(BiomePalette.ForCell(terrain, id) == (summit ? System.Numerics.Vector3.One
                        : BiomePalette.ForBiome(cell.Biome.BiomeId!)),
                        "Summits are pure white; other cells retain biome colors");
                    if (!terrain.IsLand(id))
                        Check(cell.Biome.BiomeId == "marine" && !summit &&
                            BiomePalette.ForCell(terrain, id) == BiomePalette.ForBiome("marine"),
                            "All classified oceans, including poles, stay blue and passable");
                    if (cell.Biome.BiomeId is "polar_north" or "polar_south")
                        Check(terrain.IsLand(id), "Polar biomes are land-only");
                    assigned++;
                }
                else if (cell.Biome.Status == BiomeClassificationStatus.MissingElevation)
                    Check(type != LandmassType.Pangaea && cell.Elevation is null,
                        "Only generators without heights remain pending");
                else
                {
                    Check(compatible.Length == 0, "Fallback is only used for genuine catalog gaps");
                    gaps++;
                }
                int columns = generated.Grid.Definition.Columns;
                if (id % columns == 0)
                    Check(cell.Latitude == BiomeClassifier.GetCell(terrain, id + columns - 1).Latitude,
                        "Horizontal seam preserves latitude");
            }
            if (size == GridSize.Large && type == LandmassType.Pangaea && seed == 2026)
                Console.WriteLine($"PASS: biome classification 50000 cells: {timer.Elapsed.TotalMilliseconds:F1} ms");
            if (type == LandmassType.Pangaea && seed == 2026)
                VerifyPersistence(generated);
            worlds++;
        }
        VerifyCatalogDrivenRules();
        foreach (string biome in previousTotals.Keys.Order())
        {
            int denominator = biome == "marine" ? totalCells : totalLand;
            Console.WriteLine(FormattableString.Invariant($"DISTRIBUTION TOTAL {biome}: {100.0 * previousTotals[biome] / denominator:F2}% -> {100.0 * currentTotals[biome] / denominator:F2}% ({previousTotals[biome]} -> {currentTotals[biome]}; denominator {denominator})"));
        }
        Check(gaps == 0, "All Pangaea cells have compatible catalog IDs");
        Console.WriteLine($"PASS: biome ranges, hemispheres, persistence and {worlds} original terrain regressions; " +
            $"{assigned} classified, {gaps} explicit catalog gaps");
    }

    private static void VerifySummitBoundary()
    {
        var terrain = new LandmassState(LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 1),
            [1, 1, 1, 1, 0, 0, 0, 0], 0)
            { ElevationByCellId = [0, 84, 85, 100, 0, 84, 85, 100] };
        for (int id = 0; id < 8; id++)
            Check(terrain.IsImpassableSummit(id) == (id is 2 or 3), "Exact 84/85 threshold and ocean exclusion");
        Check(!(terrain with { ElevationByCellId = null }).IsImpassableSummit(2),
            "Unknown heights cannot create summits");
        Check(BiomePalette.ForCell(terrain, 2) == System.Numerics.Vector3.One &&
            BiomePalette.ForCell(terrain, 3) == System.Numerics.Vector3.One,
            "White summit override does not depend on a biome");
    }

    private static void VerifyCatalogDrivenRules()
    {
        const string custom = """
            {"biomes":[{"id":"plains","subbiomes":[{"id":"custom",
            "elevationLevel":{"min":17,"max":18},
            "latitudeDegrees":{"min":12,"max":13,"hemisphere":"north"}}]}]}
            """;
        var classifier = new BiomeClassifier(BiomeCatalog.Parse(custom));
        Check(classifier.Classify(true, 17, 12).SubbiomeId == "custom" &&
            classifier.Classify(true, 16, 12).Status == BiomeClassificationStatus.NoCatalogMatch &&
            classifier.Classify(true, 17, -12).Status == BiomeClassificationStatus.NoCatalogMatch,
            "Changed JSON ranges drive classification without C# range lists");
    }

    private static void VerifyPersistence(GeneratedWorld generated)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-biomes-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(generated.Grid.Definition), Landmass = generated.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(loaded.SlopeByCellId!.SequenceEqual(generated.Landmass.SlopeByCellId!),
                "Slope survives save/load without recalculation");
            Check(Enumerable.Range(0, loaded.SurfaceByCellId.Length).All(id =>
                loaded.IsImpassableSummit(id) == generated.Landmass.IsImpassableSummit(id)),
                "Impassable condition survives save/load without regeneration");
            Check(loaded.Biomes!.Assignments.SequenceEqual(state.Landmass.Biomes!.Assignments) &&
                loaded.Biomes.AssignmentByCellId.SequenceEqual(state.Landmass.Biomes.AssignmentByCellId) &&
                loaded.ElevationByCellId!.SequenceEqual(state.Landmass.ElevationByCellId!) &&
                loaded.PlateByCellId!.SequenceEqual(state.Landmass.PlateByCellId!), "Biome save/load");
            state.Landmass = state.Landmass with { Biomes = null, SlopeByCellId = null };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.Biomes is null, "Existing saves without biomes remain readable");
            Check(GameSaveStore.Load(path).Landmass!.SlopeByCellId is null, "Old saves without slope remain readable");
            state.Landmass = generated.Landmass with
            {
                Biomes = generated.Landmass.Biomes! with { AssignmentByCellId = [255] }
            };
            try { GameSaveStore.Save(path, state); throw new Exception("Invalid biome data was saved."); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static string Hash(byte[]? data) => data is null ? "-" : Convert.ToHexString(SHA256.HashData(data));
    private sealed record DistributionBaseline(string Size, int Seed, int LandCells, Dictionary<string, int> Counts);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
