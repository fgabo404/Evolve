using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.Game.Graphics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class ContinentalGeologyTests
{
    public static void Run()
    {
        VerifyContactClassifier();
        var service = new WorldGenerationService();
        var world = service.Generate(LandmassProfiles.Create(GridSize.Small, LandmassType.Continents, 2026));
        var preview = service.GeneratePlatePreview(world.Landmass.Configuration).Landmass;
        var topology = new HexTopology(world.Grid);
        Check(world.Landmass.PlateByCellId!.SequenceEqual(preview.PlateByCellId!) &&
            world.Landmass.PlateByCellId.SequenceEqual(TectonicPlateGenerator.Generate(topology, 2026)) &&
            JsonSerializer.Serialize(world.Landmass.TectonicMotion) == JsonSerializer.Serialize(preview.TectonicMotion),
            "Continental surface does not change the official plates or motion");
        _ = MapViewColors.ForCell(world.Landmass, 0, MapViewMode.Tectonic);
        _ = MapViewColors.ForCell(world.Landmass, 0, MapViewMode.Compression);
        _ = MapViewColors.ForCell(world.Landmass, 0, MapViewMode.Biomes);
        VerifyPreservedGeology(world);
        VerifyApprovedNonContinentalWorlds(service);
    }

    private static void VerifyContactClassifier()
    {
        ushort[] ages = Enumerable.Repeat((ushort)80, 16).ToArray(); ages[0] = 140; ages[1] = 40;
        var edge = new PlateInteraction(0, 1, 0, 1, new(1, 0), 5, 0, 0, PlateInteractionKind.Convergent);
        var cc = GeologicalContactClassifier.Classify(0, edge, CrustKind.Continental, CrustKind.Continental, ages);
        var oc = GeologicalContactClassifier.Classify(0, edge, CrustKind.Oceanic, CrustKind.Continental, ages);
        var co = GeologicalContactClassifier.Classify(0, edge, CrustKind.Continental, CrustKind.Oceanic, ages);
        var oo = GeologicalContactClassifier.Classify(0, edge, CrustKind.Oceanic, CrustKind.Oceanic, ages);
        Check(cc.Kind == GeologicalContactKind.ContinentalCollision && cc.SubductingPlateId == byte.MaxValue &&
            oc.SubductingPlateId == 0 && co.SubductingPlateId == 1 && oo.SubductingPlateId == 0,
            "Collision and subduction retain their geological classification");
        Check(GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Divergence = 5 },
            CrustKind.Continental, CrustKind.Continental, ages).Kind == GeologicalContactKind.ContinentalRift &&
            GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Divergence = 5 },
            CrustKind.Oceanic, CrustKind.Oceanic, ages).Kind == GeologicalContactKind.OceanRidge &&
            GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Shear = 5 },
            CrustKind.Oceanic, CrustKind.Oceanic, ages).Kind == GeologicalContactKind.Transform,
            "Rifts, ridges and transforms retain their classification");
    }

    private static void VerifyPreservedGeology(GeneratedWorld continental)
    {
        var topology = new HexTopology(continental.Grid);
        int count = topology.Count;
        var motion = continental.Landmass.TectonicMotion!;
        var crust = Enumerable.Repeat((byte)CrustKind.Continental, count).ToArray();
        var input = new ContinentalCrustMap(crust, new float[count],
            motion.ArrowCellByPlateId.Take(2).ToArray(), new byte[count]);
        var noSurface = continental.Landmass with { SurfaceByCellId = [], ContinentalSurface = null,
            SurfaceGenerationPending = true, TectonicPreviewOnly = true };
        try
        {
            new ContinentalReliefGenerator().Generate(topology, noSurface, input, 0);
            throw new Exception("Relief ran without a surface");
        }
        catch (InvalidOperationException) { }
        var terrain = continental.Landmass with
        {
            SurfaceByCellId = Enumerable.Repeat((byte)1, count).ToArray(),
            SurfaceGenerationPending = false, TectonicPreviewOnly = false, ContinentalSurface = null
        };
        var relief = new ContinentalReliefGenerator();
        var full = relief.Generate(topology, terrain, input, 0);
        Check(full.ContinentalGeology!.Contacts.Any(c => c.Kind == GeologicalContactKind.ContinentalCollision) &&
            full.ContinentalGeology.Volcanoes.Length == 0 && full.ElevationByCellId!.Max() > 25,
            "Continental collision still produces mountains without pure-CC volcanoes");
        var quietMotion = motion with
        {
            Interactions = motion.Interactions.Select(edge => edge with
                { Convergence = 0, Divergence = 0, Shear = 0, Kind = PlateInteractionKind.Quiet }).ToArray(),
            CompressionByCellId = new float[count], ExtensionByCellId = new float[count],
            ShearByCellId = new float[count]
        };
        var quiet = relief.Generate(topology, terrain with { TectonicMotion = quietMotion }, input, 0);
        Check(quiet.ContinentalGeology!.Volcanoes.Length == 0 &&
            quiet.ElevationByCellId!.Max() < full.ElevationByCellId!.Max(),
            "Removing motion removes tectonic uplift and volcanism");

        string path = Path.Combine(Path.GetTempPath(), $"legacy-continental-geology-{Guid.NewGuid():N}.json");
        try
        {
            var legacyConfig = full.Configuration with
            { Continents = new ContinentsParameters(.35, 2, 34, .58, 9, .002) };
            var state = new GameState { HexGrid = new(continental.Grid.Definition),
                Landmass = full with { Configuration = legacyConfig } };
            GameSaveStore.Save(path, state);
            Check(JsonSerializer.Serialize(GameSaveStore.Load(path).Landmass!.ContinentalGeology) ==
                JsonSerializer.Serialize(full.ContinentalGeology), "Existing geological save shape remains readable");
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            json["Landmass"]!.AsObject().Remove("SurfaceGenerationPending");
            File.WriteAllText(path, json.ToJsonString());
            Check(GameSaveStore.Load(path).Landmass!.ContinentalGeology is not null,
                "Saves created before the pending flag remain compatible");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void VerifyApprovedNonContinentalWorlds(WorldGenerationService service)
    {
        using var stream = typeof(ContinentalGeologyTests).Assembly.GetManifestResourceStream("ApprovedNonContinentalBaseline.txt")!;
        using var reader = new StreamReader(stream); int unchanged = 0;
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var terrain = service.Generate(LandmassProfiles.Create(Enum.Parse<GridSize>(parts[0]),
                Enum.Parse<LandmassType>(parts[1]), int.Parse(parts[2]))).Landmass;
            string Hash(byte[]? bytes) => bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes));
            Check(terrain.DeterministicAttempt == int.Parse(parts[3]) && Hash(terrain.SurfaceByCellId) == parts[4] &&
                Hash(terrain.ElevationByCellId) == parts[5] && Hash(terrain.PlateByCellId) == parts[6],
                "Approved non-continental world unchanged");
            unchanged++;
        }
        Console.WriteLine($"PASS: {unchanged} approved Pangaea/small-continents/archipelago worlds unchanged");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
