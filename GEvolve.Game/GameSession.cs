namespace GEvolve.Game;

public enum GameScreen { MainMenu, WorldUnavailable, GridLaboratory }

/// <summary>Navigation only. Creating a geographical world is unavailable until its new contract exists.</summary>
public sealed class GameSession
{
    public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
    public GridLaboratory? Laboratory { get; private set; }
    public const string WorldUnavailableMessage = "GENERACIÓN DE MUNDOS NO DISPONIBLE EN R02";

    public void OpenLaboratory()
    {
        if (Screen != GameScreen.MainMenu) return;
        Laboratory = new(); Screen = GameScreen.GridLaboratory;
    }
    public void OpenCreateWorld()
    {
        if (Screen == GameScreen.MainMenu) Screen = GameScreen.WorldUnavailable;
    }
    public void ReturnToMenu() { Laboratory = null; Screen = GameScreen.MainMenu; }
}
