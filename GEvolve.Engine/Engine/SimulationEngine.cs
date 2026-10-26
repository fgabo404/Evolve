using GEvolve.Core;
using GEvolve.State.Game;

namespace GEvolve.Engine.Engine;

/// <summary>Deterministic host for simulation phases. Domain rules are intentionally not fabricated.</summary>
public sealed class SimulationEngine
{
    public GameState State { get; }
    public WorldMap World { get; }

    public SimulationEngine(GameState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        World = WorldSystem.Generar(state.WorldSettings);
    }

    public void AdvanceTurn()
    {
        // Future systems execute in a fixed documented order here.
        checked { State.Turn++; }
    }

    public void AdvanceTurns(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        for (int i = 0; i < count; i++) AdvanceTurn();
    }
}
