namespace GEvolve.Definitions.WorldGeneration.Configuration;

public enum GridSize { Small, Medium, Large }
public enum GridOrientation { FlatTop }

/// <summary>Configuración efectiva de la capa espacial, independiente del terreno.</summary>
public sealed record HexGridDefinition(
    GridSize Size,
    int Columns,
    int Rows,
    GridOrientation Orientation,
    bool WrapHorizontal,
    bool WrapVertical);

/// <summary>Única fuente de los tres tamaños oficiales de la capa 01.</summary>
public static class HexGridDefinitions
{
    private static readonly HexGridDefinition Small =
        new(GridSize.Small, 100, 70, GridOrientation.FlatTop, true, false);
    private static readonly HexGridDefinition Medium =
        new(GridSize.Medium, 150, 100, GridOrientation.FlatTop, true, false);
    private static readonly HexGridDefinition Large =
        new(GridSize.Large, 250, 200, GridOrientation.FlatTop, true, false);

    public static IReadOnlyList<HexGridDefinition> All { get; } = [Small, Medium, Large];

    public static HexGridDefinition Get(GridSize size) => size switch
    {
        GridSize.Small => Small,
        GridSize.Medium => Medium,
        GridSize.Large => Large,
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
}
