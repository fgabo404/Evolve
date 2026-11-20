// Exercises production Laboratory models and presentation-coordinate logic without loading OpenTK or creating a window.
// Returns nonzero on any failure; native events, GPU rendering and resource disposal still require manual validation.
using Evolve.Game;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

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
    ("Laboratory checks remain graphics independent", Headless)
];
int failures = 0;
foreach (var (name, check) in checks)
{
    try { check(); Console.WriteLine($"PASS LAB-GRID-01 {name}"); }
    catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL LAB-GRID-01 {name}: {exception}"); }
}
Console.WriteLine($"LAB-GRID-01: {checks.Length - failures}/{checks.Length} checks passed.");
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
    foreach (var (width, height) in new[] { (400, 300), (800, 600), (1280, 720), (1920, 1080) })
    {
        var layout = new LaboratoryLayout(width, height);
        Require(Math.Abs(layout.Viewport.Width / layout.Viewport.Height - 2) < 1e-10 &&
            layout.Viewport.Y >= 180 && layout.Viewport.Y + layout.Viewport.Height <= height - 55, "Viewport stretches or overlaps controls");
        foreach (var action in Enum.GetValues<LaboratoryAction>())
        {
            var bounds = layout.Control(action);
            double x = bounds.X + bounds.Width / 2, y = bounds.Y + bounds.Height / 2;
            Require(layout.HitTest(x, y) == action && !layout.Viewport.Contains(x, y), "Layout/hit mismatch");
            var physical = LaboratoryLayout.ToFramebuffer(x / 2, y / 2, width / 2, height / 2, width, height);
            Require(layout.HitTest(physical.X, physical.Y) == action, "DPI mismatch");
        }
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
