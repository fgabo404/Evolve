using System.Diagnostics;
using System.Security.Cryptography;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

public static class ContinentsCorticalFragmentTests
{
    public static void Run()
    {
        var service = new WorldGenerationService();
        var generator = new ContinentsCorticalFragmentGenerator();
        var configurations = new[] { GridSize.Small, GridSize.Medium, GridSize.Large, GridSize.Enormous };
        var hashes = new Dictionary<GridSize, HashSet<string>>();
        foreach (var size in configurations)
        {
            var fingerprints = new HashSet<string>();
            foreach (int seed in new[] { 2026, 42, -731 })
            {
                var config = LandmassProfiles.Create(size, LandmassType.Continents, seed);
                var official = service.Generate(config);
                var topology = new HexTopology(official.Grid);
                var plates = official.Landmass.PlateByCellId!;
                var motion = official.Landmass.TectonicMotion!;
                byte[] plateSnapshot = (byte[])plates.Clone();
                float[] compressionSnapshot = (float[])motion.CompressionByCellId.Clone();
                float[] extensionSnapshot = (float[])motion.ExtensionByCellId.Clone();
                float[] shearSnapshot = (float[])motion.ShearByCellId.Clone();
                var timer = Stopwatch.StartNew();
                long before = GC.GetAllocatedBytesForCurrentThread();
                var cortex = generator.Generate(topology, config, plates, motion, attempt: 0);
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                timer.Stop();

                SharedCorticalModelValidator.Validate(topology, cortex, plates);
                int continentalCells = cortex.CrustByCellId.Count(v => v == (byte)CrustKind.Continental);
                int oceanicCells = cortex.CrustByCellId.Count(v => v == (byte)CrustKind.Oceanic);
                Check(continentalCells > 0 && oceanicCells > 0 &&
                    continentalCells + oceanicCells == topology.Count &&
                    cortex.Fragments.Length >= 2 && cortex.Provinces.Length >= 2 &&
                    cortex.Fragments.Length == cortex.Provinces.Length &&
                    cortex.Fragments.Select(f => f.CoreCellId).Distinct().Count() == cortex.Fragments.Length,
                    "Real continental and oceanic cortex with distinct fragment/province cores");
                Check(plates.Distinct().Count() == 16 &&
                    plates.SequenceEqual(plateSnapshot) &&
                    motion.CompressionByCellId.SequenceEqual(compressionSnapshot) &&
                    motion.ExtensionByCellId.SequenceEqual(extensionSnapshot) &&
                    motion.ShearByCellId.SequenceEqual(shearSnapshot) &&
                    official.Landmass.SurfaceGenerationPending &&
                    official.Landmass.SurfaceByCellId.Length == 0 &&
                    official.Landmass.ElevationByCellId is null &&
                    official.Landmass.SharedCortex is null,
                    "The official pipeline, 16 plate IDs and stress fields remain untouched");
                // Crossing a boundary is allowed only where an existing convergent
                // contact justifies it. No contact is fabricated.
                var contactMap = motion.Interactions.ToDictionary(
                    edge => Key(edge.FirstCellId, edge.SecondCellId));
                int crossed = 0, mixedPlates = 0;
                var masks = new int[16];
                for (int id = 0; id < topology.Count; id++)
                {
                    int fragment = cortex.FragmentByCellId[id];
                    if (fragment >= 0) masks[plates[id]] |= 1 << fragment;
                    foreach (int neighbor in topology.Neighbors(id))
                    {
                        if (id >= neighbor || plates[id] == plates[neighbor] ||
                            fragment < 0 || fragment != cortex.FragmentByCellId[neighbor]) continue;
                        var contact = contactMap[Key(id, neighbor)];
                        Check(contact.Convergence >= .5 &&
                            contact.Convergence > contact.Divergence &&
                            contact.Convergence >= .4 * contact.Shear,
                            "Cross-plate cortex must follow an actual convergent contact");
                        crossed++;
                    }
                }
                foreach (int mask in masks) if (System.Numerics.BitOperations.PopCount((uint)mask) > 1) mixedPlates++;
                // Plate/fragment independence is a contract invariant, not a mandatory
                // morphology of every seed. The synthetic H05-A1 test covers it explicitly.
                var again = generator.Generate(topology, config, plates, motion, attempt: 0);
                Check(cortex.CrustByCellId.SequenceEqual(again.CrustByCellId) &&
                    cortex.FragmentByCellId.SequenceEqual(again.FragmentByCellId) &&
                    cortex.ProvinceByCellId.SequenceEqual(again.ProvinceByCellId) &&
                    cortex.EmergenceAffinityByCellId.SequenceEqual(again.EmergenceAffinityByCellId) &&
                    cortex.Fragments.SequenceEqual(again.Fragments) &&
                    cortex.Provinces.SequenceEqual(again.Provinces) &&
                    cortex.AssemblyLinks.SequenceEqual(again.AssemblyLinks),
                    "Full cortical output is deterministic for each official scenario");
                var persisted = SharedCorticalPersistence.ToState(cortex);
                var restored = SharedCorticalPersistence.FromState(persisted);
                Check(restored.FragmentByCellId.SequenceEqual(cortex.FragmentByCellId) &&
                    restored.EmergenceAffinityByCellId.SequenceEqual(cortex.EmergenceAffinityByCellId) &&
                    restored.AssemblyLinks.SequenceEqual(cortex.AssemblyLinks),
                    "H05-B1 model is compatible with the approved cortical persistence contract");
                fingerprints.Add(Convert.ToHexString(SHA256.HashData(cortex.CrustByCellId)));
                Check(timer.ElapsedMilliseconds < 120000 &&
                    bytes < 500_000_000,
                    "Cortical generation remains bounded in runtime and allocated bytes");
                Console.WriteLine($"PASS: H05-B1 {size}/{seed}: fragments={cortex.Fragments.Length}, " +
                    $"provinces={cortex.Provinces.Length}, continental={continentalCells}, oceanic={oceanicCells}, " +
                    $"multi-fragment plates={mixedPlates}, convergent crossings={crossed}, " +
                    $"links={cortex.AssemblyLinks.Length}, time={timer.ElapsedMilliseconds} ms, " +
                    $"allocated={bytes / 1048576.0:F1} MiB");
            }
            Check(fingerprints.Count == 3, "Different seeds must change cortical distribution");
            hashes[size] = fingerprints;
        }
        var configExample = LandmassProfiles.Create(GridSize.Small, LandmassType.Continents, 2026);
        var w = service.Generate(configExample);
        var t = new HexTopology(w.Grid);
        var a = generator.Generate(t, configExample, w.Landmass.PlateByCellId!,
            w.Landmass.TectonicMotion!, 0);
        var b = generator.Generate(t, configExample, w.Landmass.PlateByCellId!,
            w.Landmass.TectonicMotion!, 1);
        Check(!a.CrustByCellId.SequenceEqual(b.CrustByCellId), "Attempt must alter independent cortex RNG");
        Check(w.Landmass.SurfaceGenerationPending, "Continents stays pending after standalone cortex generation");
    }

    private static ulong Key(int a, int b) =>
        ((ulong)(uint)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
