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
        VerifyOfficialMotionMatrix(service, generator);
        VerifyTerrainBaselines(service);
    }

    // H04: check every real boundary for all official sizes and regression seeds.
    // No new tectonic algorithm or reconstructed world-generation fixture.
    private static void VerifyOfficialMotionMatrix(
        WorldGenerationService service, TectonicMotionGenerator generator)
    {
        foreach (GridSize size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var configuration = LandmassProfiles.Create(size, LandmassType.Pangaea, seed);
            var world = service.GeneratePlatePreview(configuration);
            var topology = new HexTopology(world.Grid);
            var plates = world.Landmass.PlateByCellId!;
            var state = world.Landmass.TectonicMotion!;
            var boundaries = PlateBoundaryMap.Analyze(topology, world.Landmass)!;
            var expectedMotions = PlateMotionGenerator.Generate(seed);

            Check(state.Motions.SequenceEqual(expectedMotions) &&
                state.Motions.Select(m => m.PlateId).SequenceEqual(
                    Enumerable.Range(0, TectonicPlateGenerator.NumberOfPlates).Select(i => (byte)i)),
                $"H04 motion RNG and plate ordering ({size}/{seed})");
            Check(state.Interactions.Length == boundaries.Edges.Count &&
                state.DistanceToBoundaryByCellId.SequenceEqual(boundaries.DistanceToBoundaryByCellId),
                $"H04 complete boundary correspondence ({size}/{seed})");

            int seamEdges = 0;
            for (int index = 0; index < state.Interactions.Length; index++)
            {
                var edge = boundaries.Edges[index];
                var interaction = state.Interactions[index];
                Check(interaction.FirstCellId == edge.FirstCellId &&
                    interaction.SecondCellId == edge.SecondCellId &&
                    edge.FirstCellId < edge.SecondCellId &&
                    plates[edge.FirstCellId] != plates[edge.SecondCellId] &&
                    topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) &&
                    interaction.FirstPlateId == plates[edge.FirstCellId] &&
                    interaction.SecondPlateId == plates[edge.SecondCellId],
                    $"H04 exact edge identities ({size}/{seed}, edge {index})");

                var normal = TectonicMotionGenerator.BoundaryNormal(
                    topology, edge.FirstCellId, edge.SecondCellId);
                var reverse = TectonicMotionGenerator.BoundaryNormal(
                    topology, edge.SecondCellId, edge.FirstCellId);
                Check(Math.Abs(normal.X + reverse.X) < 1e-12 &&
                    Math.Abs(normal.Y + reverse.Y) < 1e-12 &&
                    Math.Abs(normal.X * normal.X + normal.Y * normal.Y - 1) < 1e-12,
                    $"H04 normals are unit and antisymmetric ({size}/{seed}, edge {index})");
                var expected = generator.Project(edge.FirstCellId, edge.SecondCellId,
                    expectedMotions[interaction.FirstPlateId],
                    expectedMotions[interaction.SecondPlateId], normal);
                Check(interaction == expected &&
                    double.IsFinite(interaction.Convergence) &&
                    double.IsFinite(interaction.Divergence) &&
                    double.IsFinite(interaction.Shear) &&
                    interaction.Convergence >= 0 && interaction.Divergence >= 0 &&
                    interaction.Shear >= 0 &&
                    !(interaction.Convergence > 0 && interaction.Divergence > 0),
                    $"H04 boundary projections and classification ({size}/{seed}, edge {index})");

                int left = edge.FirstCellId % topology.Columns;
                int right = edge.SecondCellId % topology.Columns;
                if (Math.Abs(left - right) == topology.Columns - 1) seamEdges++;
            }

            Check(seamEdges > 0, $"H04 real cross-seam interactions ({size}/{seed})");
            foreach (float[] field in new[]
            {
                state.CompressionByCellId, state.ExtensionByCellId, state.ShearByCellId
            })
                Check(field.Length == topology.Count &&
                    field.All(value => float.IsFinite(value) && value >= 0 && value <= 16),
                    $"H04 finite nonnegative fields ({size}/{seed})");
            Check(state.ArrowCellByPlateId.Length == TectonicPlateGenerator.NumberOfPlates &&
                Enumerable.Range(0, state.ArrowCellByPlateId.Length)
                    .All(id => plates[state.ArrowCellByPlateId[id]] == id),
                $"H04 anchors lie inside their plates ({size}/{seed})");
            Console.WriteLine($"PASS: H04 {size}/{seed}: {state.Interactions.Length} verified contacts, {seamEdges} seam edges");
        }
    }

    private static void VerifyTerrainBaselines(WorldGenerationService service)
    {
        // Captured on 11e784c BEFORE integrating motion. Historical generation failure retained explicitly.
        using var stream = typeof(TectonicMotionTests).Assembly.GetManifestResourceStream("TectonicMotionTerrainBaseline.txt")!;
        using var reader = new StreamReader(stream);
        int unchanged = 0, failures = 0;
        var baselineMismatches = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var config = LandmassProfiles.Create(Enum.Parse<GridSize>(parts[0]), Enum.Parse<LandmassType>(parts[1]), int.Parse(parts[2]));
            if (config.Type == LandmassType.Continents)
            {
                var rebuilt = service.GenerateGeology(config);
                Check(rebuilt.Statistics is null && rebuilt.Landmass.SurfaceGenerationPending &&
                    rebuilt.Landmass.SurfaceByCellId.Length == 0 &&
                    rebuilt.Landmass.PlateByCellId is not null && rebuilt.Landmass.TectonicMotion is not null,
                    "Continents retain official tectonics while surface is pending");
                continue;
            }
            if (parts[3] == "ERROR")
            {
                try { service.GenerateGeology(config); throw new Exception("Expected existing generation failure changed"); }
                catch (InvalidOperationException error) { Check(error.Message == parts[4], "Existing failure remains identical"); }
                failures++; continue;
            }
            var terrain = service.GenerateGeology(config).Landmass;
            string Hash(byte[]? bytes) => bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes));
            var actualAttempt = terrain.DeterministicAttempt;
            var actualSurface = Hash(terrain.SurfaceByCellId);
            var actualElevation = Hash(terrain.ElevationByCellId);
            var actualPlates = Hash(terrain.PlateByCellId);
            bool attemptMatches = actualAttempt == int.Parse(parts[3]);
            bool surfaceMatches = actualSurface == parts[4];
            bool elevationMatches = actualElevation == parts[5];
            bool platesMatch = actualPlates == parts[6];
            if (!attemptMatches || !surfaceMatches || !elevationMatches || !platesMatch)
            {
                string scenario = $"{parts[0]}/{parts[1]}/{parts[2]}";
                Console.WriteLine($"BASELINE MISMATCH {scenario}");
                if (!attemptMatches)
                    Console.WriteLine($"  attempt: expected={parts[3]}, actual={actualAttempt}");
                if (!surfaceMatches)
                    Console.WriteLine($"  surface: expected={parts[4]}, actual={actualSurface}");
                if (!elevationMatches)
                    Console.WriteLine($"  elevation: expected={parts[5]}, actual={actualElevation}");
                if (!platesMatch)
                    Console.WriteLine($"  plates: expected={parts[6]}, actual={actualPlates}");
                baselineMismatches.Add(scenario);
            }
            Check(terrain.Climate is null && terrain.Biomes is null && terrain.SlopeByCellId is null,
                "Disconnected downstream stages remain disconnected");
            unchanged++;
        }
        Check(baselineMismatches.Count == 0,
            $"Pre-motion terrain baselines differ in {baselineMismatches.Count} scenario(s): {string.Join(", ", baselineMismatches)}");
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
