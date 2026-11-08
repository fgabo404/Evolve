using GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>Second isolated experiment. All constants are declared before measurement; no seed fitting.</summary>
internal static class PangaeaRegionalLowlandVariants
{
    internal sealed record Definition(string Id, string Parent, string Hypothesis, string Expected, string Risk);
    internal static readonly Definition[] Legacy = PangaeaBaseVariants.Definitions.Select(d =>
        new Definition(d.Id.ToString(), d.Parent, d.Hypothesis, "Historical reference", "Previously documented limitations")).ToArray();
    internal static readonly Definition[] Definitions = [.. Legacy,
        new("VP", "V0", "Remove pressure from summit support only; retain it in potential", "Isolate the support pathway", "Potential bands and forced maximum remain"),
        new("VS", "V1", "Add pressure to summit support only; keep noise-only potential", "Isolate support without potential pressure", "Can re-emphasize legacy bands"),
        new("V4O", "V2", "Change fixed vertical center only, 50.5 to 32.5", "Restore a low/middle continental reference", "Lower clipping can create floor plateaus"),
        new("V4S", "V4O", "Change fixed slope only, 99/3.5 to 65/2.6", "Reduce vertical spread without world fitting", "May remove useful regional highlands"),
        new("V4U", "V4O", "Compress only the continuous upper band above 50 with span 30", "Retain plains and tame upper regional relief", "Monotone curve cannot remove closed contour families"),
        new("V4C", "V4U", "Contextual compact coastal taper over half a local-noise wavelength", "Ease low-relief coastal transitions while protecting high/steep coasts", "Distance quantization may create shoreline terraces")];
    // Existing percentile levels 15/50/80 and regional coefficient 1.3 anchor the hypotheses.
    internal const double LowAnchor = 15, MiddleAnchor = 50, UpperAnchor = 80;
    internal const double Center = (LowAnchor + MiddleAnchor) / 2; // 32.5, not fitted to observed means
    internal const double OriginalSlope = 99 / 3.5;
    internal const double RevisedSlope = (UpperAnchor - LowAnchor) / (2 * PangaeaBaseVariants.RegionalWeight); //25
    internal const double UpperSpan = UpperAnchor - MiddleAnchor; //30
    internal const double RetainCoastalExcess = .5; //never replace lowlands by a constant sea-level strip
    internal const double ProtectedHeight = MiddleAnchor - 1; //one raster unit protects every rounded height >=50
    internal const double ReliefSpan = MiddleAnchor - LowAnchor; //35, existing low/middle interval
    internal const double LocalFrequency = 15; //existing D primary frequency, not a new noise sample

    internal static double Radius(HexTopology t) => Math.Min(t.Columns, t.Rows) / (2 * LocalFrequency);
    internal static double RawLevel(double potential, bool revisedScale) => Center + (revisedScale ? RevisedSlope : OriginalSlope) * potential;
    internal static double RegionalLevel(double potential, bool revisedScale) => Math.Clamp(RawLevel(potential, revisedScale), 1, 100);
    internal static double Upper(double h) => h <= MiddleAnchor ? h : MiddleAnchor + (h - MiddleAnchor) / (1 + (h - MiddleAnchor) / UpperSpan);
    private static double Smooth(double x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
    internal static double Coast(double h, double roughness, int coastDistance, double radius)
    {
        if (coastDistance >= radius || h >= ProtectedHeight || roughness >= ReliefSpan) return h;
        double spatial = 1 - Smooth(coastDistance / radius);
        double lowland = 1 - Smooth((h - LowAnchor) / (ProtectedHeight - LowAnchor));
        double gentle = 1 - Smooth(roughness / ReliefSpan);
        return 1 + (h - 1) * (1 - (1 - RetainCoastalExcess) * spatial * lowland * gentle);
    }
    internal static PangaeaBaseVariants.Result Generate(PangaeaBaseVariants.Input input, string id, PangaeaBaseVariantMetrics.Context context)
    {
        if (Enum.TryParse<PangaeaBaseVariants.Kind>(id, out var old)) return PangaeaBaseVariants.Generate(input, old);
        if (id == "VP") return PangaeaBaseVariants.GeneratePressureControl(input, true, false);
        if (id == "VS") return PangaeaBaseVariants.GeneratePressureControl(input, false, true);
        if (id is not ("V4O" or "V4S" or "V4U" or "V4C")) throw new ArgumentOutOfRangeException(nameof(id));
        PangaeaBaseVariants.Validate(input);
        int n = input.Surface.Length; var potential = new double[n]; var normalized = new byte[n]; var h = new double[n];
        for (int i = 0; i < n; i++)
        {
            potential[i] = 1.3 * input.Fields.RegionalNoise.Span[i] + .45 * input.Fields.LocalNoise.Span[i];
            if (input.Surface[i] == 0) continue;
            h[i] = RegionalLevel(potential[i], id == "V4S"); normalized[i] = Round(h[i]);
            if (id is "V4U" or "V4C") h[i] = Upper(h[i]);
        }
        var result = new byte[n];
        for (int i = 0; i < n; i++)
        {
            if (input.Surface[i] == 0) continue;
            double value = h[i];
            if (id == "V4C")
            {
                normalized[i] = Round(value); //same parent V4U raster, before coastal treatment
                double roughness = 0;
                foreach (int nb in context.Topology.Neighbors(i)) if (input.Surface[nb] == 1) roughness = Math.Max(roughness, Math.Abs(value - h[nb]));
                value = Coast(value, roughness, context.Coast[i], Radius(context.Topology));
            }
            result[i] = Round(value);
        }
        return new(PangaeaBaseVariants.Kind.V2, potential, [Center, id == "V4S" ? RevisedSlope : OriginalSlope], normalized, result);
    }
    private static byte Round(double h) => (byte)Math.Clamp((int)Math.Round(h), 1, 100);
    internal static void Controls()
    {
        if (RawLevel(0, false) != 32.5 || RawLevel(0, true) != 32.5 || RevisedSlope != 25)
            throw new InvalidOperationException("Independent center/scale anchors changed.");
        double last = 1;
        for (int i = 10; i <= 1000; i++) { double h = i / 10.0, u = Upper(h);
            if (u < last || u > h || h <= 50 && u != h) throw new InvalidOperationException("Upper curve is not monotone/preserving."); last = u; }
        if (Coast(55, 0, 0, 5) != 55 || Coast(25, 40, 0, 5) != 25 || Coast(25, 0, 5, 5) != 25)
            throw new InvalidOperationException("High/steep/outside coastal guards failed.");
        var levels = new[] { 8.0, 16, 25, 35, 45, 55 }.Select(h => Coast(h, 0, 0, 5)).ToArray();
        if (levels.Distinct().Count() != levels.Length || levels.Where((h, i) => h < 1 + .5 * (new[] { 8.0, 16, 25, 35, 45, 55 }[i] - 1)).Any())
            throw new InvalidOperationException("Coastal transition flattens or excessively removes low relief.");
        Console.WriteLine("LOWLAND_CONTROLS_PASS: independent offset/scale; monotone upper curve; varied coastal heights; high/steep/outside protection; no fixed flat coastal strip");
    }
}
