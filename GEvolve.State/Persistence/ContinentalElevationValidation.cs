using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;

namespace GEvolve.State.Persistence;

internal static class ContinentalElevationValidation
{
    public static void Validate(LandmassState terrain)
    {
        if (terrain.ContinentalElevation is not { } relief) return;
        int count = terrain.SurfaceByCellId.Length;
        var motion = terrain.TectonicMotion;
        var heights = terrain.ElevationByCellId;
        if (terrain.Configuration.Type != LandmassType.Continents || terrain.ContinentalSurface is null ||
            !terrain.HasSurface || terrain.ContinentalGeology is not null ||
            relief.Version is not ("continental-elevation-1" or "continental-elevation-2" or
                "continental-elevation-3" or "continental-elevation-4") ||
            motion is null || heights is null ||
            relief.Contacts is null || relief.Contacts.Length != motion.Interactions.Length ||
            relief.LandformByCellId is null || relief.LandformByCellId.Length != count ||
            relief.OceanDepthByCellId is null || relief.OceanDepthByCellId.Length != count)
            throw new InvalidDataException("Invalid continental elevation stage.");
        if (relief.Version is "continental-elevation-2" or "continental-elevation-3" or
            "continental-elevation-4")
        {
            if (relief.Systems is null || relief.UpliftByCellId is null ||
                relief.SubsidenceByCellId is null || relief.UpliftByCellId.Length != count ||
                relief.SubsidenceByCellId.Length != count)
                throw new InvalidDataException("Invalid regional tectonic fields.");
            if (relief.Version is "continental-elevation-3" or "continental-elevation-4" &&
                (relief.MountainTierByCellId is null ||
                    relief.MountainTierByCellId.Length != count ||
                    relief.MountainTierByCellId.Any(value => !Enum.IsDefined((TectonicMountainTier)value))))
                throw new InvalidDataException("Invalid mountain tier map.");
            for (int systemId = 0; systemId < relief.Systems.Length; systemId++)
            {
                var system = relief.Systems[systemId];
                if (system is null || system.Id != systemId ||
                    system.Kind == GeologicalContactKind.Quiet || !Enum.IsDefined(system.Kind) ||
                    system.InteractionIndices is null || system.InteractionIndices.Length == 0 ||
                    system.AxisCellIds is null || system.AxisCellIds.Length == 0 ||
                    !double.IsFinite(system.Intensity) || system.Intensity < 0 ||
                    !double.IsFinite(system.WidthHexes) || system.WidthHexes <= 0 ||
                    system.InteractionIndices.Any(index => index < 0 || index >= relief.Contacts.Length ||
                        relief.Contacts[index].Kind != system.Kind) ||
                    system.AxisCellIds.Any(id => id < 0 || id >= count) ||
                    !Enum.IsDefined(system.MountainTier))
                    throw new InvalidDataException("Invalid regional tectonic system.");
                if (relief.Version == "continental-elevation-4" &&
                    (system.AffectedLandCells < 0 || system.EffectiveMountainCells < 0 ||
                     system.EffectiveMountainCells > system.AffectedLandCells ||
                     system.LandMountainAxisHexes < 0 ||
                     system.LandMountainAxisHexes > system.AxisCellIds.Length ||
                     system.AffectedLandCells > count ||
                     system.MaximumLandElevation > 100 ||
                     !double.IsFinite(system.MeanLandElevation) ||
                     system.MeanLandElevation is < 0 or > 100 ||
                     system.MountainTier == TectonicMountainTier.Principal &&
                         system.EffectiveMountainCells < 24))
                    throw new InvalidDataException("Invalid effective mountain statistics.");
            }
        }
        for (int id = 0; id < count; id++)
        {
            if (!Enum.IsDefined((ContinentalReliefKind)relief.LandformByCellId[id]) ||
                (terrain.SurfaceByCellId[id] == 1
                    ? heights[id] is 0 or > 100 || relief.OceanDepthByCellId[id] != 0 ||
                        relief.LandformByCellId[id] == (byte)ContinentalReliefKind.Ocean
                    : heights[id] != 0 || relief.OceanDepthByCellId[id] is 0 or > 100 ||
                        relief.LandformByCellId[id] != (byte)ContinentalReliefKind.Ocean))
                throw new InvalidDataException("Continental height, depth and surface disagree.");
        }
        for (int index = 0; index < relief.Contacts.Length; index++)
            if (relief.Contacts[index].InteractionIndex != index || !Enum.IsDefined(relief.Contacts[index].Kind))
                throw new InvalidDataException("Invalid classified continental contact.");
    }
}
