using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>H05-B3 emergence only: no elevations, target land quota or artificial land bridges.</summary>
public sealed class ContinentsSurfaceGenerator
{
    public const string Version = "continents-surface-preview-1";
    // Dimensionless provisional potential, not metres. Fixed for every size and seed.
    public const double SeaLevel = .38;
    private const double CoastalPenalty = .22;
    private const double InteriorBonus = .12;
    private const double AccretionBonus = .10;
    private const double SeparationPenalty = .18;

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration,
        ReadOnlySpan<byte> plateByCellId, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly)
        => GenerateCore(topology, configuration, plateByCellId, motion, assembly, false).LocalizedSurface;

    public ContinentsSurfaceComparison CompareRegional(HexTopology topology, LandmassConfiguration configuration,
        ReadOnlySpan<byte> plateByCellId, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly)
        => GenerateCore(topology, configuration, plateByCellId, motion, assembly, true);

    private ContinentsSurfaceComparison GenerateCore(HexTopology topology, LandmassConfiguration configuration,
        ReadOnlySpan<byte> plateByCellId, TectonicMotionState motion,
        ContinentsCorticalAssemblyResult assembly, bool compare)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(motion);
        ArgumentNullException.ThrowIfNull(assembly);
        if (configuration.Type != LandmassType.Continents ||
            configuration != LandmassProfiles.Create(configuration.Size, configuration.Type, configuration.Seed) ||
            topology.Grid.Definition != HexGridDefinitions.Get(configuration.Size) ||
            motion.Version != TectonicMotionGenerator.Version || motion.Interactions is null ||
            !double.IsFinite(motion.InfluenceRadiusHexes) || motion.InfluenceRadiusHexes <= 0 ||
            motion.InfluenceRadiusHexes > topology.Count / 4.0)
            throw new ArgumentException("Official Continents topology and tectonic motion are required.");
        var cortex = assembly.Model;
        SharedCorticalModelValidator.Validate(topology, cortex, plateByCellId);
        int count = topology.Count;
        byte[] plates = plateByCellId.ToArray();
        if (plates.Distinct().Count() != TectonicPlateGenerator.NumberOfPlates)
            throw new ArgumentException("All 16 official plates are required.");
        int range = Math.Max(2, (int)Math.Ceiling(motion.InfluenceRadiusHexes * 4));
        if (assembly.Evidence is null || assembly.Evidence.Length != cortex.AssemblyLinks.Length ||
            assembly.Diagnostics.AcceptedGapHexes != range)
            throw new ArgumentException("Surface preview requires the approved 4r assembly evidence.");

        var queue = new int[count];
        var distance = new int[count];
        Array.Fill(distance, -1);
        int head = 0, tail = 0;
        // Distance to oceanic crust, over the actual cylinder; no north/south sources.
        for (int id = 0; id < count; id++)
            if (cortex.CrustByCellId[id] == (byte)CrustKind.Oceanic)
            {
                distance[id] = 0;
                queue[tail++] = id;
            }
        if (tail == 0 || tail == count)
            throw new ArgumentException("Both continental and oceanic crust are required.");
        while (head < tail)
        {
            int cell = queue[head++];
            foreach (int neighbor in topology.Neighbors(cell))
                if (distance[neighbor] < 0)
                {
                    distance[neighbor] = distance[cell] + 1;
                    queue[tail++] = neighbor;
                }
        }
        var potential = new double[count];
        double coastalWidth = Math.Max(1, Math.Ceiling(motion.InfluenceRadiusHexes));
        for (int id = 0; id < count; id++)
        {
            double interior = Math.Clamp((distance[id] - 1) / coastalWidth, 0, 1);
            // Affinity already includes compression, extension and periodic detail.
            potential[id] = cortex.EmergenceAffinityByCellId[id] +
                InteriorBonus * interior - CoastalPenalty * (1 - interior);
        }

        var uplift = new double[count];
        var subsidence = new double[count];
        for (int index = 0; index < assembly.Evidence.Length; index++)
        {
            var evidence = assembly.Evidence[index];
            var link = cortex.AssemblyLinks[index];
            if (evidence.FirstFragmentId != link.FirstFragmentId ||
                evidence.SecondFragmentId != link.SecondFragmentId || evidence.Kind != link.Kind ||
                (uint)evidence.InteractionIndex >= (uint)motion.Interactions.Length)
                throw new ArgumentException("Assembly evidence does not match its cortical link.");
            var edge = motion.Interactions[evidence.InteractionIndex];
            if ((uint)edge.FirstCellId >= (uint)count || (uint)edge.SecondCellId >= (uint)count ||
                edge.FirstCellId >= edge.SecondCellId ||
                plates[edge.FirstCellId] != edge.FirstPlateId || plates[edge.SecondCellId] != edge.SecondPlateId ||
                edge.FirstPlateId == edge.SecondPlateId ||
                !topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) ||
                !double.IsFinite(edge.Convergence) || !double.IsFinite(edge.Divergence) ||
                !double.IsFinite(edge.Shear) || edge.Convergence < 0 || edge.Divergence < 0 || edge.Shear < 0)
                throw new ArgumentException("Emergence evidence requires a real official inter-plate edge.");
            bool supported = evidence.Kind == CorticalAssemblyKind.Accreted
                ? edge.Convergence >= .5 && edge.Convergence > edge.Divergence && edge.Convergence >= .4 * edge.Shear
                : edge.Divergence >= .5 && edge.Divergence > edge.Convergence && edge.Divergence >= .4 * edge.Shear;
            if (!supported) throw new ArgumentException("Unsupported normal tectonic evidence.");

            int first = evidence.FirstFragmentId, second = evidence.SecondFragmentId;
            int forward = Sum(VisitSide(edge.FirstCellId, first, false, evidence.Kind),
                VisitSide(edge.SecondCellId, second, false, evidence.Kind));
            int reverse = Sum(VisitSide(edge.SecondCellId, first, false, evidence.Kind),
                VisitSide(edge.FirstCellId, second, false, evidence.Kind));
            int gap = Math.Min(forward, reverse);
            if (gap > range || gap != evidence.OceanGapHexes || evidence.DirectContact != (gap == 0))
                throw new ArgumentException("Invalid cortical gap or direct-contact evidence.");
            VisitSide(forward <= reverse ? edge.FirstCellId : edge.SecondCellId, first, true, evidence.Kind);
            VisitSide(forward <= reverse ? edge.SecondCellId : edge.FirstCellId, second, true, evidence.Kind);
        }

        byte[] surface = Classify(uplift, subsidence);
        if (!compare)
            return new(surface, surface, potential, uplift, subsidence, uplift, subsidence, [], []);
        var regional = RegionalEmergenceExperiment.Distribute(topology, plates, motion, assembly,
            range, uplift, subsidence, AccretionBonus, SeparationPenalty);
        return new(surface, Classify(regional.Uplift, regional.Subsidence), potential,
            uplift, subsidence, regional.Uplift, regional.Subsidence, regional.Sources, regional.Budgets);

        byte[] Classify(double[] positive, double[] negative)
        {
            var result = new byte[count];
            for (int id = 0; id < count; id++)
                // Oceanic volcanism is outside B3. Systems never paint oceanic corridors as land.
                if (cortex.CrustByCellId[id] == (byte)CrustKind.Continental &&
                    potential[id] + positive[id] - negative[id] > SeaLevel)
                    result[id] = 1;
            return result;
        }

        int VisitSide(int source, int fragment, bool apply, CorticalAssemblyKind kind)
        {
            if (cortex.FragmentByCellId[source] >= 0 && cortex.FragmentByCellId[source] != fragment)
                return int.MaxValue;
            Array.Fill(distance, -1);
            int read = 0, write = 1, nearest = int.MaxValue;
            queue[0] = source;
            distance[source] = 0;
            while (read < write)
            {
                int cell = queue[read++], d = distance[cell];
                if (cortex.FragmentByCellId[cell] == fragment)
                {
                    nearest = Math.Min(nearest, d);
                    if (apply)
                    {
                        double weight = 1 - d / (range + 1.0);
                        // Bounded maxima avoid amplifying cells merely due to link count.
                        if (kind == CorticalAssemblyKind.Accreted)
                            uplift[cell] = Math.Max(uplift[cell], AccretionBonus * weight);
                        else subsidence[cell] = Math.Max(subsidence[cell], SeparationPenalty * weight);
                    }
                }
                if (d >= range) continue;
                foreach (int neighbor in topology.Neighbors(cell))
                    if (distance[neighbor] < 0 && plates[neighbor] == plates[source] &&
                        (cortex.FragmentByCellId[neighbor] == -1 || cortex.FragmentByCellId[neighbor] == fragment))
                    {
                        distance[neighbor] = d + 1;
                        queue[write++] = neighbor;
                    }
            }
            return nearest;
        }
    }

    private static int Sum(int first, int second) => first == int.MaxValue || second == int.MaxValue
        ? int.MaxValue : first + second;
}
