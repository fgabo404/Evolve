using System.Security.Cryptography;
using System.Text.Json;
using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World;
using GEvolve.Engine.World.LandmassGeneration;
using GEvolve.Game.Screens;

public static class PangaeaBaseVariantControls
{
    public static void Run()
    {
        Check(PangaeaBaseVariants.AbsoluteLevel(-1.75) == 1 && PangaeaBaseVariants.AbsoluteLevel(0) == 50.5 &&
            PangaeaBaseVariants.AbsoluteLevel(1.75) == 100, "Theoretical amplitude uses original coefficients and official land scale");
        double previous = 1;
        for (int i = 10; i <= 1000; i++)
        {
            double h = i / 10.0, transformed = PangaeaBaseVariants.SoftUpper(h);
            Check(transformed >= previous && transformed <= h && (h > 60 || transformed == h), "Soft upper treatment is monotone, non-clipping, and preserves low/middle heights");
            previous = transformed;
        }
        Check(Math.Abs(PangaeaBaseVariants.SoftUpper(100) - (60 + 40.0 / (1 + 40.0 / 99))) < 1e-12, "Upper endpoint follows documented curve, no world maximum fit");
        var config = LandmassProfiles.Create(GridSize.Small, LandmassType.Pangaea, -1387338765);
        var service = new WorldGenerationService(); var session = service.CreatePangaeaSession(config, captureBaseFields: true);
        foreach (var stage in PangaeaGenerationSession.GeologicalStages.Take(5)) session.Execute(stage);
        var snapshot = session.GetSnapshot(); var input = PangaeaBaseVariants.Input.From(snapshot); string original = Hash(input);
        var results = PangaeaBaseVariants.Definitions.Select(d => PangaeaBaseVariants.Generate(input, d.Id)).ToArray();
        Check(original == Hash(input), "No variant can mutate input surface, official BASE, fields or anchors");
        var v0 = results[0]; var v1 = results[1]; var bridge = results[2]; var v2 = results[3]; var v3 = results[4];
        var perturbed = input with { Fields = input.Fields with { SmoothedPressure = Enumerable.Repeat(99.0, input.Surface.Length).ToArray() } };
        foreach (var kind in new[] { PangaeaBaseVariants.Kind.V1, PangaeaBaseVariants.Kind.V1T, PangaeaBaseVariants.Kind.V2, PangaeaBaseVariants.Kind.V3 })
            Check(PangaeaBaseVariants.Generate(perturbed, kind).Heights.SequenceEqual(results.Single(r => r.Variant == kind).Heights), "Regional variants are independent of legacy pressure, including summit support");
        Check(v0.Heights.SequenceEqual(snapshot.Base!) && v1.Potential.SequenceEqual(bridge.Potential) && bridge.Potential.SequenceEqual(v2.Potential) &&
            v2.Potential.SequenceEqual(v3.Potential) && v2.Normalized.SequenceEqual(v3.Normalized), "Controlled immediate-parent comparisons share unchanged factors");
        Check(Enumerable.Range(0, input.Surface.Length).All(i => input.Surface[i] == 0
            ? results.All(r => r.Heights[i] == 0) : results.All(r => r.Heights[i] is >= 1 and <= 100) &&
                (v2.Heights[i] > 60 || v3.Heights[i] == v2.Heights[i])), "Ocean remains zero; valid land; V3 preserves every low/middle value");
        Check(PangaeaLaboratoryLayout.VisibleStages.Length == 8 && session.GetStages().Count == 10, "No changes to eight visible actions or official internal DAG");
        byte old = results[0].Heights[0]; results[0].Heights[0] = 123;
        Check(input.Official[0] != 123 && session.GetSnapshot().Base![0] != 123, "Experimental output has no reference to official mutable buffers");
        results[0].Heights[0] = old;
        foreach (var stage in PangaeaGenerationSession.GeologicalStages.Skip(5)) session.Execute(stage);
        Check(Hash(session.GetCompletedWorld().Landmass) == Hash(service.GenerateGeology(config).Landmass), "Official FINAL unchanged after variants");
        Console.WriteLine("BASE_VARIANT_CONTROLS_PASS: V0 byte-exact F/G/H; immutable shared inputs; one-factor parent chain; fixed amplitude; monotone C1 summit treatment; ocean/land validity; eight laboratory actions; no official mutation or adoption");
    }
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
