using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Tectonics;

public static class GeologicalContactClassifier
{
    public static GeologicalContact Classify(int index, PlateInteraction edge, CrustKind first,
        CrustKind second, ReadOnlySpan<ushort> oceanicAgeMa)
    {
        bool continentalA = first == CrustKind.Continental, continentalB = second == CrustKind.Continental;
        double normal = Math.Max(edge.Convergence, edge.Divergence);
        if (Math.Max(normal, edge.Shear) <= ContinentalGeologySettings.ActiveNormalCmPerYear)
            return new(index, GeologicalContactKind.Quiet);
        if (normal < ContinentalGeologySettings.ActiveNormalCmPerYear ||
            normal < edge.Shear * ContinentalGeologySettings.MinimumNormalToShearRatio)
            return new(index, GeologicalContactKind.Transform);
        if (edge.Divergence > edge.Convergence)
            return new(index, continentalA || continentalB ? GeologicalContactKind.ContinentalRift : GeologicalContactKind.OceanRidge);
        if (continentalA && continentalB) return new(index, GeologicalContactKind.ContinentalCollision);
        byte subducting = continentalA ? edge.SecondPlateId : continentalB ? edge.FirstPlateId :
            oceanicAgeMa[edge.FirstPlateId] > oceanicAgeMa[edge.SecondPlateId] ? edge.FirstPlateId :
            oceanicAgeMa[edge.FirstPlateId] < oceanicAgeMa[edge.SecondPlateId] ? edge.SecondPlateId :
            Math.Min(edge.FirstPlateId, edge.SecondPlateId);
        byte upper = subducting == edge.FirstPlateId ? edge.SecondPlateId : edge.FirstPlateId;
        return new(index, continentalA || continentalB ? GeologicalContactKind.OceanContinentSubduction :
            GeologicalContactKind.OceanOceanSubduction, subducting, upper);
    }
}
