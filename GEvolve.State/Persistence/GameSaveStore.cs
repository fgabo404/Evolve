using System.Text.Json;
using GEvolve.State.Game;

namespace GEvolve.State.Persistence;

public static class GameSaveStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static void Save(string path, GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
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
        if (state.SaveFormatVersion != 1)
            throw new NotSupportedException($"Unsupported save format: {state.SaveFormatVersion}");
        return state;
    }
}
