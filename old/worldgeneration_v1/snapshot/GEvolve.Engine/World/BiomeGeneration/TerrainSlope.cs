using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.BiomeGeneration;

/// <summary>Máximo |altura propia - altura vecina| entre vecinos terrestres reales.
/// Unidad: niveles de elevación por paso entre centros hexagonales, no grados.</summary>
public static class TerrainSlope
{
    public static byte[]? Calculate(HexTopology topology, LandmassState terrain)
    {
        if (terrain.ElevationByCellId is not { } heights) return null;
        if (heights.Length != topology.Count || terrain.SurfaceByCellId.Length != topology.Count ||
            heights.Any(value => value > 100) || terrain.SurfaceByCellId.Any(value => value > 1))
            throw new ArgumentException("Invalid terrain for slope calculation.", nameof(terrain));
        var slopes = new byte[topology.Count];
        for (int id = 0; id < slopes.Length; id++)
        {
            if (!terrain.IsLand(id)) continue;
            int maximum = 0;
            foreach (int neighbor in topology.Neighbors(id))
                if (terrain.IsLand(neighbor))
                    maximum = Math.Max(maximum, Math.Abs(heights[id] - heights[neighbor]));
            slopes[id] = (byte)maximum;
        }
        return slopes;
    }
}
