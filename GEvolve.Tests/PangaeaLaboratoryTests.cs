using System.Diagnostics;
using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;

/// <summary>Code contracts only: never creates a window, GL context or screenshot.</summary>
public static class PangaeaLaboratoryTests
{
    private static readonly PangaeaGenerationStage[] Actions = [PangaeaGenerationStage.Plates, PangaeaGenerationStage.Surface,
        PangaeaGenerationStage.Base, PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final,
        PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes];
    public static void Run()
    {
        var menu = new GameSession(); menu.OpenPangaeaLaboratory();
        Check(menu.Screen == GameScreen.PangaeaLaboratory && menu.State.Landmass is null, "Laboratory remains separate from persistible automatic state");
        menu.ReturnToMenu(); menu.OpenCreateWorld(); Check(menu.Screen == GameScreen.CreateWorld, "Automatic creation remains accessible");
        foreach (var size in Enum.GetValues<GridSize>()) Exercise(size);
        ResetAndCancellation(); RandomSeeds();
        Console.WriteLine("LAB_PASS: four sizes; fixed sidebar/map viewport; five geological actions preserved, three ecological controls reachable; immediate seed/size/random reset; always-integrated numeric heights; surface >90; original geological equivalence. NO window/GL/visual approval.");
    }
    private static void Exercise(GridSize size)
    {
        using var lab = new PangaeaLaboratory(); lab.ChooseSize(size); lab.SetSeedText("-1387338765"); lab.RequestRestart();
        Check(lab.Busy && lab.Map is null && !Actions.Any(lab.CanExecute), "Automatic grid gates all visible actions until ready");
        Wait(lab); ExpectAvailable(lab, 0);
        var grid = lab.Map!.Snapshot.Grid!;
        Check(lab.Map.Snapshot.Plates is null && lab.Map.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null, "No fictitious grid attributes");
        Check(lab.SelectAt(grid.Cells[0].Center)?.Id == 0 && lab.Inspect().Contains("BASE: PENDIENTE"), "Grid-only selection distinguishes pending");
        Check(PangaeaHeightLabels.Build(lab.Map, MapViewMode.Grid, PangaeaHeightLabelTests.Camera(lab.Map)).Count == 0, "No invented labels before elevation");
        var cache = lab.Map; long revision = lab.PresentationRevision; int copies = lab.SnapshotCopies;
        for (int i = 0; i < 100; i++) Check(!lab.Update(), "Idle frame never requests snapshot");
        Check(ReferenceEquals(cache, lab.Map) && revision == lab.PresentationRevision && copies == lab.SnapshotCopies, "Stable revision cache");
        Reject(lab, PangaeaGenerationStage.Surface); Reject(lab, PangaeaGenerationStage.Final);
        Group(lab); ExpectAvailable(lab, 1);
        Check(lab.Map!.Snapshot.Plates!.Distinct().Count() == 16 && lab.Map.Snapshot.Motion is not null && lab.Map.Snapshot.Surface is null, "Combined official plates/H04 without fabricated terrain");
        Run(lab, PangaeaGenerationStage.Surface, MapViewMode.Surface); ExpectAvailable(lab, 2);
        Check(lab.Stages.Single(s => s.Stage == PangaeaGenerationStage.MountainSystems).CanExecute &&
            !lab.CanExecute(PangaeaGenerationStage.MountainSystems), "Engine retains its DAG; UI requires BASE before systems");
        Reject(lab, PangaeaGenerationStage.MountainSystems); Reject(lab, PangaeaGenerationStage.Final);
        int ocean = Array.IndexOf(lab.Map!.Snapshot.Surface!, (byte)0);
        lab.SelectAt(grid.Cells[ocean].Center);
        Check(lab.Inspect().Contains("SUPERFICIE: OCÉANO") && lab.Inspect().Contains("BASE: PENDIENTE"), "Surface does not invent zero elevation");
        Run(lab, PangaeaGenerationStage.Base, MapViewMode.ElevationBase); ExpectAvailable(lab, 3);
        Reject(lab, PangaeaGenerationStage.Final);
        Run(lab, PangaeaGenerationStage.MountainSystems, MapViewMode.Geology); ExpectAvailable(lab, 4);
        Check(lab.Map!.Snapshot.Elevations is null && lab.Inspect().Contains("APORTE: PENDIENTE"), "Geometry does not invent vertical uplift");
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        var final = lab.Map!.Snapshot.Elevations!.FinalByCellId.ToArray();
        var contribution = lab.Map.Snapshot.Elevations.ContributionByCellId.ToArray();
        foreach (var view in lab.Map.AvailableViews) Check(lab.ChooseView(view), "Available layer selection");
        Check(!lab.ChooseView(MapViewMode.Biomes), "Unavailable layer rejected");
        lab.SelectAt(grid.Cells[ocean].Center);
        Check(lab.Inspect().Contains("BASE: 0") && lab.Inspect().Contains("APORTE: 0") && lab.Inspect().Contains("FINAL: 0"), "Actual zero differs from pending");
        HeightDisplay(lab.Map); LayoutAndCamera(lab.Map);
        var inspector = lab.Inspect().ToArray();
        var inspectionCamera = PangaeaHeightLabelTests.Camera(lab.Map, close: false);
        Check(PangaeaHeightLabels.Build(lab.Map, lab.View, inspectionCamera).Count == 0 && lab.Inspect().SequenceEqual(inspector),
            "Inspector preserves exact values even when labels are hidden at far zoom");
        inspectionCamera.Zoom(100, new(inspectionCamera.ViewportX + inspectionCamera.ViewWidth / 2f, inspectionCamera.ViewHeight / 2f));
        lab.SelectAt(inspectionCamera.ScreenToWorld(inspectionCamera.WorldToScreen(grid.Cells[ocean].Center)));
        Check(lab.Inspect().SequenceEqual(inspector), "Inspector selection and fields remain unchanged through camera zoom");
        lab.Execute(PangaeaGenerationStage.Motion);
        var retainedBase = lab.Map!.Snapshot.Base ?? throw new InvalidOperationException("H04 lost independent BASE");
        Check(lab.Map.Snapshot.Elevations is null && !lab.CanExecute(PangaeaGenerationStage.Final), "Invalidate FINAL immediately and block execution");
        for (int id = 0; id < grid.Cells.Count; id++)
            Check(lab.Map.Height(id, MapViewMode.Surface) == retainedBase[id], "Immediate surface fallback to current BASE");
        Wait(lab); Run(lab, PangaeaGenerationStage.MountainSystems, MapViewMode.Geology);
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        long systemsRevision = Stage(lab, PangaeaGenerationStage.MountainSystems).Revision;
        string? systemsHash = Stage(lab, PangaeaGenerationStage.MountainSystems).Hash;
        lab.Execute(PangaeaGenerationStage.Base);
        Check(lab.Busy && lab.Map!.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            lab.Inspect().Contains("FINAL: INVALIDADO") && !Actions.Any(lab.CanExecute), "Repeating BASE hides invalidated heights and blocks incompatible actions");
        Check(lab.StageState(Stage(lab, PangaeaGenerationStage.Base)) == "EJECUTÁNDOSE", "Real running state");
        Wait(lab);
        Check(Stage(lab, PangaeaGenerationStage.MountainSystems).Revision == systemsRevision &&
            Stage(lab, PangaeaGenerationStage.MountainSystems).Hash == systemsHash, "UI sequence never rewrites engine independence or geometry");
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        Check(final.SequenceEqual(lab.Map!.Snapshot.Elevations!.FinalByCellId.ToArray()) &&
            contribution.SequenceEqual(lab.Map.Snapshot.Elevations.ContributionByCellId.ToArray()), "Exact repeatability");
        var automatic = new WorldGenerationService().Generate(size, LandmassType.Pangaea, -1387338765);
        Check(final.SequenceEqual(automatic.PangaeaElevations!.FinalByCellId.ToArray()) &&
            lab.Map.Snapshot.Base!.SequenceEqual(automatic.PangaeaElevations.BaseByCellId.ToArray()) &&
            lab.Map.Snapshot.Surface!.SequenceEqual(automatic.Landmass.SurfaceByCellId), "UI sequence equals unchanged automatic geography");
        long session = lab.SessionRevision; string seed = lab.SeedText;
        lab.RandomizeSeed();
        Check(lab.SeedText != seed && lab.SessionRevision > session && lab.Busy && lab.Map is null && lab.SelectedCell is null &&
            lab.Stages.Skip(1).All(s => s.Status == PangaeaStageStatus.Pending), "Random seed immediately destroys complete session and starts only grid");
        Wait(lab); ExpectAvailable(lab, 0);
        Check(lab.Map!.Snapshot.Configuration.Seed == int.Parse(lab.SeedText) && lab.Map.Snapshot.Motion is null &&
            lab.Map.Snapshot.Surface is null && lab.Map.Snapshot.Mountains is null && lab.Map.Snapshot.Elevations is null, "No old layer under randomized configuration");
        lab.SetSeedText("42");
        Check(lab.Map is null && lab.Stages.Count == 0 && !lab.Busy && lab.SelectedCell is null, "Manual edit immediately clears all prior data before starting new session");
        lab.RequestRestart(); Wait(lab); ExpectAvailable(lab, 0);
        lab.ChooseSize(size == GridSize.Small ? GridSize.Medium : GridSize.Small);
        Check(lab.Map is null && lab.Busy && lab.SelectedCell is null, "Size changes reset chain automatically");
        Wait(lab); ExpectAvailable(lab, 0);
        Console.WriteLine($"LAB_CASE_PASS {size}: sequential UI/unchanged engine DAG; integrated heights and >90 surface; sidebar/viewport/hit testing; cache; random/manual/size reset; exact automatic equivalence");
    }
    private static void ExpectAvailable(PangaeaLaboratory lab, int next)
    {
        for (int i = 0; i < Actions.Length; i++)
            Check(lab.CanExecute(Actions[i]) == (i <= next), "Only successful predecessors and next action can execute");
        if (next + 1 < Actions.Length)
            Check(lab.StageState(Stage(lab, Actions[next + 1])) == "BLOQUEADA", "Pending later actions are visibly blocked");
    }
    private static void Reject(PangaeaLaboratory lab, PangaeaGenerationStage stage)
    {
        var map = lab.Map; long revision = lab.PresentationRevision;
        lab.Execute(stage);
        Check(lab.Error is not null && !lab.Busy && ReferenceEquals(map, lab.Map) && revision == lab.PresentationRevision,
            "A blocked action cannot generate or mutate current layer data");
    }
    private static void ResetAndCancellation()
    {
        using var lab = new PangaeaLaboratory(); lab.SetSeedText("2147483648"); lab.RequestRestart();
        Check(lab.Error is not null && lab.Stages.Count == 0, "Invalid int32 seed reports error");
        lab.SetSeedText("2026"); lab.RequestRestart(); Wait(lab);
        lab.ExecutePlatesAndMotion(); lab.RandomizeSeed();
        Wait(lab); ExpectAvailable(lab, 0);
        Check(lab.Map!.Snapshot.Motion is null && lab.Map.Snapshot.Plates is null, "Reset during grouped worker clears queued H04 and discards obsolete result");
        lab.ChooseSize(GridSize.Enormous); lab.ChooseSize(GridSize.Small); Wait(lab);
        Check(lab.Map!.Snapshot.Grid!.Definition.Size == GridSize.Small, "Old size worker never replaces latest viewport data");
        lab.ExecutePlatesAndMotion(); lab.Cancel(); Wait(lab);
        Check(Stage(lab, PangaeaGenerationStage.Motion).Status != PangaeaStageStatus.Completed && !lab.CanExecute(PangaeaGenerationStage.Surface),
            "Cancellation stops queued H04 and preserves sequential block");
        Group(lab); ExpectAvailable(lab, 1);
        Check(lab.Error is null, "Retry recovers cancelled group");
        lab.SetSeedText("-"); lab.RequestRestart();
        Check(lab.Map is null && lab.SelectedCell is null && lab.Error is not null && lab.Stages.Count == 0, "Incomplete edited seed cannot leak old session");
        lab.SetSeedText("42"); lab.RequestRestart(); Wait(lab); ExpectAvailable(lab, 0);
        Console.WriteLine("LAB_ASYNC_PASS: reset during group/size worker, discarded obsolete results, cancelled H04 chain blocked, error/recovery, immediate selection clear");
    }
    private static void RandomSeeds()
    {
        using var lab = new PangaeaLaboratory();
        var seeds = new HashSet<int>();
        for (int i = 0; i < 8; i++)
        {
            string old = lab.SeedText; lab.RandomizeSeed();
            Check(int.TryParse(lab.SeedText, out int seed) && old != lab.SeedText && lab.Busy, "New signed random seed starts fresh grid immediately");
            seeds.Add(seed); Wait(lab); ExpectAvailable(lab, 0);
            Check(lab.Map!.Snapshot.Plates is null && lab.Map.Snapshot.Configuration.Seed == seed, "Random seed never starts a full world");
        }
        Check(seeds.Count > 1, "Random seed variety");
        lab.SetSeedText("-2147483648"); lab.RequestRestart(); Wait(lab);
        Check(lab.Map!.Snapshot.Configuration.Seed == int.MinValue, "Minimum seed remains editable/copyable");
        lab.SetSeedText("2147483647"); lab.RequestRestart(); Wait(lab);
        Check(lab.Map!.Snapshot.Configuration.Seed == int.MaxValue, "Maximum signed seed");
        Console.WriteLine("LAB_SEEDS_PASS: signed random seeds, immediate complete reset/selection clear, initial grid only, boundary editing, no confirmation or full generation");
    }
    private static void LayoutAndCamera(PangaeaMapData map)
    {
        foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480), (1920, 1080) })
        {
            var viewport = PangaeaLaboratoryLayout.Map(width, height);
            var inspector = PangaeaLaboratoryLayout.Inspector(width, height);
            Check(viewport.Width > width / 2 && viewport.X + viewport.Width == width && viewport.Y == 0 && viewport.Height == height, "Map owns majority width and full height");
            Check(inspector.X + inspector.Width < viewport.X && inspector.Y > 0, "Cell details stay bottom-left, outside map");
            var seen = new HashSet<UiAction>();
            float max = PangaeaLaboratoryLayout.MaximumScroll(width, height);
            foreach (float scroll in new[] { 0, max / 2, max })
            {
                var buttons = PangaeaLaboratoryLayout.Buttons(width, height, scroll);
                Check(buttons.Where(b => PangaeaLaboratoryLayout.IsStageAction(b.Action)).Select(b => b.Action)
                    .SequenceEqual(Actions.Select(PangaeaLaboratoryLayout.StageAction)), "Exactly eight visible actions in required order");
                Check(!buttons.Any(b => b.Action is UiAction.LabGrid or UiAction.LabMotion or UiAction.LabShowHeights or UiAction.LabConfirm), "No grid/H04/height-toggle or confirmation button");
                var controls = PangaeaLaboratoryLayout.ControlsViewport(width, height);
                foreach (var button in PangaeaLaboratoryLayout.HitButtons(width, height, scroll))
                {
                    var r = button.Bounds; seen.Add(button.Action);
                    Check(r.X >= 0 && r.X + r.Width <= viewport.X && r.Y >= 0 && r.Y + r.Height <= height, "All hit targets remain in sidebar");
                    if (button.Action != UiAction.Back)
                        Check(r.Y >= controls.Y && r.Y + r.Height <= controls.Y + controls.Height, "Scrolled targets clip to controls viewport");
                    else Check(r.Y > inspector.Y + inspector.Height, "Exit remains below inspector");
                }
                foreach (var selector in new[] { UiAction.TypeSelect, UiAction.SizeSelect, UiAction.LabView })
                foreach (var option in PangaeaLaboratoryLayout.Dropdown(width, height, selector, map, scroll))
                    Check(option.Bounds.X + option.Bounds.Width <= viewport.X && option.Bounds.Y >= controls.Y &&
                        option.Bounds.Y + option.Bounds.Height <= controls.Y + controls.Height + .01f, "Dropdowns stay entirely in sidebar controls");
            }
            Check(Actions.All(s => seen.Contains(PangaeaLaboratoryLayout.StageAction(s))), "All eight actions reachable at supported resolutions");
            var type = PangaeaLaboratoryLayout.Dropdown(width, height, UiAction.TypeSelect, map);
            Check(type.Count == 1 && type[0].Action == UiAction.Pangaea, "Only supported laboratory map type is selectable");
            foreach (var view in map.AvailableViews)
                Check(PangaeaLaboratoryLayout.View(PangaeaLaboratoryLayout.ViewAction(view)) == view, "Layer selector mapping stable");
            var grid = map.Snapshot.Grid!; var camera = new MapCamera2D();
            camera.FitViewport(grid.Definition.Columns, grid.Definition.Rows, (int)viewport.X, 0, (int)viewport.Width, height);
            Check(camera.ViewWidth == viewport.Width && camera.ViewportX == viewport.X && camera.ViewportY == 0, "Camera fits actual map area, never behind controls");
            var cell = grid.Cells[grid.Definition.Columns + 2];
            Check(Vector2.Distance(camera.ScreenToWorld(camera.WorldToScreen(cell.Center)), cell.Center) < .001f, "Viewport-aware selection transform");
            var anchor = new Vector2(viewport.X + viewport.Width / 2, height / 2f);
            var before = camera.ScreenToWorld(anchor); camera.Zoom(2, anchor);
            var delta = before - camera.ScreenToWorld(anchor);
            float period = GEvolve.Engine.Grid.FlatHexGeometry.PeriodWidth(grid.Definition.Columns);
            delta.X -= MathF.Round(delta.X / period) * period;
            Check(delta.Length() < .001f, "Zoom anchor respects sidebar origin and equivalent E/O copies");
            camera.Fit(grid.Definition.Columns, grid.Definition.Rows, width, height);
            Check(camera.ViewportX == 0 && camera.ViewportY == MapCamera2D.TopBarHeight && camera.ViewHeight == height - MapCamera2D.TopBarHeight,
                "Traditional camera retains original viewport contract");
        }
    }
    private static void HeightDisplay(PangaeaMapData map)
    {
        var s = map.Snapshot;
        PangaeaHeightLabelTests.Run(map);
        var surface = (byte[])s.Surface!.Clone(); var initial = (byte[])s.Base!.Clone();
        surface[0] = surface[1] = 1; surface[2] = surface[3] = 0; initial[0] = 90; initial[1] = 91; initial[2] = 100; initial[3] = 0;
        var baseOnly = new PangaeaMapData(s with { Surface = surface, Base = initial, Elevations = null });
        Check(baseOnly.Color(0, MapViewMode.Surface) == new Vector3(.35f, .57f, .32f) && baseOnly.Color(1, MapViewMode.Surface) == Vector3.One &&
            baseOnly.Color(2, MapViewMode.Surface) == new Vector3(.32f, .55f, .71f), "Strict surface >90 and ocean color unchanged");
        Check(baseOnly.Height(3, MapViewMode.Surface) == 0 && baseOnly.Height(3, MapViewMode.ElevationContribution) is null, "Zero distinct from pending contribution");
        var noHeights = new PangaeaMapData(s with { Surface = surface, Base = null, Elevations = null });
        Check(noHeights.Color(1, MapViewMode.Surface) == new Vector3(.35f, .57f, .32f) &&
            PangaeaHeightLabels.Build(noHeights, MapViewMode.Surface, PangaeaHeightLabelTests.Camera(noHeights)).Count == 0, "No invented values before elevations");
        for (int id = 0; id < s.Grid!.Cells.Count; id++)
            Check((map.Color(id, MapViewMode.Surface) == Vector3.One) == (s.Surface[id] == 1 && s.Elevations!.FinalByCellId.Span[id] > 90), "Actual surface white classification follows FINAL >90");
    }
    private static void Group(PangaeaLaboratory lab)
    {
        Check(lab.CanExecute(PangaeaGenerationStage.Plates), "Group enabled"); lab.ExecutePlatesAndMotion();
        Check(lab.Busy && lab.PlatesAndMotionState() == "EJECUTÁNDOSE", "Group state reflects actual operation"); Wait(lab);
        var plates = Stage(lab, PangaeaGenerationStage.Plates); var motion = Stage(lab, PangaeaGenerationStage.Motion);
        Check(plates.Status == PangaeaStageStatus.Completed && motion.Status == PangaeaStageStatus.Completed &&
            motion.InputRevisions[PangaeaGenerationStage.Plates] == plates.Revision && lab.View == MapViewMode.Motion && lab.Error is null, "Official grouped order and current results");
    }
    private static PangaeaStageInfo Stage(PangaeaLaboratory lab, PangaeaGenerationStage stage) => lab.Stages.Single(s => s.Stage == stage);
    private static void Run(PangaeaLaboratory lab, PangaeaGenerationStage stage, MapViewMode view)
    {
        Check(lab.CanExecute(stage), $"Stage {stage} enabled"); lab.Execute(stage); Wait(lab);
        Check(Stage(lab, stage).Status == PangaeaStageStatus.Completed && lab.Error is null && lab.View == view && Stage(lab, stage).Duration is not null, $"Stage {stage} complete");
    }
    private static void Wait(PangaeaLaboratory lab)
    {
        var timer = Stopwatch.StartNew();
        while (lab.Busy) { lab.Update(); if (timer.Elapsed.TotalSeconds > 60) throw new TimeoutException("Laboratory worker timed out"); Thread.Sleep(1); }
    }
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    private static bool Throws<T>(Action action) where T : Exception { try { action(); return false; } catch (T) { return true; } }
}
