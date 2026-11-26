// Exercises experimental Model A inputs, seed isolation, controlled signals and independent complete edge oracles.
// Keeps candidate parameters test-only and verifies earlier frozen results remain unchanged.
using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleCalibrationChecks
{
    internal readonly record struct Candidate(string Name, MantleCalibrationParameters Parameters);
    internal static readonly Candidate[] Candidates =
    [
        new("TECTONIC-DOM", new(2,6,4,1,1,1)), new("BALANCED", new(2,6,2,2,2,1)),
        new("BROAD-DOM", new(2,6,1,4,1,1)), new("REGIONAL-DOM", new(2,6,1,1,4,1)),
        new("TECTONIC-ONLY", new(2,6,1,0,0,1)), new("NOISE-ONLY", new(2,6,0,1,1,1)),
        new("LOW-FREQUENCY", new(1,3,2,2,2,1)), new("HIGH-FREQUENCY", new(4,12,2,2,2,1))
    ];

    internal static void ParametersAndSignals()
    {
        var good = Candidates[1].Parameters;
        foreach (int frequency in new[] { int.MinValue, -1, 0, int.MaxValue })
            Expect<ArgumentOutOfRangeException>(() => (good with { BroadFrequency = frequency }).Validate());
        Expect<ArgumentOutOfRangeException>(() => (good with { RegionalFrequency = 2 }).Validate());
        Expect<ArgumentOutOfRangeException>(() => (good with { RegionalFrequency = int.MaxValue }).Validate());
        Expect<ArgumentOutOfRangeException>(() => default(MantleCalibrationParameters).Validate());
        foreach (float bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Expect<ArgumentException>(() => (good with { TectonicWeight = bad }).Validate());
            Expect<ArgumentException>(() => (good with { BroadWeight = bad }).Validate());
            Expect<ArgumentException>(() => (good with { RegionalWeight = bad }).Validate());
            Expect<ArgumentException>(() => (good with { ReferenceSpeed = bad }).Validate());
        }
        Expect<ArgumentException>(() => (good with { ReferenceSpeed = 0 }).Validate());
        Expect<ArgumentException>(() => (good with { TectonicWeight = 0, BroadWeight = 0, RegionalWeight = 0 }).Validate());
        Expect<ArgumentException>(() => MantlePotentialField.Evaluate(float.NaN, 0, 0, 0, good));
        Expect<ArgumentException>(() => MantlePotentialField.Evaluate(0, -1, 0, 0, good));
        Expect<ArgumentException>(() => MantlePotentialField.Evaluate(0, 0, double.PositiveInfinity, 0, good));
        Expect<ArgumentException>(() => MantlePotentialField.Evaluate(0, 0, 0, 1.1, good));
        Require(MantlePotentialField.Evaluate(0, 0, 1, 1, good with { TectonicWeight = 0 }) == 1 &&
            MantlePotentialField.Evaluate(0, 0, -1, -1, good with { TectonicWeight = 0 }) == -1, "Potential endpoint bounds.");
        Require(BitConverter.SingleToUInt32Bits(MantlePotentialField.Evaluate(-0f, 0, -0d, 0, good)) == 0, "Canonical potential zero.");
        double numerator = ((double)good.TectonicWeight * ((7d - 3) / ((1d + 7) + 3)) + good.BroadWeight * 0.25) + good.RegionalWeight * -0.75;
        Require(MantlePotentialField.Evaluate(3, 7, .25, -.75, good) == (float)(numerator / 6), "Explicit evaluation order.");
        Require(BinaryPrimitives.ReadUInt64LittleEndian(Encoding.ASCII.GetBytes("WG01MNT1")) == MantleNoiseFields.ExperimentalDomain, "ASCII seed domain.");
        var grid = new HexGridData(MapSize.Small);
        foreach (long seed in TectonicInfluenceFinalChecks.Seeds)
        {
            ulong state = DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics) ^ 0x31544E4D31304757;
            ulong Next()
            {
                state = unchecked(state + 0x9E3779B97F4A7C15UL); ulong z = state;
                z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
                z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
                return z ^ (z >> 31);
            }
            var expected = (Next(), Next());
            Require(MantleNoiseFields.Seeds(seed) == expected, "Candidate seed stream order.");
            var untouched = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
            var baseline = new DeterministicStageRandom(DeterministicStageRandom.DeriveSeed(seed, GenerationStage.Tectonics));
            for (int draw = 0; draw < 3; draw++) Require(untouched.NextUInt64() == baseline.NextUInt64(), "Reserved stream prefix.");
            var noise = MantleNoiseFields.Build(grid, Request(MapSize.Small, seed), good);
            Require(untouched.NextUInt64() == baseline.NextUInt64(), "Mantle consumed original R/M draws.");
            foreach (int cell in new[] { 0, 199, 10000, 19999 })
            {
                var center = Evolve.Engine.WorldGeneration.Layers.Grid.Geometry.FlatHexGeometry.Center(grid.GetOffset(cell));
                double period = 1.5 * grid.Columns;
                double first = MantleNoiseFields.Sample(grid, center.X, center.Y, good.BroadFrequency, expected.Item1);
                double wrapped = MantleNoiseFields.Sample(grid, center.X + period, center.Y, good.BroadFrequency, expected.Item1);
                Require(Math.Abs(first - wrapped) <= 256 * Math.ScaleB(1d, -52), "Periodic sampler roundoff at repeated coordinate.");
                Require(first == noise.Broad(cell), "Physical-center sampling.");
            }
        }
    }

    internal static void ControlledAndEdges()
    {
        var grid = new HexGridData(MapSize.Small);
        float[] values = Enumerable.Repeat(.25f, grid.CellCount).ToArray();
        var constant = MantlePotentialField.FromValues(values); values[0] = -1;
        var flat = MantleConvectionField.Build(grid, constant); flat.Validate(grid);
        for (int cell = 0; cell < grid.CellCount; cell++) Require(flat[cell] == new MantleCell(.25f, 0, 0, 0), "Constant potential or input ownership.");
        float[] anomaly = new float[grid.CellCount]; anomaly[0] = 1; anomaly[grid.CellCount / 2] = -1;
        var field = MantleConvectionField.Build(grid, MantlePotentialField.FromValues(anomaly));
        MantleIndependentOracle.Check(grid, field); field.Validate(grid);
        Require(field[0].SignedVertical > 0 && field[grid.CellCount / 2].SignedVertical < 0 && field[grid.Columns - 1].SignedVertical < 0 &&
            field[grid.CellCount - grid.Columns].SignedVertical == 0, "Up/down anomaly, seam and bounded poles.");
        var parameters = Candidates.Single(candidate => candidate.Name == "TECTONIC-ONLY").Parameters;
        Require(MantlePotentialField.Evaluate(2, 0, .9, .9, parameters) < 0 && MantlePotentialField.Evaluate(0, 2, -.9, -.9, parameters) > 0,
            "Controlled convergent/divergent forcing.");
        var zero = TectonicInfluenceSources.Create(grid, [], [0]);
        var influence = TectonicInfluenceFields.Build(zero, TectonicInfluenceProfile.Resolve(grid));
        var noise = MantleNoiseFields.Build(grid, Request(MapSize.Small, 0), Candidates[5].Parameters);
        var noisePotential = MantlePotentialField.Combine(grid, influence, noise, Candidates[5].Parameters);
        var noTectonics = MantleConvectionField.Build(grid, noisePotential);
        MantleIndependentOracle.Check(grid, noTectonics);
        Require(Enumerable.Range(0, grid.CellCount).Any(cell => noTectonics[cell].SignedVertical > 0) &&
            Enumerable.Range(0, grid.CellCount).Any(cell => noTectonics[cell].SignedVertical < 0), "Noise-only circulation without tectonic signal.");
        Expect<ArgumentException>(() => MantlePotentialField.FromValues([float.NaN]));
        Expect<ArgumentException>(() => MantlePotentialField.FromValues([1.01f]));
        Expect<ArgumentException>(() => MantleConvectionField.Build(grid, MantlePotentialField.FromValues([0])));
        var storage = ((float X, float Y, float W)[])typeof(MantleConvectionField).GetField("flows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
        var saved = storage[0];
        try { storage[0] = (float.NaN, 0, 0); Expect<ArgumentException>(() => field.Validate(grid)); }
        finally { storage[0] = saved; }
        field.Validate(grid);
    }

    internal static void ReplayAndIntegrity()
    {
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        {
            var (grid, packed) = GenerateComplete(size, seed);
            uint[] original = packed.ToArray();
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            var influence = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources);
            string influenceHash = TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances));
            foreach (var candidate in Candidates)
            {
                var noise = MantleNoiseFields.Build(grid, Request(size, seed), candidate.Parameters);
                var potential = MantlePotentialField.Combine(grid, influence, noise, candidate.Parameters);
                var field = MantleConvectionField.Build(grid, potential); field.Validate(grid);
                MantleIndependentOracle.CheckPotential(grid, influence, noise, candidate.Parameters, potential);
                MantleIndependentOracle.Check(grid, field);
                var replayNoise = MantleNoiseFields.Build(grid, Request(size, seed, WorldType.RealEarth), candidate.Parameters);
                var replay = MantleConvectionField.Build(grid, MantlePotentialField.Combine(grid, influence, replayNoise, candidate.Parameters));
                for (int cell = 0; cell < grid.CellCount; cell++) Require(field[cell] == replay[cell], "Complete replay/world-type independence.");
            }
            Require(original.SequenceEqual(packed) && packed[1] == 1 && packed[2] == 7 &&
                influenceHash == TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence, distances)), "Earlier outputs or normal publication changed.");
            Console.WriteLine($"MANTLE CAL01 {size}/{seed}: all 8 candidates, complete potential/edge/replay checks; R/M/Influence unchanged.");
        }
    }
}
