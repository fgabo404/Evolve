using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class TectonicMotionTests
{
    public static void Run()
    {
        var generator = new TectonicMotionGenerator();
        var motion = PlateMotionGenerator.Generate(2026);
        Check(motion.Length == 16 && motion.SequenceEqual(PlateMotionGenerator.Generate(2026)), "16 deterministic motions");
        Check(motion.Select(p => p.DirectionDegrees).Distinct().Count() > 1 &&
            motion.Select(p => p.SpeedCmPerYear).Distinct().Count() > 1 &&
            !motion.SequenceEqual(PlateMotionGenerator.Generate(42)), "Varied seeded directions and speeds");
        foreach (var plate in motion)
            Check(plate.DirectionDegrees is >= 0 and < 360 && plate.SpeedCmPerYear is >= 1 and <= 8 &&
                Math.Abs(plate.UnitVector.X * plate.UnitVector.X + plate.UnitVector.Y * plate.UnitVector.Y - 1) < 1e-12 &&
                Math.Abs(Math.Sqrt(plate.Velocity.X * plate.Velocity.X + plate.Velocity.Y * plate.Velocity.Y) - plate.SpeedCmPerYear) < 1e-12,
                "Valid unit vectors and velocities");
        var east = new PlateMotion(0, 0, 2); var west = new PlateMotion(1, 180, 3);
        Check(PlateMotionGenerator.Generate(2026, new() { MinimumSpeedCmPerYear = 3, MaximumSpeedCmPerYear = 3 })
            .All(plate => plate.SpeedCmPerYear == 3), "Configurable speeds");
        var convergent = generator.Project(0, 1, east, west, new(1, 0));
        Check(Math.Abs(convergent.Convergence - 5) < 1e-12 && convergent.Divergence == 0 &&
            convergent.Kind == PlateInteractionKind.Convergent, "Controlled convergence");
        var divergent = generator.Project(0, 1, west, east, new(1, 0));
        Check(Math.Abs(divergent.Divergence - 5) < 1e-12 && divergent.Convergence == 0 &&
            divergent.Kind == PlateInteractionKind.Divergent, "Controlled divergence");
        var shear = generator.Project(0, 1, new(0, 90, 2), new(1, 270, 3), new(1, 0));
        Check(Math.Abs(shear.Shear - 5) < 1e-12 && shear.Kind == PlateInteractionKind.Transform, "Controlled transform");
        Check(generator.Project(0, 1, new(0, 0, 1), new(1, 90, 1), new(1, 0)).Kind == PlateInteractionKind.Mixed,
            "Oblique motion is mixed");
        Check(generator.Project(0, 1, east, east with { PlateId = 1, SpeedCmPerYear = 2.01 }, new(1, 0)).Kind ==
            PlateInteractionKind.Quiet, "Tiny relative motion remains quiet");
        var reverse = generator.Project(1, 0, west, east, new(-1, 0));
        Check(Math.Abs(reverse.Convergence - convergent.Convergence) < 1e-12 && reverse.Kind == convergent.Kind,
            "Swapping A/B and normal preserves interaction");

        var definition = HexGridDefinitions.Get(GridSize.Small);
        var topology = new HexTopology(HexGrid.Generate(definition));
        foreach (int id in new[] { 0, definition.Columns - 1, definition.Columns, topology.Count / 2 })
        foreach (int neighbor in topology.Neighbors(id))
        {
            var normal = TectonicMotionGenerator.BoundaryNormal(topology, id, neighbor);
            var back = TectonicMotionGenerator.BoundaryNormal(topology, neighbor, id);
            Check(Math.Abs(normal.X + back.X) < 1e-12 && Math.Abs(normal.Y + back.Y) < 1e-12, "Antisymmetric normals");
            Check(Math.Abs(normal.X * normal.X + normal.Y * normal.Y - 1) < 1e-12, "Unit normals at borders and seam");
        }
        int seam = topology.Id(0, 20);
        int seamNeighbor = topology.Neighbors(seam).ToArray().First(id => id % topology.Columns == topology.Columns - 1);
        Check(TectonicMotionGenerator.BoundaryNormal(topology, seam, seamNeighbor).X < 0, "Seam normal points west, not across the whole map");
        int north = topology.Id(20, 19), south = topology.Id(20, 20);
        Check(TectonicMotionGenerator.BoundaryNormal(topology, north, south) == new TectonicVector(0, 1), "Vertical hex normal");
        var southeast = TectonicMotionGenerator.BoundaryNormal(topology, south, topology.Id(21, 20));
        Check(Math.Abs(southeast.X - Math.Sqrt(3) / 2) < 1e-12 && Math.Abs(southeast.Y - .5) < 1e-12,
            "Diagonal normal follows flat-top geometry rather than column/row axes");
        var impulses = new float[topology.Count]; impulses[seam] = 5;
        var zeros = new float[topology.Count];
        var fields = TectonicMotionGenerator.Spread(topology, impulses, zeros, zeros, 2);
        Check(fields.Compression[seam] == 5 && Math.Abs(fields.Compression[seamNeighbor] - 5 * Math.Exp(-.25)) < 1e-6 &&
            fields.Extension.All(v => v == 0) && fields.Shear.All(v => v == 0), "Gaussian propagation across seam without channel mixing");
        for (int id = 0; id < topology.Count; id++)
        {
            int distance = topology.Distance(seam, id);
            double expected = distance > 6 ? 0 : 5 * Math.Exp(-Math.Pow(distance / 2.0, 2));
            Check(Math.Abs(fields.Compression[id] - expected) < 1e-6, "Isotropic hex-distance attenuation");
        }

        var service = new WorldGenerationService();
        foreach (var size in Enum.GetValues<GridSize>())
        {
            var config = LandmassProfiles.Create(size, LandmassType.Pangaea, 2026);
            var world = service.GeneratePlatePreview(config);
            var terrain = world.Landmass;
            var topo = new HexTopology(world.Grid);
            var before = terrain.PlateByCellId!.ToArray();
            var result = terrain.TectonicMotion!;
            var repeated = generator.Generate(topo, terrain);
            Check(result.Motions.SequenceEqual(motion), "Motion independent of map resolution");
            Check(result.Interactions.SequenceEqual(repeated.Interactions) &&
                result.CompressionByCellId.SequenceEqual(repeated.CompressionByCellId) &&
                result.ExtensionByCellId.SequenceEqual(repeated.ExtensionByCellId) &&
                result.ShearByCellId.SequenceEqual(repeated.ShearByCellId), "Deterministic interactions and fields");
            Check(before.SequenceEqual(terrain.PlateByCellId) &&
                before.SequenceEqual(TectonicPlateGenerator.Generate(topo, 2026)), "Original plate geometry and IDs untouched");
            foreach (var field in new[] { result.CompressionByCellId, result.ExtensionByCellId, result.ShearByCellId })
                Check(field.Length == topo.Count && field.All(v => float.IsFinite(v) && v is >= 0 and <= 16), "Finite bounded fields");
            var boundaries = PlateBoundaryMap.Analyze(topo, terrain)!;
            Check(result.DistanceToBoundaryByCellId.SequenceEqual(boundaries.DistanceToBoundaryByCellId) &&
                result.Interactions.Length == boundaries.Edges.Count, "All original boundaries retained once");
            for (int p = 0; p < 16; p++)
                Check(before[result.ArrowCellByPlateId[p]] == p, "Arrow lies inside its plate");
            if (size == GridSize.Small) Persistence(world);
            Console.WriteLine($"PASS: tectonic motion {size}, {result.Interactions.Length} interactions, radius {result.InfluenceRadiusHexes:F2} hex");
        }
        VerifyTerrainBaselines(service);
    }

    private static void VerifyTerrainBaselines(WorldGenerationService service)
    {
        // Captured on 11e784c BEFORE integrating motion. Historical generation failure retained explicitly.
        using var stream = typeof(TectonicMotionTests).Assembly.GetManifestResourceStream("TectonicMotionTerrainBaseline.txt")!;
        using var reader = new StreamReader(stream);
        int unchanged = 0, failures = 0;
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var config = LandmassProfiles.Create(Enum.Parse<GridSize>(parts[0]), Enum.Parse<LandmassType>(parts[1]), int.Parse(parts[2]));
            if (parts[3] == "ERROR")
            {
                try { service.Generate(config); throw new Exception("Expected existing generation failure changed"); }
                catch (InvalidOperationException error) { Check(error.Message == parts[4], "Existing failure remains identical"); }
                failures++; continue;
            }
            var terrain = service.Generate(config).Landmass;
            string Hash(byte[]? bytes) => bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes));
            Check(terrain.DeterministicAttempt == int.Parse(parts[3]) && Hash(terrain.SurfaceByCellId) == parts[4] &&
                Hash(terrain.ElevationByCellId) == parts[5] && Hash(terrain.PlateByCellId) == parts[6], "Pre-motion terrain hash unchanged");
            Check(terrain.Climate is null && terrain.Biomes is null && terrain.SlopeByCellId is null,
                "Disconnected downstream stages remain disconnected");
            unchanged++;
        }
        Console.WriteLine($"PASS: {unchanged} pre-motion terrain hashes unchanged; {failures} documented pre-existing generation failure unchanged");
    }

    private static void Persistence(GEvolve.Engine.World.LandmassGeneration.GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"tectonic-motion-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!.TectonicMotion!;
            var expected = world.Landmass.TectonicMotion!;
            Check(JsonSerializer.Serialize(loaded) == JsonSerializer.Serialize(expected), "All tectonic fields round-trip");
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            json["Landmass"]!.AsObject().Remove("TectonicMotion");
            File.WriteAllText(path, json.ToJsonString());
            Check(GameSaveStore.Load(path).Landmass!.TectonicMotion is null, "Old saves remain readable");
            state.Landmass = world.Landmass with { TectonicMotion = expected with { CompressionByCellId = [float.NaN] } };
            try { GameSaveStore.Save(path, state); throw new Exception("Invalid stress was saved"); }
            catch (InvalidDataException) { }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
