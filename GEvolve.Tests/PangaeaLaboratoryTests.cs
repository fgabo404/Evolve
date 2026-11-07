using System.Diagnostics;
using System.Numerics;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;

/// <summary>Headless controller, partial-render input and layout contracts. Never creates a window or GL context.</summary>
public static class PangaeaLaboratoryTests
{
    public static void Run()
    {
        var menu = new GameSession(); menu.OpenPangaeaLaboratory();
        Check(menu.Screen == GameScreen.PangaeaLaboratory && menu.State.Landmass is null && menu.Grid is null,
            "Menu opens laboratory without inventing a world or changing automatic state");
        menu.ReturnToMenu(); menu.OpenCreateWorld();
        Check(menu.Screen == GameScreen.CreateWorld, "Traditional creation remains accessible");
        foreach (var size in Enum.GetValues<GridSize>()) Exercise(size);
        ResetAndCancellation();
        RandomSeeds();
        Console.WriteLine("LAB_PASS: four sizes; auto grid; five visible actions; grouped plates/H04; signed random seeds; height labels and strict >90 surface white; non-linear dependencies; original automatic equivalence; cache/reset/cancellation. NO window, GL context, screenshots or visual approval.");
    }

    private static void Exercise(GridSize size)
    {
        using var lab = new PangaeaLaboratory();
        lab.ChooseSize(size); lab.SetSeedText("-1387338765"); lab.RequestRestart();
        Check(lab.Map is null && lab.Stages.Count == 7 && lab.Busy, "Starting session automatically runs the official grid stage");
        Check(!lab.CanExecute(PangaeaGenerationStage.Plates), "Wait for automatic grid dependency");
        Wait(lab);
        Check(Stage(lab, PangaeaGenerationStage.Grid).Status == PangaeaStageStatus.Completed && lab.View == MapViewMode.Grid,
            "Automatic grid publishes before enabling plates");
        lab.Execute(PangaeaGenerationStage.Final);
        Check(lab.Error is not null && lab.Map!.Snapshot.Elevations is null, "Rejected action exposes error without creating data");
        var grid = lab.Map!.Snapshot.Grid!;
        Check(lab.Map.Snapshot.Plates is null && lab.Map.Snapshot.Surface is null && lab.Map.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null,
            "Grid renderer input has no fabricated plate/surface/height fields");
        Check(lab.SelectAt(grid.Cells[0].Center)?.Id == 0 && lab.Inspect().Any(s => s == "BASE: PENDIENTE"), "Selection before surface distinguishes pending");
        Check(!lab.ChooseView(MapViewMode.Surface) && !lab.ChooseView(MapViewMode.Biomes), "Unavailable views cannot be selected");
        Check(Throws<InvalidOperationException>(() => lab.Map.Color(0, MapViewMode.ElevationFinal)), "Partial input rejects missing heights");
        var cache = lab.Map; long revision = lab.PresentationRevision; int copies = lab.SnapshotCopies;
        for (int i = 0; i < 100; i++) Check(!lab.Update(), "Idle frame does not copy or publish a snapshot");
        Check(ReferenceEquals(cache, lab.Map) && revision == lab.PresentationRevision && copies == lab.SnapshotCopies, "Cached representation is stable between revisions");
        Group(lab);
        Check(lab.Map!.Snapshot.Plates!.Distinct().Count() == 16 && lab.Map.Snapshot.Motion is not null &&
            lab.Map.Snapshot.Surface is null, "Grouped action publishes both original independent outputs without surface");
        Check(lab.CanExecute(PangaeaGenerationStage.Motion) && lab.CanExecute(PangaeaGenerationStage.Surface), "Surface does not require H04");
        Run(lab, PangaeaGenerationStage.Surface, MapViewMode.Surface);
        Check(lab.Map!.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            lab.CanExecute(PangaeaGenerationStage.Base) && lab.CanExecute(PangaeaGenerationStage.MountainSystems), "Surface without elevation; BASE and systems independently available");
        int ocean = Array.IndexOf(lab.Map.Snapshot.Surface!, (byte)0);
        lab.SelectAt(grid.Cells[ocean].Center);
        Check(lab.Inspect().Contains("SUPERFICIE: OCÉANO") && lab.Inspect().Contains("BASE: PENDIENTE"), "Ocean is known without inventing a zero elevation");
        Run(lab, PangaeaGenerationStage.Motion, MapViewMode.Motion);
        foreach (var view in new[] { MapViewMode.Compression, MapViewMode.Extension, MapViewMode.Shear })
            Check(lab.ChooseView(view), "H04 views use real fields");
        Run(lab, PangaeaGenerationStage.MountainSystems, MapViewMode.Geology);
        var systems = lab.Map!.Snapshot.Mountains!;
        Check(lab.Map.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            lab.Inspect().Contains("APORTE: PENDIENTE"), "Existing systems render before BASE/FINAL; uplift remains pending");
        long systemsRevision = Stage(lab, PangaeaGenerationStage.MountainSystems).Revision;
        string? systemsHash = Stage(lab, PangaeaGenerationStage.MountainSystems).Hash;
        Run(lab, PangaeaGenerationStage.Base, MapViewMode.ElevationBase);
        Check(lab.Map!.Snapshot.Elevations is null && lab.CanExecute(PangaeaGenerationStage.Final), "BASE independent of composition");
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        var final = lab.Map!.Snapshot.Elevations!.FinalByCellId.ToArray();
        var contribution = lab.Map.Snapshot.Elevations.ContributionByCellId.ToArray();
        foreach (var view in PangaeaLaboratoryLayout.Views) Check(lab.ChooseView(view), "Every completed view selectable");
        lab.SelectAt(grid.Cells[ocean].Center);
        Check(lab.Inspect().Contains("BASE: 0") && lab.Inspect().Contains("APORTE: 0") && lab.Inspect().Contains("FINAL: 0"), "A real zero differs from pending");
        CheckLayout(lab.Map);
        HeightDisplay(lab.Map);

        lab.Execute(PangaeaGenerationStage.Motion);
        Check(lab.Map!.Snapshot.Elevations is null && lab.Map.Snapshot.Base is not null,
            "Repeating H04 invalidates FINAL immediately while retaining BASE");
        for (int id = 0; id < grid.Cells.Count; id++)
            Check(lab.Map.Height(id, MapViewMode.Surface) == lab.Map.Snapshot.Base[id], "Surface immediately falls back to current BASE");
        Wait(lab); Run(lab, PangaeaGenerationStage.MountainSystems, MapViewMode.Geology);
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        systemsRevision = Stage(lab, PangaeaGenerationStage.MountainSystems).Revision;
        systemsHash = Stage(lab, PangaeaGenerationStage.MountainSystems).Hash;

        lab.Execute(PangaeaGenerationStage.Base);
        Check(lab.Busy && lab.Map!.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            !lab.Map.AvailableViews.Contains(MapViewMode.ElevationFinal) &&
            Stage(lab, PangaeaGenerationStage.Final).Status == PangaeaStageStatus.Invalidated &&
            lab.Inspect().Contains("FINAL: INVALIDADO"), "Hide invalidated FINAL immediately when regeneration starts");
        Check(lab.Map.Height(ocean, MapViewMode.Surface) is null, "No stale FINAL or BASE while both are unavailable");
        Check(!lab.CanExecute(PangaeaGenerationStage.Grid) && lab.Map.Snapshot.Mountains is not null &&
            lab.StageState(Stage(lab, PangaeaGenerationStage.Base)) == "GENERANDO", "No incompatible operations; systems remain available while BASE runs");
        Wait(lab);
        Check(Stage(lab, PangaeaGenerationStage.MountainSystems).Revision == systemsRevision &&
            Stage(lab, PangaeaGenerationStage.MountainSystems).Hash == systemsHash &&
            lab.Map!.Snapshot.Mountains!.InfluenceByCellId.SequenceEqual(systems.InfluenceByCellId), "BASE regeneration retains systems and geometry");
        Run(lab, PangaeaGenerationStage.Final, MapViewMode.ElevationFinal);
        Check(final.SequenceEqual(lab.Map!.Snapshot.Elevations!.FinalByCellId.ToArray()) &&
            contribution.SequenceEqual(lab.Map.Snapshot.Elevations.ContributionByCellId.ToArray()), "Regeneration produces identical heights");
        var automatic = new WorldGenerationService().Generate(size, LandmassType.Pangaea, -1387338765);
        Check(final.SequenceEqual(automatic.PangaeaElevations!.FinalByCellId.ToArray()) &&
            lab.Map.Snapshot.Base!.SequenceEqual(automatic.PangaeaElevations.BaseByCellId.ToArray()) &&
            lab.Map.Snapshot.Surface!.SequenceEqual(automatic.Landmass.SurfaceByCellId), "Controller equals unchanged automatic route");

        long gridRevision = Stage(lab, PangaeaGenerationStage.Grid).Revision;
        lab.ExecutePlatesAndMotion();
        Check(lab.Map!.Snapshot.Grid is not null && lab.Map.Snapshot.Surface is null && lab.Map.Snapshot.Base is null &&
            lab.Map.Snapshot.Mountains is null && lab.Map.Snapshot.Elevations is null, "Repeating grouped action invalidates descendants, preserving grid");
        Wait(lab);
        Check(lab.PlatesAndMotionState() == "COMPLETADA" && Stage(lab, PangaeaGenerationStage.Grid).Revision == gridRevision,
            "Grouped repetition keeps upstream revision");

        lab.SetSeedText("42");
        Check(lab.ConfigurationPending && lab.Map is null && lab.Inspect().Count == 0 &&
            !lab.CanExecute(PangaeaGenerationStage.Grid), "Draft configuration never exposes stale layers under a new seed");
        lab.RequestRestart();
        Check(lab.ConfirmRestart && lab.Notice!.Contains("ELIMINA"), "Warn before destroying generated layers");
        lab.CancelReset();
        Check(!lab.ConfigurationPending && lab.Map is not null && lab.SeedText == "-1387338765", "Cancel preserves active session");
        lab.ChooseSize(size == GridSize.Small ? GridSize.Medium : GridSize.Small);
        lab.SetSeedText("42"); lab.RequestRestart(); lab.ConfirmReset();
        Check(lab.Map is null && lab.SelectedCell is null && lab.Busy && lab.Stages.Skip(1).All(s => s.Status == PangaeaStageStatus.Pending), "Confirmed size and seed change clears layers and automatically starts grid");
        Wait(lab);
        Console.WriteLine($"LAB_CASE_PASS {size}: automatic grid; five visible actions; grouped plates/H04; seven engine stages; numeric heights; white threshold >90; FINAL/BASE fallback; cache/invalidation; original automatic equivalence; reset confirmation");
    }
    private static void ResetAndCancellation()
    {
        using var lab = new PangaeaLaboratory();
        lab.SetSeedText("2147483648"); lab.RequestRestart();
        Check(lab.Error is not null && lab.Stages.Count == 0, "Invalid seed reports error");
        lab.SetSeedText("2026"); lab.ChooseSize(GridSize.Enormous); lab.RequestRestart();
        Check(lab.Error is null, "Recover from invalid configuration");
        lab.SetSeedText("42"); lab.ChooseSize(GridSize.Small); lab.RequestRestart();
        Check(lab.ConfirmRestart, "Warn even while old generation is running");
        lab.ConfirmReset(); Wait(lab);
        Check(lab.Map!.Snapshot.Configuration.Seed == 42 && lab.Map.Snapshot.Grid!.Definition.Size == GridSize.Small,
            "Obsolete background result cannot replace the reset session");
        lab.ExecutePlatesAndMotion(); lab.Cancel(); Wait(lab);
        // Cancellation may race with completion; either outcome must agree with the engine's published status/data.
        var stage = Stage(lab, PangaeaGenerationStage.Plates);
        Check((stage.Status == PangaeaStageStatus.Invalidated && lab.Map!.Snapshot.Plates is null) ||
              (stage.Status == PangaeaStageStatus.Completed && lab.Map!.Snapshot.Plates is not null), "Cooperative cancellation keeps truthful availability");
        Check(Stage(lab, PangaeaGenerationStage.Motion).Status != PangaeaStageStatus.Completed, "Cancellation stops the queued H04 operation");
        Run(lab, PangaeaGenerationStage.Plates, MapViewMode.Tectonic);
        Run(lab, PangaeaGenerationStage.Surface, MapViewMode.Surface);
        Check(lab.CanExecute(PangaeaGenerationStage.Base) && !lab.CanExecute(PangaeaGenerationStage.MountainSystems),
            "Internal dependencies remain non-linear; BASE does not require H04");
        Run(lab, PangaeaGenerationStage.Base, MapViewMode.ElevationBase);
        Check(lab.Error is null && lab.CanExecute(PangaeaGenerationStage.Motion), "Recover by repeating cancelled operation");
        Check(lab.StageState(stage with { Status = PangaeaStageStatus.Failed, Error = "diagnostic failure" }) == "ERROR", "Real engine error state has a readable presentation");
        Console.WriteLine("LAB_ASYNC_PASS: invalid request/recovery, reset during worker, obsolete work detached, cooperative cancellation/retry; no UI thread blocking");
    }
    private static void CheckLayout(PangaeaMapData map)
    {
        foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480), (1920, 1080) })
        {
            var buttons = PangaeaLaboratoryLayout.Buttons(width, height, true);
            Check(buttons.Where(b => PangaeaLaboratoryLayout.IsStageAction(b.Action)).Select(b => b.Action)
                .SequenceEqual(new[] { UiAction.LabPlates, UiAction.LabSurface, UiAction.LabBase, UiAction.LabSystems, UiAction.LabFinal }) &&
                !buttons.Any(b => b.Action is UiAction.LabGrid or UiAction.LabMotion), "Exactly five numbered stage actions; grid/H04 buttons hidden");
            Check(buttons.Any(b => b.Action == UiAction.RandomSeed) && buttons.Any(b => b.Action == UiAction.LabShowHeights), "Random seed and numeric height controls");
            foreach (var button in buttons.Concat(PangaeaLaboratoryLayout.Dropdown(width, height, UiAction.SizeSelect, map))
                .Concat(PangaeaLaboratoryLayout.Dropdown(width, height, UiAction.LabView, map)))
            {
                var b = button.Bounds;
                Check(b.X >= 0 && b.Y >= 0 && b.X + b.Width <= width && b.Y + b.Height <= height && b.Width > 0 && b.Height > 0,
                    "Layout bounds fit supported sizes without opening a window");
                Check(b.Contains(b.X + b.Width / 2, b.Y + b.Height / 2), "Rendering and hit testing share bounds");
            }
        }
    }
    private static void Group(PangaeaLaboratory lab)
    {
        Check(lab.CanExecute(PangaeaGenerationStage.Plates), "Grouped plates/H04 enabled after grid");
        lab.ExecutePlatesAndMotion();
        Check(lab.Busy && lab.PlatesAndMotionState() == "GENERANDO", "Grouped action reports actual work");
        Wait(lab);
        var plates = Stage(lab, PangaeaGenerationStage.Plates);
        var motion = Stage(lab, PangaeaGenerationStage.Motion);
        Check(plates.Status == PangaeaStageStatus.Completed && motion.Status == PangaeaStageStatus.Completed &&
            motion.InputRevisions[PangaeaGenerationStage.Plates] == plates.Revision &&
            lab.PlatesAndMotionState() == "COMPLETADA" && lab.View == MapViewMode.Motion,
            "Grouped action executes independent official operations in dependency order");
    }
    private static void RandomSeeds()
    {
        using var lab = new PangaeaLaboratory();
        var seeds = new HashSet<int>();
        for (int i = 0; i < 100; i++)
        {
            lab.RandomizeSeed();
            Check(int.TryParse(lab.SeedText, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out int seed), "Every random seed is a signed int32, displayed immediately");
            seeds.Add(seed);
            Check(!lab.Busy && lab.Stages.Count == 0 && lab.Map is null, "Randomization never starts generation");
        }
        Check(seeds.Count > 1, "Session randomization supplies different seeds");
        lab.SetSeedText(int.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Check(lab.SeedText == "-2147483648", "Minimum signed seed remains editable/copyable");
        lab.SetSeedText(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Check(lab.SeedText == "2147483647", "Maximum signed seed remains editable/copyable");
        lab.RequestRestart(); Wait(lab); Group(lab);
        int original = int.Parse(lab.SeedText, System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < 10 && int.Parse(lab.SeedText, System.Globalization.CultureInfo.InvariantCulture) == original; i++) lab.RandomizeSeed();
        Check(lab.ConfigurationPending && lab.Map is null && !lab.Busy, "Random seed uses the same draft/invalidation rules, without generating");
        lab.RequestRestart(); Check(lab.ConfirmRestart, "Random seed requires the same loss-of-layers confirmation");
        lab.CancelReset(); Check(lab.Map is not null && int.Parse(lab.SeedText, System.Globalization.CultureInfo.InvariantCulture) == original,
            "Cancelling random seed changes preserves active layers");
        var map = lab.Map; lab.ToggleHeights();
        Check(lab.ShowHeights && ReferenceEquals(map, lab.Map), "Height toggle changes presentation only");
        lab.ToggleHeights(); Check(!lab.ShowHeights, "Height labels can be disabled");
        Console.WriteLine("LAB_SEEDS_PASS: 100 signed int32 samples, boundary editing, no implicit generation, confirmation/cancel and presentation-only toggle");
    }
    private static void HeightDisplay(PangaeaMapData map)
    {
        var s = map.Snapshot;
        var range = new VisibleHexRange(-1, 1, 0, 0);
        foreach (var view in new[] { MapViewMode.ElevationBase, MapViewMode.ElevationContribution, MapViewMode.ElevationFinal, MapViewMode.Surface })
        {
            var labels = PangaeaHeightLabels.Build(map, view, range, 13, true);
            Check(labels.Count == 3 && labels[0].CellId == s.Grid!.Definition.Columns - 1 && labels[0].Column == -1,
                "Visible annotations preserve wrapped cell identity and visual copy");
            foreach (var label in labels)
            {
                byte expected = view == MapViewMode.ElevationBase ? s.Base![label.CellId] :
                    view == MapViewMode.ElevationContribution ? s.Elevations!.ContributionByCellId.Span[label.CellId] :
                    s.Elevations!.FinalByCellId.Span[label.CellId];
                Check(label.Height == expected, "Annotation uses exactly the requested existing field");
            }
            Check(PangaeaHeightLabels.Build(map, view, range, 12.99f, true).Count == 0 &&
                PangaeaHeightLabels.Build(map, view, range, 35, false).Count == 0, "Zoom and toggle suppress annotation clutter");
        }
        Check(PangaeaHeightLabels.Build(map, MapViewMode.Surface, new(0, 1000, 0, 0), 35, true).Count == 0 &&
            PangaeaHeightLabels.Build(map, MapViewMode.Tectonic, range, 35, true).Count == 0, "Bound label count and avoid invented height in non-height views");
        var surface = (byte[])s.Surface!.Clone(); var initial = (byte[])s.Base!.Clone();
        surface[0] = surface[1] = 1; surface[2] = surface[3] = 0;
        initial[0] = 90; initial[1] = 91; initial[2] = 100; initial[3] = 0;
        var baseOnly = new PangaeaMapData(s with { Surface = surface, Base = initial, Elevations = null });
        Check(baseOnly.Color(0, MapViewMode.Surface) == new Vector3(.35f, .57f, .32f) &&
            baseOnly.Color(1, MapViewMode.Surface) == Vector3.One &&
            baseOnly.Color(2, MapViewMode.Surface) == new Vector3(.32f, .55f, .71f), "Strict >90 threshold applies only to laboratory land; ocean stays blue");
        Check(baseOnly.Height(3, MapViewMode.Surface) == 0 && baseOnly.Height(3, MapViewMode.ElevationContribution) is null,
            "Measured zero remains distinct from pending uplift");
        var noHeights = new PangaeaMapData(s with { Surface = surface, Base = null, Elevations = null });
        Check(noHeights.Color(1, MapViewMode.Surface) == new Vector3(.35f, .57f, .32f) &&
            noHeights.Height(1, MapViewMode.Surface) is null &&
            PangaeaHeightLabels.Build(noHeights, MapViewMode.Surface, range, 35, true).Count == 0, "Surface without heights neither invents values nor paints elevation white");
        var finalFirst = new PangaeaMapData(s with { Base = initial });
        for (int id = 0; id < s.Grid!.Cells.Count; id++)
        {
            byte height = s.Elevations!.FinalByCellId.Span[id];
            Check(finalFirst.Height(id, MapViewMode.Surface) == height, "Current FINAL takes priority over BASE");
            Check((map.Color(id, MapViewMode.Surface) == Vector3.One) == (s.Surface[id] == 1 && height > 90),
                "Real-world white classification exactly follows current FINAL >90");
        }
    }
    private static PangaeaStageInfo Stage(PangaeaLaboratory lab, PangaeaGenerationStage stage) => lab.Stages.Single(s => s.Stage == stage);
    private static void Run(PangaeaLaboratory lab, PangaeaGenerationStage stage, MapViewMode view)
    {
        Check(lab.CanExecute(stage), $"Stage {stage} enabled by real dependencies");
        long revision = lab.PresentationRevision;
        lab.Execute(stage); Wait(lab);
        Check(Stage(lab, stage).Status == PangaeaStageStatus.Completed && lab.Error is null &&
            Stage(lab, stage).Duration is not null && lab.PresentationRevision > revision && lab.View == view,
            $"Stage {stage} completes, publishes cached data and selects its available view");
    }
    private static void Wait(PangaeaLaboratory lab)
    {
        var timeout = Stopwatch.StartNew();
        while (lab.Busy) { lab.Update(); if (timeout.Elapsed.TotalSeconds > 60) throw new TimeoutException("Laboratory worker timed out."); Thread.Sleep(1); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static bool Throws<T>(Action action) where T : Exception { try { action(); return false; } catch (T) { return true; } }
}
