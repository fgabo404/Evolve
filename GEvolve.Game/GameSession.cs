using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.State.Game;

namespace GEvolve.Game;

public enum GameScreen { MainMenu, CreateWorld, Options, World }

/// <summary>Flujo de la sesión; mantiene un solo grid lógico, independiente de OpenGL.</summary>
public sealed class GameSession
{
    public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
    public GridSize SelectedSize { get; private set; } = GridSize.Small;
    public HexGrid? Grid { get; private set; }
    public GridCell? SelectedCell { get; private set; }
    public GameState State { get; } = new();
    public string? Error { get; private set; }

    public void OpenCreateWorld()
    {
        if (Screen != GameScreen.MainMenu) return;
        SelectedSize = GridSize.Small;
        Error = null;
        Screen = GameScreen.CreateWorld;
    }

    public void OpenOptions()
    {
        if (Screen == GameScreen.MainMenu) Screen = GameScreen.Options;
    }

    public void ChooseSize(GridSize size)
    {
        if (Screen != GameScreen.CreateWorld) return;
        _ = HexGridDefinitions.Get(size);
        SelectedSize = size;
        Error = null;
    }

    public HexGrid GenerateWorld()
    {
        if (Screen != GameScreen.CreateWorld)
            throw new InvalidOperationException("Open Create World before generating.");
        var definition = HexGridDefinitions.Get(SelectedSize);
        var next = HexGrid.Generate(definition);
        Grid = next;
        State.HexGrid = new HexGridState(definition);
        SelectedCell = null;
        Error = null;
        Screen = GameScreen.World;
        return next;
    }

    public GridCell? SelectAt(Vector2 worldPoint)
    {
        if (Screen != GameScreen.World || Grid is null) return null;
        var address = FlatHexGeometry.CellAt(worldPoint,
            Grid.Definition.Columns, Grid.Definition.Rows);
        SelectedCell = address is { } location && Grid.TryGet(location, out var cell)
            ? cell : null;
        return SelectedCell;
    }

    public void ReturnToMenu()
    {
        Grid = null;
        State.HexGrid = null;
        SelectedCell = null;
        Error = null;
        Screen = GameScreen.MainMenu;
    }

    public void GenerationFailed(string message)
    {
        Grid = null;
        State.HexGrid = null;
        SelectedCell = null;
        Error = message;
        Screen = GameScreen.CreateWorld;
    }
}
