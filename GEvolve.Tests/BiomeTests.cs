using System.Diagnostics;
using System.Security.Cryptography;
using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class BiomeTests
{
    public static void Run()
    {
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
            classifier.Classify(false, 0, 90).BiomeId == "polar_north" &&
            classifier.Classify(false, 0, -90).BiomeId == "polar_south",
            "Ocean and hemisphere-aware polar precedence");
        Check(classifier.Classify(true, null, 0).Status == BiomeClassificationStatus.MissingElevation,
            "Missing elevation is never fabricated");
        Check(classifier.Classify(false, 1, 0).Status == BiomeClassificationStatus.NoCatalogMatch,
            "Explicit fallback never assigns an out-of-range marine subbiome");
        for (int latitude = -90; latitude <= 90; latitude++)
        {
            Check(classifier.Classify(false, 0, latitude).Status == BiomeClassificationStatus.Assigned,
                "Catalog covers ocean at every latitude");
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
            Check(biomes.ClassifierVersion == BiomeClassifier.Version &&
                biomes.CatalogHash == catalog.ContentHash &&
                biomes.Assignments.SequenceEqual(repeated.Biomes!.Assignments) &&
                biomes.AssignmentByCellId.SequenceEqual(repeated.Biomes.AssignmentByCellId),
                "Deterministic classification and catalog provenance");
            for (int id = 0; id < terrain.SurfaceByCellId.Length; id++)
            {
                var cell = BiomeClassifier.GetCell(terrain, id);
                Check(cell.Latitude is >= -90 and <= 90 && cell.Elevation is null or <= 100,
                    "Cell query returns original elevation and valid latitude");
                var compatible = catalog.Rules.Where(rule => cell.Elevation is { } elevation &&
                    BiomeClassifier.Matches(rule, terrain.IsLand(id), elevation, cell.Latitude)).ToArray();
                if (cell.Biome.Status == BiomeClassificationStatus.Assigned)
                {
                    Check(compatible.Any(rule => rule.BiomeId == cell.Biome.BiomeId &&
                        rule.Subbiome.Id == cell.Biome.SubbiomeId),
                        "Every assigned ID belongs to its biome and respects all catalog ranges");
                    Check(BiomePalette.ForCell(terrain, id) == BiomePalette.ForBiome(cell.Biome.BiomeId!),
                        "Renderer color comes from assigned biome");
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
        Check(gaps == 0, "All Pangaea cells have compatible catalog IDs");
        Console.WriteLine($"PASS: biome ranges, hemispheres, persistence and {worlds} original terrain regressions; " +
            $"{assigned} classified, {gaps} explicit catalog gaps");
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
            Check(loaded.Biomes!.Assignments.SequenceEqual(state.Landmass.Biomes!.Assignments) &&
                loaded.Biomes.AssignmentByCellId.SequenceEqual(state.Landmass.Biomes.AssignmentByCellId) &&
                loaded.ElevationByCellId!.SequenceEqual(state.Landmass.ElevationByCellId!) &&
                loaded.PlateByCellId!.SequenceEqual(state.Landmass.PlateByCellId!), "Biome save/load");
            state.Landmass = state.Landmass with { Biomes = null };
            GameSaveStore.Save(path, state);
            Check(GameSaveStore.Load(path).Landmass!.Biomes is null, "Existing saves without biomes remain readable");
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
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
