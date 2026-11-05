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
    public LandmassType SelectedType { get; private set; } = LandmassType.Pangaea;
    public string SeedText { get; private set; } = "2026";
    public HexGrid? Grid { get; private set; }
    public GridCell? SelectedCell { get; private set; }
    public GameState State { get; } = new();
    private LandmassState? _regionalSurface;
    public bool HasSurfaceComparison => _regionalSurface is not null;
    public bool IsRegionalSurface { get; private set; }
    public C1ExperimentResult? GeometryExperiment { get; private set; }
    public bool IsMultiscaleGeometry { get; private set; }
    public LandmassState? DisplayLandmass => IsMultiscaleGeometry ? GeometryExperiment!.Selected!.Terrain :
        IsRegionalSurface ? _regionalSurface : State.Landmass;

    public void AttachGeometryComparison(C1ExperimentResult experiment)
    {
        ArgumentNullException.ThrowIfNull(experiment);
        if (Screen != GameScreen.World || State.Landmass is not { HasSurface: true, SharedCortex: not null } official ||
            official.Configuration.Type != LandmassType.Continents)
            throw new InvalidOperationException("C1 requires the official A surface first.");
        if (experiment.Selected is { } selected &&
            (selected.Terrain.Configuration != official.Configuration || !selected.Terrain.HasSurface ||
             selected.Terrain.SurfaceByCellId.Length != official.SurfaceByCellId.Length || selected.Terrain.SharedCortex is null ||
             selected.Terrain.ElevationByCellId is not null || selected.Terrain.Climate is not null || selected.Terrain.Biomes is not null ||
             !ReferenceEquals(selected.Terrain.PlateByCellId, official.PlateByCellId) ||
             !ReferenceEquals(selected.Terrain.TectonicMotion, official.TectonicMotion)))
            throw new ArgumentException("C1 must share the same official tectonics and configuration.", nameof(experiment));
        GeometryExperiment = experiment; IsMultiscaleGeometry = false;
        _regionalSurface = null; IsRegionalSurface = false;
    }

    public LandmassState SelectGeometryVariant(bool multiscale)
    {
        if (Screen != GameScreen.World || GeometryExperiment is null || multiscale && GeometryExperiment.Selected is null)
            throw new InvalidOperationException("No safely inspectable C1 geometry is available.");
        IsMultiscaleGeometry = multiscale; IsRegionalSurface = false;
        return DisplayLandmass!;
    }

    /// <summary>Session-only B. State.Landmass always remains the official persistible A.</summary>
    public void AttachSurfaceComparison(ContinentsSurfaceComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (Screen != GameScreen.World || State.Landmass is not { HasSurface: true, SharedCortex: not null } terrain ||
            terrain.Configuration.Type != LandmassType.Continents ||
            !comparison.LocalizedSurface.SequenceEqual(terrain.SurfaceByCellId) ||
            comparison.RegionalSurface.Length != terrain.SurfaceByCellId.Length ||
            comparison.RegionalSurface.Any(value => value is not (0 or 1)))
            throw new ArgumentException("Comparison must describe the official Continents surface.", nameof(comparison));
        GeometryExperiment = null; IsMultiscaleGeometry = false;
        _regionalSurface = terrain with { SurfaceByCellId = comparison.RegionalSurface };
        IsRegionalSurface = false;
    }

    public LandmassState SelectSurfaceVariant(bool regional)
    {
        if (Screen != GameScreen.World || !HasSurfaceComparison)
            throw new InvalidOperationException("Generate a surface comparison before selecting a variant.");
        IsRegionalSurface = regional;
        IsMultiscaleGeometry = false;
        return DisplayLandmass!;
    }
    public string? Error { get; private set; }

    public void OpenCreateWorld()
    {
        if (Screen != GameScreen.MainMenu) return;
        SelectedSize = GridSize.Small;
        SelectedType = LandmassType.Pangaea;
        SeedText = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue)
            .ToString(CultureInfo.InvariantCulture);
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
        if (!SelectableWorldTypes.IsSelectable(type)) throw new ArgumentOutOfRangeException(nameof(type));
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

    /// <summary>
    /// Rebuild a world with a different size and/or landmass type while keeping
    /// its deterministic seed. The tectonic layout stays shared at equal size/seed.
    /// </summary>
    public LandmassConfiguration BeginReconfigure(GridSize size, LandmassType type)
    {
        if (Screen != GameScreen.World || State.Landmass is null)
            throw new InvalidOperationException("Generate a world before changing its configuration.");
        _ = HexGridDefinitions.Get(size);
        // A historical world may be resized with its original type; new choices must be selectable.
        if (!SelectableWorldTypes.IsSelectable(type) && type != State.Landmass.Configuration.Type)
            throw new ArgumentOutOfRangeException(nameof(type));
        int seed = State.Landmass.Configuration.Seed;
        SelectedSize = size;
        SelectedType = type;
        SeedText = seed.ToString(CultureInfo.InvariantCulture);
        Screen = GameScreen.CreateWorld;
        return BeginGeneration();
    }

    public LandmassConfiguration BeginResize(GridSize size)
    {
        if (Screen != GameScreen.World || State.Landmass is null)
            throw new InvalidOperationException("Generate a world before changing its size.");
        return BeginReconfigure(size, State.Landmass.Configuration.Type);
    }

    public LandmassConfiguration BeginChangeType(LandmassType type)
    {
        if (Screen != GameScreen.World || State.Landmass is null)
            throw new InvalidOperationException("Generate a world before changing its type.");
        if (!SelectableWorldTypes.IsSelectable(type)) throw new ArgumentOutOfRangeException(nameof(type));
        return BeginReconfigure(State.Landmass.Configuration.Size, type);
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
        GeometryExperiment = null; IsMultiscaleGeometry = false;
        _regionalSurface = null;
        IsRegionalSurface = false;
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
        GeometryExperiment = null; IsMultiscaleGeometry = false;
        _regionalSurface = null;
        IsRegionalSurface = false;
        Grid = null;
        State.HexGrid = null;
        State.Landmass = null;
        SelectedCell = null;
        Error = null;
        Screen = GameScreen.MainMenu;
    }

    public void GenerationFailed(string message)
    {
        GeometryExperiment = null; IsMultiscaleGeometry = false;
        _regionalSurface = null;
        IsRegionalSurface = false;
        Grid = null;
        State.HexGrid = null;
        State.Landmass = null;
        SelectedCell = null;
        Error = message;
        Screen = GameScreen.CreateWorld;
    }
}
