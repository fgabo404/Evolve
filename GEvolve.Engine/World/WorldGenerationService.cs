using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>
/// Stage 01 universal tectonics, then stage 02 surface and relief for finished world types.
/// Ordinary Continents generation stops after stage 01; its surface preview is explicit.
/// Temperature, precipitation and biome classification remain shared future stages.
/// </summary>
public sealed class WorldGenerationService
{
    private readonly PangaeaFromPlatesGenerator _pangaea = new();
    private readonly PlateAwareElevationGenerator _relief = new();

    /// <summary>Generates a world using the official profile for the selected size, type and seed.</summary>
    public GeneratedWorld Generate(GridSize size, LandmassType worldType, int seed = 2026)
        => Generate(LandmassProfiles.Create(size, worldType, seed));

    public GeneratedWorld Generate(LandmassConfiguration configuration)
    {
        Validate(configuration);
        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);

        // The same 16 plate IDs exist for every world type at the same size/seed.
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);
        var tectonicMotion = new TectonicMotionGenerator().Generate(topology,
            new LandmassState(configuration, [], 0) { PlateByCellId = plates, TectonicPreviewOnly = true });

        if (configuration.Type == LandmassType.Continents)
        {
            // No continental surface exists yet. Keep the actual plates and motion only.
            var pending = new LandmassState(configuration, [], 0)
            {
                PlateByCellId = plates,
                TectonicMotion = tectonicMotion,
                TectonicPreviewOnly = true,
                SurfaceGenerationPending = true
            };
            return new(grid, pending, null);
        }

        IPlateAwareLandmassGenerator? landmassGenerator = configuration.Type switch
        {
            LandmassType.Pangaea => null,
            LandmassType.SmallContinents => new SmallContinentsGenerator(),
            LandmassType.Archipelagos => new ArchipelagosGenerator(),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration))
        };

        LandmassStatistics? last = null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            byte[] surface;
            byte[] elevation;
            if (configuration.Type == LandmassType.Pangaea)
                (surface, elevation) = _pangaea.Generate(topology, configuration, plates, attempt);
            else
            {
                // Each existing land generator preserves its distinct distribution:
                // large separate continents, small landmasses or island chains.
                // Plate guidance gently influences their surface *and* shared relief.
                surface = landmassGenerator!.Generate(topology, configuration, attempt, plates);
                last = LandmassAnalysis.Analyze(topology, surface);
                if (!LandmassProfileValidator.MeetsProfile(configuration.Type, last))
                    continue;
                elevation = _relief.Generate(
                    topology, surface, plates, configuration.Type, configuration.Seed, attempt);
            }

            if (configuration.Type == LandmassType.Pangaea)
            {
                last = LandmassAnalysis.Analyze(topology, surface);
                if (!LandmassProfileValidator.MeetsProfile(configuration.Type, last))
                    continue;
            }

            var terrain = new LandmassState(configuration, surface, attempt)
            {
                PlateByCellId = plates,
                TectonicMotion = tectonicMotion,
                ElevationByCellId = elevation
            };
            return new GeneratedWorld(grid, terrain, last
                ?? throw new InvalidOperationException("Landmass statistics were not calculated."));
        }

        throw new InvalidOperationException(
            $"Stage 02 {configuration.Type} did not meet the profile after four attempts " +
            $"(land={last?.LandFraction:P1}, components={last?.ComponentCount}, " +
            $"dominant={last?.DominantLandShare:P1}).");
    }

    /// <summary>Explicit H05-B3 preview: surface available, relief/climate/biomes still absent.</summary>
    public GeneratedWorld GenerateContinentsSurfacePreview(LandmassConfiguration configuration)
        => BuildContinentsSurfacePreview(configuration, false).Reference;

    /// <summary>Session-only A/B comparison. Reference remains the persistible operational A.</summary>
    public (GeneratedWorld Reference, ContinentsSurfaceComparison Comparison,
        ContinentsCorticalAssemblyResult Assembly) CompareContinentsSurfacePreview(LandmassConfiguration configuration)
    {
        var result = BuildContinentsSurfacePreview(configuration, true);
        return (result.Reference, result.Comparison ??
            throw new InvalidOperationException("Regional comparison was not calculated."), result.Assembly);
    }

    private (GeneratedWorld Reference, ContinentsSurfaceComparison? Comparison,
        ContinentsCorticalAssemblyResult Assembly) BuildContinentsSurfacePreview(
        LandmassConfiguration configuration, bool compare)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Validate(configuration);
        if (configuration.Type != LandmassType.Continents)
            throw new ArgumentException("Surface preview is restricted to Continents.", nameof(configuration));
        // Reuse the ordinary official tectonics without changing its pending-surface contract.
        var tectonics = Generate(configuration);
        var topology = new HexTopology(tectonics.Grid);
        var plates = tectonics.Landmass.PlateByCellId!;
        var motion = tectonics.Landmass.TectonicMotion!;
        var cortex = new ContinentsCorticalFragmentGenerator().Generate(topology, configuration, plates, motion);
        var assembly = new ContinentsCorticalAssemblyGenerator().Assemble(topology, configuration, plates, motion, cortex);
        var generator = new ContinentsSurfaceGenerator();
        ContinentsSurfaceComparison? comparison = compare
            ? generator.CompareRegional(topology, configuration, plates, motion, assembly) : null;
        byte[] surface = comparison?.LocalizedSurface ?? generator.Generate(topology, configuration, plates, motion, assembly);
        var terrain = new LandmassState(configuration, surface, 0)
        {
            PlateByCellId = plates,
            TectonicMotion = motion,
            SharedCortex = SharedCorticalPersistence.ToState(assembly.Model),
            TectonicPreviewOnly = false,
            SurfaceGenerationPending = false
        };
        // Ordinary preview does not calculate B or retain experiment diagnostics.
        return (new GeneratedWorld(tectonics.Grid, terrain, LandmassAnalysis.Analyze(topology, surface)),
            comparison, assembly);
    }

    /// <summary>Explicit C1 experiment. A remains official even if every C candidate is rejected.</summary>
    public (GeneratedWorld Reference, C1ExperimentResult Experiment) CompareContinentsGeometryPreview(LandmassConfiguration configuration)
    {
        var reference = BuildContinentsSurfacePreview(configuration, false);
        var world = reference.Reference; var topology = new HexTopology(world.Grid);
        var summaries = new List<C1CandidateSummary>(); C1EvaluatedCandidate? selected = null;
        foreach (var candidate in new ContinentsMultiscaleCorticalGenerator().GenerateCandidates(topology, configuration,
            world.Landmass.PlateByCellId!, world.Landmass.TectonicMotion!, reference.Assembly.Model))
        {
            if (candidate.Model is null)
            {
                summaries.Add(new(candidate.Ordinal, candidate.PlannedDomains, candidate.CorticalBudget,
                    candidate.Solvers, candidate.Deficits, null, candidate.Failure));
                continue;
            }
            var assembly = new ContinentsCorticalAssemblyGenerator().Assemble(topology, configuration,
                world.Landmass.PlateByCellId!, world.Landmass.TectonicMotion!, candidate.Model);
            var surface = new ContinentsSurfaceGenerator().Generate(topology, configuration,
                world.Landmass.PlateByCellId!, world.Landmass.TectonicMotion!, assembly);
            var terrain = world.Landmass with { SurfaceByCellId = surface, SharedCortex = SharedCorticalPersistence.ToState(assembly.Model) };
            var geography = C1GeographicEvaluation.Evaluate(topology, terrain, candidate, assembly);
            summaries.Add(new(candidate.Ordinal, candidate.PlannedDomains, candidate.CorticalBudget,
                candidate.Solvers, candidate.Deficits, geography, candidate.Failure));
            var evaluated = new C1EvaluatedCandidate(candidate, assembly, terrain, geography);
            // Keep only one safe geometry; reports retain every candidate's outcome.
            if (selected is null || Rank(evaluated) < Rank(selected)) selected = evaluated;
        }
        return (world, new(summaries.ToArray(), selected,
            world.Statistics!.ComponentSizes.Count(area => area * 100 >= topology.Count * 3)));

        static int Rank(C1EvaluatedCandidate candidate) => (candidate.Geography.Status switch
        { C1GeographicStatus.Accepted => 0, C1GeographicStatus.ReviewRequired => 1, _ => 2 }) * 100 + candidate.Formation.Ordinal;
    }

    /// <summary>Stage 01 alone, independent of the selected map type.</summary>
    public GeneratedWorld GeneratePlatePreview(LandmassConfiguration configuration)
    {
        Validate(configuration);
        HexGrid grid = HexGrid.Generate(HexGridDefinitions.Get(configuration.Size));
        var topology = new HexTopology(grid);
        byte[] plates = TectonicPlateGenerator.Generate(topology, configuration.Seed);

        // Placeholder only: zero is not an actual ocean until stage 02 is run.
        var preview = new LandmassState(configuration, new byte[topology.Count], 0)
        {
            PlateByCellId = plates,
            TectonicPreviewOnly = true
        };
        preview = preview with { TectonicMotion = new TectonicMotionGenerator().Generate(topology, preview) };
        return new GeneratedWorld(grid, preview,
            new LandmassStatistics(0, topology.Count, [], 0));
    }

    private static void Validate(LandmassConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration != LandmassProfiles.Create(
                configuration.Size, configuration.Type, configuration.Seed))
            throw new ArgumentException("World configuration differs from official profile.",
                nameof(configuration));
    }
}
