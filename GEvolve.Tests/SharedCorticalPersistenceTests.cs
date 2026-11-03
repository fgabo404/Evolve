using System.Text.Json;
using System.Text.Json.Nodes;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;
using GEvolve.State.Persistence;

public static class SharedCorticalPersistenceTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        foreach (LandmassType type in new[]
            { LandmassType.Pangaea, LandmassType.Continents, LandmassType.Archipelagos })
        {
            var world = service.Generate(LandmassProfiles.Create(GridSize.Small, type, 2026));
            var topology = new HexTopology(world.Grid);
            int count = topology.Count;
            int west = topology.Id(0, 20);
            int east = topology.Neighbors(west).ToArray()
                .First(id => id % topology.Columns == topology.Columns - 1);
            int other = topology.Id(20, 20);
            var crust = new byte[count];
            var fragments = Enumerable.Repeat(-1, count).ToArray();
            var provinces = Enumerable.Repeat(-1, count).ToArray();
            crust[west] = crust[east] = crust[other] = 1;
            fragments[west] = fragments[east] = 0; fragments[other] = 1;
            provinces[west] = provinces[east] = 0; provinces[other] = 1;
            var affinity = new float[count];
            affinity[west] = .375f; affinity[east] = -.25f; affinity[other] = 1.125f;
            var model = new SharedCorticalModel(SharedCorticalModel.CurrentVersion,
                crust, fragments, provinces, affinity,
                [new(0, west), new(1, other)],
                [new(0, 0, west), new(1, 1, other)],
                [new(0, 1, CorticalAssemblyKind.Accreted)]);
            SharedCorticalModelValidator.Validate(topology, model, world.Landmass.PlateByCellId!);
            var snapshot = SharedCorticalPersistence.ToState(model);
            var reconstructed = SharedCorticalPersistence.FromState(snapshot);
            Check(JsonSerializer.Serialize(reconstructed) == JsonSerializer.Serialize(model),
                "Lossless Engine/State cortex conversion");
            Check(!ReferenceEquals(snapshot.CrustByCellId, model.CrustByCellId),
                "Persisted model does not alias mutable Engine arrays");

            var source = world.Landmass with { SharedCortex = snapshot };
            var cortexOnly = source with
            {
                SurfaceByCellId = [], ElevationByCellId = null,
                SurfaceGenerationPending = true, TectonicPreviewOnly = false
            };
            Check(cortexOnly.GenerationStage == WorldGenerationStage.CortexAvailable &&
                !cortexOnly.HasSurface && world.Landmass.TectonicMotion is not null,
                "Cortex-only stage cannot masquerade as a classified surface");
            var completeSurface = source with { SurfaceGenerationPending = false, TectonicPreviewOnly = false };
            if (world.Landmass.HasSurface)
                Check(completeSurface.GenerationStage == WorldGenerationStage.SurfaceAvailable,
                    "Current surface is distinct from future ready state");
            if (type == LandmassType.Continents)
                Check(world.Landmass.GenerationStage == WorldGenerationStage.TectonicsOnly &&
                    world.Landmass.SurfaceGenerationPending && world.Landmass.SharedCortex is null,
                    "Official Continents remains a tectonic-only preview");

            string path = Path.Combine(Path.GetTempPath(), $"cortex-a2-{type}-{Guid.NewGuid():N}.json");
            try
            {
                var state = new GameState { HexGrid = new(world.Grid.Definition), Landmass = cortexOnly };
                GameSaveStore.Save(path, state);
                string json1 = File.ReadAllText(path);
                var loaded = GameSaveStore.Load(path).Landmass!;
                Check(loaded.GenerationStage == WorldGenerationStage.CortexAvailable &&
                    loaded.SurfaceGenerationPending && !loaded.TectonicPreviewOnly &&
                    loaded.SurfaceByCellId.Length == 0 && loaded.SharedCortex is not null &&
                    JsonSerializer.Serialize(loaded.SharedCortex) == JsonSerializer.Serialize(snapshot),
                    "Exact cortical round-trip without reconstruction");
                GameSaveStore.Save(path, new GameState
                {
                    HexGrid = new(world.Grid.Definition), Landmass = loaded
                });
                Check(File.ReadAllText(path) == json1, "Cortical save is deterministic");

                // Old JSON does not contain SharedCortex and remains readable.
                state.Landmass = world.Landmass;
                GameSaveStore.Save(path, state);
                var legacy = JsonNode.Parse(File.ReadAllText(path))!;
                legacy["Landmass"]!.AsObject().Remove("SharedCortex");
                File.WriteAllText(path, legacy.ToJsonString());
                var old = GameSaveStore.Load(path).Landmass!;
                Check(old.SharedCortex is null &&
                    old.GenerationStage == (type == LandmassType.Continents
                        ? WorldGenerationStage.TectonicsOnly : WorldGenerationStage.SurfaceAvailable),
                    "Historical saves lacking cortex remain readable");

                void Reject(SharedCorticalState invalid)
                {
                    state.Landmass = cortexOnly with { SharedCortex = invalid };
                    try { GameSaveStore.Save(path, state); throw new Exception("Corrupt cortex was saved"); }
                    catch (InvalidDataException) { }
                }
                Reject(snapshot with { Version = "shared-cortex-99" });
                Reject(snapshot with { CrustByCellId = new byte[count - 1] });
                Reject(snapshot with { EmergenceAffinityByCellId = new float[count + 1] });
                var invalidIds = (int[])snapshot.ProvinceByCellId.Clone();
                invalidIds[other] = 99;
                Reject(snapshot with { ProvinceByCellId = invalidIds });
                var invalidAffinity = (float[])snapshot.EmergenceAffinityByCellId.Clone();
                invalidAffinity[other] = float.PositiveInfinity;
                Reject(snapshot with { EmergenceAffinityByCellId = invalidAffinity });
                Reject(snapshot with { AssemblyLinks =
                    [new(0, 1, (CorticalAssemblyStateKind)255)] });
                Reject(snapshot with { AssemblyLinks =
                    [new(1, 0, CorticalAssemblyStateKind.Accreted)] });
                // Persisted topology must use E/W seam but never connect north to south.
                int top = topology.Id(5, 0), bottom = topology.Id(5, topology.Rows - 1);
                var disconnectedCrust = (byte[])snapshot.CrustByCellId.Clone();
                var disconnectedFragment = (int[])snapshot.FragmentByCellId.Clone();
                var disconnectedProvince = (int[])snapshot.ProvinceByCellId.Clone();
                disconnectedCrust[top] = disconnectedCrust[bottom] = 1;
                disconnectedFragment[top] = disconnectedFragment[bottom] = 0;
                disconnectedProvince[top] = disconnectedProvince[bottom] = 0;
                Reject(snapshot with { CrustByCellId = disconnectedCrust,
                    FragmentByCellId = disconnectedFragment, ProvinceByCellId = disconnectedProvince });
                Check(!topology.Neighbors(top).Contains(bottom), "No persisted north/south wrap");

                // Loading malformed JSON must fail rather than silently repairing cortex.
                state.Landmass = cortexOnly;
                GameSaveStore.Save(path, state);
                var doc = JsonNode.Parse(File.ReadAllText(path))!;
                doc["Landmass"]!["SharedCortex"]!["Version"] = "unsupported";
                File.WriteAllText(path, doc.ToJsonString());
                try { GameSaveStore.Load(path); throw new Exception("Unknown cortex version loaded"); }
                catch (InvalidDataException) { }
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
        Console.WriteLine("PASS: H05-A2 cortex persistence, versions, corrupt data and world stages");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
