using System.Globalization;
using System.Numerics;
using GEvolve.Engine.World;

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
        if (snapshot.Motion is not null) views.AddRange([MapViewMode.Compression, MapViewMode.Extension, MapViewMode.Shear]);
        if (snapshot.Surface is not null) views.Add(MapViewMode.Surface);
        if (snapshot.Base is not null) views.Add(MapViewMode.ElevationBase);
        if (snapshot.Mountains is not null) views.Add(MapViewMode.Geology);
        if (snapshot.Elevations is not null) views.AddRange([MapViewMode.ElevationContribution, MapViewMode.ElevationFinal]);
        AvailableViews = views.AsReadOnly();
    }
    public Vector3 Color(int id, MapViewMode view)
    {
        if (!AvailableViews.Contains(view)) throw new InvalidOperationException("The requested laboratory layer is unavailable.");
        var s = Snapshot;
        bool ocean = s.Surface is not null && s.Surface[id] == 0;
        if (view == MapViewMode.Grid) return new(.84f, .88f, .90f);
        if (view == MapViewMode.Tectonic) return PlatePalette.ForPlate(s.Plates![id], ocean);
        if (view == MapViewMode.Compression) return TectonicStressPalette.ForValue(s.Motion!.CompressionByCellId[id], view);
        if (view == MapViewMode.Extension) return TectonicStressPalette.ForValue(s.Motion!.ExtensionByCellId[id], view);
        if (view == MapViewMode.Shear) return TectonicStressPalette.ForValue(s.Motion!.ShearByCellId[id], view);
        if (view == MapViewMode.Surface) return ocean ? new(.32f, .55f, .71f) : new(.35f, .57f, .32f);
        if (ocean) return new(.08f, .17f, .30f);
        if (view == MapViewMode.Geology)
            return s.Mountains!.InfluenceByCellId[id] == 0 ? new(.30f, .34f, .30f) : new(.60f, .38f, .17f);
        if (view == MapViewMode.ElevationBase) return new(s.Base![id] / 100f);
        if (view == MapViewMode.ElevationFinal) return new(s.Elevations!.FinalByCellId.Span[id] / 100f);
        float fraction = Math.Clamp(s.Elevations!.ContributionByCellId.Span[id] / 25f, 0, 1);
        return Vector3.Lerp(new(.12f, .14f, .16f), new(1f, .65f, .12f), fraction);
    }
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
        return lines;
    }
    public static string ViewLabel(MapViewMode view) => view switch
    {
        MapViewMode.Grid => "CUADRÍCULA", MapViewMode.Tectonic => "PLACAS Y MOVIMIENTO",
        MapViewMode.Compression => "COMPRESIÓN", MapViewMode.Extension => "EXTENSIÓN", MapViewMode.Shear => "CIZALLA",
        MapViewMode.Surface => "SUPERFICIE", MapViewMode.ElevationBase => "ALTURAS BASE",
        MapViewMode.Geology => "RELIEVE: SISTEMAS", MapViewMode.ElevationContribution => "APORTE CORDILLERAS",
        MapViewMode.ElevationFinal => "ALTURAS FINAL", _ => throw new ArgumentOutOfRangeException(nameof(view))
    };
}
