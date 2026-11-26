// Resolves a small prespecified CAL02 matrix from frozen Influence distributions before alternative runs.
// Reads exact CAL01 references and records reproducible parameters without selecting a Mantle profile.
using System.Buffers.Binary;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;
using static Evolve.WorldGeneration.Tectonics.Checks.MantleSpatialAlternatives;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleSpatialDesign
{
    internal static readonly long[] Seeds = [0, -1, long.MinValue, long.MaxValue];
    internal sealed record SurveyRow(string Size, long Seed, int Cells, int PositiveCells, double ZeroPercent, float Q25, float Q50, float Q75);
    internal sealed record Survey(SurveyRow[] Worlds, float Low, float Middle, float High);
    internal sealed record Candidate(string Name, string Factor, Operator Operation, Geometry SpatialPolicy, MantleCalibrationParameters Parameters);

    internal static Survey Resolve()
    {
        var worlds = new List<SurveyRow>();
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in Seeds)
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var influence = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            float[] positive = Enumerable.Range(0, grid.CellCount).Select(cell => (float)((double)influence[cell].Compression + influence[cell].Extension)).Where(value => value > 0).Order().ToArray();
            Require(positive.Length > 0, "Vref survey has no positive amplitudes.");
            worlds.Add(new(size.ToString(), seed, grid.CellCount, positive.Length, 100d * (grid.CellCount - positive.Length) / grid.CellCount,
                positive[(int)(.25 * (positive.Length - 1))], positive[(positive.Length - 1) / 2], positive[(int)(.75 * (positive.Length - 1))]));
        }
        float Median(IEnumerable<float> values) { float[] sorted = values.Order().ToArray(); return (float)(((double)sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2); }
        return new(worlds.ToArray(), Median(worlds.Select(row => row.Q25)), Median(worlds.Select(row => row.Q50)), Median(worlds.Select(row => row.Q75)));
    }

    internal static Candidate[] Matrix(Survey survey)
    {
        var p = new MantleCalibrationParameters(2, 6, 2, 2, 2, 1);
        return
        [
            new("CONTROL", "CONTROL", Operator.R0, Geometry.A0, p),
            new("R1-NATIVE", "RES-01", Operator.R1Native, Geometry.A0, p), new("R2-NATIVE", "RES-01", Operator.R2Native, Geometry.A0, p),
            new("R1-WIDTH", "RES-01", Operator.R1Width, Geometry.A0, p), new("R2-WIDTH", "RES-01", Operator.R2Width, Geometry.A0, p),
            new("A1", "ANISO-01", Operator.R0, Geometry.A1, p),
            new("LOW", "ALIAS-01", Operator.R0, Geometry.A0, p with { BroadFrequency=1, RegionalFrequency=3 }),
            new("HIGH", "ALIAS-01", Operator.R0, Geometry.A0, p with { BroadFrequency=4, RegionalFrequency=12 }),
            new("VREF-Q25", "VREF", Operator.R0, Geometry.A0, p with { ReferenceSpeed=survey.Low }),
            new("VREF-Q50", "VREF", Operator.R0, Geometry.A0, p with { ReferenceSpeed=survey.Middle }),
            new("VREF-Q75", "VREF", Operator.R0, Geometry.A0, p with { ReferenceSpeed=survey.High }),
            new("TECTONIC-DOM", "WEIGHT-CONTROL", Operator.R0, Geometry.A0, p with { TectonicWeight=4, BroadWeight=1, RegionalWeight=1 }),
            new("BROAD-DOM", "WEIGHT-CONTROL", Operator.R0, Geometry.A0, p with { TectonicWeight=1, BroadWeight=4, RegionalWeight=1 }),
            new("COMBINED-A1-R2", "COMBINED", Operator.R2Width, Geometry.A1, p),
            new("COMBINED-A1-LOW-R2", "COMBINED", Operator.R2Width, Geometry.A1, p with { BroadFrequency=1, RegionalFrequency=3 })
        ];
    }

    internal static string Hash(Result result)
    {
        byte[] bytes = new byte[checked(result.Count * 16)];
        for (int cell = 0; cell < result.Count; cell++)
        {
            var value = result[cell];
            Write(0, value.Potential); Write(4, value.FlowX); Write(8, value.FlowY); Write(12, value.SignedVertical);
            void Write(int offset, float f) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cell * 16 + offset), BitConverter.SingleToUInt32Bits(f));
        }
        return TectonicInfluenceVectors.Hash(bytes);
    }

    internal static void VerifyHistorical(string size, long seed, string candidate, string hash)
    {
        string original = candidate == "CONTROL" ? "BALANCED" : candidate;
        if (original is not ("BALANCED" or "TECTONIC-DOM" or "BROAD-DOM")) return;
        string path = Path.Combine(AppContext.BaseDirectory, "experiments", "mantle-cal01", "raw", "results.csv");
        var rows = File.ReadLines(path).Skip(1).Select(line => line.Split(',').Select(value => value.Trim('"')).ToArray())
            .Where(row => row[0] == size && row[1] == seed.ToString(System.Globalization.CultureInfo.InvariantCulture) && row[2] == original && row[3] == "1").ToArray();
        Require(rows.Length == 1 && rows[0][4] == hash, "Canonical CAL01 control hash differs.");
    }
}
