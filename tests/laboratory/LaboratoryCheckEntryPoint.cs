// Exercises production Laboratory models and presentation-coordinate logic without loading OpenTK or creating a window.
// Returns nonzero on any failure; native events, GPU rendering and resource disposal still require manual validation.
using Evolve.Game;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;

(string Name, Action Check)[] checks =
[
    ("navigation stays within the existing session model", Navigation),
    ("enum selectors cycle only approved values", Configuration),
    ("numeric seeds use signed 64-bit invariant validation", Seeds),
    ("only real Grid runs for every configuration", Generation),
    ("metadata edits discard stale visible results", SessionReplacement),
    ("selection uses valid engine identities", Selection),
    ("responsive layout and high-DPI hit detection agree", Layout),
    ("camera transforms preserve scale, anchors and navigation", Camera),
    ("static outlines retain approved centers and regular shape", Outlines),
    ("picking centers, boundaries and seam IDs is consistent", Picking),
    ("Laboratory checks remain graphics independent", Headless),
    ("INICIAR stays unavailable without opening or generating", StartUnavailable),
    ("LAB creates a pending context and advances only manually", ManualStages),
    ("manual capabilities advance exactly once per explicit request", CapabilityDiscovery),
    ("failure projection and manual retry preserve partial Grid", ManualFailure),
    ("explicit seed randomization creates valid requests", Randomization),
    ("dropdown navigation and scrolling agree with hit detection", Dropdowns),
    ("completed views and inspection survive LAB reopening", SharedInspection),
    ("real Tectonics data, colors, geometry, arrows and inspection", TectonicInspection)
];
int failures = 0;
foreach (var (name, check) in checks)
{
    try { check(); Console.WriteLine($"PASS LAB-GRID-02-FIX01 {name}"); }
    catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL LAB-GRID-02-FIX01 {name}: {exception}"); }
}
Console.WriteLine($"LAB-GRID-02-FIX01: {checks.Length - failures}/{checks.Length} checks passed.");
return failures == 0 ? 0 : 1;

// These tests call actual production methods rather than a test-only navigation or generation pipeline.
static void Navigation()
{
    var session = new LaboratorySession();
    Require(!session.IsOpen, "Initial screen must be the menu");
    session.Open();
    Require(session.IsOpen, "LAB did not open");
    session.CreateSession();
    session.RunGrid();
    var context = session.Context;
    session.ReturnToMenu();
    Require(!session.IsOpen, "Back did not return to menu");
    session.Open();
    Require(ReferenceEquals(context, session.Context) && session.Grid is not null, "Reopening lost inspection state");
}

static void Configuration()
{
    var session = new LaboratorySession();
    foreach (var size in Enum.GetValues<MapSize>())
    {
        Require(session.Size == size, "Size enum order changed");
        session.CycleSize();
    }
    Require(session.Size == MapSize.Small, "Size did not wrap");
    foreach (var type in Enum.GetValues<WorldType>())
    {
        Require(session.WorldType == type, "World type enum order changed");
        session.CycleWorldType();
    }
    Require(session.WorldType == WorldType.Pangea, "Type did not wrap");
}

static void Seeds()
{
    var session = new LaboratorySession();
    foreach (string seed in new[] { "0", "-1", "+42", long.MinValue.ToString(), long.MaxValue.ToString() })
    {
        session.SetSeedText(seed);
        Require(session.CreateSession() && session.Context!.Request.Seed == long.Parse(seed), "Valid seed rejected");
    }
    foreach (string seed in new[] { "", "-", "1.5", "1e3", "abc", " 1", "9223372036854775808", "-9223372036854775809" })
    {
        session.SetSeedText(seed);
        Require(!session.CreateSession() && session.Context is null && session.Grid is null, "Invalid seed generated a context");
    }
}

static void Generation()
{
    var session = new LaboratorySession();
    Require(!session.RunGrid(), "Generation without a session succeeded");
    foreach (var size in Enum.GetValues<MapSize>())
    {
        foreach (var type in Enum.GetValues<WorldType>())
        {
            foreach (string seed in new[] { "-42", "17" })
            {
                session.SetSeedText(seed);
                Require(session.CreateSession() && session.GridStatus == GenerationStageStatus.Pending && session.Grid is null, "Create executed Grid");
                Require(session.Context!.Request.Settings.MapSize == size && session.Context.Request.Settings.WorldType == type, "Settings mismatch");
                Require(session.RunGrid() && session.GridStatus == GenerationStageStatus.Completed, "Grid did not complete");
                var data = session.Context.GetLayerData(GenerationStage.Grid) as LayerData<HexGridDescriptor>;
                Require(data?.Count == 1 && data[0].Size == size && session.Grid!.CellCount == data[0].ToGridData().CellCount, "Visible grid is not the published result");
                var published = session.Grid;
                Require(!session.CanRunGrid && !session.RunGrid() && ReferenceEquals(published, session.Grid), "Unavailable next stage executed");
                foreach (var stage in Enum.GetValues<GenerationStage>().Skip(1))
                    Require(session.Context.GetProgress().GetStatus(stage) == GenerationStageStatus.Pending &&
                        session.Context.GetLayerData(stage) is null, "Fake downstream output was generated");
            }
            session.CycleWorldType();
        }
        session.CycleSize();
    }
}

static void SessionReplacement()
{
    var session = new LaboratorySession();
    session.CreateSession(); session.RunGrid(); session.Select(0);
    var prior = session.Context!;
    session.CycleSize();
    Require(session.Context is null && session.Grid is null && session.SelectedCell is null, "Stale data survived metadata edits");
    session.CreateSession();
    Require(!ReferenceEquals(prior, session.Context) && prior.GetProgress().GetStatus(GenerationStage.Grid) == GenerationStageStatus.Completed, "Prior request/context mutated");
}

static void Selection()
{
    var session = new LaboratorySession();
    Expect<InvalidOperationException>(() => session.Select(0));
    session.CreateSession(); session.RunGrid(); session.Select(0);
    Require(session.SelectedCell == 0 && session.Grid!.GetNeighbors(0).Any(id => session.Grid.GetOffset(id).Column == session.Grid.Columns - 1), "Seam identity was lost");
    Expect<ArgumentOutOfRangeException>(() => session.Select(-1));
    Expect<ArgumentOutOfRangeException>(() => session.Select(session.Grid!.CellCount));
    Require(session.SelectedCell == 0, "Invalid selection changed state");
    session.Select(null);
    Require(session.SelectedCell is null, "Selection did not clear");
}

static void Layout()
{
    foreach (var (clientWidth, clientHeight) in new[] { (400, 300), (800, 600), (1280, 720), (1920, 1080) })
    foreach (double scale in new[] { 1d, 1.25d, 2d })
    {
        int width = (int)(clientWidth * scale), height = (int)(clientHeight * scale);
        var layout = new LaboratoryLayout(width, height, 0, scale);
        Require(layout.Viewport.X > layout.SidebarWidth && layout.Viewport.Width > 0 &&
            layout.Viewport.Y >= 0 && layout.Viewport.Y + layout.Viewport.Height <= height, "Map/sidebar overlap");
        foreach (var action in LaboratoryLayout.Controls)
        {
            var bounds = layout.Control(action);
            double x = bounds.X + bounds.Width / 2, y = bounds.Y + bounds.Height / 2;
            if (!layout.SidebarClip.Contains(x, y) && action != LaboratoryAction.Back) continue;
            Require(layout.HitTest(x, y) == new LaboratoryHit(action) && !layout.Viewport.Contains(x, y), "Control hit mismatch");
            var physical = LaboratoryLayout.ToFramebuffer(x / scale, y / scale, clientWidth, clientHeight, width, height);
            Require(layout.HitTest(physical.X, physical.Y) == new LaboratoryHit(action), "High-DPI hit mismatch");
        }
        foreach (var stage in Enum.GetValues<GenerationStage>())
        {
            var original = layout.Stage(stage);
            var scrolled = new LaboratoryLayout(width, height,
                original.Y - layout.SidebarClip.Y - layout.SidebarClip.Height / 2 + original.Height / 2, scale);
            var bounds = scrolled.Stage(stage);
            double x = bounds.X + bounds.Width / 2, y = bounds.Y + bounds.Height / 2;
            Require(scrolled.SidebarClip.Contains(x, y) &&
                scrolled.HitTest(x, y) == new LaboratoryHit(LaboratoryAction.Stage, (int)stage), "Stage unreachable after scroll");
        }
        var bottom = new LaboratoryLayout(width, height, double.MaxValue, scale);
        var info = bottom.Card(610, 106);
        Require(info.Y >= bottom.SidebarClip.Y &&
            info.Y + info.Height <= bottom.SidebarClip.Y + bottom.SidebarClip.Height, "Information card unreachable");
    }
    Require(LaboratoryLayout.ToFramebuffer(10, 10, 0, 0, 0, 0) == (0d, 0d), "Minimized client division by zero");
}

static void Camera()
{
    var camera = new GridCamera();
    camera.SetViewport(new LaboratoryLayout(1280, 720).Viewport);
    var bounds = GridVisualGeometry.Bounds(new HexGridData(MapSize.Large));
    camera.SetBounds(bounds);
    var topLeft = camera.ToScreen(bounds.X, bounds.Y);
    var bottomRight = camera.ToScreen(bounds.X + bounds.Width, bounds.Y + bounds.Height);
    Require(camera.Viewport.Contains(topLeft.X, topLeft.Y) && camera.Viewport.Contains(bottomRight.X, bottomRight.Y), "Initial fit cropped grid");
    var point = camera.ToScreen(42, 17);
    var world = camera.ToWorld(point.X, point.Y);
    Require(Math.Abs(world.X - 42) < 1e-9 && Math.Abs(world.Y - 17) < 1e-9, "Camera inverse failed");
    camera.Zoom(3, point.X, point.Y);
    world = camera.ToWorld(point.X, point.Y);
    Require(Math.Abs(world.X - 42) < 1e-9 && Math.Abs(world.Y - 17) < 1e-9, "Zoom anchor moved");
    var before = camera.ToScreen(42, 17);
    camera.Pan(21, -13);
    var after = camera.ToScreen(42, 17);
    Require(Math.Abs(after.X - before.X - 21) < 1e-9 && Math.Abs(after.Y - before.Y + 13) < 1e-9, "Pan transformation mismatch");
    double scale = camera.Scale, center = camera.CenterX;
    camera.SetViewport(new LaboratoryLayout(800, 600).Viewport);
    Require(camera.Scale == scale && camera.CenterX == center, "Resize discarded user navigation");
    camera.Fit();
    Require(camera.Scale > 0 && double.IsFinite(camera.Scale), "Fit after resize failed");
}

static void Outlines()
{
    foreach (var size in Enum.GetValues<MapSize>())
    {
        var grid = new HexGridData(size);
        var lines = GridVisualGeometry.BuildLines(grid);
        Require(lines.Length == grid.CellCount * 24 && lines.All(float.IsFinite), "Wrong cached line geometry size");
        foreach (int id in new[] { 0, grid.CellCount / 2, grid.CellCount - 1 })
        {
            var center = FlatHexGeometry.Center(grid.GetOffset(id));
            for (int edge = 0; edge < 6; edge++)
            {
                int index = id * 24 + edge * 4;
                double dx = lines[index] - center.X, dy = lines[index + 1] - center.Y;
                Require(Math.Abs(Math.Sqrt(dx * dx + dy * dy) - 1) < 0.0001, "Hex radius distorted");
                dx = lines[index + 2] - lines[index]; dy = lines[index + 3] - lines[index + 1];
                Require(Math.Abs(Math.Sqrt(dx * dx + dy * dy) - 1) < 0.0001, "Hex side distorted");
            }
        }
    }
}

static void Picking()
{
    foreach (var size in Enum.GetValues<MapSize>())
    {
        var grid = new HexGridData(size);
        foreach (int id in new[] { 0, 1, grid.Columns - 1, grid.Columns, grid.CellCount / 2, grid.CellCount - grid.Columns, grid.CellCount - 1 })
        {
            var center = FlatHexGeometry.Center(grid.GetOffset(id));
            Require(GridVisualGeometry.Pick(grid, center.X, center.Y) == id, "Wrong cell selected at engine center");
            Require(GridVisualGeometry.Pick(grid, center.X + 0.1, center.Y - 0.1) == id, "Wrong interior selection");
        }
        Require(GridVisualGeometry.Pick(grid, -5, -5) is null, "Outside the strip selected a cell");
        var camera = new GridCamera();
        camera.SetViewport(new LaboratoryLayout(800, 600).Viewport);
        camera.SetBounds(GridVisualGeometry.Bounds(grid));
        camera.Zoom(4, 500, 350); camera.Pan(20, 30);
        var origin = FlatHexGeometry.Center(grid.GetOffset(grid.CellCount - 1));
        var screen = camera.ToScreen(origin.X, origin.Y);
        var world = camera.ToWorld(screen.X, screen.Y);
        Require(GridVisualGeometry.Pick(grid, world.X, world.Y) == grid.CellCount - 1, "Transformed selection mismatch");
    }
}

static void Headless()
{
    Require(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name?.StartsWith("OpenTK", StringComparison.Ordinal) == true), "Headless tests loaded graphics");
}

static void Expect<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void StartUnavailable()
{
    var session = new LaboratorySession();
    var menu = new MainMenuInteraction();
    Require(!session.OpenFromMenu(MainMenuButton.Start) && !menu.Activate(MainMenuButton.Start) &&
        menu.FooterMessage is { } notice && notice.Contains("INICIAR") && notice.Contains("DISPONIBLE") &&
        !session.IsOpen && session.Context is null && session.Grid is null, "INICIAR opened or generated instead of showing its notice");
    Require(!session.OpenFromMenu(MainMenuButton.Options) && !session.OpenFromMenu(MainMenuButton.Exit) &&
        !session.OpenFromMenu(null), "Unrelated action opened LAB");
    Require(session.OpenFromMenu(MainMenuButton.Laboratory) && session.IsOpen, "LAB did not open");
    session.CreateSession(); session.RunNextStage();
    var previous = session.Context!;
    var data = previous.GetLayerData(GenerationStage.Grid);
    session.ReturnToMenu();
    Require(!session.OpenFromMenu(MainMenuButton.Start) && !menu.Activate(MainMenuButton.Start) &&
        !session.IsOpen && ReferenceEquals(previous, session.Context) &&
        ReferenceEquals(data, session.Context.GetLayerData(GenerationStage.Grid)) &&
        previous.GetProgress().NextStage == GenerationStage.Tectonics, "INICIAR modified the retained LAB session");
    Require(typeof(LaboratorySession).GetMethod("CreateWorld") is null &&
        typeof(LaboratorySession).GetProperty("Mode") is null, "Normal world-creation behavior remains exposed");
}

static void ManualStages()
{
    var session = new LaboratorySession();
    session.Open();
    foreach (var size in Enum.GetValues<MapSize>())
    foreach (var type in Enum.GetValues<WorldType>())
    {
        session.SetSize(size); session.SetWorldType(type); session.SetSeedText("-12345");
        Require(session.CreateSession() && session.Grid is null && session.GridStatus == GenerationStageStatus.Pending &&
            session.Context!.GetLayerData(GenerationStage.Grid) is null, "Creating a session generated Grid automatically");
        Require(session.RunNextStage() && session.Grid is not null &&
            session.GridStatus == GenerationStageStatus.Completed, "Manual Grid failed");
        Require(session.Context!.GetProgress().NextStage == GenerationStage.Tectonics && session.CanAdvance &&
            session.Tectonics is null, "Tectonics executed without a second request");
        Require(session.RunNextStage() && session.Tectonics is not null && !session.CanAdvance && !session.RunNextStage(),
            "Real Tectonics failed or flow advanced past capability");
        Require(session.StageStatus(GenerationStage.Grid) == "COMPLETADA", "Completed projection");
        foreach (var stage in Enum.GetValues<GenerationStage>().Skip(2))
            Require(!session.IsImplemented(stage) && session.StageStatus(stage) == "NO DISPONIBLE" &&
                session.Context.GetProgress().GetStatus(stage) == GenerationStageStatus.Pending &&
                session.Context.GetLayerData(stage) is null, "Unavailable stage reported fake completion");
        session.ClearSession();
        Require(session.Grid is null && session.Context is null && session.Size == size && session.WorldType == type &&
            session.SeedText == "-12345", "Returning to configuration discarded selected settings");
    }
}

static void CapabilityDiscovery()
{
    int calls = 0;
    var session = new LaboratorySession([new HexGridLayer(), new TestLayer(GenerationStage.Tectonics, inputs =>
    {
        calls++;
        Require(inputs.GetLayerData(GenerationStage.Grid) is LayerData<HexGridDescriptor>, "Discovered layer skipped Grid");
        return new TectonicPlatesLayer().Execute(inputs);
    })]);
    Require(session.CreateSession() && calls == 0 && session.Grid is null, "Session creation executed a capability");
    Require(session.RunNextStage() && calls == 0 && session.Context!.GetProgress().LastCompletedStage == GenerationStage.Grid,
        "One manual request ran more than Grid");
    Require(session.RunNextStage() && calls == 1 &&
        session.Context!.GetProgress().LastCompletedStage == GenerationStage.Tectonics &&
        session.Context.GetProgress().NextStage == GenerationStage.Landmasses, "Manual capability request failed");
    Require(session.Grid is not null && !session.CanAdvance && session.StageStatus(GenerationStage.Landmasses) == "NO DISPONIBLE", "Discovery fabricated later data");
}

static void ManualFailure()
{
    foreach (bool invalidCandidate in new[] { false, true })
    {
        bool fail = true;
        var session = new LaboratorySession([new HexGridLayer(), new TestLayer(GenerationStage.Tectonics, inputs =>
            fail ? (invalidCandidate
                ? LayerExecutionResult.Success(new LayerData<int>(GenerationStage.Climate, [999]))
                : LayerExecutionResult.Failed("Injected failure."))
                : new TectonicPlatesLayer().Execute(inputs))]);
        Require(session.CreateSession() && session.RunNextStage() && !session.RunNextStage() && session.Grid is not null &&
            session.StageStatus(GenerationStage.Tectonics) == "FALLIDA", "Failure not displayed from progress");
        var gridData = session.Context!.GetLayerData(GenerationStage.Grid);
        Require(session.Context.GetLayerData(GenerationStage.Tectonics) is null &&
            session.Context.GetProgress().GetStatus(GenerationStage.Landmasses) == GenerationStageStatus.Pending, "Partial candidate escaped");
        fail = false;
        Require(session.RunNextStage() && session.StageStatus(GenerationStage.Tectonics) == "COMPLETADA" &&
            ReferenceEquals(gridData, session.Context.GetLayerData(GenerationStage.Grid)), "Retry changed earlier Grid");
    }
}

static void Randomization()
{
    var session = new LaboratorySession();
    session.CreateSession(); session.RunNextStage();
    for (int index = 0; index < 20; index++)
    {
        session.RandomizeSeed();
        Require(long.TryParse(session.SeedText, out long seed) && session.Context is null && session.Grid is null &&
            session.CreateSession() && session.Context!.Request.Seed == seed, "Randomization bypassed validation or retained old world");
    }
    Expect<ArgumentOutOfRangeException>(() => session.SetSize((MapSize)99));
    Expect<ArgumentOutOfRangeException>(() => session.SetWorldType((WorldType)99));
    Expect<ArgumentNullException>(() => session.SetSeedText(null!));
}

static void Dropdowns()
{
    var ui = new LaboratoryUiState();
    foreach (var action in new[] { LaboratoryAction.Size, LaboratoryAction.WorldType, LaboratoryAction.View })
    {
        ui.Toggle(action);
        Require(ui.Dropdown == action && ui.OptionCount > 0, "Dropdown did not open");
        ui.MoveOption(-1);
        Require(ui.HighlightedOption == ui.OptionCount - 1, "Dropdown navigation did not wrap");
        ui.MoveOption(1);
        Require(ui.HighlightedOption == 0, "Dropdown forward wrap");
        var layout = new LaboratoryLayout(400, 300);
        for (int index = 0; index < ui.OptionCount; index++)
        {
            var option = layout.DropdownOption(action, index, ui.OptionCount);
            double x = option.X + option.Width / 2, y = option.Y + option.Height / 2;
            Require(layout.SidebarClip.Contains(x, y) && ui.HitTest(layout, x, y) ==
                new LaboratoryHit(LaboratoryAction.Option, index), "Popup clipped or inconsistent hit target");
            ui.HoverOption(index);
            Require(ui.HighlightedOption == index, "Popup hover mismatch");
        }
        ui.CloseTransient();
    }
    var original = new LaboratoryLayout(400, 300);
    ui.Toggle(LaboratoryAction.WorldType);
    ui.ScrollBy(99999, original);
    Require(ui.Scroll == original.MaximumScroll && ui.Dropdown is null, "Scroll not clamped or popup stale");
    ui.Reset();
    Require(ui.Scroll == 0 && !ui.EditingSeed && ui.Dropdown is null, "Navigation reset retained stale editing state");
}

static void SharedInspection()
{
    var session = new LaboratorySession();
    session.OpenFromMenu(MainMenuButton.Laboratory);
    Require(!session.SelectView(GenerationStage.Grid), "Pending data displayed as generated");
    session.CreateSession(); session.RunNextStage();
    Require(session.SelectView(GenerationStage.Grid) && !session.SelectView(GenerationStage.Climate), "Unavailable view displayed");
    session.Select(19999);
    var data = session.Context!.GetLayerData(GenerationStage.Grid);
    var grid = session.Grid;
    session.ReturnToMenu();
    session.OpenFromMenu(MainMenuButton.Laboratory);
    Require(session.SelectedView == GenerationStage.Grid && session.Grid!.GetOffset(session.SelectedCell!.Value) ==
        new HexOffset(199, 99), "Reopening LAB lost inspection");
    session.SelectView(GenerationStage.Grid);
    Require(ReferenceEquals(grid, session.Grid) && ReferenceEquals(data, session.Context.GetLayerData(GenerationStage.Grid)),
        "Inspection regenerated a layer");
}

// Inspect actual packed M results and the mesh used by the native renderer without a GPU.
static void TectonicInspection()
{
    var session = new LaboratorySession();
    foreach (var size in Enum.GetValues<MapSize>())
    foreach (string seed in new[] { "0", "-1" })
    {
        session.SetSize(size); session.SetSeedText(seed); session.CreateSession();
        Require(session.RunNextStage() && session.Tectonics is null && !session.SelectView(GenerationStage.Tectonics), "Premature Tectonics view.");
        var grid = session.Grid!;
        var camera = new GridCamera();
        camera.SetViewport(new LaboratoryLayout(1280, 720).Viewport); camera.SetBounds(GridVisualGeometry.Bounds(grid));
        camera.Pan(12, 15); camera.Zoom(2, 600, 300);
        var transform = camera.ToScreen(100, 50);
        Require(session.RunNextStage() && session.SelectedView == GenerationStage.Tectonics && session.Tectonics is not null, "Manual real Tectonics failed.");
        var data = session.Tectonics!;
        var published = (LayerData<uint>)session.Context!.GetLayerData(GenerationStage.Tectonics)!;
        var words = published.ToArray();
        Require(published[2] == 7 && data.HasMotion && data.HasBoundaries && !data.HasInfluence && !session.CanAdvance, "Wrong visible capabilities.");
        var mesh = TectonicVisualGeometry.Build(grid, data);
        Require(mesh.Positions.Length == grid.CellCount * 36 && mesh.Counts.Sum() == grid.CellCount * 18 &&
            mesh.Arrows.Length == 14 * 12, "Incomplete visible regions/arrows.");
        int seams = Enumerable.Range(0, data.BoundaryCount).Select(data.GetBoundary).Count(edge =>
            Math.Abs(grid.GetOffset(edge.FirstCellId).Column - grid.GetOffset(edge.SecondCellId).Column) > 1);
        Require(mesh.Boundaries.Length == 4 * (data.BoundaryCount + seams), "Seam interfaces not shown on both cuts.");
        Require(Enumerable.Range(0, 14).Select(id => TectonicVisualGeometry.Color((byte)id)).Distinct().Count() == 14, "Indistinguishable palette entries.");
        int[] cursor = mesh.Starts.Select(value => value * 2).ToArray();
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            byte plate = data.GetPlateId(cell);
            var center = FlatHexGeometry.Center(grid.GetOffset(cell));
            for (int corner = 0; corner < 6; corner++)
            {
                int start = cursor[plate];
                Require(mesh.Positions[start] == center.X && mesh.Positions[start + 1] == center.Y, "Fill belongs to a different cell/plate.");
                var expected = center + GridVisualGeometry.Corner(corner);
                Require(mesh.Positions[start + 2] == expected.X && mesh.Positions[start + 3] == expected.Y, "Distorted fill corner.");
                cursor[plate] += 6;
            }
        }
        for (byte plate = 0; plate < 14; plate++)
        {
            int anchor = TectonicVisualGeometry.Anchor(data, plate);
            Require(data.GetPlateId(anchor) == plate && mesh.Counts[plate] == data.GetPlate(plate).CellCount * 18, "Arrow anchor/area mismatch.");
            var start = FlatHexGeometry.Center(grid.GetOffset(anchor));
            int arrow = plate * 12;
            Require(mesh.Arrows[arrow] == start.X && mesh.Arrows[arrow + 1] == start.Y, "Unstable motion anchor.");
            var velocity = data.GetMotion(plate).Velocity;
            double dx = mesh.Arrows[arrow + 2] - start.X, dy = mesh.Arrows[arrow + 3] - start.Y;
            Require(dx * velocity.X + dy * velocity.Y > 0 && Math.Abs(dx * velocity.Y - dy * velocity.X) < .002, "Incorrect arrow direction.");
            session.Select(anchor);
            Require(session.DescribeSelection().Contains($"PLATEID: {plate}") && session.DescribeSelection().Contains("CM/ANO"), "Missing actual selected plate information.");
        }
        session.ToggleBoundaries(); session.ToggleMotion();
        Require(!session.ShowBoundaries && !session.ShowMotion, "Overlay toggles failed.");
        session.ToggleBoundaries(); session.ToggleMotion();
        session.ReturnToMenu(); session.OpenFromMenu(MainMenuButton.Laboratory);
        Require(ReferenceEquals(session.Tectonics, data) && ReferenceEquals(session.Grid, grid) && published.SequenceEqual(words), "Inspection/reopening regenerated or mutated data.");
        session.SelectView(GenerationStage.Grid); session.SelectView(GenerationStage.Tectonics);
        Require(camera.ToScreen(100, 50) == transform && ReferenceEquals(session.Grid, grid), "View switch requires a different Grid/camera.");
        var layout = new LaboratoryLayout(800, 500, double.MaxValue, 2, true);
        Require(layout.InformationHeight == 166 && layout.Card(610, layout.InformationHeight).Y + 166 * layout.Scale <=
            layout.SidebarClip.Y + layout.SidebarClip.Height, "Plate information cannot be reached by scrolling.");
        session.SetSeedText("1");
        Require(session.Tectonics is null && session.Grid is null, "Changed request retained stale Tectonics.");
    }
}

// Test-only execution proves capability discovery and failures without adding downstream production geography.
internal sealed class TestLayer(GenerationStage stage, Func<GenerationLayerInputs, LayerExecutionResult> execute) : IWorldGenerationLayer
{
    public GenerationStage Stage => stage;
    public LayerExecutionResult Execute(GenerationLayerInputs inputs) => execute(inputs);
}
