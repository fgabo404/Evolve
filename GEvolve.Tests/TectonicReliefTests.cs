using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using System.Text.Json.Nodes;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class TectonicReliefTests
{
    public static void Run()
    {
        var topology = new HexTopology(HexGrid.Generate(HexGridDefinitions.Get(GridSize.Small)));
        int count = topology.Count;
        var surface = new byte[count];
        var baseHeight = new byte[count];
        var c = new float[count];
        var d = new float[count];
        var s = new float[count];
        var distances = new ushort[count];
        Array.Fill(distances, ushort.MaxValue);

        int mountain = topology.Id(0, 25), rift = topology.Id(10, 25),
            fault = topology.Id(20, 25), trench = topology.Id(30, 25),
            ridge = topology.Id(40, 25), oceanFault = topology.Id(50, 25),
            quietLand = topology.Id(60, 25), farLand = topology.Id(70, 25);
        foreach (int id in new[] { mountain, rift, fault, quietLand, farLand })
        {
            surface[id] = 1;
            baseHeight[id] = 50;
        }
        c[mountain] = c[trench] = c[farLand] = 8;
        d[rift] = d[ridge] = 8;
        s[fault] = s[oceanFault] = 8;
        foreach (int id in new[] { mountain, rift, fault, trench, ridge, oceanFault })
            distances[id] = 0;

        var motion = new TectonicMotionState("controlled", [], [], c, d, s, distances, [], 2);
        var generator = new TectonicReliefGenerator();
        var output = generator.Generate(topology, surface, baseHeight, motion);
        var repeated = generator.Generate(topology, surface, baseHeight, motion);

        Check(output.ElevationByCellId.SequenceEqual(repeated.ElevationByCellId) &&
            output.OceanFloorDepthByCellId.SequenceEqual(repeated.OceanFloorDepthByCellId) &&
            output.GeologicFeaturesByCellId.SequenceEqual(repeated.GeologicFeaturesByCellId),
            "Tectonic landform generation is deterministic");
        Check(output.ElevationByCellId[mountain] == 86 &&
            output.ElevationByCellId[rift] == 25 &&
            output.ElevationByCellId[fault] == 34 &&
            output.ElevationByCellId[quietLand] == 50 &&
            output.ElevationByCellId[farLand] == 50,
            "Real convergence raises mountain belts; extension and transform motion cut valleys");
        Check(output.OceanFloorDepthByCellId[trench] == 78 &&
            output.OceanFloorDepthByCellId[ridge] == 29 &&
            output.OceanFloorDepthByCellId[oceanFault] == 60 &&
            output.ElevationByCellId[trench] == 0 &&
            output.OceanFloorDepthByCellId[mountain] == 0,
            "Ocean trenches and ridges use separate depth without lifting sea-level elevation");
        Check((GeologicFeature)output.GeologicFeaturesByCellId[mountain] == GeologicFeature.MountainRange &&
            (GeologicFeature)output.GeologicFeaturesByCellId[rift] == GeologicFeature.RiftValley &&
            (GeologicFeature)output.GeologicFeaturesByCellId[fault] == GeologicFeature.FaultValley &&
            (GeologicFeature)output.GeologicFeaturesByCellId[trench] == GeologicFeature.OceanTrench &&
            (GeologicFeature)output.GeologicFeaturesByCellId[ridge] == GeologicFeature.MidOceanRidge &&
            (GeologicFeature)output.GeologicFeaturesByCellId[oceanFault] == GeologicFeature.OceanTransformFault &&
            (GeologicFeature)output.GeologicFeaturesByCellId[farLand] == GeologicFeature.None,
            "Geological features follow the dominating actual motion channel and boundary distance");
        Check(output.ElevationByCellId.All(value => value <= 100) &&
            output.OceanFloorDepthByCellId.All(value => value <= 100) &&
            surface[trench] == 0 && surface[mountain] == 1 && baseHeight[mountain] == 50,
            "Land mask and original terrain remain untouched");

        var motionless = motion with
        {
            CompressionByCellId = new float[count],
            ExtensionByCellId = new float[count],
            ShearByCellId = new float[count]
        };
        var noMotion = generator.Generate(topology, surface, baseHeight, motionless);
        Check(noMotion.ElevationByCellId[mountain] == 50 &&
            noMotion.GeologicFeaturesByCellId[mountain] == (byte)GeologicFeature.None &&
            noMotion.OceanFloorDepthByCellId[trench] == 52,
            "No motion means no motion-created mountain, fault or ocean trough");

        // Boundary-derived deformation remains continuous through the east/west wrap.
        int seam = topology.Id(topology.Columns - 1, 25);
        float[] impulses = new float[count]; impulses[mountain] = 8;
        var spread = TectonicMotionGenerator.Spread(topology, impulses, new float[count],
            new float[count], 2);
        Check(spread.Compression[seam] > 0, "Deformation crosses the periodic hex seam");

        // The generated world persists the new arrays without regenerating terrain.
        var generated = new WorldGenerationService().Generate(
            LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 2026));
        var terrain = generated.Landmass;
        string path = Path.Combine(Path.GetTempPath(), $"evolve-geology-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState
            {
                HexGrid = new HexGridState(generated.Grid.Definition),
                Landmass = terrain
            };
            GameSaveStore.Save(path, state);
            var restored = GameSaveStore.Load(path).Landmass!;
            Check(restored.GeologicModelVersion == TectonicReliefGenerator.Version &&
                restored.ElevationByCellId!.SequenceEqual(terrain.ElevationByCellId!) &&
                restored.OceanFloorDepthByCellId!.SequenceEqual(terrain.OceanFloorDepthByCellId!) &&
                restored.GeologicFeaturesByCellId!.SequenceEqual(terrain.GeologicFeaturesByCellId!),
                "Geological relief survives JSON save and load");

            state.Landmass = terrain with
            {
                GeologicFeaturesByCellId = [255]
            };
            bool rejected = false;
            try { GameSaveStore.Save(path, state); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Invalid geological arrays cannot be persisted");

            // Historical snapshots without these optional properties still load.
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            var land = json["Landmass"]!.AsObject();
            land.Remove("OceanFloorDepthByCellId");
            land.Remove("GeologicFeaturesByCellId");
            land.Remove("GeologicModelVersion");
            File.WriteAllText(path, json.ToJsonString());
            var older = GameSaveStore.Load(path).Landmass!;
            Check(older.GeologicModelVersion is null &&
                older.GeologicFeaturesByCellId is null &&
                older.OceanFloorDepthByCellId is null,
                "Old saves without geological relief still load");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        Console.WriteLine("PASS: motion-driven mountains, rifts, faults, trenches, ridges, persistence and legacy saves");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
