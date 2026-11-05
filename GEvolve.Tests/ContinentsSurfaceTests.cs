using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Game;
using GEvolve.Game.Graphics;
using GEvolve.Game.Screens;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class ContinentsSurfaceTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        var generator = new ContinentsSurfaceGenerator();
        ExpectArgument(() => service.GenerateContinentsSurfacePreview(
            LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, 2026)));
        foreach (GridSize size in Enum.GetValues<GridSize>())
        foreach (int seed in new[] { 2026, 42, -731 })
        {
            var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var world = service.GenerateContinentsSurfacePreview(config);
            timer.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            var terrain = world.Landmass;
            var topology = new HexTopology(world.Grid);
            var plates = terrain.PlateByCellId!;
            var motion = terrain.TectonicMotion!;
            Check(terrain.HasSurface && !terrain.SurfaceGenerationPending && !terrain.TectonicPreviewOnly &&
                terrain.GenerationStage == WorldGenerationStage.SurfaceAvailable &&
                terrain.SharedCortex is not null && terrain.ElevationByCellId is null &&
                terrain.Climate is null && terrain.Biomes is null && terrain.SlopeByCellId is null,
                $"Surface-only preview state: {size}/{seed}");
            byte[] surface = terrain.SurfaceByCellId;
            Check(surface.Length == topology.Count && surface.All(value => value is 0 or 1) &&
                surface.Contains((byte)0) && surface.Contains((byte)1),
                $"Complete land/ocean surface: {size}/{seed}");
            var cortex = SharedCorticalPersistence.FromState(terrain.SharedCortex!);
            var assembly = new ContinentsCorticalAssemblyGenerator().Assemble(topology, config, plates, motion, cortex);
            string inputs = JsonSerializer.Serialize(new { Plates = plates, Motion = motion, Assembly = assembly });
            Check(generator.Generate(topology, config, plates, motion, assembly).SequenceEqual(surface),
                $"Service composes the specialized surface generator: {size}/{seed}");
            Check(inputs == JsonSerializer.Serialize(new { Plates = plates, Motion = motion, Assembly = assembly }),
                $"Emergence never mutates tectonics, cortex or assembly: {size}/{seed}");
            Check(JsonSerializer.Serialize(terrain) ==
                JsonSerializer.Serialize(service.GenerateContinentsSurfacePreview(config).Landmass),
                $"Complete preview is deterministic: {size}/{seed}");
            int continental = 0, submerged = 0;
            for (int id = 0; id < topology.Count; id++)
            {
                if (cortex.CrustByCellId[id] == 1)
                {
                    continental++;
                    if (surface[id] == 0) submerged++;
                }
                else Check(surface[id] == 0, "B3 never invents land outside continental cortex");
                Check(MapViewColors.ForCell(terrain, id, MapViewMode.Surface) ==
                    (surface[id] == 1 ? MapViewColors.SurfaceLandColor : MapViewColors.SurfaceOceanColor),
                    "Surface colors need no elevation or biome assignment");
            }
            Check(submerged > 0 && submerged < continental,
                $"Continental cortex is partly submerged: {size}/{seed}");
            var land = Components(topology, surface, 1);
            var ocean = Components(topology, surface, 0);
            Check(world.Statistics is { } stats && stats.LandCells == land.Sizes.Sum() &&
                stats.ComponentSizes.SequenceEqual(land.Sizes), "Surface statistics match independent BFS");
            for (int row = 0; row < topology.Rows; row++)
            {
                int west = topology.Id(0, row), east = topology.Id(topology.Columns - 1, row);
                Check(topology.Neighbors(west).Contains(east) && topology.Neighbors(east).Contains(west),
                    "Preview retains both horizontal seam directions");
                if (surface[west] == surface[east])
                {
                    int[] labels = surface[west] == 1 ? land.Labels : ocean.Labels;
                    Check(labels[west] == labels[east], "Components join across the real seam");
                }
            }
            Check(!topology.Neighbors(topology.Id(0, 0)).Contains(topology.Id(0, topology.Rows - 1)),
                "Preview never wraps north/south");
            string beforeViews = JsonSerializer.Serialize(terrain);
            foreach (var mode in new[] { MapViewMode.Surface, MapViewMode.Tectonic,
                MapViewMode.Compression, MapViewMode.Extension, MapViewMode.Shear })
            {
                Vector3 color = MapViewColors.ForCell(terrain, 0, mode);
                Check(float.IsFinite(color.X) && float.IsFinite(color.Y) && float.IsFinite(color.Z),
                    "Available preview layers have finite colors");
            }
            Check(beforeViews == JsonSerializer.Serialize(terrain), "Viewing layers does not regenerate the preview");
            VerifyPersistence(world);
            if (size == GridSize.Small && seed == 2026)
            {
                VerifyContacts(world);
                VerifyUi(world);
                ExpectArgument(() => generator.Generate(topology, config, Array.Empty<byte>(), motion, assembly));
                var invalidAffinity = (float[])cortex.EmergenceAffinityByCellId.Clone();
                invalidAffinity[0] = float.NaN;
                ExpectArgument(() => generator.Generate(topology, config, plates, motion,
                    assembly with { Model = cortex with { EmergenceAffinityByCellId = invalidAffinity } }));
            }
            string main = string.Join(",", land.Sizes.Take(4).Select(value => $"{100.0 * value / land.Sizes.Sum():F1}%"));
            Console.WriteLine($"H05-B3 {size}/{seed}: land={100.0 * land.Sizes.Sum() / topology.Count:F1}%, " +
                $"masses={land.Sizes.Length}, mainLandShares=[{main}], oceans={ocean.Sizes.Length}, " +
                $"largestOcean={100.0 * ocean.Sizes[0] / ocean.Sizes.Sum():F1}%, " +
                $"submergedCortex={100.0 * submerged / continental:F1}%, " +
                $"generationMs={timer.ElapsedMilliseconds}, allocatedMiB={allocated / 1048576.0:F1}");
        }
        Console.WriteLine("PASS: H05-B3 preliminary surface, topology, persistence and palette; visual audit pending");
    }

    private static void VerifyContacts(GeneratedWorld world)
    {
        var topology = new HexTopology(world.Grid);
        var config = world.Landmass.Configuration;
        var plates = world.Landmass.PlateByCellId!;
        var motion = world.Landmass.TectonicMotion!;
        var generator = new ContinentsSurfaceGenerator();
        var assembler = new ContinentsCorticalAssemblyGenerator();
        foreach (var kind in new[] { CorticalAssemblyKind.Accreted, CorticalAssemblyKind.RiftSeparated })
        {
            var edge = motion.Interactions.First(e => kind == CorticalAssemblyKind.Accreted
                ? e.Convergence >= .5 && e.Convergence > e.Divergence && e.Convergence >= .4 * e.Shear
                : e.Divergence >= .5 && e.Divergence > e.Convergence && e.Divergence >= .4 * e.Shear);
            // Singleton provinces at a real contact isolate the local emergence effect.
            var cortex = TwoCores(topology.Count, edge.FirstCellId, edge.SecondCellId,
                kind == CorticalAssemblyKind.Accreted ? .55f : .65f);
            var assembly = assembler.Assemble(topology, config, plates, motion, cortex);
            var neutral = generator.Generate(topology, config, plates, motion, WithoutEvidence(assembly));
            var influenced = generator.Generate(topology, config, plates, motion, assembly);
            foreach (int id in new[] { edge.FirstCellId, edge.SecondCellId })
                Check(kind == CorticalAssemblyKind.Accreted
                    ? neutral[id] == 0 && influenced[id] == 1
                    : neutral[id] == 1 && influenced[id] == 0,
                    "Real accretion can favor emergence; real separation can submerge cortex");
            Check(assembly.Evidence.Length == 1 && assembly.Evidence[0].DirectContact,
                "Controlled case has one real direct contact");
            ExpectArgument(() => generator.Generate(topology, config, plates, motion,
                assembly with { Evidence = [assembly.Evidence[0] with { OceanGapHexes = 1 }] }));
            ExpectArgument(() => generator.Generate(topology, config, plates, motion,
                assembly with { Evidence = [assembly.Evidence[0] with { InteractionIndex = -1 }] }));
        }
        // Synthetic land across the seam connects; equally separated polar cells do not.
        int row = topology.Rows / 2;
        foreach (bool seam in new[] { true, false })
        {
            int first = topology.Id(0, seam ? row : 0);
            int second = seam ? topology.Id(topology.Columns - 1, row) : topology.Id(0, topology.Rows - 1);
            var model = TwoCores(topology.Count, first, second, .9f);
            var assembly = WithoutEvidence(assembler.Assemble(topology, config, plates, motion, model));
            byte[] surface = generator.Generate(topology, config, plates, motion, assembly);
            Check(surface.Count(v => v == 1) == 2 && Components(topology, surface, 1).Sizes.Length == (seam ? 1 : 2),
                "Emergence and components respect horizontal seam and open poles");
        }
    }

    private static ContinentsCorticalAssemblyResult WithoutEvidence(ContinentsCorticalAssemblyResult result)
        => result with { Model = result.Model with { AssemblyLinks = [] }, Evidence = [] };

    private static SharedCorticalModel TwoCores(int count, int first, int second, float affinity)
    {
        var crust = new byte[count];
        var fragments = new int[count];
        var provinces = new int[count];
        var potential = new float[count];
        Array.Fill(fragments, -1); Array.Fill(provinces, -1); Array.Fill(potential, -1);
        crust[first] = crust[second] = 1;
        fragments[first] = provinces[first] = 0;
        fragments[second] = provinces[second] = 1;
        potential[first] = potential[second] = affinity;
        return new SharedCorticalModel(SharedCorticalModel.CurrentVersion, crust, fragments, provinces, potential,
            [new(0, first), new(1, second)], [new(0, 0, first), new(1, 1, second)], []);
    }

    private static (int[] Sizes, int[] Labels) Components(HexTopology topology, byte[] surface, byte value)
    {
        var labels = new int[topology.Count];
        Array.Fill(labels, -1);
        var queue = new int[topology.Count];
        var sizes = new List<int>();
        for (int start = 0; start < topology.Count; start++)
        {
            if (surface[start] != value || labels[start] >= 0) continue;
            int head = 0, tail = 1;
            queue[0] = start; labels[start] = sizes.Count;
            while (head < tail)
                foreach (int neighbor in topology.Neighbors(queue[head++]))
                    if (surface[neighbor] == value && labels[neighbor] < 0)
                    {
                        labels[neighbor] = sizes.Count;
                        queue[tail++] = neighbor;
                    }
            sizes.Add(tail);
        }
        return (sizes.OrderDescending().ToArray(), labels);
    }

    private static void VerifyPersistence(GeneratedWorld world)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evolve-surface-{Guid.NewGuid():N}.json");
        try
        {
            GameSaveStore.Save(path, new GameState
            {
                HexGrid = new HexGridState(world.Grid.Definition), Landmass = world.Landmass
            });
            var loaded = GameSaveStore.Load(path).Landmass!;
            Check(JsonSerializer.Serialize(world.Landmass) == JsonSerializer.Serialize(loaded),
                "Save/load preserves complete surface and cortex without regeneration");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void VerifyUi(GeneratedWorld world)
    {
        var regular = ScreenLayout.Buttons(GameScreen.World, 1280, 720);
        var preview = ScreenLayout.Buttons(GameScreen.World, 1280, 720, preliminaryContinents: true);
        var button = preview.Single(b => b.Action == UiAction.PreviewContinentsSurface);
        Check(button.Text == "SUPERFICIE PRELIMINAR" && button.Bounds ==
            regular.Single(b => b.Action == UiAction.ToggleSurface).Bounds &&
            preview.Any(b => b.Action == UiAction.ToggleTectonic) &&
            !regular.Any(b => b.Action == UiAction.PreviewContinentsSurface),
            "Explicit preview action reuses layout and retains tectonic access");
        var session = new GameSession();
        session.OpenCreateWorld(); session.ChooseSize(world.Landmass.Configuration.Size);
        session.ChooseType(LandmassType.Continents);
        session.SetSeedText(world.Landmass.Configuration.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        session.BeginGeneration(); session.CompleteGeneration(world);
        Check(session.BeginReconfigure(session.SelectedSize, session.SelectedType) == world.Landmass.Configuration,
            "Preview transition preserves dimensions, type and seed");
    }

    private static void ExpectArgument(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid preview input was accepted.");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
