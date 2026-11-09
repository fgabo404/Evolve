using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Climate;

/// <summary>Deterministic 2-D moisture transport on the existing hex topology.</summary>
public sealed class PrecipitationGenerator
{
    public const string Version = "precipitation-2";
    private const int Iterations = 72;
    private const int AveragedIterations = 12;

    private static double SmoothStep(double a, double b, double x)
    {
        double t = Math.Clamp((x - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }

    // East positive, south positive. Smooth trade / westerly / polar transitions.
    public static (double East, double South) Wind(double latitude, double perturbation = 0)
    {
        if (!double.IsFinite(latitude) || Math.Abs(latitude) > 90 ||
            !double.IsFinite(perturbation) || Math.Abs(perturbation) > 1)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        double a = Math.Abs(latitude);
        double westerly = SmoothStep(23, 41, a) * (1 - SmoothStep(57, 74, a));
        double equatorward = latitude >= 0 ? 1 : -1;
        return (-1 + 2 * westerly + .24 * perturbation,
            equatorward * (.32 - .75 * westerly) + .36 * perturbation);
    }

    public static double CirculationRainFactor(double latitude)
    {
        double a = Math.Abs(latitude);
        return Math.Clamp(.95 +
            .42 * Math.Exp(-Math.Pow(a / 16, 2)) -
            .52 * Math.Exp(-Math.Pow((a - 29) / 10, 2)) +
            .28 * Math.Exp(-Math.Pow((a - 52) / 15, 2)), .30, 1.55);
    }

    public LandmassState Apply(HexTopology topology, LandmassState terrain)
    {
        if (terrain.ElevationByCellId is not { } heights ||
            terrain.Climate is not { } climate) return terrain;
        int count = topology.Count, width = topology.Columns, rows = topology.Rows;
        if (terrain.SurfaceByCellId.Length != count || heights.Length != count ||
            heights.Any(h => h > 100) ||
            climate.BaseTemperatureTenthsByCellId.Length != count)
            throw new ArgumentException("Invalid climate and terrain dimensions.", nameof(terrain));

        // Precompute a compact 6-neighbour advection network.
        var targets = new int[checked(count * 6)];
        var weights = new double[targets.Length];
        var neighbourCounts = new byte[count];
        var condensation = new double[count];
        var evaporation = new double[count];
        for (int id = 0; id < count; id++)
        {
            int row = id / width, column = id % width;
            double lat = BiomeClassifier.Latitude(row, rows);
            double perturbation = PeriodicNoise.Sample(column, row, width, rows, 3,
                terrain.Configuration.Seed + 87119);
            var wind = Wind(lat, perturbation);
            double weightSum = 0, windwardRise = 0;
            foreach (int nb in topology.Neighbors(id))
            {
                int dx = nb % width - column;
                if (dx > width / 2) dx -= width;
                if (dx < -width / 2) dx += width;
                int dy = nb / width - row;
                double projection = wind.East * dx + wind.South * dy;
                if (projection > 0)
                {
                    int slot = id * 6 + neighbourCounts[id]++;
                    targets[slot] = nb;
                    weights[slot] = projection;
                    weightSum += projection;
                }
                else if (projection < 0 && terrain.IsLand(id))
                    windwardRise = Math.Max(windwardRise,
                        Math.Max(0, (int)heights[id] - heights[nb]));
            }
            if (weightSum > 0)
                for (int i = 0; i < neighbourCounts[id]; i++)
                    weights[id * 6 + i] /= weightSum;
            bool land = terrain.IsLand(id);
            double warmth = Math.Clamp((climate.BaseCelsius(id) + 15) / 45, .15, 1.25);
            evaporation[id] = (land ? .010 : .175) * warmth;
            double orographic = land ? Math.Min(.35, windwardRise * .020) : 0;
            condensation[id] = Math.Clamp(
                (land ? .092 : .080) * CirculationRainFactor(lat) + orographic, .025, .58);
        }

        var vapor = new double[count];
        var next = new double[count];
        var rainTotal = new double[count];
        for (int pass = 0; pass < Iterations; pass++)
        {
            Array.Clear(next);
            for (int id = 0; id < count; id++)
            {
                double air = vapor[id] + evaporation[id];
                double rainfall = air * condensation[id];
                if (pass >= Iterations - AveragedIterations) rainTotal[id] += rainfall;
                double remaining = air - rainfall;
                double transported = remaining * .82;
                next[id] += remaining - transported;
                int offset = id * 6, n = neighbourCounts[id];
                if (n == 0) next[id] += transported;
                else for (int i = 0; i < n; i++)
                    next[targets[offset + i]] += transported * weights[offset + i];
            }
            (vapor, next) = (next, vapor);
        }

        var precipitation = new ushort[count];
        var moisture = new byte[count];
        for (int id = 0; id < count; id++)
        {
            double mm = rainTotal[id] / AveragedIterations * 6500;
            precipitation[id] = (ushort)Math.Clamp((int)Math.Round(mm), 0, ushort.MaxValue);
            double demand = 380 + Math.Max(0, climate.BaseCelsius(id)) * 28;
            moisture[id] = (byte)Math.Clamp((int)Math.Round(100 * mm / (mm + demand)), 0, 100);
        }
        return terrain with { Climate = climate with {
            BasePrecipitationMmByCellId = precipitation,
            BaseMoistureByCellId = moisture,
            PrecipitationModelVersion = Version
        } };
    }
}
