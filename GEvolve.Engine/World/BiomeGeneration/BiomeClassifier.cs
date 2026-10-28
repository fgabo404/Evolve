using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.BiomeGeneration;

public sealed record CellEnvironment(byte? Elevation, double Latitude,
    BiomeAssignment Biome);

/// <summary>Etapa posterior al terreno: no modifica superficie, alturas ni placas.</summary>
public sealed class BiomeClassifier
{
    public const string Version = "biomes-1";
    private static readonly BiomeAssignment Missing = new(null, null,
        BiomeClassificationStatus.MissingElevation);
    private static readonly BiomeAssignment NoMatch = new(null, null,
        BiomeClassificationStatus.NoCatalogMatch);
    private static readonly BiomeAssignment NotGenerated = new(null, null,
        BiomeClassificationStatus.NotGenerated);
    private readonly BiomeCatalog _catalog;
    private readonly BiomeRule[] _ordered;

    public BiomeClassifier(BiomeCatalog? catalog = null)
    {
        _catalog = catalog ?? BiomeCatalog.Default;
        _ordered = _catalog.Rules.OrderBy(Priority)
            .ThenBy(rule => (rule.Subbiome.ElevationLevel.Max - rule.Subbiome.ElevationLevel.Min) *
                (rule.Subbiome.LatitudeDegrees.Max - rule.Subbiome.LatitudeDegrees.Min))
            .ThenBy(rule => rule.Subbiome.Id, StringComparer.Ordinal).ToArray();
    }

    // Preferencias provisionales, no rangos climáticos. El resto se ordena
    // por especificidad de los rangos y, en empates, por ID ordinal.
    private static int Priority(BiomeRule rule) => rule.BiomeId switch
    {
        "polar_north" or "polar_south" => 0,
        "mountain" when rule.Subbiome.Id != "mountain_cliffs" => 1,
        "tundra" => 2,
        "marine" when rule.Subbiome.Id == "marine_open_ocean" => 3,
        "mountain" => 5, // Sin pendientes, acantilados es solo último recurso compatible.
        _ => 4
    };

    public static double Latitude(double row, int rows)
    {
        if (rows < 1 || !double.IsFinite(row) || row < 0 || row > rows - 1)
            throw new ArgumentOutOfRangeException(nameof(row));
        return rows == 1 ? 0 : 90 - 180 * row / (rows - 1);
    }

    public static bool Matches(BiomeRule rule, bool land, byte elevation, double latitude)
    {
        bool polar = rule.BiomeId is "polar_north" or "polar_south";
        if (land ? rule.BiomeId == "marine" : rule.BiomeId != "marine" && !polar)
            return false;
        var height = rule.Subbiome.ElevationLevel;
        var lat = rule.Subbiome.LatitudeDegrees;
        return elevation >= height.Min && elevation <= height.Max &&
            Math.Abs(latitude) >= lat.Min && Math.Abs(latitude) <= lat.Max &&
            (lat.Hemisphere == "either" || lat.Hemisphere == "north" && latitude >= 0 ||
                lat.Hemisphere == "south" && latitude <= 0);
    }

    public BiomeAssignment Classify(bool land, byte? elevation, double latitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90 || elevation is > 100)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (elevation is not { } height) return Missing;
        foreach (var rule in _ordered)
            if (Matches(rule, land, height, latitude))
                return new(rule.BiomeId, rule.Subbiome.Id, BiomeClassificationStatus.Assigned);
        return NoMatch;
    }

    public LandmassState Apply(LandmassState terrain)
    {
        var definition = HexGridDefinitions.Get(terrain.Configuration.Size);
        int count = checked(definition.Columns * definition.Rows);
        if (terrain.SurfaceByCellId.Length != count ||
            terrain.SurfaceByCellId.Any(value => value > 1) ||
            terrain.ElevationByCellId is { } heights &&
                (heights.Length != count || heights.Any(value => value > 100)))
            throw new ArgumentException("Terrain dimensions or values are invalid.", nameof(terrain));
        var assignments = new List<BiomeAssignment>();
        var indices = new Dictionary<BiomeAssignment, byte>();
        var byCell = new byte[count];
        // Elevation is integral 0..100; cache by row, surface and elevation.
        var rowCache = new Dictionary<(bool, byte?), byte>();
        for (int row = 0; row < definition.Rows; row++)
        {
            rowCache.Clear();
            double latitude = Latitude(row, definition.Rows);
            for (int column = 0; column < definition.Columns; column++)
            {
                int id = row * definition.Columns + column;
                var key = (terrain.IsLand(id), terrain.ElevationByCellId?[id]);
                if (!rowCache.TryGetValue(key, out byte index))
                {
                    var assignment = Classify(key.Item1, key.Item2, latitude);
                    if (!indices.TryGetValue(assignment, out index))
                    {
                        index = checked((byte)assignments.Count);
                        indices.Add(assignment, index);
                        assignments.Add(assignment);
                    }
                    rowCache.Add(key, index);
                }
                byCell[id] = index;
            }
        }
        return terrain with { Biomes = new(Version, _catalog.ContentHash, assignments.ToArray(), byCell) };
    }

    public static CellEnvironment GetCell(LandmassState terrain, int cellId)
    {
        var definition = HexGridDefinitions.Get(terrain.Configuration.Size);
        if ((uint)cellId >= (uint)terrain.SurfaceByCellId.Length)
            throw new ArgumentOutOfRangeException(nameof(cellId));
        return new(terrain.ElevationByCellId?[cellId],
            Latitude(cellId / definition.Columns, definition.Rows),
            terrain.Biomes?.Get(cellId) ?? NotGenerated);
    }
}
