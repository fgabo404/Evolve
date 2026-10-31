using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>The sole active Continents route: official motion, crust, global sea level and geology.</summary>
public sealed class ContinentsGenerator : IPlateAwareLandmassGenerator
{
    public const string Version = "continents-tectonic-1";
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
        var crust = new ContinentalCrustGenerator().Generate(topology, plates, profile.CoreCount, config.Seed, motion);
        var (surface, seaLevel) = SelectSurface(topology, crust, profile);
        return new ContinentalReliefGenerator().Generate(topology,
            new(config, surface, attempt) { PlateByCellId = plates, TectonicMotion = motion }, crust, seaLevel);
    }

    private static (byte[] Surface, float SeaLevel) SelectSurface(HexTopology topology,
        ContinentalCrustMap crust, ContinentsParameters profile)
    {
        var sorted = crust.EmergenceAffinityByCellId.Order().ToArray();
        byte[]? best = null; float bestLevel = 0; double bestError = double.PositiveInfinity;
        // One sea datum for the entire world. No land budget per craton.
        for (double fraction = .30; fraction <= .551; fraction += .01)
        {
            float level = sorted[Math.Clamp((int)((1 - fraction) * (sorted.Length - 1)), 0, sorted.Length - 1)];
            var emerged = new byte[topology.Count];
            for (int id = 0; id < emerged.Length; id++)
                if (crust.CrustByCellId[id] == 1 && crust.EmergenceAffinityByCellId[id] >= level) emerged[id] = 1;
            var connected = new byte[topology.Count]; var visited = new bool[topology.Count]; var queue = new Queue<int>();
            foreach (int root in crust.MainContinentCoreCells)
            {
                emerged[root] = 1; visited[root] = true; queue.Enqueue(root);
                while (queue.TryDequeue(out int id))
                {
                    connected[id] = 1;
                    foreach (int next in topology.Neighbors(id))
                        if (!visited[next] && emerged[next] == 1 && crust.ProvinceByCellId[next] == crust.ProvinceByCellId[id])
                        { visited[next] = true; queue.Enqueue(next); }
                }
            }
            var stats = LandmassAnalysis.Analyze(topology, connected);
            if (stats.ComponentSizes.Count(size => size >= 100) != profile.CoreCount ||
                !LandmassGenerationService.MeetsProfile(LandmassType.Continents, stats)) continue;
            double error = Math.Abs(stats.LandFraction - profile.TargetLandFraction);
            if (error < bestError) { bestError = error; best = connected; bestLevel = level; }
        }
        return best is null ? throw new InvalidOperationException("Continental crust did not emerge as the configured mainlands.")
            : (best, bestLevel);
    }
}
