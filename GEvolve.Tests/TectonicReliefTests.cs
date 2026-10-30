using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
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

        Console.WriteLine("PASS: motion-driven mountains, rifts, fault valleys, trenches, ridges and quiet terrain");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
