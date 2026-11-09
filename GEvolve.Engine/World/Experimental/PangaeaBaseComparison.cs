using GEvolve.Engine.World.LandmassGeneration;

namespace GEvolve.Engine.World.Experimental;

/// <summary>Transient laboratory choices, never world configuration or persisted state.</summary>
public enum PangaeaBaseVariant { V0, V1, V4O, V4U }

/// <summary>
/// Opt-in pure comparison of detached, captured official fields. No RNG, topology,
/// pipeline execution or mutation. P03-A.8.2b tests remain independent references.
/// </summary>
public static class PangaeaBaseComparison
{
    public static string Label(PangaeaBaseVariant variant) => variant switch
    {
        PangaeaBaseVariant.V0 => "V0 — Oficial",
        PangaeaBaseVariant.V1 => "V1 — Sin presión",
        PangaeaBaseVariant.V4O => "V4O — Regional",
        PangaeaBaseVariant.V4U => "V4U — Regional moderada",
        _ => throw new ArgumentOutOfRangeException(nameof(variant))
    };

    public static byte[] Generate(PangaeaGenerationSnapshot snapshot, PangaeaBaseVariant variant)
    {
        if (!Enum.IsDefined(variant)) throw new ArgumentOutOfRangeException(nameof(variant));
        var surface = snapshot.Surface;
        var official = snapshot.Base;
        if (surface is null || official is null || surface.Length != official.Length ||
            surface.Length == 0 || surface.Any(v => v > 1) || !surface.Contains((byte)1))
            throw new ArgumentException("Completed official surface and BASE required.", nameof(snapshot));
        if (variant == PangaeaBaseVariant.V0) return official.ToArray();
        var source = snapshot.BaseSources ?? throw new ArgumentException("Captured BASE sources required.", nameof(snapshot));
        var fields = source.Fields ?? throw new ArgumentException("Explicit captureBaseFields required.", nameof(snapshot));
        int n = surface.Length;
        if (fields.RegionalNoise.Length != n || fields.LocalNoise.Length != n ||
            source.Quantiles.Length < 2 || source.Quantiles.Length != source.ElevationAnchors.Length)
            throw new ArgumentException("Captured field dimensions differ.", nameof(snapshot));
        var potential = new double[n];
        for (int id = 0; id < n; id++)
        {
            double c = fields.RegionalNoise.Span[id], d = fields.LocalNoise.Span[id];
            if (!double.IsFinite(c) || !double.IsFinite(d) || Math.Abs(c) > 1 || Math.Abs(d) > 1)
                throw new ArgumentException("Original finite noise in [-1,1] required.", nameof(snapshot));
            potential[id] = 1.3 * c + .45 * d;
        }
        static byte Round(double value) => (byte)Math.Clamp((int)Math.Round(value), 1, 100);
        var heights = new byte[n];
        if (variant == PangaeaBaseVariant.V1)
        {
            double[] sorted = Enumerable.Range(0, n).Where(i => surface[i] == 1).Select(i => potential[i]).Order().ToArray();
            double[] anchors = source.Quantiles.Select(q => sorted[(int)Math.Round(q * (sorted.Length - 1))]).ToArray();
            for (int id = 0; id < n; id++)
            {
                if (surface[id] == 0) continue;
                int section = 0; while (section < anchors.Length - 2 && potential[id] > anchors[section + 1]) section++;
                double span = anchors[section + 1] - anchors[section];
                double fraction = span > 1e-12 ? Math.Clamp((potential[id] - anchors[section]) / span, 0, 1) : 0;
                heights[id] = Round(source.ElevationAnchors[section] + fraction * (source.ElevationAnchors[section + 1] - source.ElevationAnchors[section]));
            }
            var shaped = PangaeaFromPlatesGenerator.ShapeUpperRelief(surface, heights, fields.LocalNoise.ToArray(), new double[n]);
            PangaeaFromPlatesGenerator.RecoverRegionalShoulders(heights, shaped);
            return shaped;
        }
        for (int id = 0; id < n; id++)
        {
            if (surface[id] == 0) continue;
            double h = Math.Clamp(32.5 + (99 / 3.5) * potential[id], 1, 100);
            if (variant == PangaeaBaseVariant.V4U && h > 50)
                h = 50 + (h - 50) / (1 + (h - 50) / 30);
            heights[id] = Round(h);
        }
        return heights;
    }
}
