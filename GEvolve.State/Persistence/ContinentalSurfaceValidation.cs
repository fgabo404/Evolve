using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;

namespace GEvolve.State.Persistence;

internal static class ContinentalSurfaceValidation
{
    public static void Validate(LandmassState terrain)
    {
        if (terrain.ContinentalSurface is not { } surface) return; // Legacy saves predate this stage.
        int count = terrain.SurfaceByCellId.Length;
        var plates = terrain.PlateByCellId;
        if (terrain.Configuration.Type != LandmassType.Continents || !terrain.HasSurface ||
            terrain.TectonicMotion is null ||
            ((terrain.ElevationByCellId is null) != (terrain.ContinentalElevation is null)) ||
            terrain.ContinentalGeology is not null || terrain.Climate is not null || terrain.Biomes is not null ||
            surface.Version is not ("plate-groups-surface-1" or "plate-groups-surface-2") ||
            !float.IsFinite(surface.SeaLevel) ||
            plates is null || plates.Length != count ||
            surface.GroupByPlateId is null || surface.GroupByPlateId.Length != 16 ||
            surface.CrustByCellId is null || surface.CrustByCellId.Length != count ||
            surface.ContinentByCellId is null || surface.ContinentByCellId.Length != count)
            throw new InvalidDataException("Invalid continental surface stage.");
        var groups = surface.GroupByPlateId.Where(group => group != byte.MaxValue).Distinct().Order().ToArray();
        if (groups.Length is < 2 or > 6 || !groups.SequenceEqual(Enumerable.Range(0, groups.Length).Select(i => (byte)i)))
            throw new InvalidDataException("Invalid continental plate groups.");
        if (surface.Selection is not null && surface.Version != "plate-groups-surface-2")
            throw new InvalidDataException("Legacy continental surfaces cannot contain selection diagnostics.");
        if (surface.Selection is { } selection &&
            (selection.EvaluatedGroupings is < 1 or > 8 ||
             selection.SelectedAttempt < 0 || selection.SelectedAttempt >= selection.EvaluatedGroupings ||
             selection.AvailableConvergentMargins < 1 || selection.Attempts is null ||
             selection.Attempts.Length != selection.EvaluatedGroupings ||
             selection.Attempts.Where((entry, index) => entry is null || entry.Attempt != index ||
                 entry.Outcome is null).Any() ||
             selection.Attempts[selection.SelectedAttempt].PrincipalSystems < 1 ||
             terrain.ContinentalElevation is null ||
             (terrain.ContinentalElevation.Version is "continental-elevation-5" or "continental-elevation-6"
                 ? selection.FinalPrincipalSystems < 1 ||
                   selection.FinalPrincipalSystems != terrain.ContinentalElevation.Systems.Count(system =>
                       system.MountainTier == TectonicMountainTier.Principal) ||
                   selection.FinalSecondarySystems != terrain.ContinentalElevation.Systems.Count(system =>
                       system.MountainTier == TectonicMountainTier.Secondary)
                 : terrain.ContinentalElevation.Systems.Count(system =>
                       system.MountainTier == TectonicMountainTier.Principal) !=
                       selection.Attempts[selection.SelectedAttempt].PrincipalSystems)))
            throw new InvalidDataException("Invalid continental selection diagnostics.");
        for (int id = 0; id < count; id++)
        {
            byte group = surface.GroupByPlateId[plates[id]];
            if (surface.CrustByCellId[id] > 1 ||
                (terrain.SurfaceByCellId[id] == 1
                    ? surface.CrustByCellId[id] != 1 || surface.ContinentByCellId[id] != group || group == byte.MaxValue
                    : surface.ContinentByCellId[id] != byte.MaxValue))
                throw new InvalidDataException("Continental crust, identity and surface disagree.");
        }
    }
}
