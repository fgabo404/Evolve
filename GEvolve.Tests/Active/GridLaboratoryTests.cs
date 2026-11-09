using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using OpenTK.Windowing.GraphicsLibraryFramework;

/// <summary>Pure code checks. No renderer instance, window, GL context or geographical data.</summary>
internal static class GridLaboratoryTests
{
    public static void Grid()
    {
        foreach (var (size, columns, rows) in new[] { (GridSize.Small, 200, 133), (GridSize.Medium, 250, 167), (GridSize.Large, 300, 200), (GridSize.Enormous, 400, 267) })
        {
            var grid = HexGrid.Generate(HexGridDefinitions.Get(size));
            Check(grid.Definition.Columns == columns && grid.Definition.Rows == rows && grid.Cells.Count == columns * rows, "Official dimensions");
            Check(grid.Cells.SequenceEqual(HexGrid.Generate(HexGridDefinitions.Get(size)).Cells), "Deterministic shared geometry");
            foreach (var cell in grid.Cells)
            {
                Check(cell.Id == cell.Address.Row * columns + cell.Address.Column && grid.TryGet(cell.Address, out var found) && found == cell, "Stable ID/address");
                Check(FlatHexGeometry.CellAt(cell.Center, columns, rows) == cell.Address, "Picking geometry");
                foreach (var neighbor in grid.Neighbors(cell))
                    Check(grid.Distance(cell, neighbor) == 1 && grid.Neighbors(neighbor).Any(n => n.Id == cell.Id), "Wrapped symmetric unit neighbors");
            }
            var edge = grid.Cells[(rows / 2) * columns];
            Check(grid.Neighbors(edge).Any(n => n.Address.Column == columns - 1), "E/O seam remains connected");
            Check(grid.Neighbors(grid.Cells[0]).All(n => n.Address.Row < rows - 1), "Open N/S bounds");
        }
    }
    public static void Laboratory()
    {
        var session = new GameSession(); session.OpenCreateWorld();
        Check(session.Screen == GameScreen.WorldUnavailable && session.Laboratory is null, "World creation has no generator or fallback");
        session.ReturnToMenu(); session.OpenLaboratory(); var lab = session.Laboratory!;
        Check(session.Screen == GameScreen.GridLaboratory && lab.Size == GridSize.Small && lab.Seed == 2026, "Fresh laboratory defaults");
        foreach (var size in Enum.GetValues<GridSize>())
        {
            lab.ChooseSize(size); var grid = lab.Grid;
            lab.SetSeedText("-2147483648"); Check(lab.Seed == int.MinValue && ReferenceEquals(grid, lab.Grid), "Seed metadata cannot alter grid geometry");
            lab.SetSeedText("2147483647"); Check(lab.Seed == int.MaxValue, "Positive seed boundary");
            foreach (string invalid in new[] { "2147483648", "-2147483649", "word", "1-2" })
            { string old = lab.SeedText; lab.SetSeedText(invalid); Check(lab.SeedText == old, "Invalid seeds rejected atomically"); }
            int seed = lab.Seed; lab.RandomizeSeed(); Check(lab.Seed != seed && lab.Error is null && ReferenceEquals(grid, lab.Grid), "Randomization changes metadata only");
            lab.SetSeedText("-"); Check(lab.Error is not null && ReferenceEquals(grid, lab.Grid), "Partial editing never creates content");
            lab.SetSeedText("2026");
            foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480) })
            {
                var map = GridLaboratoryLayout.Map(width, height);
                var buttons = GridLaboratoryLayout.Buttons(GameScreen.GridLaboratory, width, height);
                Check(buttons.Count == 7 && buttons.Count(b => GridLaboratoryLayout.IsSize(b.Action)) == 4, "Only seed/random, four sizes and menu exit");
                foreach (var b in buttons) Check(b.Bounds.X >= 0 && b.Bounds.Y >= 0 && b.Bounds.X + b.Bounds.Width <= map.X && b.Bounds.Y + b.Bounds.Height <= height, "Controls remain inside sidebar");
                Check(map.Width > width / 2 && map.Height == height, "Map is primary area");
                var camera = new MapCamera2D();
                camera.FitViewport(grid.Definition.Columns, grid.Definition.Rows, (int)map.X, 0, (int)map.Width, height);
                var point = new Vector2(map.X + map.Width / 2, height / 2f);
                var before = camera.ScreenToWorld(point); camera.Zoom(1, point);
                var delta = before - camera.ScreenToWorld(point);
                float period = FlatHexGeometry.PeriodWidth(grid.Definition.Columns); delta.X -= MathF.Round(delta.X / period) * period;
                Check(delta.Length() < .001f, "Zoom anchor with periodic camera");
                int revision = camera.Version; camera.Pan(40, 20); Check(camera.Version > revision, "Pan invalidates visible grid cache");
                Check(ReferenceEquals(grid, lab.Grid), "Navigation never changes data");
            }
        }
        session.ReturnToMenu(); Check(session.Laboratory is null, "Leaving releases session");
        session.OpenLaboratory(); Check(session.Laboratory!.Size == GridSize.Small && session.Laboratory.Seed == 2026, "Re-entry has fresh spatial session");
    }
    public static void TextEditing()
    {
        var lab = new GridLaboratory(); var other = new GridLaboratory(); var editor = new TextFieldEditor(); string clipboard = "-731";
        bool Key(Keys key, bool ctrl = false, bool shift = false) => editor.KeyDown(key, ctrl, shift, false, false, () => clipboard, s => clipboard = s);
        Check(!Key(Keys.V, true), "Global shortcuts untouched without focus");
        editor.Focus(lab, () => lab.SeedText, lab.SetSeedText); Key(Keys.A, true); Key(Keys.V, true);
        Check(lab.SeedText == "-731" && !editor.TextInput("v"), "Signed paste without duplicate text event");
        Key(Keys.A, true); Key(Keys.C, true); Check(clipboard == "-731", "Copy selection"); Key(Keys.X, true);
        Check(lab.SeedText == "", "Cut selection allows partial field"); Key(Keys.V, true); Check(lab.Seed == -731, "Paste after cut");
        Key(Keys.A, true); clipboard = "2147483648"; Key(Keys.V, true); Check(lab.SeedText == "-731", "Out of range paste preserves field");
        clipboard = "123"; Key(Keys.V, true); Key(Keys.Home); Key(Keys.Delete); Check(lab.SeedText == "23", "Home/Delete");
        Key(Keys.End); Key(Keys.Backspace); Check(lab.SeedText == "2", "End/Backspace");
        editor.Focus(other, () => other.SeedText, other.SetSeedText); Key(Keys.A, true); clipboard = "42"; Key(Keys.V, true);
        Check(lab.SeedText == "2" && other.Seed == 42, "Only focused model changes"); editor.Blur(); Check(!Key(Keys.A, true), "Blur releases shortcuts");
    }
    public static void Isolation()
    {
        // Reflection inspects metadata only; it does not initialize graphics resources.
        Check(typeof(HexGrid).Assembly.GetType("GEvolve.Engine.World.WorldGenerationService") is null, "Legacy orchestrator absent from Engine assembly");
        Check(typeof(HexGrid).Assembly.GetType("GEvolve.Engine.HexDemoWorld") is null, "Old synthetic terrain demo excluded");
        Check(typeof(HexGrid).Assembly.GetTypes().All(t =>
        {
            string? ns = t.Namespace;
            return ns is null || !ns.StartsWith("GEvolve.Engine.World", StringComparison.Ordinal) ||
                ns.StartsWith("GEvolve.Engine.World.WorldGeneration", StringComparison.Ordinal);
        }), "No old world algorithms in official Engine");
        Check(typeof(GameSession).Assembly.GetType("GEvolve.Game.PangaeaLaboratory") is null, "Archived laboratory absent from Game assembly");
        Check(typeof(GameSession).Assembly.GetTypes().All(t => !t.Name.Contains("Pangaea", StringComparison.Ordinal)), "No historical comparator or snapshots in Game");
        Check(!typeof(GameSession).Assembly.GetReferencedAssemblies().Any(a => a.Name == "GEvolve.Tests"), "No dependency on historical test code");
    }
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
