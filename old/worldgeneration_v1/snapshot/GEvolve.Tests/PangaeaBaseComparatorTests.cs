using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World;
using GEvolve.Engine.World.Experimental;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

/// <summary>Real controller/data/label contracts; no window, GL or visual evaluation.</summary>
public static class PangaeaBaseComparatorTests
{
    private const string History = "GEvolve.Tests/ValidationReports/P03-A82b_20261108_234756449Z_a6c3ecc0/lowland-variants.log";
    public static void Run()
    {
        var history = File.ReadLines(History).Where(l => l.StartsWith("BASE_VARIANT_RESULT ", StringComparison.Ordinal))
            .Select(l => JsonDocument.Parse(l["BASE_VARIANT_RESULT ".Length..]))
            .ToDictionary(d => $"{d.RootElement.GetProperty("Size").GetString()}/{d.RootElement.GetProperty("Seed").GetInt32()}/{d.RootElement.GetProperty("Variant").GetString()}",
                d => { using (d) return d.RootElement.GetProperty("HeightsHash").GetString()!; });
        Check(history.Count == 176, "Immutable P03-A.8.2b history must remain complete");
        foreach (var size in Enum.GetValues<GridSize>()) foreach (int seed in new[] { -1387338765, -132790047, 2026, 42 })
            Exercise(size, seed, history);
        Console.WriteLine("COMPARATOR_PASS: 64 exact historical fields; 16 worlds; four sizes; detached asynchronous cache; official DAG/BASE/FINAL/ecology/persistence intact; labels/inspector/other views; reset and stale-result protection. NO window/GL/visual approval.");
    }
    private static void Exercise(GridSize size, int seed, Dictionary<string, string> history)
    {
        var official = new WorldGenerationService().Generate(size, LandmassType.Pangaea, seed);
        string officialHash = Hash(official.Landmass);
        using var lab = new PangaeaLaboratory(); lab.ChooseSize(size); lab.SetSeedText(seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        lab.RequestRestart(); Wait(lab);
        Check(!lab.CanCompareBase && !lab.ChooseBaseVariant(PangaeaBaseVariant.V1), "No comparator before BASE");
        Check(!PangaeaLaboratoryLayout.Buttons(1280, 720).Any(b => b.Action == UiAction.LabBaseVariant), "Selector initially absent");
        lab.ExecutePlatesAndMotion(); Wait(lab); Run(lab, PangaeaGenerationStage.Surface); Run(lab, PangaeaGenerationStage.Base);
        Check(lab.CanCompareBase && lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0, "BASE enables clean V0 default");
        var snapshot = lab.Map!.Snapshot;
        var surface = snapshot.Surface ?? throw new InvalidOperationException("Completed surface required");
        var input = PangaeaBaseVariants.Input.From(snapshot);
        var context = new PangaeaBaseVariantMetrics.Context(new HexTopology(official.Grid), official.Landmass, false);
        string frozen = Hash(snapshot), dag = Hash(lab.Stages);
        var camera = PangaeaHeightLabelTests.Camera(lab.Map);
        camera.Pan(17, 23); camera.Zoom(1, new(780, 340));
        long cameraVersion = camera.Version;
        lab.SelectAt(snapshot.Grid!.Cells[snapshot.Grid.Definition.Columns + 2].Center);
        var selected = lab.SelectedCell;
        var expectedFields = new Dictionary<PangaeaBaseVariant, byte[]>();
        foreach (var variant in Enum.GetValues<PangaeaBaseVariant>())
        {
            var old = PangaeaRegionalLowlandVariants.Generate(input, variant.ToString(), context).Heights;
            var times = new List<double>(); var allocations = new List<long>();
            _ = PangaeaBaseComparison.Generate(snapshot, variant);
            for (int repeat = 0; repeat < 3; repeat++)
            {
                long allocated = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
                byte[] result = PangaeaBaseComparison.Generate(snapshot, variant); timer.Stop();
                allocations.Add(GC.GetAllocatedBytesForCurrentThread() - allocated); times.Add(timer.Elapsed.TotalMilliseconds);
                Check(result.SequenceEqual(old) && HashBytes(result) == history[$"{size}/{seed}/{variant}"], "Pure module must equal independent original code AND historical bytes/hash");
                expectedFields[variant] = result;
            }
            var selectionTimer = Stopwatch.StartNew();
            Check(lab.ChooseBaseVariant(variant), "Valid visual selection");
            double dispatchMs = selectionTimer.Elapsed.TotalMilliseconds;
            Wait(lab); selectionTimer.Stop();
            var map = lab.Map!;
            Check(lab.BaseVariant == variant && map.BaseVariant == variant && lab.View == MapViewMode.ElevationBase, "Selection publishes only completed requested field");
            Check(ReferenceEquals(snapshot, map.Snapshot) && Hash(snapshot) == frozen && Hash(lab.Stages) == dag,
                "Switching never runs a stage, changes a snapshot or the official DAG");
            Check(lab.SelectedCell == selected && camera.Version == cameraVersion && ReferenceEquals(snapshot.Grid, map.Snapshot.Grid), "Same grid/selection and camera version");
            for (int id = 0; id < old.Length; id++)
            {
                Check(map.Height(id, MapViewMode.ElevationBase) == old[id] && (snapshot.Surface![id] == 0 ? old[id] == 0 : old[id] is >= 1 and <= 100), "Visible heights preserve ocean/mask");
                Check(map.Color(id, MapViewMode.ElevationBase) == (surface[id] == 0 ? new Vector3(.08f, .17f, .30f) : new Vector3(old[id] / 100f)), "Shared absolute 0..100 palette, unchanged blue ocean");
            }
            Check(lab.Inspect().Any(l => l.StartsWith("BASE: VISIBLE " + PangaeaBaseComparison.Label(variant), StringComparison.Ordinal)) &&
                lab.Inspect().Contains("BASE: OFICIAL MOTOR " + snapshot.Base![selected!.Value.Id]), "Inspector distinguishes visible experimental field from official motor");
            var labels = PangaeaHeightLabels.Build(map, MapViewMode.ElevationBase, camera);
            Check(labels.Count > 0 && labels.All(l => l.Height == old[l.CellId] && l.Text == old[l.CellId].ToString(System.Globalization.CultureInfo.InvariantCulture) && l.Pixel >= PangaeaHeightLabels.MinimumPixel), "Readable labels use visible variant");
            Check(PangaeaHeightLabels.Build(map, MapViewMode.ElevationBase, PangaeaHeightLabelTests.Camera(map, false)).Count == 0, "No scattered labels at distant zoom");
            Console.WriteLine("COMPARATOR_FIELD " + JsonSerializer.Serialize(new { Size = size.ToString(), Seed = seed, Variant = variant.ToString(), HeightHash = HashBytes(old), SamplesMs = times, AllocatedBytes = allocations, PayloadBytes = old.Length, Labels = labels.Count, DispatchMs = dispatchMs, ReadyMs = selectionTimer.Elapsed.TotalMilliseconds }));
        }
        Check(lab.CachedBaseVariants == 3, "Only three experimental byte arrays cached");
        Layout(lab.Map!);
        int copies = lab.SnapshotCopies;
        var warmTimes = new List<double>();
        for (int repeat = 0; repeat < 4; repeat++) foreach (var variant in Enum.GetValues<PangaeaBaseVariant>())
        {
            var timer = Stopwatch.StartNew(); lab.ChooseBaseVariant(variant); timer.Stop();
            warmTimes.Add(timer.Elapsed.TotalMilliseconds);
            Check(!lab.ComparingBase && lab.SnapshotCopies == copies, "Warm switch never copies snapshots or schedules computation");
        }
        Console.WriteLine("COMPARATOR_CACHE " + JsonSerializer.Serialize(new { Size = size.ToString(), Seed = seed, SamplesMs = warmTimes, CachedPayloadBytes = lab.CachedBaseVariants * snapshot.Base!.Length, SnapshotCopiesAdded = lab.SnapshotCopies - copies }));
        lab.ChooseBaseVariant(PangaeaBaseVariant.V1);
        Check(!lab.ComparingBase, "Warm switching is immediate, without worker or pipeline");
        foreach (var stage in new[] { PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final,
            PangaeaGenerationStage.Temperature, PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes }) Run(lab, stage);
        var complete = lab.Map!.Snapshot;
        var completedBase = complete.Base ?? throw new InvalidOperationException("Completed BASE required");
        string completedHash = Hash(complete);
        Check(complete.Base!.SequenceEqual(official.PangaeaElevations!.BaseByCellId.ToArray()) &&
            complete.Elevations!.FinalByCellId.Span.SequenceEqual(official.PangaeaElevations.FinalByCellId.Span) &&
            Hash(complete.Climate) == Hash(official.Landmass.Climate) && Hash(complete.Biomes) == Hash(official.Landmass.Biomes) &&
            complete.Slopes!.SequenceEqual(official.Landmass.SlopeByCellId!), "Continuing stages with V1 displayed preserves official BASE/FINAL/ecology");
        var officialMap = new PangaeaMapData(complete);
        foreach (var variant in Enum.GetValues<PangaeaBaseVariant>())
        {
            lab.ChooseBaseVariant(variant); Wait(lab);
            foreach (var view in officialMap.AvailableViews.Where(v => v != MapViewMode.ElevationBase))
            {
                lab.ChooseView(view);
                for (int id = 0; id < completedBase.Length; id += 37)
                    Check(lab.Map!.Height(id, view) == officialMap.Height(id, view) && lab.Map.Color(id, view) == officialMap.Color(id, view), "Non-BASE views remain official");
                Check(!lab.Inspect().Any(l => l.StartsWith("BASE: VISIBLE", StringComparison.Ordinal)), "Other views show official inspector");
            }
        }
        Check(completedHash == Hash(lab.Map!.Snapshot) && officialHash == Hash(official.Landmass), "Completed official state unchanged after all variants/views");
        Persist(official, officialHash);
        // Re-executing BASE or surface discards all variants synchronously, including unfinished work.
        Run(lab, PangaeaGenerationStage.Base);
        Check(lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0, "BASE invalidation restores V0 and frees cache");
        lab.ChooseBaseVariant(PangaeaBaseVariant.V1); lab.ChooseBaseVariant(PangaeaBaseVariant.V0); lab.Update();
        Check(lab.BaseVariant == PangaeaBaseVariant.V0 && !lab.ComparingBase && lab.CachedBaseVariants == 0, "Latest selection wins over discarded cold result");
        lab.ChooseBaseVariant(PangaeaBaseVariant.V1); lab.Execute(PangaeaGenerationStage.Surface);
        Check(!lab.CanCompareBase && !lab.ComparingBase && lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0, "Surface invalidates pending comparison immediately");
        Wait(lab); Check(lab.Map!.Snapshot.Base is null && lab.BaseVariant == PangaeaBaseVariant.V0, "Old task cannot overlay regenerated surface");
        Run(lab, PangaeaGenerationStage.Base); lab.ChooseBaseVariant(PangaeaBaseVariant.V4O);
        lab.SetSeedText(seed == 42 ? "2026" : "42"); Wait(lab);
        Check(lab.Map is null && lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0 && !lab.ComparingBase, "Seed edit discards stale task/map/cache");
        lab.RequestRestart(); Wait(lab); Run(lab, PangaeaGenerationStage.Plates); Run(lab, PangaeaGenerationStage.Motion);
        Run(lab, PangaeaGenerationStage.Surface); Run(lab, PangaeaGenerationStage.Base);
        lab.ChooseBaseVariant(PangaeaBaseVariant.V4U); Wait(lab);
        lab.ChooseSize(size == GridSize.Small ? GridSize.Medium : GridSize.Small); Wait(lab);
        Check(!lab.CanCompareBase && lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0, "Size reset restores V0");
        Run(lab, PangaeaGenerationStage.Plates); Run(lab, PangaeaGenerationStage.Motion);
        Run(lab, PangaeaGenerationStage.Surface); Run(lab, PangaeaGenerationStage.Base);
        lab.ChooseBaseVariant(PangaeaBaseVariant.V4U); Wait(lab); lab.RequestRestart(); Wait(lab);
        Check(!lab.CanCompareBase && lab.BaseVariant == PangaeaBaseVariant.V0 && lab.CachedBaseVariants == 0, "Explicit restart restores V0");
        using var reentry = new PangaeaLaboratory();
        Check(reentry.BaseVariant == PangaeaBaseVariant.V0 && !reentry.CanCompareBase, "Re-entry has no persistent variant");
        Console.WriteLine($"COMPARATOR_CASE_PASS {size}/{seed}: four exact histories, asynchronous and cached selections, official views/DAG/FINAL/ecology, resets, save compatibility");
    }
    private static void Layout(PangaeaMapData map)
    {
        foreach (var (width, height) in new[] { (1280, 720), (800, 600), (640, 480) })
        {
            var seen = new HashSet<UiAction>();
            float max = PangaeaLaboratoryLayout.MaximumScroll(width, height, compareBase: true);
            foreach (float scroll in new[] { 0, max / 2, max })
            {
                var controls = PangaeaLaboratoryLayout.ControlsViewport(width, height);
                foreach (var b in PangaeaLaboratoryLayout.HitButtons(width, height, scroll, true))
                {
                    seen.Add(b.Action);
                    Check(b.Bounds.X + b.Bounds.Width <= PangaeaLaboratoryLayout.Map(width, height).X &&
                        (b.Action == UiAction.Back || b.Bounds.Y >= controls.Y && b.Bounds.Y + b.Bounds.Height <= controls.Y + controls.Height), "Comparator hit regions never overlap map/inspector");
                }
                var options = PangaeaLaboratoryLayout.Dropdown(width, height, UiAction.LabBaseVariant, map, scroll);
                Check(options.Select(o => PangaeaLaboratoryLayout.Variant(o.Action)).SequenceEqual(Enum.GetValues<PangaeaBaseVariant>()), "Four required options in order");
                Check(options.All(b => b.Bounds.X + b.Bounds.Width <= PangaeaLaboratoryLayout.Map(width, height).X && b.Bounds.Y >= controls.Y && b.Bounds.Y + b.Bounds.Height <= controls.Y + controls.Height + .01), "Dropdown constrained to sidebar");
            }
            Check(seen.Contains(UiAction.LabBaseVariant) && PangaeaLaboratoryLayout.VisibleStages.All(s => seen.Contains(PangaeaLaboratoryLayout.StageAction(s))), "Comparator and every official action reachable by existing scrolling");
            Check(PangaeaLaboratoryLayout.Buttons(width, height, compareBase: true).Where(b => PangaeaLaboratoryLayout.IsStageAction(b.Action)).Select(b => b.Action)
                .SequenceEqual(PangaeaLaboratoryLayout.VisibleStages.Select(PangaeaLaboratoryLayout.StageAction)), "No extra DAG action or reordered official control");
        }
    }
    private static void Persist(GeneratedWorld world, string hash)
    {
        foreach (var terrain in new[] { world.Landmass, world.Landmass with { Climate = null, Biomes = null, SlopeByCellId = null } })
        {
            string path = Path.Combine(Path.GetTempPath(), "evolve-base-comparator-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                GameSaveStore.Save(path, new GameState { HexGrid = new(world.Grid.Definition), Landmass = terrain });
                Check(Hash(GameSaveStore.Load(path).Landmass) == Hash(terrain), "Current/historical saves remain compatible with no variant state");
                Check(!File.ReadAllText(path).Contains("BaseVariant", StringComparison.Ordinal), "No experimental selector persisted");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
        Check(Hash(world.Landmass) == hash, "Persistence never changes official state");
    }
    private static void Run(PangaeaLaboratory lab, PangaeaGenerationStage stage) { lab.Execute(stage); Wait(lab); Check(lab.Error is null, "Official stage completes: " + stage); }
    private static void Wait(PangaeaLaboratory lab)
    {
        var timer = Stopwatch.StartNew();
        while (lab.Busy || lab.ComparingBase) { lab.Update(); if (timer.Elapsed.TotalSeconds > 90) throw new TimeoutException("Laboratory/comparator worker timed out"); Thread.Sleep(1); }
        Check(lab.Error is null, "No worker/comparison error: " + lab.Error);
    }
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string HashBytes(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
