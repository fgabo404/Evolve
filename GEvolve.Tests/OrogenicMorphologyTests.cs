using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

internal static class OrogenicMorphologyTests
{
    internal static readonly int[] Seeds = [2026, 42, -731, 7, 99, 12345, 314159,
        -2026, 8675309, 0, 1, 123456789, -1358931540];
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string MotionHash(LandmassState terrain) =>
        Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(terrain.TectonicMotion)));

    internal static string[] ReferenceRows()
    {
        using var stream = typeof(OrogenicMorphologyTests).Assembly.GetManifestResourceStream("OrogenicInputBaseline.txt")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool Capture(GridSize size, int seed) =>
        seed == 42 && size != GridSize.Enormous ||
        size == GridSize.Enormous && seed is 2026 or -1358931540;

    public static void InspectAttempts()
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            var before = RebuildReference(LandmassProfiles.Create(GridSize.Enormous,
                LandmassType.Continents, -1358931540), attempt);
            var topology = new HexTopology(before.Grid);
            var after = new ContinentalElevationGenerator().Generate(topology, before.Landmass);
            Console.WriteLine($"ATTEMPT {attempt}");
            foreach (var s in after.ContinentalElevation!.Systems.Where(s => s.ContinuousCrestLength > 15)
                .OrderByDescending(s => s.ContinuousCrestLength))
                Console.WriteLine($" {s.Id} {s.Kind} {s.MountainTier}: length {s.ContinuousCrestLength}, width {s.EffectiveBeltWidth:F2}, " +
                    $"area {s.ConnectedMountainCells}, prominence {s.MeanCrestProminence:F1}, eligible {OrogenicRangeMetrics.IsPrincipal(topology, s)}");
        }
    }

    public static void Run()
    {
        int failed = 0, changed = 0, regenerated = 0;
        long enormousMs = 0, worstMs = 0;
        var references = ReferenceRows().Select(line => line.Split('|'))
            .ToDictionary(row => (Enum.Parse<GridSize>(row[0]), int.Parse(row[1])));
        var comparison = new List<string> { "size,seed,old_attempt,new_attempt,surface_changed,before_mountain_cells,after_mountain_cells,before_largest_connected,after_largest_connected,before_connected_extent,after_connected_extent,principal,maximum_crest,ms" };
        foreach (var size in Enum.GetValues<GridSize>())
        foreach (int seed in Seeds)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var world = new WorldGenerationService().Generate(
                    LandmassProfiles.Create(size, LandmassType.Continents, seed));
                var terrain = world.Landmass;
                var relief = terrain.ContinentalElevation!;
                long elapsed = watch.ElapsedMilliseconds;
                if (size == GridSize.Enormous) { enormousMs += elapsed; worstMs = Math.Max(worstMs, elapsed); }
                var topology = new HexTopology(world.Grid);
                string[] reference = references[(size, seed)];
                var before = RebuildReference(terrain.Configuration, int.Parse(reference[2]));
                Check(ReferenceIdentity(before.Landmass).SequenceEqual(reference.Skip(3)),
                    "The old candidate's surface, crust, groups, plate geometry or motion changed");
                Check(Hash(terrain.PlateByCellId!) == reference[4] && MotionHash(terrain) == reference[5],
                    "Selected world changed original plates or motion");
                bool surfaceChanged = Hash(terrain.SurfaceByCellId) != reference[3];
                if (surfaceChanged) changed++;
                Check(!surfaceChanged || terrain.ContinentalSurface!.Selection!.SelectedAttempt != int.Parse(reference[2]),
                    "Surface changed without a different selection attempt");
                // Replaying the selected candidate isolates the immutable surface algorithm
                // from the intentionally stronger mountain acceptance criteria.
                var replay = RebuildReference(terrain.Configuration,
                    terrain.ContinentalSurface!.Selection!.SelectedAttempt).Landmass;
                Check(ReferenceIdentity(replay).SequenceEqual(ReferenceIdentity(terrain)),
                    "Morphology changed the surface generator inputs or outputs");
                Check(replay.ContinentalElevation!.OceanDepthByCellId.SequenceEqual(relief.OceanDepthByCellId),
                    "Terrestrial morphology must retain ocean bathymetry for the same candidate");
                ValidateCrests(topology, terrain);
                int principal = relief.Systems.Count(s => s.MountainTier == TectonicMountainTier.Principal);
                Check(principal >= 1 && relief.Systems.Where(s => s.MountainTier == TectonicMountainTier.Principal)
                    .All(s => OrogenicRangeMetrics.IsPrincipal(topology, s)),
                    "Every accepted principal range must pass connected terrestrial metrics");
                Check(terrain.ContinentalSurface.Selection.Attempts[terrain.ContinentalSurface.Selection.SelectedAttempt].PrincipalSystems == principal,
                    "Selection must evaluate the actual displayed mountain ranges");
                if (seed is 2026 or -1358931540)
                {
                    var repeated = new WorldGenerationService().Generate(terrain.Configuration).Landmass;
                    Check(JsonSerializer.Serialize(repeated) == JsonSerializer.Serialize(terrain), "Full world determinism");
                    VerifyPersistence(world, before);
                }
                if (size == GridSize.Small && seed == 2026) VerifyRejectedFragments(topology);
                var oldStats = MountainSummary(topology, before.Landmass);
                var newStats = MountainSummary(topology, terrain);
                comparison.Add($"{size},{seed},{reference[2]},{terrain.ContinentalSurface.Selection.SelectedAttempt},{surfaceChanged}," +
                    $"{oldStats.Count},{newStats.Count},{oldStats.Largest},{newStats.Largest},{oldStats.Extent},{newStats.Extent},{principal}," +
                    $"{relief.Systems.Max(s => s.ContinuousCrestLength)},{elapsed}");
                Console.WriteLine($"MORPHOLOGY {size}/{seed}: attempt {terrain.ContinentalSurface!.Selection!.SelectedAttempt}, " +
                    $"{elapsed} ms, mountains {oldStats.Count}->{newStats.Count}, connected extent {oldStats.Extent}->{newStats.Extent}, surface changed {surfaceChanged}");
                foreach (var s in relief.Systems.Where(s => s.MountainTier == TectonicMountainTier.Principal))
                    Console.WriteLine($"  RANGE {s.Id} {s.Kind}: tectonic {s.LengthHexEdges}, crest {s.ContinuousCrestLength}, " +
                        $"connected {s.ConnectedMountainCells}, width {s.EffectiveBeltWidth:F1}, prominence {s.MeanCrestProminence:F1}, max {s.MaximumLandElevation}");
                if (Capture(size, seed))
                {
                    ContinentalElevationDiagnostics.Export($"orogenic-before-{size}-{seed}.svg", before);
                    ContinentalElevationDiagnostics.Export($"orogenic-after-{size}-{seed}.svg", world);
                    ContinentalElevationDiagnostics.ExportBitmap($"orogenic-after-{size}-{seed}.bmp", world);
                }
                regenerated++;
            }
            catch (Exception error) { failed++; Console.WriteLine($"MORPHOLOGY FAIL {size}/{seed}: {error.Message}"); }
        }
        File.WriteAllLines("orogenic-comparison.csv", comparison);
        Console.WriteLine($"OROGENY: {regenerated}/52 worlds verified, {changed}/52 selected surfaces changed, " +
            $"all 52 original candidates replayed unchanged; Enormous mean {enormousMs / 13} ms, max {worstMs} ms");
        if (failed > 0) throw new Exception($"{failed} morphology worlds failed");
    }

    internal static GeneratedWorld RebuildReference(LandmassConfiguration config, int attempt)
    {
        var preview = new WorldGenerationService().GeneratePlatePreview(config);
        var topology = new HexTopology(preview.Grid);
        var original = preview.Landmass;
        var opportunity = OrogenicOpportunityAnalyzer.Analyze(topology, original.TectonicMotion!);
        var generated = new PlateGroupSurfaceGenerator().Generate(topology, original.PlateByCellId!,
            original.TectonicMotion!, config.Seed, attempt, opportunity);
        var terrain = new LandmassState(config, generated.Surface, 0)
        {
            PlateByCellId = original.PlateByCellId, TectonicMotion = original.TectonicMotion,
            ContinentalSurface = generated.State
        };
        terrain = new ContinentalElevationGenerator().Generate(topology, terrain, useOrogenicMorphology: false);
        return new(preview.Grid, terrain, LandmassAnalysis.Analyze(topology, generated.Surface));
    }

    internal static string[] ReferenceIdentity(LandmassState t) => [Hash(t.SurfaceByCellId),
        Hash(t.PlateByCellId!), MotionHash(t), Hash(t.ContinentalSurface!.GroupByPlateId),
        Hash(t.ContinentalSurface.CrustByCellId), Hash(t.ContinentalSurface.ContinentByCellId)];

    private static void ValidateCrests(HexTopology topology, LandmassState terrain)
    {
        foreach (var system in terrain.ContinentalElevation!.Systems)
        {
            Check(system.CrestCellIds.Length == system.CrestSourceCellIds.Length, "Crest provenance is complete");
            for (int i = 0; i < system.CrestCellIds.Length; i++)
            {
                int id = system.CrestCellIds[i], source = system.CrestSourceCellIds[i];
                Check(terrain.SurfaceByCellId[id] == 1 &&
                    terrain.ContinentalSurface!.CrustByCellId[id] == (byte)CrustKind.Continental &&
                    system.AxisCellIds.Contains(source) &&
                    topology.Distance(id, source) <= Math.Ceiling(system.WidthHexes * 2.8),
                    "Every terrestrial crest must originate in the finite tectonic deformation zone");
                Check(system.Kind == GeologicalContactKind.ContinentalCollision
                    ? system.ParticipatingPlateIds.Contains(terrain.PlateByCellId![id])
                    : system.Kind == GeologicalContactKind.OceanContinentSubduction &&
                        terrain.PlateByCellId![id] == system.OverridingPlateId,
                    "Collision and subduction must deform their proper continental blocks");
            }
            for (int i = 0; i < system.MainCrestCellIds.Length; i++)
            {
                int id = system.MainCrestCellIds[i];
                Check(terrain.ElevationByCellId![id] >= 44 && system.CrestCellIds.Contains(id),
                    "Effective crest consists of actual elevated land");
                if (i > 0) Check(topology.Neighbors(id).Contains(system.MainCrestCellIds[i - 1]),
                    "Every step in the effective crest is a real hex neighbour, never a north-south wrap");
            }
        }
        for (int id = 0; id < topology.Count; id++)
            Check(terrain.SurfaceByCellId[id] == 0 ? terrain.ElevationByCellId![id] == 0 :
                terrain.ElevationByCellId![id] is >= 1 and <= 100, "Land and ocean height contract");
    }

    private static (int Count, int Largest, int Extent) MountainSummary(HexTopology topology, LandmassState terrain)
    {
        var cells = Enumerable.Range(0, topology.Count).Where(id => terrain.ElevationByCellId![id] >= 44).ToHashSet();
        int total = cells.Count, largest = 0, extent = 0;
        while (cells.Count > 0)
        {
            int start = cells.Min();
            var component = new HashSet<int> { start }; cells.Remove(start);
            var queue = new Queue<int>(); queue.Enqueue(start);
            int last = start;
            while (queue.Count > 0)
            {
                last = queue.Dequeue();
                foreach (int next in topology.Neighbors(last))
                    if (cells.Remove(next)) { component.Add(next); queue.Enqueue(next); }
            }
            var distances = new Dictionary<int, int> { [last] = 0 }; queue.Enqueue(last);
            int diameter = 0;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue(); diameter = Math.Max(diameter, distances[id]);
                foreach (int next in topology.Neighbors(id))
                    if (component.Contains(next) && distances.TryAdd(next, distances[id] + 1)) queue.Enqueue(next);
            }
            largest = Math.Max(largest, component.Count); extent = Math.Max(extent, diameter);
        }
        return (total, largest, extent);
    }

    private static void VerifyRejectedFragments(HexTopology topology)
    {
        var heights = new byte[topology.Count];
        int[] isolated = Enumerable.Range(0, 50).Select(i => topology.Id(i * 3, 20)).ToArray();
        foreach (int id in isolated) heights[id] = 90;
        var system = new TectonicReliefSystem(0, GeologicalContactKind.ContinentalCollision,
            0, 1, byte.MaxValue, [0], isolated, 8, 8) { CrestCellIds = isolated };
        var measured = OrogenicRangeMetrics.Measure(topology, system, heights, isolated, [0]);
        Check(measured.ContinuousCrestLength == 0 && !OrogenicRangeMetrics.IsPrincipal(topology, measured),
            "Many isolated summits cannot qualify as a principal range");
        int[] seam = Enumerable.Range(-20, 41).Select(x => topology.Id(x, 40)).ToArray();
        foreach (int id in seam) heights[id] = 60;
        measured = OrogenicRangeMetrics.Measure(topology, system with { CrestCellIds = seam }, heights, seam, [0]);
        Check(measured.ContinuousCrestLength == 40, "Crest continuity must cross the horizontal seam");
        int[] poles = [topology.Id(40, 0), topology.Id(40, topology.Rows - 1)];
        foreach (int id in poles) heights[id] = 90;
        measured = OrogenicRangeMetrics.Measure(topology, system with { CrestCellIds = poles }, heights, poles, [0]);
        Check(measured.ContinuousCrestLength == 0, "North and south boundaries must not join");
    }

    private static void VerifyPersistence(GeneratedWorld world, GeneratedWorld previous)
    {
        string path = Path.Combine(Path.GetTempPath(), $"orogenic-{Guid.NewGuid():N}.json");
        try
        {
            var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = world.Landmass };
            GameSaveStore.Save(path, state);
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(JsonSerializer.Serialize(loaded) == JsonSerializer.Serialize(world.Landmass),
                "Crest geometry, attribution and metrics must survive persistence");
            state.Landmass = previous.Landmass;
            GameSaveStore.Save(path, state);
            var oldJson = JsonNode.Parse(File.ReadAllText(path))!;
            foreach (var system in oldJson["Landmass"]!["ContinentalElevation"]!["Systems"]!.AsArray())
                foreach (string property in new[] { "CrestCellIds", "CrestSourceCellIds", "MainCrestCellIds",
                    "ContinuousCrestLength", "ConnectedMountainCells", "EffectiveBeltWidth", "MeanCrestProminence" })
                    system!.AsObject().Remove(property);
            File.WriteAllText(path, oldJson.ToJsonString());
            loaded = GameSaveStore.Load(path).Landmass!;
            Check(loaded.ContinentalElevation!.Version == "continental-elevation-5" &&
                loaded.ContinentalElevation.Systems.All(s => s.CrestCellIds.Length == 0),
                "Version 5 saves remain readable without fabricated crests");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
