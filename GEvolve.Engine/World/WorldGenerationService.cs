using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.Grid;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World;

/// <summary>
/// Stage 01 universal tectonics, then stage 02 surface and relief for finished world types.
/// Continents derives surface from plate groups and elevation from existing motion;
/// volcanism and climate remain pending.
/// Temperature, precipitation and biome classification remain shared future stages.
/// </summary>
public sealed class WorldGenerationService
{
    public const int MaxContinentalSelectionAttempts = 8;
    private readonly PangaeaFromPlatesGenerator _pangaea = new();
    private readonly PlateAwareElevationGenerator _relief = new();

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
            var opportunities = OrogenicOpportunityAnalyzer.Analyze(topology, tectonicMotion);
            if (opportunities.MarginCount == 0)
                throw new InvalidOperationException("Continents cannot form a principal mountain system: no connected convergent margin exists for the fixed plates and motion.");
            var surfaceGenerator = new PlateGroupSurfaceGenerator();
            var elevationGenerator = new ContinentalElevationGenerator();
            var attempts = new List<ContinentalSelectionAttempt>();
            GeneratedWorld? fallback = null;
            int fallbackAttempt = -1, fallbackScore = -1;
            bool preferMultiple = configuration.Size is GridSize.Large or GridSize.Enormous;
            for (int attempt = 0; attempt < MaxContinentalSelectionAttempts; attempt++)
            {
                PlateGroupSurfaceGenerator.Result generated;
                try { generated = surfaceGenerator.Generate(topology, plates, tectonicMotion,
                    configuration.Seed, attempt, opportunities); }
                catch (InvalidOperationException error)
                {
                    attempts.Add(new(attempt, 0, 0, $"surface rejected: {error.Message}"));
                    continue;
                }
                var statistics = LandmassAnalysis.Analyze(topology, generated.Surface);
                if (!MeetsContinentalSurfaceQuality(topology, generated, statistics))
                {
                    attempts.Add(new(attempt, 0, 0, "surface rejected: disconnected or undersized continental landmass"));
                    continue;
                }
                var terrain = new LandmassState(configuration, generated.Surface, 0)
                {
                    PlateByCellId = plates,
                    TectonicMotion = tectonicMotion,
                    ContinentalSurface = generated.State
                };
                // Keep the approved candidate ranking independent of the new belt geometry.
                terrain = elevationGenerator.Generate(topology, terrain, useRegionalBelts: false);
                int principal = terrain.ContinentalElevation!.Systems.Count(system =>
                    system.MountainTier == TectonicMountainTier.Principal);
                int secondary = terrain.ContinentalElevation.Systems.Count(system =>
                    system.MountainTier == TectonicMountainTier.Secondary);
                var world = new GeneratedWorld(grid, terrain, statistics);
                if (principal == 0)
                {
                    attempts.Add(new(attempt, principal, secondary, "rejected: no principal land mountain system"));
                    continue;
                }
                if (!preferMultiple || principal >= 2)
                {
                    attempts.Add(new(attempt, principal, secondary, "selected"));
                    return Finish(world, attempt);
                }
                attempts.Add(new(attempt, principal, secondary, "one principal system; seeking multiple"));
                if (principal * 100 + secondary > fallbackScore)
                {
                    fallback = world;
                    fallbackAttempt = attempt;
                    fallbackScore = principal * 100 + secondary;
                }
            }
            if (fallback is not null)
            {
                attempts[fallbackAttempt] = attempts[fallbackAttempt] with { Outcome =
                    "selected: one principal system; no better grouping within attempt limit" };
                return Finish(fallback, fallbackAttempt);
            }
            throw new InvalidOperationException($"Continents cannot form a principal land mountain system from the fixed plates and motion after {MaxContinentalSelectionAttempts} deterministic continental groupings (size={configuration.Size}, seed={configuration.Seed}).");

            GeneratedWorld Finish(GeneratedWorld selected, int selectedAttempt)
            {
                var surface = selected.Landmass.ContinentalSurface! with
                {
                    Selection = new(attempts.Count, selectedAttempt,
                        opportunities.MarginCount, attempts.ToArray())
                };
                var finalTerrain = elevationGenerator.Generate(topology,
                    selected.Landmass with { ContinentalSurface = surface });
                int finalPrincipal = finalTerrain.ContinentalElevation!.Systems.Count(system =>
                    system.MountainTier == TectonicMountainTier.Principal);
                int finalSecondary = finalTerrain.ContinentalElevation.Systems.Count(system =>
                    system.MountainTier == TectonicMountainTier.Secondary);
                surface = surface with { Selection = surface.Selection! with
                    { FinalPrincipalSystems = finalPrincipal, FinalSecondarySystems = finalSecondary } };
                return selected with { Landmass = finalTerrain with { ContinentalSurface = surface } };
            }
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
                if (!LandmassGenerationService.MeetsProfile(configuration.Type, last))
                    continue;
                elevation = _relief.Generate(
                    topology, surface, plates, configuration.Type, configuration.Seed, attempt);
            }

            if (configuration.Type == LandmassType.Pangaea)
            {
                last = LandmassAnalysis.Analyze(topology, surface);
                if (!LandmassGenerationService.MeetsProfile(configuration.Type, last))
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

    private static bool MeetsContinentalSurfaceQuality(HexTopology topology,
        PlateGroupSurfaceGenerator.Result generated, LandmassStatistics statistics)
    {
        if (statistics.LandFraction is < .33 or > .37) return false;
        var identities = generated.State.ContinentByCellId;
        int groups = generated.State.GroupByPlateId.Where(group => group != byte.MaxValue)
            .Distinct().Count();
        if (groups is < 2 or > 6) return false;
        for (byte group = 0; group < groups; group++)
        {
            var ownLand = new byte[topology.Count];
            for (int id = 0; id < ownLand.Length; id++)
                if (identities[id] == group) ownLand[id] = 1;
            var own = LandmassAnalysis.Analyze(topology, ownLand);
            if (own.LandCells < topology.Count * .01 || own.DominantLandShare < .50)
                return false;
        }
        return true;
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
