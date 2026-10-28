using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

public enum MapViewMode { Biomes, Elevation, Infrared, Rainfall }

public static class MapViewColors
{
    public static Vector3 ForCell(LandmassState terrain, int id, MapViewMode mode) => mode switch
    {
        MapViewMode.Rainfall when terrain.Climate?.BasePrecipitationMmByCellId is { } rainfall =>
            RainfallPalette.ForMillimetres(rainfall[id]),
        MapViewMode.Infrared when terrain.Climate is { } climate =>
            ThermalPalette.ForCelsius(climate.CurrentCelsius(id)),
        MapViewMode.Elevation when !terrain.IsImpassableSummit(id) &&
            terrain.ElevationByCellId is { } heights => new Vector3(heights[id] / 100f),
        _ => BiomePalette.ForCell(terrain, id)
    };
}
