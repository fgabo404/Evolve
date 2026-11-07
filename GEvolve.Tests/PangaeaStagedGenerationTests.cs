using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.Grid;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class PangaeaStagedGenerationTests
{
    private sealed record Reference(string Size, string Type, int Seed, int Attempt, string Grid,
        string State, string Statistics, string Surface, string Plates, string Motion,
        string? Base, string? Contribution, string? Final, string? Systems);

    private static IEnumerable<(GridSize Size, LandmassType Type, int Seed)> Cases()
    {
        foreach (var size in Enum.GetValues<GridSize>())
        {
            foreach (int seed in new[] { -1387338765, -132790047, -714216669, -2024691550,
                -174327693, -1979952984, 2076374193 })
                yield return (size, LandmassType.Pangaea, seed);
            foreach (var type in new[] { LandmassType.Continents, LandmassType.Archipelagos, LandmassType.SmallContinents })
                yield return (size, type, 2026);
        }
    }

    public static void Run()
    {
        using var stream = typeof(PangaeaStagedGenerationTests).Assembly.GetManifestResourceStream("PangaeaStagedReference.txt")
            ?? throw new Exception("Missing independent pre-staging reference");
        using var reader = new StreamReader(stream);
        var references = new Dictionary<string, Reference>();
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
            var reference = JsonSerializer.Deserialize<Reference>(line)!;
            references.Add($"{reference.Size}/{reference.Type}/{reference.Seed}", reference);
        }
        Check(references.Count == 40, "All 40 independent reference worlds are required");
        foreach (var (size, type, seed) in Cases())
        {
            var timer = Stopwatch.StartNew();
            var world = new WorldGenerationService().Generate(size, type, seed);
            timer.Stop();
            var expected = references[$"{size}/{type}/{seed}"];
            Check(Signature(world) == expected, $"Automatic pipeline changed from pre-refactor reference: {size}/{type}/{seed}");
            if (type != LandmassType.Pangaea)
            {
                Console.WriteLine($"STAGED_OTHER_PASS {size}/{type}/{seed}: fullState={expected.State}");
                continue;
            }
            var session = new WorldGenerationService().CreatePangaeaSession(world.Landmass.Configuration);
            Check(session.GetStages().Count(s => s.CanExecute) == 1 && session.GetSnapshot().Grid is null,
                "Fresh session has only Grid available and no fabricated data");
            Expect<InvalidOperationException>(() => session.Execute(PangaeaGenerationStage.Final));
            Expect<InvalidOperationException>(() => session.GetCompletedWorld());
            session.Execute(PangaeaGenerationStage.Grid);
            session.Execute(PangaeaGenerationStage.Plates);
            session.Execute(PangaeaGenerationStage.Surface); // Actual dependency: no H04 required.
            var surface = session.GetSnapshot();
            Check(surface.Motion is null && surface.Base is null && surface.Elevations is null && surface.Mountains is null,
                "Surface stage cannot pretend to have calculated H04, BASE or mountains");
            Check(surface.SelectedAttempt == expected.Attempt && surface.Attempts.Count == expected.Attempt + 1 &&
                surface.Attempts[^1].Accepted && surface.Attempts.Take(surface.Attempts.Count - 1).All(a => !a.Accepted),
                "First valid attempt, rejection history and original profile are preserved");
            foreach (var attempt in surface.Attempts)
                Check(attempt.Accepted == LandmassProfileValidator.MeetsProfile(type, attempt.Statistics), "Recorded acceptance is the real profile predicate");
            Expect<InvalidOperationException>(() => session.Execute(PangaeaGenerationStage.MountainSystems));
            session.Execute(PangaeaGenerationStage.Motion);
            session.Execute(PangaeaGenerationStage.MountainSystems); // Demonstrates independence from BASE.
            var selected = session.GetSnapshot();
            Check(selected.Base is null && selected.Elevations is null && selected.Mountains is not null,
                "Selection and influence exist without calculating any height");
            Check(selected.Mountains!.InfluenceByCellId.SequenceEqual(world.Landmass.PangaeaMountainRanges!.RangeByCellId),
                "Pre-BASE influence exactly matches automatic composition");
            Check(selected.Mountains.CandidateDecisions.GetValueOrDefault(MountainCandidateDecision.Selected) ==
                world.Landmass.PangaeaMountainRanges.Ranges.Length, "Actual selection diagnostics are retained");
            long selectionRevision = Info(session, PangaeaGenerationStage.MountainSystems).Revision;
            session.Execute(PangaeaGenerationStage.Base);
            Check(Info(session, PangaeaGenerationStage.MountainSystems).Revision == selectionRevision,
                "BASE does not invalidate independent mountain geometry");
            Expect<InvalidOperationException>(() => session.GetCompletedWorld());
            session.Execute(PangaeaGenerationStage.Final);
            foreach (var info in session.GetStages())
                Check(info.Status == PangaeaStageStatus.Completed && info.Hash?.Length == 64 && info.Duration >= TimeSpan.Zero &&
                    info.InputHashes.All(entry => entry.Value == Info(session, entry.Key).Hash),
                    "Stage evidence records actual completion, time and dependency hashes");
            AssertEqual(world, session.GetCompletedWorld(), $"automatic/manual {size}/{seed}");
            Console.WriteLine("STAGED_EQ " + JsonSerializer.Serialize(Signature(session.GetCompletedWorld())));
            Console.WriteLine("STAGED_STEPS " + JsonSerializer.Serialize(new { Size = size.ToString(), Seed = seed,
                Stages = session.GetStages(), Samples = selected.Mountains.PropagationSamples,
                Attempts = surface.Attempts, AutomaticMs = timer.Elapsed.TotalMilliseconds }));
            VerifyBaseRepeat(session, world);
            if (seed == -1387338765) VerifyInvalidationGraph(session, world);
            Console.WriteLine($"STAGED_CASE_PASS {size}/{seed}: automatic=manual=original; BASE repeat preserves geometry; fullState={expected.State}");
        }
        VerifyIsolationAndAsync().GetAwaiter().GetResult();
        Console.WriteLine("STAGED_PASS: 28 Pangaea acceptance worlds across four sizes + 12 unchanged other-type controls; exact original/automatic/manual equivalence, invalidation, persistence, snapshot isolation and async reset/cancellation");
    }

    private static void VerifyBaseRepeat(PangaeaGenerationSession session, GeneratedWorld expected)
    {
        var before = session.GetStages().ToDictionary(s => s.Stage);
        session.Execute(PangaeaGenerationStage.Base);
        foreach (var stage in new[] { PangaeaGenerationStage.Grid, PangaeaGenerationStage.Plates,
            PangaeaGenerationStage.Motion, PangaeaGenerationStage.Surface, PangaeaGenerationStage.MountainSystems })
            Check(Info(session, stage).Revision == before[stage].Revision && Info(session, stage).Hash == before[stage].Hash,
                $"Regenerating BASE unnecessarily changed {stage}");
        Check(Info(session, PangaeaGenerationStage.Final).Status == PangaeaStageStatus.Invalidated && session.GetSnapshot().Elevations is null,
            "Previous FINAL must disappear when BASE is regenerated");
        Expect<InvalidOperationException>(() => session.GetCompletedWorld());
        session.Execute(PangaeaGenerationStage.Final);
        Check(Info(session, PangaeaGenerationStage.Final).InputRevisions[PangaeaGenerationStage.Base] == Info(session, PangaeaGenerationStage.Base).Revision &&
            Info(session, PangaeaGenerationStage.Final).InputRevisions[PangaeaGenerationStage.MountainSystems] == before[PangaeaGenerationStage.MountainSystems].Revision,
            "FINAL consumes the new BASE and the retained mountain plan");
        AssertEqual(expected, session.GetCompletedWorld(), "repeat BASE/FINAL");
        Console.WriteLine($"STAGED_INVALIDATION {expected.Grid.Definition.Size}/{expected.Landmass.Configuration.Seed}: BASE -> FINAL invalidated; upstream revisions and selection preserved; rebuilt FINAL identical");
    }

    private static void VerifyInvalidationGraph(PangaeaGenerationSession session, GeneratedWorld expected)
    {
        var baseInfo = Info(session, PangaeaGenerationStage.Base);
        var surfaceInfo = Info(session, PangaeaGenerationStage.Surface);
        session.Execute(PangaeaGenerationStage.Motion);
        Check(Info(session, PangaeaGenerationStage.Base).Revision == baseInfo.Revision &&
            Info(session, PangaeaGenerationStage.Surface).Revision == surfaceInfo.Revision &&
            session.GetSnapshot().Mountains is null && session.GetSnapshot().Elevations is null,
            "H04 invalidates systems/FINAL but retains independent surface/BASE");
        Complete(session);
        AssertEqual(expected, session.GetCompletedWorld(), "repeat H04");
        var motion = Info(session, PangaeaGenerationStage.Motion);
        session.Execute(PangaeaGenerationStage.Surface);
        var snapshot = session.GetSnapshot();
        Check(Info(session, PangaeaGenerationStage.Motion).Revision == motion.Revision &&
            snapshot.Base is null && snapshot.BaseSources is null && snapshot.Mountains is null && snapshot.Elevations is null,
            "Surface invalidates BASE/systems/FINAL only");
        Complete(session);
        AssertEqual(expected, session.GetCompletedWorld(), "repeat surface");
        long grid = Info(session, PangaeaGenerationStage.Grid).Revision;
        session.Execute(PangaeaGenerationStage.Plates);
        snapshot = session.GetSnapshot();
        Check(Info(session, PangaeaGenerationStage.Grid).Revision == grid && snapshot.Motion is null && snapshot.Surface is null &&
            snapshot.SelectedAttempt is null && snapshot.Attempts.Count == 0 && snapshot.SurfaceStatistics is null &&
            snapshot.Base is null && snapshot.Mountains is null && snapshot.Elevations is null,
            "Plates invalidate every dependent field including attempts and diagnostics");
        Complete(session);
        AssertEqual(expected, session.GetCompletedWorld(), "repeat plates");
        session.Execute(PangaeaGenerationStage.Grid);
        snapshot = session.GetSnapshot();
        Check(snapshot.Plates is null && snapshot.Motion is null && snapshot.Surface is null && snapshot.Base is null &&
            snapshot.Mountains is null && snapshot.Elevations is null, "Grid invalidates all downstream data");
        Complete(session);
        AssertEqual(expected, session.GetCompletedWorld(), "repeat grid");
        string path = Path.Combine(Path.GetTempPath(), $"evolve-staged-{Guid.NewGuid():N}.json");
        try
        {
            var completed = session.GetCompletedWorld();
            GameSaveStore.Save(path, new GameState { HexGrid = new HexGridState(completed.Grid.Definition), Landmass = completed.Landmass });
            Check(Hash(GameSaveStore.Load(path).Landmass) == Hash(expected.Landmass), "Completed manual world uses the unchanged save contract");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
        Console.WriteLine($"STAGED_GRAPH_PASS {expected.Grid.Definition.Size}: H04, surface, plates, grid transitive invalidations and save/load");
    }

    private static async Task VerifyIsolationAndAsync()
    {
        var service = new WorldGenerationService();
        var configuration = LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, -1387338765);
        var session = service.CreatePangaeaSession(configuration);
        Complete(session);
        var expected = session.GetCompletedWorld();
        var view = session.GetSnapshot();
        view.Plates![0] = 99; view.Motion!.CompressionByCellId[0] = -999;
        view.Surface![0] = 99; view.Base![0] = 99; view.BaseSources!.Quantiles[0] = -99;
        view.Mountains!.InfluenceByCellId[0] = ushort.MaxValue;
        if (view.Mountains.Systems.Length > 0) view.Mountains.Systems[0].AxisCellIds[0] = -1;
        ((GridCell[])view.Grid!.Cells)[0] = new GridCell(999, default, default, default);
        var export = session.GetCompletedWorld();
        export.Landmass.SurfaceByCellId[0] = 99;
        export.Landmass.PangaeaMountainRanges!.RangeByCellId[0] = ushort.MaxValue;
        export.PangaeaElevations!.Sources!.Quantiles[0] = -99;
        if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(export.PangaeaElevations.FinalByCellId, out var segment))
            segment.Array![segment.Offset] = 99;
        AssertEqual(expected, session.GetCompletedWorld(), "Snapshot/export mutations cannot corrupt the session");
        Check(session.GetCompletedWorld().Grid.Cells[0].Id == 0, "Grid snapshot storage is detached");
        Check(session.GetSnapshot().BaseSources!.Quantiles[0] >= 0 && session.GetSnapshot().Motion!.CompressionByCellId[0] >= 0,
            "Intermediate diagnostic sources are also isolated");
        Expect<ArgumentException>(() => session.Reset(configuration with { Pangaea = configuration.Pangaea! with { TargetLandFraction = .9 } }));
        Expect<ArgumentException>(() => service.CreatePangaeaSession(LandmassProfiles.Create(GridSize.Small, LandmassType.Archipelagos, 42)));
        Expect<ArgumentOutOfRangeException>(() => session.Execute((PangaeaGenerationStage)999));
        using (var alreadyCancelled = new CancellationTokenSource())
        {
            alreadyCancelled.Cancel();
            Expect<OperationCanceledException>(() => session.Execute(PangaeaGenerationStage.Grid, alreadyCancelled.Token));
            AssertEqual(expected, session.GetCompletedWorld(), "Pre-cancelled work cannot invalidate a current world");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            var running = session.ExecuteAsync(PangaeaGenerationStage.Surface, cancellation.Token);
            Check(session.GetSnapshot().Elevations is null && session.GetSnapshot().Base is null,
                "Starting a repeat immediately hides previous dependent data");
            cancellation.Cancel();
            try { await running; } catch (OperationCanceledException) { }
            Check(Info(session, PangaeaGenerationStage.Surface).Status is PangaeaStageStatus.Invalidated or PangaeaStageStatus.Completed,
                "Cancellation is settled without a running/stale state (completion before cancellation is allowed)");
            Complete(session);
        }
        var oldTask = session.ExecuteAsync(PangaeaGenerationStage.Surface);
        var next = LandmassProfiles.Create(GridSize.Medium, LandmassType.Pangaea, -132790047);
        session.Reset(next);
        // New work may proceed while the obsolete calculation finishes; it cannot publish.
        await session.ExecuteAsync(PangaeaGenerationStage.Grid);
        try { await oldTask; } catch (OperationCanceledException) { }
        view = session.GetSnapshot();
        Check(view.Configuration == next && view.ConfigurationRevision == 2 && view.Grid!.Definition.Size == GridSize.Medium &&
            view.Surface is null && view.Plates is null && view.Elevations is null &&
            Info(session, PangaeaGenerationStage.Surface).Status == PangaeaStageStatus.Pending,
            "Old work cannot publish fields or errors into a reset configuration");
        Complete(session);
        AssertEqual(service.Generate(next), session.GetCompletedWorld(), "new configuration after overlapping reset");
        Console.WriteLine("STAGED_ASYNC_PASS: detached snapshots/exports; invalid requests atomic; cancellation; seed+size reset; obsolete results discarded; async stages complete");
    }

    private static void Complete(PangaeaGenerationSession session)
    {
        foreach (var stage in Enum.GetValues<PangaeaGenerationStage>())
            if (Info(session, stage).Status != PangaeaStageStatus.Completed) session.Execute(stage);
    }
    private static PangaeaStageInfo Info(PangaeaGenerationSession session, PangaeaGenerationStage stage) => session.GetStages().Single(s => s.Stage == stage);
    private static void AssertEqual(GeneratedWorld expected, GeneratedWorld actual, string context)
    {
        Check(Signature(expected) == Signature(actual), $"Field hashes differ: {context}");
        Check(JsonSerializer.SerializeToUtf8Bytes(expected.Landmass).AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(actual.Landmass)) &&
            expected.PangaeaElevations!.BaseByCellId.Span.SequenceEqual(actual.PangaeaElevations!.BaseByCellId.Span) &&
            expected.PangaeaElevations.ContributionByCellId.Span.SequenceEqual(actual.PangaeaElevations.ContributionByCellId.Span) &&
            expected.PangaeaElevations.FinalByCellId.Span.SequenceEqual(actual.PangaeaElevations.FinalByCellId.Span),
            $"Exact state or diagnostic bytes differ: {context}");
    }
    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }

    private static Reference Signature(GeneratedWorld world)
    {
        var terrain = world.Landmass;
        var fields = world.PangaeaElevations;
        return new(terrain.Configuration.Size.ToString(), terrain.Configuration.Type.ToString(),
            terrain.Configuration.Seed, terrain.DeterministicAttempt, Hash(world.Grid.Definition),
            Hash(terrain), Hash(world.Statistics), Hash(terrain.SurfaceByCellId), Hash(terrain.PlateByCellId!),
            Hash(terrain.TectonicMotion), fields is null ? null : Hash(fields.BaseByCellId.ToArray()),
            fields is null ? null : Hash(fields.ContributionByCellId.ToArray()),
            terrain.ElevationByCellId is null ? null : Hash(terrain.ElevationByCellId),
            terrain.PangaeaMountainRanges is null ? null : Hash(terrain.PangaeaMountainRanges));
    }

    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
