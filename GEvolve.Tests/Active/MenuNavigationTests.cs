using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Game;
using GEvolve.Game.Screens;
using OpenTK.Windowing.GraphicsLibraryFramework;

/// <summary>Actual session, form and shared hit regions; no GameWindow, renderer or GL.</summary>
internal static class MenuNavigationTests
{
    public static void Run()
    {
        foreach (var (width, height) in new[] { (640, 480), (800, 600), (1280, 720), (1920, 1080) }) Layout(width, height);
        var session = new GameSession();
        session.OpenCreateWorld(); var form = session.Creation ?? throw new InvalidOperationException("Creation form missing");
        Check(session.Screen == GameScreen.CreateWorld && session.Laboratory is null && !form.CanGenerate, "Create opens a form, not a world/laboratory");
        form.ChooseSize(GridSize.Enormous); form.ChooseType(LandmassType.Archipelagos); form.SetSeedText("-2147483648");
        Check(form.Size == GridSize.Enormous && form.Type == LandmassType.Archipelagos && form.SeedText == "-2147483648" && !form.CanGenerate, "Preferences never enable generation");
        foreach (string invalid in new[] { "2147483648", "-2147483649", "bad", "1-2" })
        { string before = form.SeedText; form.SetSeedText(invalid); Check(form.SeedText == before, "Creation seed int32 validation"); }
        var editor = new TextFieldEditor(); string clipboard = "42";
        editor.Focus(form, () => form.SeedText, form.SetSeedText);
        editor.KeyDown(Keys.A, true, false, false, false, () => clipboard, s => clipboard = s);
        editor.KeyDown(Keys.V, true, false, false, false, () => clipboard, s => clipboard = s);
        Check(form.SeedText == "42" && !editor.TextInput("v"), "Creation form uses existing editor without duplicated paste");
        string oldSeed = form.SeedText; form.RandomizeSeed();
        Check(form.SeedText != oldSeed && int.TryParse(form.SeedText, out _) && !form.CanGenerate, "Random seed remains metadata");
        session.OpenLaboratory(); session.OpenOptions(); session.RequestExit();
        Check(session.Screen == GameScreen.CreateWorld && ReferenceEquals(form, session.Creation) && session.Laboratory is null && !session.ExitRequested, "Menu-only destinations cannot interrupt creation");
        editor.Blur(); session.ReturnToMenu();
        Check(session.Screen == GameScreen.MainMenu && session.Creation is null && session.Laboratory is null, "Back releases form");
        session.OpenOptions(); Check(session.Screen == GameScreen.Options && session.Creation is null && session.Laboratory is null, "Options is independent");
        session.OpenCreateWorld(); session.OpenLaboratory(); session.RequestExit();
        Check(session.Screen == GameScreen.Options && !session.ExitRequested, "Options has no world/laboratory side effects");
        session.ReturnToMenu(); session.OpenLaboratory(); var grid = session.Laboratory!;
        Check(session.Screen == GameScreen.GridLaboratory && session.Creation is null && grid.Size == GridSize.Small && grid.Seed == 2026, "Laboratory entry still creates only default grid");
        grid.SetSeedText("-731"); session.OpenCreateWorld(); session.OpenOptions();
        Check(ReferenceEquals(grid, session.Laboratory) && grid.Seed == -731 && session.Creation is null, "Other menu requests never replace active grid");
        session.ReturnToMenu(); session.OpenCreateWorld();
        Check(session.Creation is { Size: GridSize.Small, Type: LandmassType.Pangaea, SeedText: "2026", CanGenerate: false } && session.Laboratory is null, "Re-entry has independent defaults");
        session.ReturnToMenu(); session.RequestExit(); Check(session.ExitRequested, "Exit request is available from menu without opening a window");
    }
    private static void Layout(int width, int height)
    {
        var menu = GridLaboratoryLayout.Buttons(GameScreen.MainMenu, width, height);
        Check(menu.Select(b => b.Action).SequenceEqual(new[] { GridLabAction.OpenCreateWorld, GridLabAction.OpenLaboratory, GridLabAction.OpenOptions, GridLabAction.Exit }), "Original four menu destinations in order");
        Check(menu.Select(b => b.Text).SequenceEqual(new[] { "Crear mundo", "Laboratorio", "Opciones", "Salir" }) && menu.All(b => b.Enabled), "Menu entries remain navigable");
        for (int i = 0; i < menu.Count; i++)
        {
            var r = menu[i].Bounds; Bounds(r, width, height);
            Check(Math.Abs(r.X + r.Width / 2 - width / 2f) < .01f && Hit(menu[i]) == menu[i].Action, "Centered original menu geometry and hit target");
            if (i > 0) Check(menu[i - 1].Bounds.Y + menu[i - 1].Bounds.Height < r.Y, "Menu targets do not overlap");
        }
        var create = GridLaboratoryLayout.Buttons(GameScreen.CreateWorld, width, height);
        Check(create.Select(b => b.Action).SequenceEqual(new[] { GridLabAction.CreateSizeSelect, GridLabAction.CreateTypeSelect, GridLabAction.CreateSeed, GridLabAction.CreateRandomSeed, GridLabAction.GenerateUnavailable, GridLabAction.Back }), "Original creation form controls retained");
        foreach (var b in create) Bounds(b.Bounds, width, height);
        var generate = create.Single(b => b.Action == GridLabAction.GenerateUnavailable);
        Check(!generate.Enabled && Hit(generate, GameScreen.CreateWorld) is null, "Disabled generation has no clickable route");
        foreach (var b in create.Where(b => b.Enabled)) Check(Hit(b, GameScreen.CreateWorld) == b.Action, "Creation controls reachable");
        foreach (var selector in new[] { GridLabAction.CreateSizeSelect, GridLabAction.CreateTypeSelect })
        {
            var options = GridLaboratoryLayout.Dropdown(GameScreen.CreateWorld, width, height, selector);
            Check(options.Count == (selector == GridLabAction.CreateSizeSelect ? 4 : 3), "Size/type metadata options");
            foreach (var b in options)
            {
                Bounds(b.Bounds, width, height);
                Check(Hit(b, GameScreen.CreateWorld, selector) == b.Action, "Dropdown has priority in shared hit testing");
                if (GridLaboratoryLayout.IsSize(b.Action)) _ = GridLaboratoryLayout.Size(b.Action);
                else Check(SelectableWorldTypes.IsSelectable(GridLaboratoryLayout.Type(b.Action)), "Only visible definition types, no generator");
            }
        }
        var back = GridLaboratoryLayout.Buttons(GameScreen.Options, width, height).Single();
        Check(back.Action == GridLabAction.Back && Hit(back, GameScreen.Options) == GridLabAction.Back, "Options returns to menu");
        Check(GridLaboratoryLayout.Dropdown(GameScreen.GridLaboratory, width, height, GridLabAction.CreateTypeSelect).Count == 0, "Creation selectors never become laboratory layers");
        GridLabAction? Hit(GridLabButton b, GameScreen screen = GameScreen.MainMenu, GridLabAction? selector = null)
            => GridLaboratoryLayout.Hit(screen, width, height, b.Bounds.X + b.Bounds.Width / 2, b.Bounds.Y + b.Bounds.Height / 2, selector);
    }
    private static void Bounds(GridLabRect r, int width, int height) => Check(r.X >= 0 && r.Y >= 0 && r.X + r.Width <= width + .01f && r.Y + r.Height <= height + .01f, "Controls remain inside window");
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
