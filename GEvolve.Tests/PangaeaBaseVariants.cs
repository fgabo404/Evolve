using System.Reflection;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;

/// <summary>Tests-only variants over detached official fields. Never returns a GeneratedWorld or save state.</summary>
internal static class PangaeaBaseVariants
{
    internal enum Kind { V0, V1, V1T, V2, V3 }
    internal sealed record Definition(Kind Id, string Parent, string Hypothesis);
    internal static readonly Definition[] Definitions = [
        new(Kind.V0, "Official", "Exact official percentile + ShapeUpperRelief + RecoverRegionalShoulders"),
        new(Kind.V1, "V0", "Remove legacy pressure from potential and summit support; keep official transformations"),
        new(Kind.V1T, "V1", "Bypass summit treatment only; retain noise-only potential and percentile normalization"),
        new(Kind.V2, "V1T", "Replace percentile normalization by fixed theoretical noise-amplitude scale; retain identity summit treatment"),
        new(Kind.V3, "V2", "Apply a monotone C1 soft upper-band compander; preserve continuous heights <=60")];
    internal const double RegionalWeight = 1.3, LocalWeight = .45, Shoulder = 60, LandMinimum = 1, LandMaximum = 100;
    internal const double NoiseAmplitude = RegionalWeight + LocalWeight;
    internal sealed record Input(byte[] Surface, byte[] Official, PangaeaBaseFields Fields,
        double[] Quantiles, double[] Levels, byte[] PercentileReference, byte[] ShapedReference)
    {
        internal static Input From(PangaeaGenerationSnapshot snapshot)
        {
            var source = snapshot.BaseSources ?? throw new ArgumentException("Completed official BASE sources required.");
            return new(snapshot.Surface!.ToArray(), snapshot.Base!.ToArray(), source.Fields ??
                throw new ArgumentException("Explicit captureBaseFields is required."), source.Quantiles.ToArray(),
                source.ElevationAnchors.ToArray(), source.PreviousBaseByCellId.ToArray(), source.PriorCalibrationBaseByCellId.ToArray());
        }
    }
    internal sealed record Result(Kind Variant, double[] Potential, double[] Anchors, byte[] Normalized, byte[] Heights);

    // Bind the existing pure transformations instead of copying or changing production algorithms.
    // A renamed/signature-changed method fails explicitly; no alternate implementation is substituted.
    private static readonly Func<byte[], byte[], double[], double[], byte[]> Shape = Method("ShapeUpperRelief")
        .CreateDelegate<Func<byte[], byte[], double[], double[], byte[]>>();
    private static readonly Action<byte[], byte[]> Recover = Method("RecoverRegionalShoulders").CreateDelegate<Action<byte[], byte[]>>();
    private static MethodInfo Method(string name) => typeof(PangaeaFromPlatesGenerator).GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException(typeof(PangaeaFromPlatesGenerator).FullName, name);

    internal static Result Generate(Input input, Kind kind)
        => GenerateCore(input, kind, kind == Kind.V0, kind == Kind.V0);

    internal static Result GeneratePressureControl(Input input, bool potentialPressure, bool supportPressure)
        => GenerateCore(input, Kind.V1, potentialPressure, supportPressure);

    private static Result GenerateCore(Input input, Kind kind, bool potentialPressure, bool supportPressure)
    {
        Validate(input);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        int count = input.Surface.Length;
        var potential = new double[count];
        for (int id = 0; id < count; id++)
            potential[id] = potentialPressure
                ? 2.5 * input.Fields.SmoothedPressure.Span[id] + RegionalWeight * input.Fields.RegionalNoise.Span[id] + LocalWeight * input.Fields.LocalNoise.Span[id]
                : RegionalWeight * input.Fields.RegionalNoise.Span[id] + LocalWeight * input.Fields.LocalNoise.Span[id];
        double[] anchors;
        var normalized = new byte[count];
        if (kind is Kind.V2 or Kind.V3)
        {
            anchors = [-NoiseAmplitude, NoiseAmplitude];
            for (int id = 0; id < count; id++) if (input.Surface[id] == 1) normalized[id] = Rounded(AbsoluteLevel(potential[id]));
        }
        else
        {
            double[] sorted = Enumerable.Range(0, count).Where(i => input.Surface[i] == 1).Select(i => potential[i]).Order().ToArray();
            anchors = input.Quantiles.Select(q => sorted[(int)Math.Round(q * (sorted.Length - 1))]).ToArray();
            for (int id = 0; id < count; id++)
            {
                if (input.Surface[id] == 0) continue;
                int section = 0; while (section < anchors.Length - 2 && potential[id] > anchors[section + 1]) section++;
                double span = anchors[section + 1] - anchors[section];
                double fraction = span > 1e-12 ? Math.Clamp((potential[id] - anchors[section]) / span, 0, 1) : 0;
                normalized[id] = Rounded(input.Levels[section] + fraction * (input.Levels[section + 1] - input.Levels[section]));
            }
        }
        byte[] result;
        if (kind is Kind.V0 or Kind.V1)
        {
            var pressure = supportPressure ? input.Fields.SmoothedPressure.ToArray() : new double[count];
            result = Shape(input.Surface, normalized, input.Fields.LocalNoise.ToArray(), pressure);
            if (kind == Kind.V0 && !result.AsSpan().SequenceEqual(input.ShapedReference))
                throw new InvalidOperationException("V0 must reproduce the official pre-recovery field byte-for-byte.");
            Recover(normalized, result);
        }
        else if (kind == Kind.V3)
        {
            result = new byte[count];
            for (int id = 0; id < count; id++) if (input.Surface[id] == 1) result[id] = Rounded(SoftUpper(AbsoluteLevel(potential[id])));
        }
        else result = normalized.ToArray();
        if (kind == Kind.V0 && (!normalized.AsSpan().SequenceEqual(input.PercentileReference) || !result.AsSpan().SequenceEqual(input.Official)))
            throw new InvalidOperationException("V0 is not the exact official BASE reference.");
        return new(kind, potential, anchors, normalized, result);
    }
    internal static double AbsoluteLevel(double potential) => LandMinimum + (LandMaximum - LandMinimum) *
        Math.Clamp((potential + NoiseAmplitude) / (2 * NoiseAmplitude), 0, 1);
    internal static double SoftUpper(double height)
    {
        if (height <= Shoulder) return height;
        double excess = height - Shoulder;
        return Shoulder + excess / (1 + excess / (LandMaximum - LandMinimum));
    }
    private static byte Rounded(double value) => (byte)Math.Clamp((int)Math.Round(value), 1, 100);
    internal static void Validate(Input input)
    {
        int n = input.Surface.Length;
        if (n == 0 || input.Surface.Any(v => v > 1) || !input.Surface.Contains((byte)1) || input.Official.Length != n ||
            input.PercentileReference.Length != n || input.ShapedReference.Length != n || input.Fields.SmoothedPressure.Length != n ||
            input.Fields.RegionalNoise.Length != n || input.Fields.LocalNoise.Length != n || input.Quantiles.Length != input.Levels.Length || input.Quantiles.Length < 2)
            throw new ArgumentException("Detached complete official BASE input required.");
        for (int id = 0; id < n; id++)
            if (!double.IsFinite(input.Fields.SmoothedPressure.Span[id]) || !double.IsFinite(input.Fields.RegionalNoise.Span[id]) ||
                !double.IsFinite(input.Fields.LocalNoise.Span[id]) || Math.Abs(input.Fields.RegionalNoise.Span[id]) > 1 || Math.Abs(input.Fields.LocalNoise.Span[id]) > 1)
                throw new ArgumentException("Finite original noise fields in [-1,1] required.");
    }
}
