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
    public const string Version = "biomes-4";
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

    // Only rank catalog-compatible choices. No climate means legacy fallback.
    private static int ClimaticPriority(BiomeRule rule, byte height, byte? slope,
        double temperature, ushort rainfall, byte moisture)
    {
        int oldPriority = Priority(rule, height, slope);
        if (rule.BiomeId is "polar_north" or "polar_south" or "marine")
            return oldPriority;
        if (rule.BiomeId == "mountain" && oldPriority == 1) return 1;
        bool arid = rainfall < 250 && moisture <= 25;
        bool dry = rainfall < 500 || moisture < 38;
        bool wet = rainfall >= 1000 && moisture >= 45;
        bool veryWet = rainfall >= 1800 && moisture >= 58;
        bool cold = temperature < -4;
        return rule.Subbiome.Id switch
        {
            "desert_sandy" when arid && !cold && (slope ?? 0) < 4 => 2,
            "desert_rocky" when arid && !cold => 2,
            "desert_salt_flat" => 16, // Dryness is not proof of salt deposits.
            "desert_sandy" or "desert_rocky" => 14,
            "tundra_tundra" when cold => 2,
            "tundra_tundra" => 11,
            "plains_steppe" when dry && !arid => 3,
            "plains_savanna" when !arid && !cold && temperature >= 18 &&
                rainfall is >= 250 and < 1500 => 3,
            "plains_prairie" when !arid && !cold &&
                rainfall is >= 450 and < 1600 => 3,
            "forest_tropical" when veryWet && temperature >= 20 => 2,
            "forest_subtropical" when wet && temperature >= 12 => 3,
            "forest_temperate" when wet && temperature >= 2 => 3,
            "forest_boreal" when rainfall >= 500 &&
                temperature is >= -12 and < 12 => 3,
            "forest_wooded_swamp" or "plains_open_wetland" => 12,
            "forest_tropical" or "forest_subtropical" or
                "forest_temperate" or "forest_boreal" when arid => 13,
            "forest_tropical" or "forest_subtropical" or
                "forest_temperate" or "forest_boreal" when wet => 4,
            "plains_steppe" when arid => 5,
            "plains_savanna" or "plains_prairie" when arid => 7,
            "mountain_alpine" => 8,
            "mountain_rocky" => 9,
            "mountain_cliffs" => 10,
            _ => Math.Max(4, oldPriority)
        };
    }

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

    public BiomeAssignment Classify(bool land, byte? elevation, double latitude, byte? slope = null,
        double? temperatureCelsius = null, ushort? rainfallMm = null, byte? moisture = null)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90 || elevation is > 100 ||
            slope is > 100 || moisture is > 100 ||
            temperatureCelsius is not null && !double.IsFinite(temperatureCelsius.Value))
            throw new ArgumentOutOfRangeException(nameof(latitude));
        // Sin relieve disponible, el océano usa la referencia marina del catálogo.
        // Esto no crea ni modifica el arreglo de elevaciones del terreno.
        if (!land && elevation is null) elevation = 0;
        if (elevation is not { } height) return Missing;
        BiomeRule? selected = null;
        int bestPriority = int.MaxValue;
        foreach (var rule in _ordered)
        {
            int priority = land && temperatureCelsius is { } temp &&
                rainfallMm is { } rain && moisture is { } humid
                ? ClimaticPriority(rule, height, slope, temp, rain, humid)
                : Priority(rule, height, slope);
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
        byte[]? slopes = terrain.SlopeByCellId;
        if (terrain.ElevationByCellId is not null && slopes is null)
        {
            topology ??= new HexTopology(HexGrid.Generate(definition));
            if (topology.Grid.Definition != definition)
                throw new ArgumentException("Slope topology differs from the terrain.", nameof(topology));
            slopes = TerrainSlope.Calculate(topology, terrain);
        }
        var climate = terrain.Climate;
        var rain = climate?.BasePrecipitationMmByCellId;
        var moisture = climate?.BaseMoistureByCellId;
        bool hasClimate = climate is not null && rain is not null && moisture is not null;
        if (hasClimate && (climate!.BaseTemperatureTenthsByCellId.Length != count ||
            rain!.Length != count || moisture!.Length != count))
            throw new ArgumentException("Climate dimensions differ from terrain.", nameof(terrain));
        var assignments = new List<BiomeAssignment>();
        var indices = new Dictionary<BiomeAssignment, byte>();
        var byCell = new byte[count];
        // La pendiente forma parte de la clave: misma altura no implica mismo paisaje.
        var rowCache = new Dictionary<(bool, byte?, byte?, short?, ushort?, byte?), byte>();
        for (int row = 0; row < definition.Rows; row++)
        {
            rowCache.Clear();
            double latitude = Latitude(row, definition.Rows);
            for (int column = 0; column < definition.Columns; column++)
            {
                int id = row * definition.Columns + column;
                var key = (terrain.IsLand(id), terrain.ElevationByCellId?[id], slopes?[id],
                    hasClimate ? climate!.BaseTemperatureTenthsByCellId[id] : (short?)null,
                    hasClimate ? rain![id] : (ushort?)null,
                    hasClimate ? moisture![id] : (byte?)null);
                if (!rowCache.TryGetValue(key, out byte index))
                {
                    var assignment = Classify(key.Item1, key.Item2, latitude, key.Item3,
                        key.Item4 is { } tenths ? tenths / 10.0 : null, key.Item5, key.Item6);
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
