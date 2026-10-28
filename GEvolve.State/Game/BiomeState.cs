namespace GEvolve.State.Game;

public enum BiomeClassificationStatus { Assigned, MissingElevation, NoCatalogMatch, NotGenerated }
public sealed record BiomeAssignment(string? BiomeId, string? SubbiomeId,
    BiomeClassificationStatus Status);

/// <summary>Un índice por celda; los IDs solo se almacenan una vez en la tabla.</summary>
public sealed record BiomeState(string ClassifierVersion, string CatalogHash,
    BiomeAssignment[] Assignments, byte[] AssignmentByCellId)
{
    public BiomeAssignment Get(int cellId) => Assignments[AssignmentByCellId[cellId]];
}
