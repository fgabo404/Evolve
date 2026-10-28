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
            (landmass.PlateByCellId is { } plates && plates.Length != landmass.SurfaceByCellId.Length))
            throw new InvalidDataException("Invalid landmass save data.");
    }
}
