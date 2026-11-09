using System.Numerics;
using GEvolve.State.Game;
using GEvolve.Engine.World.LandmassGeneration;

namespace GEvolve.Game.Graphics;

public enum MapViewMode { Biomes, Elevation, Infrared, Rainfall, Tectonic, Compression, Extension, Shear, Geology,
    ElevationBase, ElevationContribution, ElevationFinal, Grid, Surface, Motion, Moisture }

public static class PangaeaElevationViews
{
    public static bool IsDiagnostic(MapViewMode mode) => mode is MapViewMode.ElevationBase or
        MapViewMode.ElevationContribution or MapViewMode.ElevationFinal;

    public static MapViewMode Next(MapViewMode current, bool available) => available
        ? current switch { MapViewMode.ElevationBase => MapViewMode.ElevationContribution,
            MapViewMode.ElevationContribution => MapViewMode.ElevationFinal,
            _ => MapViewMode.ElevationBase }
        : current == MapViewMode.Elevation ? MapViewMode.Biomes : MapViewMode.Elevation;

    public static string Label(MapViewMode mode) => mode switch
    {
        MapViewMode.ElevationContribution => "ALTURAS: APORTE CORDILLERAS",
        MapViewMode.ElevationFinal => "ALTURAS: FINAL",
        _ => "ALTURAS: BASE"
    };

    public static byte Value(PangaeaElevationDiagnostics diagnostics, int id, MapViewMode mode) => mode switch
    {
        MapViewMode.ElevationBase => diagnostics.BaseByCellId.Span[id],
        MapViewMode.ElevationContribution => diagnostics.ContributionByCellId.Span[id],
        _ => diagnostics.FinalByCellId.Span[id]
    };
}

public static class MapViewColors
{
    public static Vector3 ForCell(LandmassState terrain, int id, MapViewMode mode)
        => ForCell(terrain, id, mode, null);

    public static Vector3 ForCell(LandmassState terrain, int id, MapViewMode mode,
        PangaeaElevationDiagnostics? elevationDiagnostics)
    {
        if (!terrain.HasSurface && mode is not (MapViewMode.Tectonic or MapViewMode.Compression or
            MapViewMode.Extension or MapViewMode.Shear))
            throw new InvalidOperationException("This map layer requires a generated surface.");
        if (PangaeaElevationViews.IsDiagnostic(mode) && elevationDiagnostics is not null)
        {
            if (!terrain.IsLand(id)) return new Vector3(.08f, .17f, .30f);
            byte value = PangaeaElevationViews.Value(elevationDiagnostics, id, mode);
            if (mode != MapViewMode.ElevationContribution) return new Vector3(value / 100f);
            float fraction = Math.Clamp(value / 25f, 0, 1);
            return Vector3.Lerp(new Vector3(.12f, .14f, .16f), new Vector3(1f, .65f, .12f), fraction);
        }
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
