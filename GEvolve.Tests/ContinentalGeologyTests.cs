using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.Tectonics;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class ContinentalGeologyTests
{
    public static void Run()
    {
        ushort[] ages = Enumerable.Repeat((ushort)80, 16).ToArray(); ages[0] = 140; ages[1] = 40;
        var edge = new PlateInteraction(0, 1, 0, 1, new(1, 0), 5, 0, 0, PlateInteractionKind.Convergent);
        var cc = GeologicalContactClassifier.Classify(0, edge, CrustKind.Continental, CrustKind.Continental, ages);
        var oc = GeologicalContactClassifier.Classify(0, edge, CrustKind.Oceanic, CrustKind.Continental, ages);
        var co = GeologicalContactClassifier.Classify(0, edge, CrustKind.Continental, CrustKind.Oceanic, ages);
        var oo = GeologicalContactClassifier.Classify(0, edge, CrustKind.Oceanic, CrustKind.Oceanic, ages);
        Check(cc.Kind == GeologicalContactKind.ContinentalCollision && cc.SubductingPlateId == byte.MaxValue,
            "Continental collision is not subduction");
        Check(oc.SubductingPlateId == 0 && co.SubductingPlateId == 1 && oo.SubductingPlateId == 0,
            "Ocean crust subducts under continental crust; older ocean lithosphere subducts in OO contact");
        Check(GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Divergence = 5 },
            CrustKind.Continental, CrustKind.Continental, ages).Kind == GeologicalContactKind.ContinentalRift &&
            GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Divergence = 5 },
            CrustKind.Oceanic, CrustKind.Oceanic, ages).Kind == GeologicalContactKind.OceanRidge &&
            GeologicalContactClassifier.Classify(0, edge with { Convergence = 0, Shear = 5 },
            CrustKind.Oceanic, CrustKind.Oceanic, ages).Kind == GeologicalContactKind.Transform,
            "Divergent and transform contacts distinguish crust");
        Check(GeologicalContactClassifier.Classify(0, edge with { Convergence = .01 },
            CrustKind.Oceanic, CrustKind.Continental, ages).Kind == GeologicalContactKind.Quiet, "Inactive contacts do not subduct");

        var service = new WorldGenerationService();
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731,
            Enumerable.Range(0, 128).First(s => LandmassProfiles.ContinentCountForSeed(s) == 5),
            Enumerable.Range(0, 128).First(s => LandmassProfiles.ContinentCountForSeed(s) == 6) })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            var world = service.Generate(config); var terrain = world.Landmass;
            var heights = terrain.ElevationByCellId!;
            var geology = terrain.ContinentalGeology!; var motion = terrain.TectonicMotion!;
            Check(LandmassGenerationService.MeetsProfile(config.Type, world.Statistics) &&
                world.Statistics.ComponentSizes.Count(n => n >= 100) == config.Continents!.CoreCount,
                "Unchanged geographic profile and configured mainland count");
            Check(geology.CrustByCellId.Contains((byte)0) && geology.CrustByCellId.Contains((byte)1) &&
                Enumerable.Range(0, terrain.SurfaceByCellId.Length).Any(id => geology.CrustByCellId[id] == 1 && !terrain.IsLand(id)),
                "Mixed ocean/continental crust with submerged continental shelves");
            Check(Enumerable.Range(0, terrain.SurfaceByCellId.Length).Where(terrain.IsLand)
                .GroupBy(id => geology.ProvinceByCellId[id]).Any(group => group.Select(id => terrain.PlateByCellId![id]).Distinct().Count() > 1) &&
                Enumerable.Range(0, terrain.SurfaceByCellId.Length).GroupBy(id => terrain.PlateByCellId![id])
                .Any(group => group.Select(id => geology.CrustByCellId[id]).Distinct().Count() > 1),
                "Continental assemblies can span plates and individual plates contain both crust kinds");
            Check(terrain.Climate is null && terrain.Biomes is null && terrain.SlopeByCellId is null,
                "Downstream climate and ecological stages remain disabled");
            Check(heights.All(h => h <= 100) && Enumerable.Range(0, terrain.SurfaceByCellId.Length).All(id =>
                terrain.IsLand(id) ? heights[id] > 0 && geology.OceanDepthByCellId[id] == 0 :
                    heights[id] == 0 && geology.OceanDepthByCellId[id] > 0), "Land height and ocean depth contract");
            ConnectedCratons(world);
            foreach (var volcano in geology.Volcanoes)
            {
                var contact = geology.Contacts[volcano.InteractionIndex]; var interaction = motion.Interactions[volcano.InteractionIndex];
                Check(contact.Kind is GeologicalContactKind.OceanContinentSubduction or GeologicalContactKind.OceanOceanSubduction &&
                    terrain.PlateByCellId![volcano.CellId] == contact.OverridingPlateId &&
                    volcano.SubductingPlateId == contact.SubductingPlateId && interaction.Convergence >= .5,
                    "Every persisted volcano is tied to an active subduction edge on the upper plate");
            }
            if (size == GridSize.Small)
            {
                var repeated = service.Generate(config).Landmass;
                Check(JsonSerializer.Serialize(geology) == JsonSerializer.Serialize(repeated.ContinentalGeology) &&
                    terrain.SurfaceByCellId.SequenceEqual(repeated.SurfaceByCellId) &&
                    heights.SequenceEqual(repeated.ElevationByCellId!), "Deterministic full geology");
                Persistence(world);
                if (seed == 2026) { ControlledRelief(world); TectonicGrowth(world); }
            }
            Console.WriteLine($"CONTINENTS {size}/{seed}: {config.Continents!.CoreCount} mainlands, {world.Statistics.LandFraction:P2} land, " +
                $"{geology.Volcanoes.Length} volcanoes, {geology.Contacts.Count(c => c.Kind is GeologicalContactKind.OceanContinentSubduction or GeologicalContactKind.OceanOceanSubduction)} subduction edges, max height {heights.Max()}");
        }
        using var stream = typeof(ContinentalGeologyTests).Assembly.GetManifestResourceStream("ApprovedNonContinentalBaseline.txt")!;
        using var reader = new StreamReader(stream); int unchanged = 0;
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|');
            var terrain = service.Generate(LandmassProfiles.Create(Enum.Parse<GridSize>(parts[0]),
                Enum.Parse<LandmassType>(parts[1]), int.Parse(parts[2]))).Landmass;
            string Hash(byte[]? bytes) => bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes));
            Check(terrain.DeterministicAttempt == int.Parse(parts[3]) && Hash(terrain.SurfaceByCellId) == parts[4] &&
                Hash(terrain.ElevationByCellId) == parts[5] && Hash(terrain.PlateByCellId) == parts[6], "Approved non-continental world unchanged");
            if (parts[0] == "Small" && parts[1] == "Pangaea" && parts[2] == "2026")
                Console.WriteLine($"APPROVED PANGEA: {Enumerable.Range(0, terrain.SurfaceByCellId.Length).Count(id => terrain.IsLand(id) && terrain.ElevationByCellId![id] == 0)} land cells at height zero; unchanged historical output");
            unchanged++;
        }
        Console.WriteLine($"PASS: {unchanged} approved Pangea/small-continents/archipelago worlds unchanged");
    }
    private static void ConnectedCratons(GeneratedWorld world)
    {
        var topology = new GEvolve.Engine.World.LandmassGeneration.Shared.HexTopology(world.Grid);
        var land = world.Landmass.SurfaceByCellId;
        var components = Enumerable.Repeat(-1, topology.Count).ToArray();
        int component = 0;
        var queue = new Queue<int>();
        for (int id = 0; id < topology.Count; id++)
        {
            if (land[id] == 0 || components[id] >= 0) continue;
            components[id] = component; queue.Enqueue(id);
            while (queue.TryDequeue(out int current))
                foreach (int next in topology.Neighbors(current))
                    if (land[next] == 1 && components[next] < 0)
                    { components[next] = component; queue.Enqueue(next); }
            component++;
        }
        var roots = world.Landmass.ContinentalGeology!.CratonCellIds;
        Check(roots.All(id => land[id] == 1) && roots.Select(id => components[id]).Distinct().Count() == roots.Length,
            "Every craton anchors a distinct connected mainland");
        // A land bridge through the east-west seam must belong to one component.
        int wrappedLandEdges = 0;
        for (int row = 0; row < topology.Rows; row++)
        {
            int first = row * topology.Columns;
            foreach (int next in topology.Neighbors(first))
                if (next % topology.Columns == topology.Columns - 1 && land[first] == 1 && land[next] == 1)
                {
                    wrappedLandEdges++;
                    Check(components[first] == components[next], "Wrapped mainland remains connected");
                }
        }
        if (world.Grid.Definition.Size == GridSize.Medium && world.Landmass.Configuration.Seed == 42)
            Check(wrappedLandEdges > 0, "A mainland actually crosses the east-west seam");
    }

    private static void TectonicGrowth(GeneratedWorld world)
    {
        var topology = new GEvolve.Engine.World.LandmassGeneration.Shared.HexTopology(world.Grid);
        var terrain = world.Landmass;
        var motion = terrain.TectonicMotion!;
        int length = topology.Count;
        var convergent = motion with
        {
            Interactions = motion.Interactions.Select(edge => edge with
                { Convergence = 8, Divergence = 0, Shear = 0, Kind = PlateInteractionKind.Convergent }).ToArray(),
            CompressionByCellId = Enumerable.Repeat(8f, length).ToArray(), ExtensionByCellId = new float[length]
        };
        var divergent = motion with
        {
            Interactions = motion.Interactions.Select(edge => edge with
                { Convergence = 0, Divergence = 8, Shear = 0, Kind = PlateInteractionKind.Divergent }).ToArray(),
            CompressionByCellId = new float[length], ExtensionByCellId = Enumerable.Repeat(8f, length).ToArray()
        };
        var generator = new ContinentalCrustGenerator();
        var a = generator.Generate(topology, terrain.PlateByCellId!, terrain.Configuration.Continents!.CoreCount,
            terrain.Configuration.Seed, convergent, terrain.DeterministicAttempt);
        var b = generator.Generate(topology, terrain.PlateByCellId!, terrain.Configuration.Continents.CoreCount,
            terrain.Configuration.Seed, divergent, terrain.DeterministicAttempt);
        Check(a.MainContinentCoreCells.SequenceEqual(b.MainContinentCoreCells),
            "Controlled motion comparison keeps the same plate geometry and cratons");
        int changed = Enumerable.Range(0, length).Count(id =>
            Math.Abs(a.EmergenceAffinityByCellId[id] - b.EmergenceAffinityByCellId[id]) > .1);
        Check(changed > length / 5 && a.EmergenceAffinityByCellId.Average() > b.EmergenceAffinityByCellId.Average() &&
            !a.CrustByCellId.SequenceEqual(b.CrustByCellId),
            "Convergence/accretion and divergence/rifting alter the connected growth and crust map");
        static byte[] Emerged(ContinentalCrustMap map)
        {
            var sorted = map.EmergenceAffinityByCellId.Order().ToArray();
            float sea = sorted[(int)(.65 * (sorted.Length - 1))];
            return Enumerable.Range(0, map.CrustByCellId.Length).Select(id =>
                map.CrustByCellId[id] == 1 && map.EmergenceAffinityByCellId[id] >= sea ? (byte)1 : (byte)0).ToArray();
        }
        var convergentLand = Emerged(a); var divergentLand = Emerged(b);
        Check(Enumerable.Range(0, length).Count(id => convergentLand[id] != divergentLand[id]) > length / 100,
            "Changing only tectonic interactions changes the emerged land at a global sea datum");
    }
    private static void ControlledRelief(GeneratedWorld world)
    {
        var topology = new GEvolve.Engine.World.LandmassGeneration.Shared.HexTopology(world.Grid);
        int count = topology.Count;
        var motion = world.Landmass.TectonicMotion!;
        var continentalCrust = Enumerable.Repeat((byte)1, count).ToArray();
        var prototype = new ContinentalCrustMap(continentalCrust, new float[count],
            world.Landmass.ContinentalGeology!.CratonCellIds, [], new byte[count], motion.CompressionByCellId, motion.ExtensionByCellId);
        var pure = world.Landmass with { SurfaceByCellId = Enumerable.Repeat((byte)1, count).ToArray() };
        var generator = new ContinentalReliefGenerator();
        var collisionWorld = generator.Generate(topology, pure, prototype, 0);
        Check(collisionWorld.ContinentalGeology!.Volcanoes.Length == 0 &&
            collisionWorld.ContinentalGeology.Contacts.Any(c => c.Kind == GeologicalContactKind.ContinentalCollision),
            "Pure CC collision never creates volcanoes");
        var inactiveMotion = motion with
        {
            Interactions = motion.Interactions.Select(edge => edge with { Convergence = 0, Divergence = 0, Shear = 0,
                Kind = PlateInteractionKind.Quiet }).ToArray(),
            CompressionByCellId = new float[count], ExtensionByCellId = new float[count], ShearByCellId = new float[count]
        };
        var quiet = generator.Generate(topology, pure with { TectonicMotion = inactiveMotion }, prototype, 0);
        Check(quiet.ContinentalGeology!.Volcanoes.Length == 0 && quiet.ElevationByCellId!.Max() < 25 &&
            collisionWorld.ElevationByCellId!.Max() > quiet.ElevationByCellId!.Max(),
            "Removing relative motion removes tectonic mountain uplift and volcanism");
        foreach (bool transform in new[] { false, true })
        {
            var changed = inactiveMotion with { Interactions = inactiveMotion.Interactions.Select(edge =>
                edge with { Divergence = transform ? 0 : 5, Shear = transform ? 5 : 0 }).ToArray() };
            var cut = generator.Generate(topology, pure with { TectonicMotion = changed }, prototype, 0);
            Check(cut.ContinentalGeology!.Volcanoes.Length == 0 &&
                cut.ElevationByCellId!.Average(h => (double)h) < quiet.ElevationByCellId!.Average(h => (double)h),
                "Rift/transform deformation cuts valleys without subduction volcanoes");
        }
    }
    private static void Persistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"continental-geology-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            Check(JsonSerializer.Serialize(GameSaveStore.Load(path).Landmass!.ContinentalGeology) ==
                JsonSerializer.Serialize(state.Landmass.ContinentalGeology), "Geology save/load");
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            json["Landmass"]!.AsObject().Remove("ContinentalGeology"); File.WriteAllText(path, json.ToJsonString());
            Check(GameSaveStore.Load(path).Landmass!.ContinentalGeology is null, "Old saves without geology remain readable");
            var original = state.Landmass.ContinentalGeology!;
            state.Landmass = state.Landmass with { ContinentalGeology = original with { CrustByCellId = [0] } };
            try { GameSaveStore.Save(path, state); throw new Exception("Malformed geology was saved"); }
            catch (InvalidDataException) { }
            if (original.Volcanoes.Length > 0)
            {
                var invalid = original.Volcanoes.ToArray();
                invalid[0] = invalid[0] with { OverridingPlateId = invalid[0].SubductingPlateId };
                state.Landmass = world.Landmass with { ContinentalGeology = original with { Volcanoes = invalid } };
                try { GameSaveStore.Save(path, state); throw new Exception("Wrong-plate volcano was saved"); }
                catch (InvalidDataException) { }
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
