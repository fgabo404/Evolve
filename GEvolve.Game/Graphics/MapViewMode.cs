using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

public enum MapViewMode { Biomes, Elevation, Infrared, Rainfall, Tectonic, Compression, Extension, Shear, Geology }

public static class MapViewColors
{
    public static Vector3 ForCell(LandmassState terrain, int id, MapViewMode mode)
    {
        if (!terrain.HasSurface && mode is not (MapViewMode.Tectonic or MapViewMode.Compression or
            MapViewMode.Extension or MapViewMode.Shear))
            throw new InvalidOperationException("This map layer requires a generated surface.");
        return mode switch
    {
        MapViewMode.Geology when terrain.ContinentalGeology is not null => ContinentalGeologyPalette.ForCell(terrain, id),
        MapViewMode.Geology when terrain.PangaeaMountainRanges is { } ranges =>
            !terrain.IsLand(id) ? new Vector3(.08f, .17f, .30f) :
            ranges.RangeByCellId[id] == 0 ? new Vector3(.30f, .34f, .30f) :
            new Vector3(.60f, .38f, .17f) +
                new Vector3(terrain.ElevationByCellId![id] / 300f),
        MapViewMode.Compression when terrain.TectonicMotion is { } motion =>
            TectonicStressPalette.ForValue(motion.CompressionByCellId[id], mode),
        MapViewMode.Extension when terrain.TectonicMotion is { } motion =>
            TectonicStressPalette.ForValue(motion.ExtensionByCellId[id], mode),
        MapViewMode.Shear when terrain.TectonicMotion is { } motion =>
            TectonicStressPalette.ForValue(motion.ShearByCellId[id], mode),
        MapViewMode.Tectonic when terrain.PlateByCellId is { } plates =>
            PlatePalette.ForPlate(plates[id], !terrain.TectonicPreviewOnly && !terrain.IsLand(id)),
        MapViewMode.Rainfall when terrain.Climate?.BasePrecipitationMmByCellId is { } rainfall =>
            RainfallPalette.ForMillimetres(rainfall[id]),
        MapViewMode.Infrared when terrain.Climate is { } climate =>
            ThermalPalette.ForCelsius(climate.CurrentCelsius(id)),
        MapViewMode.Elevation when !terrain.IsImpassableSummit(id) &&
            terrain.ElevationByCellId is { } heights => new Vector3(heights[id] / 100f),
        _ => BiomePalette.ForCell(terrain, id)
        };
    }
}
