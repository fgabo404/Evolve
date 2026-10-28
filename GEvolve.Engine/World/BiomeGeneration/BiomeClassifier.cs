using GEvolve.Definitions.Catalogs.World;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.State.Game;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.BiomeGeneration;

public sealed record CellEnvironment(byte? Elevation, double Latitude,
    BiomeAssignment Biome, bool IsImpassableSummit, byte? Slope);

/// <summary>Etapa posterior al terreno: no modifica superficie, alturas ni placas.</summary>
public sealed class BiomeClassifier
{
    public const string Version = "biomes-3";
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
        _ordered = _catalog.Rules.OrderBy(rule =>
                rule.Subbiome.LatitudeDegrees.Max - rule.Subbiome.LatitudeDegrees.Min)
            .ThenBy(rule => rule.Subbiome.Id, StringComparer.Ordinal).ToArray();
    }

    // Preferencias provisionales, no rangos climáticos. El resto se ordena
    // por amplitud de latitud y, en empates, por ID ordinal. Ampliar el rango
    // de elevación de vegetación no debe penalizarla en sus tierras bajas.
    private static int Priority(BiomeRule rule, byte height, byte? slope) => rule.BiomeId switch
    {
        "polar_north" or "polar_south" => 0,
        "marine" => rule.Subbiome.Id == "marine_open_ocean" ? 0 : 1,
        "mountain" when rule.Subbiome.Id == "mountain_rocky" &&
            (height >= TerrainRules.ImpassableSummitMinimumElevation ||
             height >= BiomeTerrainRules.SlopedMountainMinimum &&
             slope >= BiomeTerrainRules.PronouncedSlopeMinimum) => 1,
        "mountain" when rule.Subbiome.Id == "mountain_alpine" &&
            height >= BiomeTerrainRules.HighMountainMinimum &&
            slope >= BiomeTerrainRules.ModerateSlopeMinimum => 1,
        "tundra" => 2,
        "plains" when rule.Subbiome.Id != "plains_open_wetland" &&
            slope < BiomeTerrainRules.ModerateSlopeMinimum => 3,
        "forest" when rule.Subbiome.Id != "forest_wooded_swamp" &&
            slope >= BiomeTerrainRules.ModerateSlopeMinimum => 3,
        "mountain" => rule.Subbiome.Id switch
        {
            "mountain_alpine" => 8, "mountain_rocky" => 9, _ => 10
        },
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
        if (land ? rule.BiomeId == "marine" : rule.BiomeId != "marine")
            return false;
        var height = rule.Subbiome.ElevationLevel;
        var lat = rule.Subbiome.LatitudeDegrees;
        return elevation >= height.Min && elevation <= height.Max &&
            Math.Abs(latitude) >= lat.Min && Math.Abs(latitude) <= lat.Max &&
            (lat.Hemisphere == "either" || lat.Hemisphere == "north" && latitude >= 0 ||
                lat.Hemisphere == "south" && latitude <= 0);
    }

    public BiomeAssignment Classify(bool land, byte? elevation, double latitude, byte? slope = null)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90 || elevation is > 100 || slope is > 100)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        // Sin relieve disponible, el océano usa la referencia marina del catálogo.
        // Esto no crea ni modifica el arreglo de elevaciones del terreno.
        if (!land && elevation is null) elevation = 0;
        if (elevation is not { } height) return Missing;
        BiomeRule? selected = null;
        int bestPriority = int.MaxValue;
        foreach (var rule in _ordered)
        {
            int priority = Priority(rule, height, slope);
            if (priority < bestPriority && Matches(rule, land, height, latitude))
            {
                selected = rule;
                bestPriority = priority;
            }
        }
        return selected is null ? NoMatch :
            new(selected.BiomeId, selected.Subbiome.Id, BiomeClassificationStatus.Assigned);
    }

    public LandmassState Apply(LandmassState terrain, HexTopology? topology = null)
    {
        var definition = HexGridDefinitions.Get(terrain.Configuration.Size);
        int count = checked(definition.Columns * definition.Rows);
        if (terrain.SurfaceByCellId.Length != count ||
            terrain.SurfaceByCellId.Any(value => value > 1) ||
            terrain.ElevationByCellId is { } heights &&
                (heights.Length != count || heights.Any(value => value > 100)))
            throw new ArgumentException("Terrain dimensions or values are invalid.", nameof(terrain));
        byte[]? slopes = null;
        if (terrain.ElevationByCellId is not null)
        {
            topology ??= new HexTopology(HexGrid.Generate(definition));
            if (topology.Grid.Definition != definition)
                throw new ArgumentException("Slope topology differs from the terrain.", nameof(topology));
            slopes = TerrainSlope.Calculate(topology, terrain);
        }
        var assignments = new List<BiomeAssignment>();
        var indices = new Dictionary<BiomeAssignment, byte>();
        var byCell = new byte[count];
        // La pendiente forma parte de la clave: misma altura no implica mismo paisaje.
        var rowCache = new Dictionary<(bool, byte?, byte?), byte>();
        for (int row = 0; row < definition.Rows; row++)
        {
            rowCache.Clear();
            double latitude = Latitude(row, definition.Rows);
            for (int column = 0; column < definition.Columns; column++)
            {
                int id = row * definition.Columns + column;
                var key = (terrain.IsLand(id), terrain.ElevationByCellId?[id], slopes?[id]);
                if (!rowCache.TryGetValue(key, out byte index))
                {
                    var assignment = Classify(key.Item1, key.Item2, latitude, key.Item3);
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
        return terrain with { SlopeByCellId = slopes,
            Biomes = new(Version, _catalog.ContentHash, assignments.ToArray(), byCell) };
    }

    public static CellEnvironment GetCell(LandmassState terrain, int cellId)
    {
        var definition = HexGridDefinitions.Get(terrain.Configuration.Size);
        if ((uint)cellId >= (uint)terrain.SurfaceByCellId.Length)
            throw new ArgumentOutOfRangeException(nameof(cellId));
        return new(terrain.ElevationByCellId?[cellId],
            Latitude(cellId / definition.Columns, definition.Rows),
            terrain.Biomes?.Get(cellId) ?? NotGenerated, terrain.IsImpassableSummit(cellId),
            terrain.SlopeByCellId?[cellId]);
    }
}
