using System.Numerics;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

public enum MapViewMode { Biomes, Elevation, Infrared, Rainfall, Tectonic, Compression, Extension, Shear, Geology }

public static class MapViewColors
{
    public static Vector3 ForCell(LandmassState terrain, int id, MapViewMode mode) => mode switch
    {
        MapViewMode.Geology when terrain.GeologicFeaturesByCellId is { } features =>
            GeologicPalette.ForFeature((GeologicFeature)features[id], terrain.IsLand(id)),
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
