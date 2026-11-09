using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Climate;

public sealed class TemperatureGenerator
{
    public const string Version = "temperature-2";
    private readonly TemperatureSettings _settings;

    public TemperatureGenerator(TemperatureSettings? settings = null)
    {
        _settings = settings ?? new();
        _settings.Validate();
    }

    // Multi-source BFS: O(cells + edges), including the horizontal seam.
    // -1 means no ocean is reachable; it receives no ocean influence.
    public static int[] OceanDistances(HexTopology topology, LandmassState terrain)
    {
        if (terrain.SurfaceByCellId.Length != topology.Count ||
            terrain.SurfaceByCellId.Any(value => value > 1))
            throw new ArgumentException("Invalid surface for climate.", nameof(terrain));
        var distances = Enumerable.Repeat(-1, topology.Count).ToArray();
        var queue = new Queue<int>();
        for (int id = 0; id < topology.Count; id++)
            if (!terrain.IsLand(id)) { distances[id] = 0; queue.Enqueue(id); }
        while (queue.TryDequeue(out int id))
            foreach (int neighbor in topology.Neighbors(id))
                if (distances[neighbor] < 0)
                {
                    distances[neighbor] = distances[id] + 1;
                    queue.Enqueue(neighbor);
                }
        return distances;
    }

    public double CalculateCelsius(double latitude, byte elevation, bool land,
        int oceanDistance, double regionalNoise)
    {
        if (!double.IsFinite(latitude) || Math.Abs(latitude) > 90 || elevation > 100 ||
            oceanDistance < -1 || !double.IsFinite(regionalNoise) || Math.Abs(regionalNoise) > 1)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        double polarWeight = Math.Pow(Math.Sin(latitude * Math.PI / 180), 2);
        double continental = _settings.EquatorCelsius +
            (_settings.PoleCelsius - _settings.EquatorCelsius) * polarWeight;
        double maritime = _settings.OceanEquatorCelsius +
            (_settings.OceanPoleCelsius - _settings.OceanEquatorCelsius) * polarWeight;
        double influence = !land ? 1 : oceanDistance < 0 ? 0 :
            _settings.CoastalStrength * Math.Exp(-oceanDistance / _settings.OceanDecayHexSteps);
        double annualMean = continental + influence * (maritime - continental) -
            elevation * _settings.MetresPerElevationLevel / 1000 * _settings.LapseCelsiusPerKilometre +
            _settings.RegionalAmplitudeCelsius * regionalNoise;
        return Math.Min(annualMean, _settings.MaximumBaseCelsius);
    }

    public LandmassState Apply(HexTopology topology, LandmassState terrain)
    {
        // Other generators have no elevations yet; do not fabricate their climate.
        if (terrain.ElevationByCellId is not { } heights) return terrain;
        if (topology.Grid.Definition != HexGridDefinitions.Get(terrain.Configuration.Size) ||
            heights.Length != topology.Count || heights.Any(value => value > 100))
            throw new ArgumentException("Invalid terrain for climate.", nameof(terrain));
        int[] distances = OceanDistances(topology, terrain);
        var temperatures = new short[topology.Count];
        var grid = topology.Grid.Definition;
        for (int id = 0; id < temperatures.Length; id++)
        {
            int column = id % grid.Columns, row = id / grid.Columns;
            double noise = PeriodicNoise.Sample(column, row, grid.Columns, grid.Rows,
                _settings.RegionalFrequency, terrain.Configuration.Seed);
            double value = CalculateCelsius(BiomeClassifier.Latitude(row, grid.Rows),
                heights[id], terrain.IsLand(id), distances[id], noise);
            temperatures[id] = checked((short)Math.Round(value * 10, MidpointRounding.AwayFromZero));
        }
        return terrain with { Climate = new(Version, temperatures) };
    }
}
