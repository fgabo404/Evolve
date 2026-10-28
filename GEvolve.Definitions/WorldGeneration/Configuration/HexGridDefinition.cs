namespace GEvolve.Definitions.WorldGeneration.Configuration;

public enum GridSize { Small, Medium, Large, Enormous }
public enum GridOrientation { FlatTop }

/// <summary>Configuración efectiva de la capa espacial, independiente del terreno.</summary>
public sealed record HexGridDefinition(
    GridSize Size,
    int Columns,
    int Rows,
    GridOrientation Orientation,
    bool WrapHorizontal,
    bool WrapVertical);

/// <summary>Four official world sizes; columns retained and rows rounded to ≈3:2.</summary>
public static class HexGridDefinitions
{
    private static readonly HexGridDefinition Small =
        new(GridSize.Small, 200, 133, GridOrientation.FlatTop, true, false);
    private static readonly HexGridDefinition Medium =
        new(GridSize.Medium, 250, 167, GridOrientation.FlatTop, true, false);
    private static readonly HexGridDefinition Large =
        new(GridSize.Large, 300, 200, GridOrientation.FlatTop, true, false);
    private static readonly HexGridDefinition Enormous =
        new(GridSize.Enormous, 400, 267, GridOrientation.FlatTop, true, false);

    public static IReadOnlyList<HexGridDefinition> All { get; } = [Small, Medium, Large, Enormous];

    public static HexGridDefinition Get(GridSize size) => size switch
    {
        GridSize.Small => Small,
        GridSize.Medium => Medium,
        GridSize.Large => Large,
        GridSize.Enormous => Enormous,
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
}
