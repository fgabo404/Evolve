using System.Diagnostics;
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
        Console.WriteLine("LAB_PASS: four sizes; seven real stages; non-linear dependencies; partial input contracts; revision cache; regeneration; reset; obsolete completion; cancellation; errors/recovery. NO window, GL context, screenshots or visual approval.");
    }

    private static void Exercise(GridSize size)
    {
        using var lab = new PangaeaLaboratory();
        lab.ChooseSize(size); lab.SetSeedText("-1387338765"); lab.RequestRestart();
        Check(lab.Map is null && lab.Stages.Count == 7 && lab.CanExecute(PangaeaGenerationStage.Grid), "Create empty session");
        Check(!lab.CanExecute(PangaeaGenerationStage.Plates) && lab.UnavailableReason(PangaeaGenerationStage.Plates).Contains("01"), "Missing dependency explanation");
        lab.Execute(PangaeaGenerationStage.Final);
        Check(lab.Error is not null && lab.Map is null, "Rejected action exposes error without creating data");
        Run(lab, PangaeaGenerationStage.Grid, MapViewMode.Grid);
        var grid = lab.Map!.Snapshot.Grid!;
        Check(lab.Map.Snapshot.Plates is null && lab.Map.Snapshot.Surface is null && lab.Map.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null,
            "Grid renderer input has no fabricated plate/surface/height fields");
        Check(lab.SelectAt(grid.Cells[0].Center)?.Id == 0 && lab.Inspect().Any(s => s == "BASE: PENDIENTE"), "Selection before surface distinguishes pending");
        Check(!lab.ChooseView(MapViewMode.Surface) && !lab.ChooseView(MapViewMode.Biomes), "Unavailable views cannot be selected");
        Check(Throws<InvalidOperationException>(() => lab.Map.Color(0, MapViewMode.ElevationFinal)), "Partial input rejects missing heights");
        var cache = lab.Map; long revision = lab.PresentationRevision; int copies = lab.SnapshotCopies;
        for (int i = 0; i < 100; i++) Check(!lab.Update(), "Idle frame does not copy or publish a snapshot");
        Check(ReferenceEquals(cache, lab.Map) && revision == lab.PresentationRevision && copies == lab.SnapshotCopies, "Cached representation is stable between revisions");
        Run(lab, PangaeaGenerationStage.Plates, MapViewMode.Tectonic);
        Check(lab.Map!.Snapshot.Plates!.Distinct().Count() == 16 && lab.Map.Snapshot.Motion is null &&
            lab.Map.Snapshot.Surface is null && lab.Map.AvailableViews.SequenceEqual([MapViewMode.Grid, MapViewMode.Tectonic]), "Plate-only view is real and independent of surface");
        Check(lab.CanExecute(PangaeaGenerationStage.Motion) && lab.CanExecute(PangaeaGenerationStage.Surface), "Surface does not require H04");
        Run(lab, PangaeaGenerationStage.Surface, MapViewMode.Surface);
        Check(lab.Map!.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            lab.CanExecute(PangaeaGenerationStage.Base) && !lab.CanExecute(PangaeaGenerationStage.MountainSystems), "Surface without elevation; systems require H04");
        int ocean = Array.IndexOf(lab.Map.Snapshot.Surface!, (byte)0);
        lab.SelectAt(grid.Cells[ocean].Center);
        Check(lab.Inspect().Contains("SUPERFICIE: OCÉANO") && lab.Inspect().Contains("BASE: PENDIENTE"), "Ocean is known without inventing a zero elevation");
        Run(lab, PangaeaGenerationStage.Motion, MapViewMode.Tectonic);
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

        lab.Execute(PangaeaGenerationStage.Base);
        Check(lab.Busy && lab.Map!.Snapshot.Base is null && lab.Map.Snapshot.Elevations is null &&
            !lab.Map.AvailableViews.Contains(MapViewMode.ElevationFinal) &&
            Stage(lab, PangaeaGenerationStage.Final).Status == PangaeaStageStatus.Invalidated &&
            lab.Inspect().Contains("FINAL: INVALIDADO"), "Hide invalidated FINAL immediately when regeneration starts");
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

        lab.SetSeedText("42");
        Check(lab.ConfigurationPending && lab.Map is null && lab.Inspect().Count == 0 &&
            !lab.CanExecute(PangaeaGenerationStage.Grid), "Draft configuration never exposes stale layers under a new seed");
        lab.RequestRestart();
        Check(lab.ConfirmRestart && lab.Notice!.Contains("ELIMINA"), "Warn before destroying generated layers");
        lab.CancelReset();
        Check(!lab.ConfigurationPending && lab.Map is not null && lab.SeedText == "-1387338765", "Cancel preserves active session");
        lab.ChooseSize(size == GridSize.Small ? GridSize.Medium : GridSize.Small);
        lab.SetSeedText("42"); lab.RequestRestart(); lab.ConfirmReset();
        Check(lab.Map is null && lab.SelectedCell is null && lab.Stages.All(s => s.Status == PangaeaStageStatus.Pending), "Confirmed size and seed change clears all data");
        Console.WriteLine($"LAB_CASE_PASS {size}: seven stages, cached partial data, dependency buttons, real zero/pending/invalidated, BASE repeat, original automatic equivalence, layout contracts, reset confirmation");
    }
    private static void ResetAndCancellation()
    {
        using var lab = new PangaeaLaboratory();
        lab.SetSeedText("2147483648"); lab.RequestRestart();
        Check(lab.Error is not null && lab.Stages.Count == 0, "Invalid seed reports error");
        lab.SetSeedText("2026"); lab.ChooseSize(GridSize.Enormous); lab.RequestRestart();
        Check(lab.Error is null, "Recover from invalid configuration");
        lab.Execute(PangaeaGenerationStage.Grid);
        lab.SetSeedText("42"); lab.ChooseSize(GridSize.Small); lab.RequestRestart();
        Check(lab.ConfirmRestart, "Warn even while old generation is running");
        lab.ConfirmReset(); Run(lab, PangaeaGenerationStage.Grid, MapViewMode.Grid);
        Check(lab.Map!.Snapshot.Configuration.Seed == 42 && lab.Map.Snapshot.Grid!.Definition.Size == GridSize.Small,
            "Obsolete background result cannot replace the reset session");
        lab.Execute(PangaeaGenerationStage.Plates); lab.Cancel(); Wait(lab);
        // Cancellation may race with completion; either outcome must agree with the engine's published status/data.
        var stage = Stage(lab, PangaeaGenerationStage.Plates);
        Check((stage.Status == PangaeaStageStatus.Invalidated && lab.Map!.Snapshot.Plates is null) ||
              (stage.Status == PangaeaStageStatus.Completed && lab.Map!.Snapshot.Plates is not null), "Cooperative cancellation keeps truthful availability");
        Run(lab, PangaeaGenerationStage.Plates, MapViewMode.Tectonic);
        Check(lab.Error is null && lab.CanExecute(PangaeaGenerationStage.Motion), "Recover by repeating cancelled operation");
        Check(lab.StageState(stage with { Status = PangaeaStageStatus.Failed, Error = "diagnostic failure" }) == "ERROR", "Real engine error state has a readable presentation");
        Console.WriteLine("LAB_ASYNC_PASS: invalid request/recovery, reset during worker, obsolete work detached, cooperative cancellation/retry; no UI thread blocking");
    }
    private static void CheckLayout(PangaeaMapData map)
    {
        foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480), (1920, 1080) })
        {
            var buttons = PangaeaLaboratoryLayout.Buttons(width, height, true);
            Check(buttons.Count(b => PangaeaLaboratoryLayout.IsStageAction(b.Action)) == 7, "Seven stage controls");
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
