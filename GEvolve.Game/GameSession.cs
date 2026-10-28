using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.State.Game;

namespace GEvolve.Game;

public enum GameScreen { MainMenu, CreateWorld, Options, Generating, World }

/// <summary>Una sesión lógica; generación y GPU se completan en el hilo de Game.</summary>
public sealed class GameSession
{
    public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
    public GridSize SelectedSize { get; private set; } = GridSize.Small;
    public LandmassType SelectedType { get; private set; } = LandmassType.Continents;
    public string SeedText { get; private set; } = "2026";
    public HexGrid? Grid { get; private set; }
    public GridCell? SelectedCell { get; private set; }
    public GameState State { get; } = new();
    public string? Error { get; private set; }

    public void OpenCreateWorld()
    {
        if (Screen != GameScreen.MainMenu) return;
        SelectedSize = GridSize.Small;
        SelectedType = LandmassType.Continents;
        SeedText = "2026";
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

    public void ChooseType(LandmassType type)
    {
        if (Screen != GameScreen.CreateWorld) return;
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        SelectedType = type;
        Error = null;
    }

    public void SetSeedText(string text)
    {
        if (Screen != GameScreen.CreateWorld) return;
        if (text.Length > 11 || text.Any((character) =>
            !char.IsAsciiDigit(character) && character != '-')) return;
        if (text.Count(character => character == '-') > 1 ||
            (text.Contains('-') && text[0] != '-')) return;
        SeedText = text;
        Error = null;
    }

    public void RandomizeSeed()
    {
        if (Screen != GameScreen.CreateWorld) return;
        SeedText = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue)
            .ToString(CultureInfo.InvariantCulture);
        Error = null;
    }

    /// <summary>Valida el formulario y cambia a una pantalla de progreso.</summary>
    public LandmassConfiguration BeginGeneration()
    {
        if (Screen != GameScreen.CreateWorld)
            throw new InvalidOperationException("Open Create World before generating.");
        if (!int.TryParse(SeedText, NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out int seed))
        {
            Error = "LA SEMILLA DEBE SER UN ENTERO DE 32 BITS";
            throw new FormatException(Error);
        }
        var configuration = LandmassProfiles.Create(SelectedSize, SelectedType, seed);
        Screen = GameScreen.Generating;
        Error = null;
        return configuration;
    }

    public LandmassConfiguration BeginRegeneration()
    {
        if (Screen != GameScreen.World || State.Landmass is null)
            throw new InvalidOperationException("Generate a world before regenerating.");
        SelectedSize = State.Landmass.Configuration.Size;
        SelectedType = State.Landmass.Configuration.Type;
        SeedText = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue)
            .ToString(CultureInfo.InvariantCulture);
        Screen = GameScreen.CreateWorld;
        return BeginGeneration();
    }

    /// <summary>Recrea el mismo tipo y semilla con otro tamaño desde la vista del mundo.</summary>
    public LandmassConfiguration BeginResize(GridSize size)
    {
        if (Screen != GameScreen.World || State.Landmass is null)
            throw new InvalidOperationException("Generate a world before changing its size.");
        _ = HexGridDefinitions.Get(size);
        SelectedSize = size;
        SelectedType = State.Landmass.Configuration.Type;
        SeedText = State.Landmass.Configuration.Seed.ToString(CultureInfo.InvariantCulture);
        Screen = GameScreen.CreateWorld;
        return BeginGeneration();
    }

    public void CompleteGeneration(GeneratedWorld result)
    {
        if (Screen != GameScreen.Generating)
            throw new InvalidOperationException("World generation was not requested.");
        if (result.Landmass.Configuration.Size != SelectedSize ||
            result.Landmass.Configuration.Type != SelectedType ||
            result.Grid.Definition.Size != SelectedSize)
            throw new ArgumentException("Generated world differs from the selected configuration.");
        Grid = result.Grid;
        State.HexGrid = new HexGridState(result.Grid.Definition);
        State.Landmass = result.Landmass;
        SelectedCell = null;
        Error = null;
        Screen = GameScreen.World;
    }

    /// <summary>Ruta síncrona para pruebas; la ventana usa Begin/Complete con Task.Run.</summary>
    public HexGrid GenerateWorld()
    {
        var configuration = BeginGeneration();
        try
        {
            CompleteGeneration(new WorldGenerationService().Generate(configuration));
            return Grid!;
        }
        catch
        {
            GenerationFailed("NO SE PUDO GENERAR EL MUNDO");
            throw;
        }
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
        State.Landmass = null;
        SelectedCell = null;
        Error = null;
        Screen = GameScreen.MainMenu;
    }

    public void GenerationFailed(string message)
    {
        Grid = null;
        State.HexGrid = null;
        State.Landmass = null;
        SelectedCell = null;
        Error = message;
        Screen = GameScreen.CreateWorld;
    }
}
