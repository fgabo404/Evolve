using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;

public static class SlopeTests
{
    public static void Run()
    {
        var definition = HexGridDefinitions.Get(GridSize.Small);
        var topology = new HexTopology(HexGrid.Generate(definition));
        var heights = Enumerable.Repeat((byte)70, topology.Count).ToArray();
        var surface = Enumerable.Repeat((byte)1, topology.Count).ToArray();
        var terrain = new LandmassState(LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 42),
            surface, 0) { ElevationByCellId = heights };
        Check(TerrainSlope.Calculate(topology, terrain)!.All(value => value == 0), "Level plateau has zero slope");
        int center = topology.Id(0, definition.Rows / 2);
        int wrapped = topology.Neighbors(center).ToArray().First(id => id % definition.Columns == definition.Columns - 1);
        heights[wrapped] = 86;
        var beforeHeights = heights.ToArray();
        var beforeSurface = surface.ToArray();
        var slopes = TerrainSlope.Calculate(topology, terrain)!;
        Check(slopes[center] == 16 && slopes[wrapped] == 16,
            "Slope uses real wrapped neighbors and absolute level difference");
        Check(slopes.SequenceEqual(TerrainSlope.Calculate(topology, terrain)!) &&
            heights.SequenceEqual(beforeHeights) && surface.SequenceEqual(beforeSurface),
            "Slope is deterministic and leaves terrain unchanged");

        Array.Fill(heights, (byte)70);
        Array.Fill(heights, (byte)100, topology.Count - definition.Columns, definition.Columns);
        Check(TerrainSlope.Calculate(topology, terrain)!.Take(definition.Columns).All(value => value == 0),
            "North and south do not wrap; only valid edge neighbors count");

        Array.Clear(surface);
        Array.Clear(heights);
        surface[center] = 1; heights[center] = 80;
        Check(TerrainSlope.Calculate(topology, terrain)![center] == 0,
            "Ocean elevation zero cannot create an artificial coastal slope");
        surface[wrapped] = 1; heights[wrapped] = 75;
        slopes = TerrainSlope.Calculate(topology, terrain)!;
        Check(slopes[center] == 5 && slopes[wrapped] == 5 &&
            slopes.Where((_, id) => !terrain.IsLand(id)).All(value => value == 0),
            "Only land neighbors contribute and ocean slope remains zero");
        Check(TerrainSlope.Calculate(topology, terrain with { ElevationByCellId = null }) is null,
            "No slope is invented for generators without elevation");

        var classifier = new BiomeClassifier();
        Check(classifier.Classify(true, 70, 40, 0).BiomeId == "plains" &&
            classifier.Classify(true, 80, 40, 0).BiomeId == "plains",
            "High level plateaus remain vegetated where the catalog allows it");
        Check(classifier.Classify(true, 60, 40, 6).BiomeId == "forest" &&
            classifier.Classify(true, 70, 40, 9).BiomeId == "forest",
            "Moderate high slopes support forest");
        Check(classifier.Classify(true, 70, 40, 10).SubbiomeId == "mountain_rocky" &&
            classifier.Classify(true, 80, 40, 6).SubbiomeId == "mountain_alpine",
            "Pronounced slopes and high alpine slopes prefer appropriate mountains");
        Check(classifier.Classify(true, 64, 40, 10).BiomeId == "forest" &&
            classifier.Classify(true, 65, 40, 10).BiomeId == "mountain",
            "Central mountain elevation boundary complements slope");
        var flatAssignment = classifier.Classify(true, 70, 40, 0);
        var steepAssignment = classifier.Classify(true, 70, 40, 20);
        Check(flatAssignment != steepAssignment, "Same height and latitude can have different landscapes");
        foreach (var (land, height, latitude, slope) in new (bool, byte, double, byte)[]
            { (true, 70, 40, 0), (true, 70, 40, 20), (true, 80, -80, 20), (false, 0, 90, 0) })
        {
            var result = classifier.Classify(land, height, latitude, slope);
            Check(BiomeCatalog.Default.Rules.Any(rule => rule.Subbiome.Id == result.SubbiomeId &&
                BiomeClassifier.Matches(rule, land, height, latitude)), "Slope never bypasses catalog compatibility");
        }
        VerifyPalette();
        Console.WriteLine("PASS: deterministic hex slopes, coast exclusion, plateaus, forested slopes and exact palette");
    }

    private static void VerifyPalette()
    {
        // Independent acceptance values from the requested palette, not a runtime palette.
        var expected = new Dictionary<string, string>
        {
            ["desert"] = "D8BC82", ["forest"] = "43845A", ["plains"] = "A7C47C",
            ["marine"] = "347FA8", ["tundra"] = "91A99A", ["mountain"] = "918B80",
            ["polar_north"] = "DCEEF1", ["polar_south"] = "EDF4F5"
        };
        foreach (var (id, hex) in expected)
        {
            var color = BiomePalette.ForBiome(id);
            string actual = $"{(int)MathF.Round(color.X * 255):X2}{(int)MathF.Round(color.Y * 255):X2}{(int)MathF.Round(color.Z * 255):X2}";
            Check(actual == hex, $"Exact palette color {id}");
        }
        Check(BiomePalette.ImpassableSummitColor == System.Numerics.Vector3.One, "Summit remains #FFFFFF");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
