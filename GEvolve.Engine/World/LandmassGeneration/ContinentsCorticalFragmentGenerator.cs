using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// H05-B1: deterministic cortical fragments for Continents, using official plates and
/// existing relative motion. Returns cortex only: never land, sea level or relief.
/// </summary>
public sealed class ContinentsCorticalFragmentGenerator
{
    public const string Version = "continents-fragments-1";
    private const int Stream = 0x43524631; // Independent from plate/motion/surface RNG.
    private const int CandidateSamples = 160;

    public SharedCorticalModel Generate(HexTopology topology, LandmassConfiguration configuration,
        ReadOnlySpan<byte> plateByCellId, TectonicMotionState motion, int attempt = 0)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(motion);
        if (configuration.Type != LandmassType.Continents || attempt < 0)
            throw new ArgumentException("Continents profile and nonnegative attempt are required.");
        if (configuration != LandmassProfiles.Create(configuration.Size, configuration.Type, configuration.Seed) ||
            topology.Grid.Definition != HexGridDefinitions.Get(configuration.Size))
            throw new ArgumentException("Cortex requires the official grid and configuration.");

        int count = topology.Count, columns = topology.Columns;
        if (plateByCellId.Length != count ||
            motion.Version != TectonicMotionGenerator.Version ||
            motion.Motions is null || motion.Motions.Length != TectonicPlateGenerator.NumberOfPlates ||
            motion.Interactions is null || motion.CompressionByCellId is null ||
            motion.ExtensionByCellId is null || motion.ShearByCellId is null ||
            motion.CompressionByCellId.Length != count ||
            motion.ExtensionByCellId.Length != count || motion.ShearByCellId.Length != count)
            throw new ArgumentException("Official plate and tectonic motion arrays are required.");
        // One compact snapshot avoids capturing the caller span in frontier expansion.
        byte[] plates = plateByCellId.ToArray();
        Span<bool> existingPlates = stackalloc bool[TectonicPlateGenerator.NumberOfPlates];
        foreach (byte id in plateByCellId)
        {
            if (id >= existingPlates.Length)
                throw new ArgumentException("Invalid universal tectonic plate ID.");
            existingPlates[id] = true;
        }
        foreach (bool exists in existingPlates)
            if (!exists) throw new ArgumentException("All 16 tectonic plates must be present.");

        for (int plate = 0; plate < motion.Motions.Length; plate++)
            if (motion.Motions[plate] is null || motion.Motions[plate].PlateId != plate)
                throw new ArgumentException("Invalid official plate motion descriptor.");

        // Only official adjacent inter-plate contacts are eligible for cross-plate expansion.
        // The key is direction-independent; no new plate boundaries are invented.
        var contacts = new Dictionary<ulong, PlateInteraction>(motion.Interactions.Length);
        foreach (var edge in motion.Interactions)
        {
            if (edge.FirstCellId < 0 || edge.SecondCellId >= count ||
                edge.FirstCellId >= edge.SecondCellId ||
                plateByCellId[edge.FirstCellId] != edge.FirstPlateId ||
                plateByCellId[edge.SecondCellId] != edge.SecondPlateId ||
                edge.FirstPlateId == edge.SecondPlateId ||
                !topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) ||
                !double.IsFinite(edge.Convergence) || !double.IsFinite(edge.Divergence) ||
                !double.IsFinite(edge.Shear) || edge.Convergence < 0 ||
                edge.Divergence < 0 || edge.Shear < 0 ||
                !contacts.TryAdd(Key(edge.FirstCellId, edge.SecondCellId), edge))
                throw new ArgumentException("Inconsistent official tectonic boundary interaction.");
        }
        // Every real inter-plate hex edge must be accounted for by official motion.
        int actualContacts = 0;
        for (int cell = 0; cell < count; cell++)
        foreach (int neighbor in topology.Neighbors(cell))
        {
            if (cell >= neighbor || plates[cell] == plates[neighbor]) continue;
            actualContacts++;
            if (!contacts.ContainsKey(Key(cell, neighbor)))
                throw new ArgumentException("Missing official tectonic interaction.");
        }
        if (contacts.Count != actualContacts)
            throw new ArgumentException("Official interactions do not match the actual plate partition.");
        for (int cell = 0; cell < count; cell++)
        {
            if (!float.IsFinite(motion.CompressionByCellId[cell]) ||
                !float.IsFinite(motion.ExtensionByCellId[cell]) ||
                !float.IsFinite(motion.ShearByCellId[cell]) ||
                motion.CompressionByCellId[cell] < 0 || motion.ExtensionByCellId[cell] < 0 ||
                motion.ShearByCellId[cell] < 0)
                throw new ArgumentException("Invalid tectonic stress field.");
        }

        var random = new DeterministicRandom(configuration.Seed,
            unchecked((uint)(Stream + attempt * 0x9E37)));
        // Structural fragment density is a heuristic relative to universal plate count;
        // it is NOT the number of emerged continents (determined in future H05-B stages).
        int fragmentCount = TectonicPlateGenerator.NumberOfPlates / 2;
        int[] cores = new int[fragmentCount];
        for (int i = 0; i < cores.Length; i++)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int sample = 0; sample < CandidateSamples; sample++)
            {
                int column = random.NextInt(columns);
                int row = Math.Clamp((int)(topology.Rows * (.08 + .84 * random.NextDouble())),
                    0, topology.Rows - 1);
                int id = topology.Id(column, row);
                int separation = i == 0 ? Math.Min(columns, topology.Rows) / 3
                    : Enumerable.Range(0, i).Min(j => topology.Distance(id, cores[j]));
                // Core placement is biased toward stable interiors and compressive
                // regions; location depends on official tectonics, not noise alone.
                double compression = Math.Tanh(motion.CompressionByCellId[id] / 5.0);
                double extension = Math.Tanh(motion.ExtensionByCellId[id] / 5.0);
                double score = separation + 3.5 * compression - 4.5 * extension +
                    (random.NextDouble() - .5) * 2;
                if (score > bestScore) { bestScore = score; best = id; }
            }
            if (best < 0 || Array.IndexOf(cores, best, 0, i) >= 0)
                throw new InvalidOperationException("Unable to place distinct cortical cores.");
            cores[i] = best;
        }

        var crust = new byte[count];
        var fragments = new int[count];
        var provinces = new int[count];
        Array.Fill(fragments, -1);
        Array.Fill(provinces, -1);
        var affinity = new float[count];
        int[] budgets = new int[fragmentCount];
        int[] owned = new int[fragmentCount];
        // Multiple continental fragments and intervening oceanic sectors on any plate.
        // Budgets scale by grid area, not absolute tile count.
        for (int i = 0; i < fragmentCount; i++)
            budgets[i] = Math.Max(1, (int)Math.Round(count * (.033 + .014 * random.NextDouble())));

        var frontier = new PriorityQueue<(int Cell, int Fragment, int Depth), (double, int, int)>();
        for (int i = 0; i < cores.Length; i++)
        {
            int core = cores[i];
            crust[core] = (byte)CrustKind.Continental;
            fragments[core] = provinces[core] = i;
            owned[i] = 1;
            AddNeighbors(core, i, 0);
        }

        while (frontier.TryDequeue(out var entry, out _))
        {
            int id = entry.Cell, fragment = entry.Fragment;
            if (fragments[id] != -1 || owned[fragment] >= budgets[fragment]) continue;
            // A cell can be queued through an eligible contact but touch other
            // already-owned cells across nonconvergent boundaries. The entire
            // resulting same-fragment interface must satisfy the contact rule.
            bool validInterface = true;
            foreach (int neighbor in topology.Neighbors(id))
            {
                if (fragments[neighbor] != fragment || plates[id] == plates[neighbor])
                    continue;
                if (!contacts.TryGetValue(Key(id, neighbor), out var edge) ||
                    edge.Convergence < .5 || edge.Convergence <= edge.Divergence ||
                    edge.Convergence < .4 * edge.Shear)
                {
                    validInterface = false;
                    break;
                }
            }
            if (!validInterface) continue;
            crust[id] = (byte)CrustKind.Continental;
            fragments[id] = provinces[id] = fragment;
            owned[fragment]++;
            AddNeighbors(id, fragment, entry.Depth);
        }

        // Affinity is a continuous cortical potential, NOT a 0/1 surface mask.
        // Structural uplift/subsidence dominates; periodic detail only perturbs it.
        for (int id = 0; id < count; id++)
        {
            double compression = Math.Tanh(motion.CompressionByCellId[id] / 5.0);
            double extension = Math.Tanh(motion.ExtensionByCellId[id] / 5.0);
            double detail = PeriodicNoise.Sample(id % columns, id / columns,
                columns, topology.Rows, 5, configuration.Seed ^ unchecked(attempt * 3119 + 0x434F5245));
            affinity[id] = (float)((crust[id] == (byte)CrustKind.Continental ? .35 : -.35) +
                .30 * compression - .35 * extension + .10 * detail);
        }

        // Document only actual inter-fragment, inter-plate contacts. The relationships
        // do not merge provinces or classify emerged land.
        var relations = new SortedDictionary<(int, int), (double Strength, CorticalAssemblyKind Kind)>();
        foreach (var edge in motion.Interactions)
        {
            int a = fragments[edge.FirstCellId], b = fragments[edge.SecondCellId];
            if (a < 0 || b < 0 || a == b) continue;
            double strength = Math.Max(edge.Convergence, edge.Divergence);
            if (strength < .5) continue;
            var kind = edge.Convergence >= edge.Divergence
                ? CorticalAssemblyKind.Accreted : CorticalAssemblyKind.RiftSeparated;
            var key = (Math.Min(a, b), Math.Max(a, b));
            if (!relations.TryGetValue(key, out var existing) || strength > existing.Strength)
                relations[key] = (strength, kind);
        }

        var result = new SharedCorticalModel(SharedCorticalModel.CurrentVersion,
            crust, fragments, provinces, affinity,
            cores.Select((cell, i) => new CorticalFragment(i, cell)).ToArray(),
            cores.Select((cell, i) => new CorticalProvince(i, i, cell)).ToArray(),
            relations.Select(pair => new CorticalAssemblyLink(pair.Key.Item1, pair.Key.Item2,
                pair.Value.Kind)).ToArray());
        SharedCorticalModelValidator.Validate(topology, result, plateByCellId);
        return result;

        void AddNeighbors(int source, int fragment, int depth)
        {
            if (owned[fragment] >= budgets[fragment]) return;
            foreach (int neighbor in topology.Neighbors(source))
            {
                if (fragments[neighbor] != -1) continue;
                double boundaryCost = 0;
                if (plates[source] != plates[neighbor])
                {
                    if (!contacts.TryGetValue(Key(source, neighbor), out var edge) ||
                        edge.Convergence < .5 || edge.Convergence <= edge.Divergence ||
                        edge.Convergence < .4 * edge.Shear)
                        continue; // Crossing is permitted by convergent evidence, not automatic.
                    boundaryCost = .75 - .18 * Math.Tanh(edge.Convergence / 5.0);
                }
                double compression = Math.Tanh(motion.CompressionByCellId[neighbor] / 5.0);
                double extension = Math.Tanh(motion.ExtensionByCellId[neighbor] / 5.0);
                double shear = Math.Tanh(motion.ShearByCellId[neighbor] / 5.0);
                double noise = PeriodicNoise.Sample(neighbor % columns, neighbor / columns,
                    columns, topology.Rows, 9,
                    configuration.Seed ^ unchecked(attempt * 3571 + fragment * 811 + 0x46524731));
                double priority = (depth + 1) * .24 + boundaryCost +
                    .40 * extension - .26 * compression + .08 * shear + .24 * noise;
                frontier.Enqueue((neighbor, fragment, depth + 1), (priority, fragment, neighbor));
            }
        }
    }

    private static ulong Key(int first, int second)
    {
        uint a = (uint)Math.Min(first, second), b = (uint)Math.Max(first, second);
        return ((ulong)a << 32) | b;
    }
}
