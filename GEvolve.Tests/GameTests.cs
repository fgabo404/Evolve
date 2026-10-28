using System.Diagnostics;
using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;

public static class GameTests
{
    public static void Run()
    {
        var session = new GameSession();
        Check(session.Screen == GameScreen.MainMenu && session.Grid is null &&
            session.State.HexGrid is null, "Start at menu without generation");
        Check(ScreenLayout.Buttons(session.Screen, 1280, 720).Select(button => button.Action)
            .SequenceEqual([UiAction.Create, UiAction.Options, UiAction.Exit]), "Main menu actions");
        session.OpenOptions();
        Check(session.Screen == GameScreen.Options && session.Grid is null, "Options screen");
        session.ReturnToMenu();
        session.OpenCreateWorld();
        Check(session.Screen == GameScreen.CreateWorld && session.SelectedSize == GridSize.Small,
            "Create screen defaults to Small");
        Check(session.SelectedType == LandmassType.Pangaea && session.SeedText == "2026" &&
            session.State.Landmass is null, "Default world type and visible seed");
        var createButtons = ScreenLayout.Buttons(session.Screen, 1280, 720);
        Check(createButtons.Where(button => button.Action is UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous)
            .Select(button => ScreenLayout.SizeFor(button.Action))
            .SequenceEqual([GridSize.Small, GridSize.Medium, GridSize.Large, GridSize.Enormous]), "Four official sizes");
        Check(createButtons.Count(button => button.Action == UiAction.Pangaea) == 1 &&
            !createButtons.Any(button => button.Action is UiAction.Continents or
                UiAction.SmallContinents or UiAction.Archipelagos),
            "Pangaea stage 02 is selectable while other map types remain deferred");
        Check(createButtons.Any(button => button.Action == UiAction.SeedField) &&
            createButtons.Any(button => button.Action == UiAction.RandomSeed),
            "Editable and random seed controls");
        session.SetSeedText("2147483648");
        Check(Throws<FormatException>(() => session.BeginGeneration()) &&
            session.Screen == GameScreen.CreateWorld && session.Error is not null,
            "Out-of-range seed is rejected without leaving the form");
        session.RandomizeSeed();
        Check(int.TryParse(session.SeedText, out _), "Random seed is a valid integer");
        session.SetSeedText("-731");
        session.ChooseType(LandmassType.Pangaea);
        var request = session.BeginGeneration();
        Check(session.Screen == GameScreen.Generating && request.Seed == -731 &&
            request.Type == LandmassType.Pangaea && session.Grid is null,
            "Generation begins without an implicit grid");
        session.CompleteGeneration(new WorldGenerationService().Generate(request));
        Check(session.Screen == GameScreen.World &&
            session.State.Landmass?.Configuration == request,
            "Background-style completion transfers Engine result into State");
        session.ReturnToMenu();
        session.OpenCreateWorld();

        foreach (var size in Enum.GetValues<GridSize>())
        {
            session.ChooseSize(size);
            var definition = HexGridDefinitions.Get(size);
            var timer = Stopwatch.StartNew();
            var grid = session.GenerateWorld();
            timer.Stop();
            Check(session.Screen == GameScreen.World && ReferenceEquals(grid, session.Grid),
                "One Engine grid serves the world screen");
            Check(session.State.HexGrid?.Definition == definition &&
                session.State.Landmass?.Configuration.Size == size &&
                grid.Cells.Count == definition.Columns * definition.Rows &&
                definition.Orientation == GridOrientation.FlatTop, "Effective state and cell count");

            var terrain = session.State.Landmass!;
            Check(!terrain.TectonicPreviewOnly &&
                terrain.PlateByCellId is { } plates &&
                plates.Length == grid.Cells.Count &&
                plates.Distinct().Count() == 16 &&
                terrain.SurfaceByCellId.Any(value => value == 1) &&
                terrain.SurfaceByCellId.Any(value => value == 0) &&
                terrain.ElevationByCellId is { } elevations &&
                elevations.Length == grid.Cells.Count &&
                terrain.Climate is null && terrain.Biomes is null,
                "Stage 02 generates land and elevation from universal tectonic plates");
            int landId = Array.IndexOf(terrain.SurfaceByCellId, (byte)1);
            int oceanId = Array.IndexOf(terrain.SurfaceByCellId, (byte)0);
            Check(session.SelectAt(grid.Cells[landId].Center)?.Id == landId &&
                session.SelectAt(grid.Cells[oceanId].Center)?.Id == oceanId,
                "Land and ocean remain individually selectable");

            var cell = grid.Cells[definition.Columns * (definition.Rows / 2) + 2];
            Check(session.SelectAt(cell.Center)?.Id == cell.Id, "Cell selection");
            Vector2 repeated = cell.Center + new Vector2(
                FlatHexGeometry.PeriodWidth(definition.Columns), 0);
            Check(session.SelectAt(repeated)?.Id == cell.Id,
                "Repeated visual cell preserves logical ID");

            var camera = new MapCamera2D();
            camera.Fit(definition.Columns, definition.Rows, 1280, 720);
            Vector2 screen = camera.WorldToScreen(cell.Center);
            Check(Vector2.Distance(camera.ScreenToWorld(screen), cell.Center) < .001f,
                "Camera screen/world conversion");
            float oldScale = camera.Scale;
            camera.Zoom(8, new Vector2(640, 360));
            Check(camera.Scale > oldScale && camera.VisibleRange().Count < grid.Cells.Count,
                "Zoom culls cells outside the viewport");
            float oldX = camera.X;
            camera.Pan(100, 0);
            Check(camera.X >= 0 && camera.X < FlatHexGeometry.PeriodWidth(definition.Columns) &&
                camera.X != oldX, "Horizontal drag wraps camera");
            camera.Pan(0, 100_000);
            Check(FlatHexGeometry.CellAt(new Vector2(cell.Center.X, -20),
                definition.Columns, definition.Rows) is null,
                "North remains open");
            Check(timer.ElapsedMilliseconds < (size == GridSize.Enormous ? 60_000 : 30_000),
                "World generation remains within a generous smoke-test budget");
            Console.WriteLine($"PASS: Game {size} ({grid.Cells.Count} cells, {timer.ElapsedMilliseconds} ms)");

            session.ReturnToMenu();
            Check(session.Screen == GameScreen.MainMenu && session.Grid is null &&
                session.State.HexGrid is null && session.State.Landmass is null &&
                session.SelectedCell is null,
                "Returning to menu releases logical world");
            session.OpenCreateWorld();
        }

        var resizing = new GameSession();
        resizing.OpenCreateWorld();
        resizing.ChooseType(LandmassType.Pangaea);
        resizing.SetSeedText("42");
        resizing.GenerateWorld();
        var selectedSeed = resizing.State.Landmass!.Configuration.Seed;
        var resizeConfig = resizing.BeginResize(GridSize.Medium);
        Check(resizeConfig.Size == GridSize.Medium &&
            resizeConfig.Type == LandmassType.Pangaea &&
            resizeConfig.Seed == selectedSeed &&
            resizing.Screen == GameScreen.Generating, "World view resize keeps seed and type");
        resizing.CompleteGeneration(new WorldGenerationService().Generate(resizeConfig));
        Check(resizing.Grid!.Cells.Count == HexGridDefinitions.Get(GridSize.Medium).Columns *
                HexGridDefinitions.Get(GridSize.Medium).Rows &&
            resizing.State.Landmass!.Configuration.Seed == selectedSeed,
            "Resized world replaces logical state");
        var worldButtons = ScreenLayout.Buttons(GameScreen.World, 1280, 720);
        Check(worldButtons.Count(button => button.Action is UiAction.Small or UiAction.Medium or UiAction.Large or UiAction.Enormous) == 4,
            "Four size controls are available in world view");
        Check(worldButtons.Any(button => button.Action == UiAction.ToggleTectonic) &&
            worldButtons.Any(button => button.Action == UiAction.ToggleSurface) &&
            worldButtons.Any(button => button.Action == UiAction.ToggleGrayscale),
            "Stage 01 plates, stage 02 land and elevation are independently viewable");

        foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480) })
        foreach (var screen in new[] { GameScreen.MainMenu, GameScreen.CreateWorld, GameScreen.Options, GameScreen.World })
        foreach (var button in ScreenLayout.Buttons(screen, width, height))
            Check(button.Bounds.X >= 0 && button.Bounds.Y >= 0 &&
                button.Bounds.X + button.Bounds.Width <= width &&
                button.Bounds.Y + button.Bounds.Height <= height,
                "Buttons fit the window");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}
