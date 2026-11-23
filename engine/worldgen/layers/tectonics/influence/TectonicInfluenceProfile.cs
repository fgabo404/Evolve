// Identifies the owner-selected HISTORICAL candidate and exact numeric inputs for E2-FINAL review.
// Remains internal and inactive in normal publication until separate integration and acceptance.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal static class TectonicInfluenceProfile
{
    internal const string Identifier = "tectonic-influence-historical-v1";
    internal const uint Revision = 1;
    internal static float RadiusFraction => BitConverter.UInt32BitsToSingle(0x3CA3D70A);
    internal static float MinimumRadius => BitConverter.UInt32BitsToSingle(0x3F800000);
    internal static float Cutoff => BitConverter.UInt32BitsToSingle(0x40400000);

    internal static TectonicInfluenceParameters Resolve(HexGridData grid) =>
        TectonicInfluenceParameters.Resolve(grid, MinimumRadius, RadiusFraction, Cutoff);
}
