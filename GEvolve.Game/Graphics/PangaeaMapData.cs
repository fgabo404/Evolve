using System.Globalization;
using System.Numerics;
using GEvolve.Engine.World;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.State.Game;

namespace GEvolve.Game.Graphics;

/// <summary>Presentation of existing optional layers. Never constructs an incomplete persistible world.</summary>
public sealed class PangaeaMapData
{
    public PangaeaGenerationSnapshot Snapshot { get; }
    public IReadOnlyList<MapViewMode> AvailableViews { get; }
    public PangaeaMapData(PangaeaGenerationSnapshot snapshot)
    {
        if (snapshot.Grid is null) throw new ArgumentException("A generated grid is required.", nameof(snapshot));
        Snapshot = snapshot;
        var views = new List<MapViewMode> { MapViewMode.Grid };
        if (snapshot.Plates is not null) views.Add(MapViewMode.Tectonic);
        if (snapshot.Motion is not null) views.AddRange([MapViewMode.Motion, MapViewMode.Compression, MapViewMode.Extension, MapViewMode.Shear]);
        if (snapshot.Surface is not null) views.Add(MapViewMode.Surface);
        if (snapshot.Base is not null) views.Add(MapViewMode.ElevationBase);
        if (snapshot.Mountains is not null) views.Add(MapViewMode.Geology);
        if (snapshot.Elevations is not null) views.AddRange([MapViewMode.ElevationContribution, MapViewMode.ElevationFinal]);
        if (snapshot.Climate is not null) views.Add(MapViewMode.Infrared);
        if (snapshot.Climate?.BasePrecipitationMmByCellId is not null) views.AddRange([MapViewMode.Rainfall, MapViewMode.Moisture]);
        if (snapshot.Biomes is not null) views.Add(MapViewMode.Biomes);
        AvailableViews = views.AsReadOnly();
    }
    public Vector3 Color(int id, MapViewMode view)
    {
        if (!AvailableViews.Contains(view)) throw new InvalidOperationException("The requested laboratory layer is unavailable.");
        var s = Snapshot;
        bool ocean = s.Surface is not null && s.Surface[id] == 0;
        if (view == MapViewMode.Infrared) return ThermalPalette.ForCelsius(s.Climate!.BaseCelsius(id));
        if (view == MapViewMode.Rainfall) return RainfallPalette.ForMillimetres(s.Climate!.BasePrecipitationMmByCellId![id]);
        if (view == MapViewMode.Moisture) return Vector3.Lerp(new(.65f, .43f, .20f), new(.14f, .55f, .72f), s.Climate!.BaseMoistureByCellId![id] / 100f);
        if (view == MapViewMode.Biomes) return s.Biomes!.Get(id) is { Status: BiomeClassificationStatus.Assigned, BiomeId: { } biome }
            ? BiomePalette.ForBiome(biome) : new(.43f, .43f, .43f);
        if (view == MapViewMode.Grid) return new(.84f, .88f, .90f);
        if (view is MapViewMode.Tectonic or MapViewMode.Motion) return PlatePalette.ForPlate(s.Plates![id], ocean);
        if (view == MapViewMode.Compression) return TectonicStressPalette.ForValue(s.Motion!.CompressionByCellId[id], view);
        if (view == MapViewMode.Extension) return TectonicStressPalette.ForValue(s.Motion!.ExtensionByCellId[id], view);
        if (view == MapViewMode.Shear) return TectonicStressPalette.ForValue(s.Motion!.ShearByCellId[id], view);
        if (view == MapViewMode.Surface) return ocean ? new(.32f, .55f, .71f) :
            Height(id, view) is > 90 ? Vector3.One : new(.35f, .57f, .32f);
        if (ocean) return new(.08f, .17f, .30f);
        if (view == MapViewMode.Geology)
            return s.Mountains!.InfluenceByCellId[id] == 0 ? new(.30f, .34f, .30f) : new(.60f, .38f, .17f);
        if (view == MapViewMode.ElevationBase) return new(s.Base![id] / 100f);
        if (view == MapViewMode.ElevationFinal) return new(s.Elevations!.FinalByCellId.Span[id] / 100f);
        float fraction = Math.Clamp(s.Elevations!.ContributionByCellId.Span[id] / 25f, 0, 1);
        return Vector3.Lerp(new(.12f, .14f, .16f), new(1f, .65f, .12f), fraction);
    }
    /// <summary>Only current data; null is distinct from a measured zero.</summary>
    public byte? Height(int id, MapViewMode view) => view switch
    {
        MapViewMode.ElevationBase when Snapshot.Base is { } initial => initial[id],
        MapViewMode.ElevationContribution when Snapshot.Elevations is { } fields => fields.ContributionByCellId.Span[id],
        MapViewMode.ElevationFinal when Snapshot.Elevations is { } fields => fields.FinalByCellId.Span[id],
        MapViewMode.ElevationBase or MapViewMode.ElevationContribution or MapViewMode.ElevationFinal => null,
        _ => Snapshot.Elevations is { } fields
            ? fields.FinalByCellId.Span[id] : Snapshot.Base is { } initial ? initial[id] : null,
    };
    public IReadOnlyList<string> Inspect(int id)
    {
        var s = Snapshot; var cell = s.Grid!.Cells[id];
        string Missing(PangaeaGenerationStage stage) => s.Stages.Single(i => i.Stage == stage).Status switch
        { PangaeaStageStatus.Invalidated => "INVALIDADO", PangaeaStageStatus.Running => "GENERANDO",
            PangaeaStageStatus.Failed => "ERROR", _ => "PENDIENTE" };
        string Value<T>(T[]? values, PangaeaGenerationStage stage) => values is null ? Missing(stage) :
            Convert.ToString(values[id], CultureInfo.InvariantCulture)!;
        var lines = new List<string>
        {
            $"CELLID: {id}  COL: {cell.Address.Column}  FILA: {cell.Address.Row}",
            $"AXIAL: {cell.Axial.Q}, {cell.Axial.R}",
            "PLACA: " + Value(s.Plates, PangaeaGenerationStage.Plates),
            "SUPERFICIE: " + (s.Surface is null ? Missing(PangaeaGenerationStage.Surface) : s.Surface[id] == 1 ? "TIERRA" : "OCÉANO"),
            "BASE: " + Value(s.Base, PangaeaGenerationStage.Base)
        };
        if (s.Motion is { } motion && s.Plates is { } plates)
        {
            var movement = motion.Motions.Single(m => m.PlateId == plates[id]);
            lines.Add(FormattableString.Invariant($"MOVIMIENTO: {movement.DirectionDegrees:F2} GRADOS  {movement.SpeedCmPerYear:F2} CM/AÑO"));
            lines.Add(FormattableString.Invariant($"COMP: {motion.CompressionByCellId[id]:F4}  EXT: {motion.ExtensionByCellId[id]:F4}  CIZ: {motion.ShearByCellId[id]:F4}"));
        }
        else lines.Add("MOVIMIENTO Y ESFUERZOS: " + Missing(PangaeaGenerationStage.Motion));
        if (s.Mountains is { } mountains)
        {
            int system = mountains.InfluenceByCellId[id];
            lines.Add("SISTEMA: " + (system == 0 ? "0 (SIN SISTEMA)" : system));
            var axis = mountains.Systems.FirstOrDefault(m => m.Id == system);
            int index = axis is null ? -1 : Array.IndexOf(axis.AxisCellIds, id);
            if (index >= 0) lines.Add($"EJE. ANCHURA: {axis!.WidthByAxisCell[index]} HEX");
        }
        else lines.Add("SISTEMA: " + Missing(PangaeaGenerationStage.MountainSystems));
        lines.Add("APORTE: " + (s.Elevations is null ? Missing(PangaeaGenerationStage.Final) : s.Elevations.ContributionByCellId.Span[id].ToString(CultureInfo.InvariantCulture)));
        lines.Add("FINAL: " + (s.Elevations is null ? Missing(PangaeaGenerationStage.Final) : s.Elevations.FinalByCellId.Span[id].ToString(CultureInfo.InvariantCulture)));
        lines.Add(FormattableString.Invariant($"LATITUD: {BiomeClassifier.Latitude(cell.Address.Row, s.Grid.Definition.Rows):F2} GRADOS"));
        lines.Add("PENDIENTE: " + (s.Slopes is null ? Missing(PangaeaGenerationStage.Temperature) : $"{s.Slopes[id]} NIVELES/PASO"));
        lines.Add("TEMPERATURA: " + (s.Climate is null ? Missing(PangaeaGenerationStage.Temperature) : FormattableString.Invariant($"{s.Climate.BaseCelsius(id):F1} C")));
        lines.Add("PRECIPITACIÓN: " + (s.Climate?.BasePrecipitationMmByCellId is not { } rain ? Missing(PangaeaGenerationStage.Precipitation) : $"{rain[id]} MM/AÑO"));
        lines.Add("HUMEDAD: " + (s.Climate?.BaseMoistureByCellId is not { } humid ? Missing(PangaeaGenerationStage.Precipitation) : $"{humid[id]} / 100"));
        var assignment = s.Biomes?.Get(id);
        lines.Add("BIOMA: " + (assignment?.BiomeId ?? (assignment is null ? Missing(PangaeaGenerationStage.Biomes) : "SIN COINCIDENCIA")));
        lines.Add("SUBBIOMA: " + (assignment?.SubbiomeId ?? (assignment is null ? Missing(PangaeaGenerationStage.Biomes) : "SIN COINCIDENCIA")));
        lines.Add("CLASIFICACIÓN: " + (assignment?.Status.ToString() ?? Missing(PangaeaGenerationStage.Biomes)));
        return lines;
    }
    public static string ViewLabel(MapViewMode view) => view switch
    {
        MapViewMode.Grid => "CUADRÍCULA", MapViewMode.Tectonic => "PLACAS", MapViewMode.Motion => "MOVIMIENTO / H04",
        MapViewMode.Compression => "COMPRESIÓN", MapViewMode.Extension => "EXTENSIÓN", MapViewMode.Shear => "CIZALLA",
        MapViewMode.Surface => "SUPERFICIE", MapViewMode.ElevationBase => "ALTURAS BASE",
        MapViewMode.Geology => "RELIEVE: SISTEMAS", MapViewMode.ElevationContribution => "APORTE CORDILLERAS",
        MapViewMode.ElevationFinal => "ALTURAS FINAL", MapViewMode.Infrared => "TEMPERATURA", MapViewMode.Rainfall => "PRECIPITACIÓN",
        MapViewMode.Moisture => "HUMEDAD", MapViewMode.Biomes => "BIOMAS", _ => throw new ArgumentOutOfRangeException(nameof(view))
    };
}
