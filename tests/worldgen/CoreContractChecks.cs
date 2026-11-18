// Verifies the actual CORE-01 contracts, registry and minimal request configuration.
// Uses test-only layer metadata and production project references without OpenTK or a GPU.

using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;

namespace Evolve.WorldGeneration.Checks;

internal static class CoreContractChecks
{
    // Keep a dependency-free executable suite consistent with the existing menu checks.
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("Eight canonical stage identities have stable order and values", CanonicalStages),
            ("Eight distinct fake layer implementations register successfully", ValidRegistry),
            ("Duplicate stage identities are rejected", DuplicateStages),
            ("Every missing stage and incorrect count are rejected", MissingStages),
            ("Out-of-order registrations are rejected rather than sorted", OutOfOrderStages),
            ("Unknown stages are rejected at every registry position", UnknownStages),
            ("Null registrations and null layers are rejected", NullRegistrations),
            ("Registry membership is a read-only independent snapshot", RegistrySnapshot),
            ("Unsupported stage lookup is rejected", InvalidLookup),
            ("Only four world types and three dimensionless map sizes are defined", ConfigurationChoices),
            ("Requests preserve valid immutable selections and signed seeds", GenerationRequests),
            ("Invalid configuration values and null settings are rejected", InvalidConfiguration),
            ("Layer contract exposes identity without fabricated execution", LayerBoundary),
            ("Production assemblies contain no fake or geographic layer implementation", NoProductionLayers),
            ("Core and definitions have no OpenTK or UI assembly dependencies", HeadlessDependencies)
        ];

        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try
            {
                check();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    // Assert the approved wire identities independently of registry implementation details.
    private static void CanonicalStages()
    {
        string[] names = ["Grid", "Tectonics", "Landmasses", "Elevation", "Landforms", "Hydrology", "Climate", "Biomes"];
        var stages = Enum.GetValues<GenerationStage>();
        Require(stages.Length == names.Length, "Expected exactly eight stages.");
        for (int index = 0; index < names.Length; index++)
            Require((int)stages[index] == index && stages[index].ToString() == names[index], $"Invalid stage identity at {index}.");
    }

    // Registration never needs an execution method, context or generated world.
    private static void ValidRegistry()
    {
        var layers = FakeWorldGenerationLayers.Create();
        Require(layers.Select(layer => layer.GetType()).Distinct().Count() == 8, "Expected eight fake implementations.");
        var registry = new WorldGenerationLayerRegistry(layers);
        Require(registry.Layers.Count == 8, "Registry is incomplete.");
        foreach (var stage in Enum.GetValues<GenerationStage>())
            Require(ReferenceEquals(registry.GetLayer(stage), layers[(int)stage]), $"Layer lookup mismatch for {stage}.");
    }

    // Exercise every pair of conflicting identities, not just an extra ninth entry.
    private static void DuplicateStages()
    {
        for (int source = 0; source < 8; source++)
        for (int target = 0; target < 8; target++)
        {
            if (source == target) continue;
            var layers = FakeWorldGenerationLayers.Create();
            layers[target] = layers[source];
            Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        }
        var distinctInstances = FakeWorldGenerationLayers.Create();
        distinctInstances[7] = new FakeGridLayer();
        Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(distinctInstances));
    }

    // Omission of any approved stage must fail instead of exposing a partial pipeline.
    private static void MissingStages()
    {
        for (int missing = 0; missing < 8; missing++)
        {
            var layers = FakeWorldGenerationLayers.Create().Where((_, index) => index != missing);
            Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        }
        Throws<ArgumentException>(() => new WorldGenerationLayerRegistry([]));
        var oversized = FakeWorldGenerationLayers.Create().Append(new FakeGridLayer());
        Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(oversized));
    }

    // Reject each adjacent swap and a reversed pipeline without silently correcting callers.
    private static void OutOfOrderStages()
    {
        for (int index = 0; index < 7; index++)
        {
            var layers = FakeWorldGenerationLayers.Create();
            (layers[index], layers[index + 1]) = (layers[index + 1], layers[index]);
            Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        }
        Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(FakeWorldGenerationLayers.Create().Reverse()));
    }

    // Enum casts outside the declared identities must not be accepted as extension stages.
    private static void UnknownStages()
    {
        foreach (int unknown in new[] { -1, 8, int.MinValue, int.MaxValue })
        for (int index = 0; index < 8; index++)
        {
            var layers = FakeWorldGenerationLayers.Create();
            layers[index] = new FakeGridLayer((GenerationStage)unknown);
            Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        }
    }

    // Null input cannot leave a partially initialized registry behind.
    private static void NullRegistrations()
    {
        Throws<ArgumentNullException>(() => new WorldGenerationLayerRegistry(null!));
        for (int index = 0; index < 8; index++)
        {
            var layers = FakeWorldGenerationLayers.Create();
            layers[index] = null!;
            Throws<ArgumentException>(() => new WorldGenerationLayerRegistry(layers));
        }
    }

    // Caller-owned containers cannot change the validated registration membership later.
    private static void RegistrySnapshot()
    {
        var layers = FakeWorldGenerationLayers.Create();
        var expectedGrid = layers[0];
        var registry = new WorldGenerationLayerRegistry(layers);
        layers[0] = layers[7];
        Require(ReferenceEquals(registry.GetLayer(GenerationStage.Grid), expectedGrid), "Source array mutated the registry.");

        var sourceList = FakeWorldGenerationLayers.Create().ToList();
        var fromList = new WorldGenerationLayerRegistry(sourceList);
        sourceList.Clear();
        Require(fromList.Layers.Count == 8, "Source list cleared the registry.");
        var collection = registry.Layers as IList<IWorldGenerationLayer>
            ?? throw new InvalidOperationException("Read-only membership wrapper is missing.");
        Throws<NotSupportedException>(() => collection[0] = new FakeBiomesLayer());
        Require(ReferenceEquals(registry.GetLayer(GenerationStage.Grid), expectedGrid), "Read-only wrapper changed membership.");
    }

    // Lookup validation must reject cast enum values before indexing the registry.
    private static void InvalidLookup()
    {
        var registry = new WorldGenerationLayerRegistry(FakeWorldGenerationLayers.Create());
        Throws<ArgumentOutOfRangeException>(() => registry.GetLayer((GenerationStage)(-1)));
        Throws<ArgumentOutOfRangeException>(() => registry.GetLayer((GenerationStage)8));
    }

    // Options describe approved choices only; dimensions are not invented by CORE-01.
    private static void ConfigurationChoices()
    {
        Require(Enum.GetNames<WorldType>().SequenceEqual(["Pangea", "Continents", "Archipelagos", "RealEarth"]), "World types differ from the approved four.");
        Require(Enum.GetNames<MapSize>().SequenceEqual(["Small", "Medium", "Large"]), "Map sizes differ from the approved three.");
        Require(typeof(WorldGenerationSettings).GetProperties().Select(property => property.Name).Order()
            .SequenceEqual(new[] { "MapSize", "WorldType" }), "Settings contain unsupported fields or dimensions.");
    }

    // All approved combinations and signed seed boundaries are accepted as immutable data.
    private static void GenerationRequests()
    {
        foreach (var worldType in Enum.GetValues<WorldType>())
        foreach (var mapSize in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { long.MinValue, -1L, 0L, 1L, long.MaxValue })
        {
            var settings = new WorldGenerationSettings(worldType, mapSize);
            var request = new GenerationRequest(seed, settings);
            Require(request.Seed == seed && request.Settings.WorldType == worldType && request.Settings.MapSize == mapSize,
                "Request changed a caller selection or seed.");
            Require(settings == new WorldGenerationSettings(worldType, mapSize), "Settings value equality failed.");
        }
        foreach (var type in new[] { typeof(GenerationRequest), typeof(WorldGenerationSettings) })
            Require(type.GetProperties().All(property => property.SetMethod is null), $"{type.Name} exposes mutable request metadata.");
    }

    // Invalid enum casts are rejected at configuration construction, not deferred to algorithms.
    private static void InvalidConfiguration()
    {
        foreach (int invalid in new[] { -1, 4, int.MaxValue })
            Throws<ArgumentOutOfRangeException>(() => new WorldGenerationSettings((WorldType)invalid, MapSize.Small));
        foreach (int invalid in new[] { -1, 3, int.MaxValue })
            Throws<ArgumentOutOfRangeException>(() => new WorldGenerationSettings(WorldType.Pangea, (MapSize)invalid));
        Throws<ArgumentNullException>(() => new GenerationRequest(0, null!));
    }

    // CORE-01 must not fabricate execution parameters, an empty context or a no-op Execute method.
    private static void LayerBoundary()
    {
        var contract = typeof(IWorldGenerationLayer);
        var stage = contract.GetProperty(nameof(IWorldGenerationLayer.Stage));
        Require(stage?.PropertyType == typeof(GenerationStage) && stage.SetMethod is null, "Layer stage must be get-only.");
        Require(contract.GetMethods().Length == 1, "Execution or unrelated members were introduced ahead of their increment.");
    }

    // Real and fake executors belong to later layer work and the test assembly respectively.
    private static void NoProductionLayers()
    {
        var core = typeof(WorldGenerationLayerRegistry).Assembly;
        Require(!core.GetTypes().Any(type => !type.IsInterface && typeof(IWorldGenerationLayer).IsAssignableFrom(type)),
            "Production core contains a layer implementation.");
        int fakeCount = typeof(FakeWorldGenerationLayers).Assembly.GetTypes()
            .Count(type => !type.IsInterface && typeof(IWorldGenerationLayer).IsAssignableFrom(type));
        Require(fakeCount == 8, "Expected exactly eight test-only layer implementations.");
    }

    // Verify actual compiled dependencies, rather than assuming that a test running headless is enough.
    private static void HeadlessDependencies()
    {
        var definitions = typeof(WorldGenerationSettings).Assembly;
        foreach (var assembly in new[] { typeof(WorldGenerationLayerRegistry).Assembly, definitions })
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            string name = reference.Name ?? "";
            Require(name.StartsWith("System", StringComparison.Ordinal) || name == definitions.GetName().Name,
                $"Unexpected dependency in {assembly.GetName().Name}: {name}.");
        }
        Require(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            assembly.GetName().Name?.StartsWith("OpenTK", StringComparison.Ordinal) == true), "Checks loaded OpenTK.");
    }

    // Keep rejection assertions type-specific so accidental runtime errors do not count as validation.
    private static void Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    // A failed contract check must produce a nonzero suite result.
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
