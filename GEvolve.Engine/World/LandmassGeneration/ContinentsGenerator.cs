using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>The sole active Continents route: official motion, crust, global sea level and geology.</summary>
public sealed class ContinentsGenerator : IPlateAwareLandmassGenerator
{
    public const string Version = "continents-tectonic-2";
    public LandmassType Type => LandmassType.Continents;
    public byte[] Generate(HexTopology topology, LandmassConfiguration config, int attempt)
        => Generate(topology, config, attempt, TectonicPlateGenerator.Generate(topology, config.Seed));
    public byte[] Generate(HexTopology topology, LandmassConfiguration config, int attempt, ReadOnlySpan<byte> plates)
    {
        var ids = plates.ToArray();
        var input = new LandmassState(config, new byte[topology.Count], attempt) { PlateByCellId = ids };
        var motion = new TectonicMotionGenerator().Generate(topology, input);
        return GenerateTerrain(topology, config, ids, motion, attempt).SurfaceByCellId;
    }

    public LandmassState GenerateTerrain(HexTopology topology, LandmassConfiguration config,
        byte[] plates, TectonicMotionState motion, int attempt = 0)
    {
        var profile = config.Continents ?? throw new ArgumentException("Continental settings required.");
        if (config.Type != Type) throw new ArgumentException("Only Continents use this route.");
        string? lastFailure = null;
        ContinentalCrustMap? bestCrust = null;
        byte[]? bestSurface = null;
        float bestSeaLevel = 0;
        int bestAssembly = 0;
        double bestError = double.PositiveInfinity;
        for (int assembly = 0; assembly < 32; assembly++)
        {
            var crust = new ContinentalCrustGenerator().Generate(topology, plates, profile.CoreCount,
                config.Seed, motion, attempt * 32 + assembly);
            try
            {
                var (surface, seaLevel) = SelectSurface(topology, crust, profile);
                double error = Math.Abs(LandmassAnalysis.Analyze(topology, surface).LandFraction - profile.TargetLandFraction);
                if (error < bestError)
                { bestError = error; bestCrust = crust; bestSurface = surface; bestSeaLevel = seaLevel; bestAssembly = assembly; }
                if (bestError <= .005) break;
            }
            catch (InvalidOperationException error) { lastFailure = error.Message; }
        }
        if (bestCrust is null || bestSurface is null)
            throw new InvalidOperationException($"No connected continental assembly met the geographic profile in 32 deterministic layouts. Last: {lastFailure}");
        return new ContinentalReliefGenerator().Generate(topology,
            new(config, bestSurface, attempt * 32 + bestAssembly) { PlateByCellId = plates, TectonicMotion = motion },
            bestCrust, bestSeaLevel);
    }

    private static (byte[] Surface, float SeaLevel) SelectSurface(HexTopology topology,
        ContinentalCrustMap crust, ContinentsParameters profile)
    {
        var sorted = crust.EmergenceAffinityByCellId.Order().ToArray();
        byte[]? best = null; float bestLevel = 0; double bestError = double.PositiveInfinity;
        var diagnostics = new List<string>();
        // One sea datum for the entire world. No land budget per craton.
        for (double fraction = .30; fraction <= .551; fraction += .01)
        {
            float level = sorted[Math.Clamp((int)((1 - fraction) * (sorted.Length - 1)), 0, sorted.Length - 1)];
            var emerged = new byte[topology.Count];
            for (int id = 0; id < emerged.Length; id++)
                if (crust.CrustByCellId[id] == 1 && crust.EmergenceAffinityByCellId[id] >= level) emerged[id] = 1;
            var stats = LandmassAnalysis.Analyze(topology, emerged);
            diagnostics.Add($"{fraction:F2}:{stats.LandFraction:F2}/{stats.ComponentSizes.Count(size => size >= 100)}");
            if (stats.ComponentSizes.Count(size => size >= 100) != profile.CoreCount ||
                !CratonsAnchorSeparateMainlands(topology, emerged, crust.MainContinentCoreCells) ||
                !LandmassGenerationService.MeetsProfile(LandmassType.Continents, stats)) continue;
            double error = Math.Abs(stats.LandFraction - profile.TargetLandFraction);
            if (error < bestError) { bestError = error; best = emerged; bestLevel = level; }
        }
        return best is null ? throw new InvalidOperationException($"Continental crust did not emerge as {profile.CoreCount} mainlands. " + string.Join(",", diagnostics))
            : (best, bestLevel);
    }

    private static bool CratonsAnchorSeparateMainlands(HexTopology topology, byte[] surface, int[] cratons)
    {
        var seen = new bool[topology.Count];
        var queue = new Queue<int>();
        foreach (int root in cratons)
        {
            if (surface[root] != 1 || seen[root]) return false;
            int cells = 0;
            seen[root] = true; queue.Enqueue(root);
            while (queue.TryDequeue(out int id))
            {
                cells++;
                foreach (int next in topology.Neighbors(id))
                    if (surface[next] == 1 && !seen[next])
                    { seen[next] = true; queue.Enqueue(next); }
            }
            if (cells < 100) return false;
        }
        return true;
    }
}
