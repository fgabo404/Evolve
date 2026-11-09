namespace GEvolve.Game;

public enum GameScreen { MainMenu, CreateWorld, GridLaboratory, Options }

/// <summary>Navigation only. Creating a geographical world is unavailable until its new contract exists.</summary>
public sealed class GameSession
{
    public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
    public GridLaboratory? Laboratory { get; private set; }
    public WorldCreationForm? Creation { get; private set; }
    public bool ExitRequested { get; private set; }
    public const string WorldUnavailableMessage = "GENERACIÓN DE MUNDOS NO DISPONIBLE EN R02";

    public void OpenLaboratory()
    {
        if (Screen != GameScreen.MainMenu) return;
        Laboratory = new(); Screen = GameScreen.GridLaboratory;
    }
    public void OpenCreateWorld()
    {
        if (Screen != GameScreen.MainMenu) return;
        Creation = new(); Screen = GameScreen.CreateWorld;
    }
    public void OpenOptions() { if (Screen == GameScreen.MainMenu) Screen = GameScreen.Options; }
    public void RequestExit() { if (Screen == GameScreen.MainMenu) ExitRequested = true; }
    public void ReturnToMenu() { Laboratory = null; Creation = null; Screen = GameScreen.MainMenu; }
}
