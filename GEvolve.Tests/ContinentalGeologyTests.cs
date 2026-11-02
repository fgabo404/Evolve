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
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var world = service.Generate(config);
            var terrain = world.Landmass;
            var preview = service.GeneratePlatePreview(config).Landmass;
            var topology = new HexTopology(world.Grid);
            Check(world.Statistics is null && terrain.SurfaceGenerationPending && terrain.TectonicPreviewOnly &&
                !terrain.HasSurface && terrain.SurfaceByCellId.Length == 0 &&
                terrain.ElevationByCellId is null && terrain.ContinentalGeology is null &&
                terrain.Biomes is null && terrain.Climate is null && terrain.SlopeByCellId is null,
                "Continents have no classified surface or surface-derived data");
            Check(terrain.PlateByCellId!.Length == topology.Count &&
                terrain.PlateByCellId.Distinct().Count() == 16 &&
                terrain.PlateByCellId.SequenceEqual(preview.PlateByCellId!) &&
                terrain.PlateByCellId.SequenceEqual(TectonicPlateGenerator.Generate(topology, seed)) &&
                JsonSerializer.Serialize(terrain.TectonicMotion) == JsonSerializer.Serialize(preview.TectonicMotion),
                "Pending Continents preserve the exact official 16 plates, motion and stress fields");
            try { _ = terrain.IsLand(0); throw new Exception("Pending land was classified"); }
            catch (InvalidOperationException) { }
            _ = MapViewColors.ForCell(terrain, 0, MapViewMode.Tectonic);
            _ = MapViewColors.ForCell(terrain, 0, MapViewMode.Compression);
            try { _ = MapViewColors.ForCell(terrain, 0, MapViewMode.Biomes); throw new Exception("Unavailable surface layer was colored"); }
            catch (InvalidOperationException) { }
            if (size == GridSize.Small)
            {
                var repeated = service.Generate(config).Landmass;
                Check(repeated.SurfaceByCellId.Length == 0 &&
                    repeated.PlateByCellId!.SequenceEqual(terrain.PlateByCellId) &&
                    JsonSerializer.Serialize(repeated.TectonicMotion) == JsonSerializer.Serialize(terrain.TectonicMotion),
                    "Pending world is deterministic");
                VerifyPendingPersistence(world);
                if (seed == 2026) VerifyPreservedGeology(world);
            }
            Console.WriteLine($"PENDING CONTINENTS {size}/{seed}: 16 plates, {terrain.TectonicMotion!.Interactions.Length} interactions, no surface");
        }
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

    private static void VerifyPendingPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pending-continents-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(loaded.SurfaceGenerationPending && loaded.SurfaceByCellId.Length == 0 &&
                loaded.PlateByCellId!.SequenceEqual(world.Landmass.PlateByCellId!) &&
                JsonSerializer.Serialize(loaded.TectonicMotion) == JsonSerializer.Serialize(world.Landmass.TectonicMotion),
                "Pending surface, plates and motion survive save/load");
            foreach (var invalid in new[]
            {
                world.Landmass with { SurfaceByCellId = new byte[world.Grid.Cells.Count] },
                world.Landmass with { ElevationByCellId = new byte[world.Grid.Cells.Count] },
                world.Landmass with { TectonicMotion = null }
            })
            {
                state.Landmass = invalid;
                try { GameSaveStore.Save(path, state); throw new Exception("Malformed pending surface was saved"); }
                catch (InvalidDataException) { }
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void VerifyPreservedGeology(GeneratedWorld pending)
    {
        var topology = new HexTopology(pending.Grid);
        int count = topology.Count;
        var motion = pending.Landmass.TectonicMotion!;
        var crust = Enumerable.Repeat((byte)CrustKind.Continental, count).ToArray();
        var input = new ContinentalCrustMap(crust, new float[count],
            motion.ArrowCellByPlateId.Take(2).ToArray(), new byte[count]);
        try
        {
            new ContinentalReliefGenerator().Generate(topology, pending.Landmass, input, 0);
            throw new Exception("Relief ran without a surface");
        }
        catch (InvalidOperationException) { }
        var terrain = pending.Landmass with
        {
            SurfaceByCellId = Enumerable.Repeat((byte)1, count).ToArray(),
            SurfaceGenerationPending = false, TectonicPreviewOnly = false
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
            var state = new GameState { HexGrid = new(pending.Grid.Definition),
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
        var mismatches = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var terrain = service.Generate(LandmassProfiles.Create(Enum.Parse<GridSize>(parts[0]),
                Enum.Parse<LandmassType>(parts[1]), int.Parse(parts[2]))).Landmass;
            string Hash(byte[]? bytes) => bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes));
            string scenario = $"{parts[0]}/{parts[1]}/{parts[2]}";
            int attempt = terrain.DeterministicAttempt;
            string surface = Hash(terrain.SurfaceByCellId);
            string elevation = Hash(terrain.ElevationByCellId);
            string plates = Hash(terrain.PlateByCellId);
            bool attemptMatches = attempt == int.Parse(parts[3]);
            bool surfaceMatches = surface == parts[4];
            bool elevationMatches = elevation == parts[5];
            bool platesMatch = plates == parts[6];
            if (!attemptMatches || !surfaceMatches || !elevationMatches || !platesMatch)
            {
                Console.WriteLine($"APPROVED BASELINE MISMATCH {scenario}");
                if (!attemptMatches) Console.WriteLine($"  attempt: expected={parts[3]}, actual={attempt}");
                if (!surfaceMatches) Console.WriteLine($"  surface: expected={parts[4]}, actual={surface}");
                if (!elevationMatches) Console.WriteLine($"  elevation: expected={parts[5]}, actual={elevation}");
                if (!platesMatch) Console.WriteLine($"  plates: expected={parts[6]}, actual={plates}");
                mismatches.Add(scenario);
            }
            else unchanged++;
        }
        Check(mismatches.Count == 0,
            $"Approved non-continental world baselines differ in {mismatches.Count} scenario(s): {string.Join(", ", mismatches)}");
        Console.WriteLine($"PASS: {unchanged} approved Pangaea/small-continents/archipelago worlds unchanged");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
