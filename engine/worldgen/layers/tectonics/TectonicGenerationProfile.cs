// Defines the exact owner-approved CAL01 R profile for fourteen connected tectonic plates.
// Freezes canonical IEEE-754 cost bits and numeric constants independently of game VERSION and schema version.
namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal sealed class TectonicGenerationProfile
{
    internal const int PlateCount = 14;
    internal const uint PartitionRevision = 2;
    internal const string AlgorithmVersion = "tectonic-partition-r-v2";
    internal static TectonicGenerationProfile Approved { get; } = new();
    internal int CandidateSamples => 160;
    internal double WarpFraction => 0.055;
    internal double Irregularity => 0.95;
    internal double MinimumResistance => 0.50;
    internal double MaximumResistance => 2.00;
    internal double BroadWeight => 1.65;
    internal double MediumWeight => 1.10;
    internal double FineWeight => 0.44;
    internal ReadOnlySpan<int> Frequencies => [3, 5, 9, 13, 21, 29];

    // Exact float words extracted from the complete CAL01 R profile, not recomputed from rounded report summaries.
    private static ReadOnlySpan<uint> CostBits =>
    [
        0x3F42F6BB, 0x3F4EA9FA, 0x3F5C589A, 0x3F694625, 0x3F77740E, 0x3F81F792, 0x3F883C29,
        0x3F8F0061, 0x3F964F03, 0x3F9E72F3, 0x3FA75A8E, 0x3FB10713, 0x3FBB2894, 0x3FC64BF8
    ];

    private TectonicGenerationProfile() { }
    internal float GetCost(int rank) => BitConverter.UInt32BitsToSingle(CostBits[rank]);
}
