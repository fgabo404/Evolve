// Validates the selected Influence policy, dense full fields and complete real M sources independently.
// Exercises immutable results and trust-boundary recomputation without enabling normal publication.
using System.Reflection;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class TectonicInfluenceFinalChecks
{
    internal static readonly long[] Seeds = [0, 1, -1, long.MinValue, long.MaxValue];

    internal static void PolicyAndDense()
    {
        Require(TectonicInfluenceProfile.Identifier == "tectonic-influence-historical-v1" && TectonicInfluenceProfile.Revision == 1 &&
            BitConverter.SingleToUInt32Bits(TectonicInfluenceProfile.RadiusFraction) == 0x3CA3D70A &&
            BitConverter.SingleToUInt32Bits(TectonicInfluenceProfile.MinimumRadius) == 0x3F800000 &&
            BitConverter.SingleToUInt32Bits(TectonicInfluenceProfile.Cutoff) == 0x40400000, "Selected policy exact bits.");
        foreach (var (size, radius, reach) in new[] { (MapSize.Small, 2f, 6), (MapSize.Medium, 3f, 9), (MapSize.Large, 4f, 12) })
        {
            var grid = new HexGridData(size);
            var parameters = TectonicInfluenceProfile.Resolve(grid);
            Require(parameters.Radius == radius && parameters.Cutoff == 3 && parameters.GetReach(grid) == reach, "Effective radius/reach.");
        }
        var small = new HexGridData(MapSize.Small);
        var samples = Enumerable.Range(0, small.CellCount).Select(cell => new TectonicInfluenceSource(cell,
            new((cell % 31 + 1) / 16f, (cell % 23 + 1) / 8f, (cell % 17 + 1) / 4f))).ToArray();
        var reference = new TectonicInfluenceOracle.Inputs(small, samples.Select(sample => sample.Channels).ToArray(),
            Enumerable.Repeat(true, small.CellCount).ToArray());
        var sources = TectonicInfluenceSources.Create(small, samples.Reverse(), Enumerable.Range(0, small.CellCount));
        var fields = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(small));
        var distances = TectonicBoundaryDistances.Build(sources);
        TectonicInfluenceOracle.CheckSources(reference, sources);
        TectonicInfluenceOracle.Compare(TectonicInfluenceOracle.Compute(reference, fields.Parameters), fields, distances);
        fields.ValidateComplete(sources, distances);
        var before = TectonicInfluenceVectors.Serialize(fields, distances);
        Array.Fill(samples, new TectonicInfluenceSource(0, new(99, 99, 99)));
        Require(before.SequenceEqual(TectonicInfluenceVectors.Serialize(fields, distances)), "Input mutations alter retained fields.");
        Require(sources.ActiveCount == small.CellCount, "Dense fixture must cover every source cell.");

        // Deliberately bypass encapsulation to test corrupted candidate detection; no mutator is added for testing.
        var storage = (TectonicInfluenceChannels[])typeof(TectonicInfluenceFields).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fields)!;
        var saved = storage[0];
        try
        {
            storage[0] = saved with { Compression = saved.Compression / 2 };
            fields.Validate(sources, distances);
            Expect<ArgumentException>(() => fields.ValidateComplete(sources, distances));
            storage[0] = new(float.NaN, 0, 0);
            Expect<ArgumentException>(() => fields.Validate(sources, distances));
        }
        finally { storage[0] = saved; }
        Require(before.SequenceEqual(TectonicInfluenceVectors.Serialize(fields, distances)), "Restored immutable fixture differs.");
        Console.WriteLine("E2-FINAL DENSE: all 20000 source cells; 60000 channel values and 20000 distances matched independent target BFS.");
    }

    internal static void CompleteOracles()
    {
        long cells = 0;
        foreach (var size in Enum.GetValues<MapSize>())
        foreach (long seed in Seeds)
        {
            var (grid, packed) = GenerateComplete(size, seed);
            var reference = TectonicInfluenceOracle.Extract(grid, packed);
            var sources = TectonicInfluenceSources.Extract(grid, TectonicData.FromLayerData(packed, grid));
            TectonicInfluenceOracle.CheckSources(reference, sources);
            var fields = TectonicInfluenceFields.Build(sources, TectonicInfluenceProfile.Resolve(grid));
            var distances = TectonicBoundaryDistances.Build(sources);
            TectonicInfluenceOracle.Compare(TectonicInfluenceOracle.Compute(reference, fields.Parameters), fields, distances);
            fields.ValidateComplete(sources, distances);
            cells += grid.CellCount;
            Console.WriteLine($"E2-FINAL ORACLE {size}/{seed}: all {grid.CellCount} sources/distances and {grid.CellCount * 3} channel values matched.");
        }
        Console.WriteLine($"E2-FINAL REAL ORACLES: 15 full maps; {cells} cells; {cells * 3} exact float comparisons; no target sampling.");
    }
}
