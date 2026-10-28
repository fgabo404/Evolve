using System.Text.Json;
using GEvolve.State.Game;
using GEvolve.Definitions.Versioning;

namespace GEvolve.State.Persistence;

public static class GameSaveStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static void Save(string path, GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Validate(state);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var tempPath = fullPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(state, Options));
        File.Move(tempPath, fullPath, overwrite: true);
    }
    public static GameState Load(string path)
    {
        var state = JsonSerializer.Deserialize<GameState>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Save file is empty.");
        if (state.SaveFormatVersion is not 1 && state.SaveFormatVersion != EvolveVersion.SaveFormat)
            throw new NotSupportedException($"Unsupported save format: {state.SaveFormatVersion}");
        Validate(state);
        return state;
    }

    private static void Validate(GameState state)
    {
        if (state.Landmass is not { } landmass) return;
        if (state.SaveFormatVersion != EvolveVersion.SaveFormat || state.HexGrid is null ||
            state.HexGrid.Definition.Size != landmass.Configuration.Size ||
            landmass.SurfaceByCellId.Length != checked(state.HexGrid.Definition.Columns *
                state.HexGrid.Definition.Rows) ||
            landmass.SurfaceByCellId.Any(surface => surface is not 0 and not 1) ||
            (landmass.ElevationByCellId is { } heights &&
                (heights.Length != landmass.SurfaceByCellId.Length || heights.Any(level => level > 100))) ||
            (landmass.PlateByCellId is { } plates && plates.Length != landmass.SurfaceByCellId.Length))
            throw new InvalidDataException("Invalid landmass save data.");
        if (landmass.SlopeByCellId is { } slopes &&
            (landmass.ElevationByCellId is null || slopes.Length != landmass.SurfaceByCellId.Length ||
             slopes.Where((value, id) => value > 100 || !landmass.IsLand(id) && value != 0).Any()))
            throw new InvalidDataException("Invalid terrain slope data.");
        if (landmass.Biomes is { } biomes)
        {
            if (string.IsNullOrWhiteSpace(biomes.ClassifierVersion) ||
                string.IsNullOrWhiteSpace(biomes.CatalogHash) ||
                biomes.Assignments.Length is < 1 or > 256 ||
                biomes.AssignmentByCellId.Length != landmass.SurfaceByCellId.Length ||
                biomes.AssignmentByCellId.Any(index => index >= biomes.Assignments.Length) ||
                biomes.Assignments.Any(entry => entry is null || !Enum.IsDefined(entry.Status) ||
                    (entry.Status == BiomeClassificationStatus.Assigned
                        ? string.IsNullOrWhiteSpace(entry.BiomeId) || string.IsNullOrWhiteSpace(entry.SubbiomeId)
                        : entry.BiomeId is not null || entry.SubbiomeId is not null)))
                throw new InvalidDataException("Invalid biome save data.");
        }
    }
}
