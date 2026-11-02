using GEvolve.State.Game;
using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Persistence;

internal static class ContinentalGeologyValidation
{
    public static void Validate(LandmassState terrain)
    {
        if (terrain.ContinentalGeology is not { } geology) return;
        int count = terrain.SurfaceByCellId.Length;
        var motion = terrain.TectonicMotion;
        if (terrain.Configuration.Type != LandmassType.Continents || motion is null ||
            terrain.ElevationByCellId is null || terrain.PlateByCellId is null ||
            string.IsNullOrWhiteSpace(geology.Version) || !float.IsFinite(geology.SeaLevel) ||
            geology.CratonCellIds is null || geology.CratonCellIds.Length is < 2 or > 6 ||
            geology.CratonCellIds.Distinct().Count() != geology.CratonCellIds.Length ||
            geology.OceanicAgeMaByPlateId is null || geology.OceanicAgeMaByPlateId.Length != 16 ||
            geology.OceanicAgeMaByPlateId.Any(age => age is < 20 or > 180) ||
            geology.Contacts is null || geology.Contacts.Length != motion.Interactions.Length || geology.Volcanoes is null ||
            geology.CrustByCellId is null || geology.CrustByCellId.Length != count || geology.CrustByCellId.Any(value => value > 1) ||
            geology.ProvinceByCellId is null || geology.ProvinceByCellId.Length != count ||
            geology.ProvinceByCellId.Any(value => value >= geology.CratonCellIds.Length) ||
            geology.LandformByCellId is null || geology.LandformByCellId.Length != count ||
            geology.LandformByCellId.Any(value => !Enum.IsDefined((ContinentalLandform)value)) ||
            geology.OceanDepthByCellId is null || geology.OceanDepthByCellId.Length != count)
            throw new InvalidDataException("Invalid continental geology arrays.");
        foreach (int id in geology.CratonCellIds)
            if ((uint)id >= (uint)count || geology.CrustByCellId[id] != 1 || !terrain.IsLand(id))
                throw new InvalidDataException("Invalid continental craton.");
        for (int id = 0; id < count; id++)
            if (terrain.IsLand(id) ? geology.OceanDepthByCellId[id] != 0 || terrain.ElevationByCellId[id] is 0 or > 100 :
                geology.OceanDepthByCellId[id] is 0 or > 100 || terrain.ElevationByCellId[id] != 0)
                throw new InvalidDataException("Invalid continental height/depth.");
        for (int i = 0; i < geology.Contacts.Length; i++)
        {
            var contact = geology.Contacts[i];
            if (contact.InteractionIndex != i || !Enum.IsDefined(contact.Kind)) throw new InvalidDataException("Invalid geological contact.");
            bool subduction = contact.Kind is GeologicalContactKind.OceanContinentSubduction or GeologicalContactKind.OceanOceanSubduction;
            var edge = motion.Interactions[i];
            if (subduction)
            {
                if (edge.Convergence < ContinentalGeologySettings.ActiveNormalCmPerYear ||
                    contact.SubductingPlateId == contact.OverridingPlateId ||
                    !(contact.SubductingPlateId == edge.FirstPlateId && contact.OverridingPlateId == edge.SecondPlateId ||
                      contact.SubductingPlateId == edge.SecondPlateId && contact.OverridingPlateId == edge.FirstPlateId) ||
                    geology.CrustByCellId[contact.SubductingPlateId == edge.FirstPlateId ? edge.FirstCellId : edge.SecondCellId] != 0)
                    throw new InvalidDataException("Invalid subduction sides.");
            }
            else if (contact.SubductingPlateId != byte.MaxValue || contact.OverridingPlateId != byte.MaxValue)
                throw new InvalidDataException("Non-subduction contact has a subducting plate.");
        }
        if (geology.Volcanoes.Any(v => v is null) || geology.Volcanoes.Select(v => v.CellId).Distinct().Count() != geology.Volcanoes.Length)
            throw new InvalidDataException("Duplicate volcano locations.");
        foreach (var volcano in geology.Volcanoes)
        {
            if (volcano is null || (uint)volcano.CellId >= (uint)count ||
                (uint)volcano.InteractionIndex >= (uint)geology.Contacts.Length || !Enum.IsDefined(volcano.Kind))
                throw new InvalidDataException("Invalid volcano.");
            var contact = geology.Contacts[volcano.InteractionIndex];
            if (contact.Kind is not (GeologicalContactKind.OceanContinentSubduction or GeologicalContactKind.OceanOceanSubduction) ||
                contact.SubductingPlateId != volcano.SubductingPlateId || contact.OverridingPlateId != volcano.OverridingPlateId ||
                terrain.PlateByCellId[volcano.CellId] != volcano.OverridingPlateId || !terrain.IsLand(volcano.CellId) ||
                geology.LandformByCellId[volcano.CellId] != (byte)ContinentalLandform.VolcanicArc)
                throw new InvalidDataException("Volcano is not on an upper-plate subduction arc.");
        }
    }
}
