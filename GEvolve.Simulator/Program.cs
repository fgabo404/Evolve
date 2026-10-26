using GEvolve.Core;
using GEvolve.State.Game;
using GEvolve.State.Persistence;
using GEvolve.Engine.Engine;
using GEvolve.Simulator.Map;

var save = Arg("--save") ?? "saves/prototype.json";
var output = Arg("--map") ?? "output/strategic-map.html";
var loaded = File.Exists(save) && !args.Contains("--new", StringComparer.OrdinalIgnoreCase);
GameState state = loaded ? GameSaveStore.Load(save) : new GameState
{
    WorldSettings = new WorldSettings(Ancho: 40, Alto: 25, Semilla: 2026)
};
var engine = new SimulationEngine(state);
if (int.TryParse(Arg("--turns"), out var turns)) engine.AdvanceTurns(turns);
GameSaveStore.Save(save, state);
StrategicMap2D.Export(engine.World, output, state.Turn);
Console.WriteLine($"Turn {state.Turn} | Seed {state.WorldSettings.Semilla} | Save: {Path.GetFullPath(save)}");
Console.WriteLine($"2D map: {Path.GetFullPath(output)} (open in a browser)");
Console.WriteLine("Experimental map only: no resource/population/technology rules are active yet.");

string? Arg(string key)
{
    int index = Array.FindIndex(args, a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
