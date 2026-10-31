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
            relief.Version != "continental-elevation-1" || motion is null || heights is null ||
            relief.Contacts is null || relief.Contacts.Length != motion.Interactions.Length ||
            relief.LandformByCellId is null || relief.LandformByCellId.Length != count ||
            relief.OceanDepthByCellId is null || relief.OceanDepthByCellId.Length != count)
            throw new InvalidDataException("Invalid continental elevation stage.");
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
