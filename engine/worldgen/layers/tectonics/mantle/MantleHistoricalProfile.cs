// Defines the strictly identified BALANCED historical Mantle candidate without enabling normal publication.
// Delegates unchanged A0 signals, potential arithmetic and R0 fluxes; formal freezing remains an owner decision.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal readonly record struct MantleProfileIdentity(string Identifier, uint Revision, string Model, string Operator,
    string Sampling, string NumericPolicy, ulong SeedDomain);

internal static class MantleHistoricalProfile
{
    internal const string Identifier = "tectonic-mantle-balanced-historical-v1";
    internal const uint Revision = 1;
    internal static MantleProfileIdentity Identity => new(Identifier, Revision, "A", "R0", "A0",
        "sequential-double-potential-and-edges-final-float-v1", 0x31544E4D31304757);
    internal static MantleCalibrationParameters Parameters => new(2, 6, Weight, Weight, Weight, BitConverter.UInt32BitsToSingle(0x3F800000));
    private static float Weight => BitConverter.UInt32BitsToSingle(0x40000000);

    internal static void Validate(MantleProfileIdentity identity, MantleCalibrationParameters parameters)
    {
        parameters.Validate();
        if (identity != Identity || identity.SeedDomain != MantleNoiseFields.ExperimentalDomain ||
            parameters.BroadFrequency != 2 || parameters.RegionalFrequency != 6 ||
            BitConverter.SingleToUInt32Bits(parameters.TectonicWeight) != 0x40000000 ||
            BitConverter.SingleToUInt32Bits(parameters.BroadWeight) != 0x40000000 ||
            BitConverter.SingleToUInt32Bits(parameters.RegionalWeight) != 0x40000000 ||
            BitConverter.SingleToUInt32Bits(parameters.ReferenceSpeed) != 0x3F800000)
            throw new ArgumentException("Unsupported Mantle candidate identity or exact parameters.");
    }

    internal static MantleConvectionField Build(HexGridData grid, GenerationRequest request, TectonicInfluenceFields influence)
    {
        Validate(Identity, Parameters);
        var noise = MantleNoiseFields.Build(grid, request, Parameters);
        var potential = MantlePotentialField.Combine(grid, influence, noise, Parameters);
        var result = MantleConvectionField.Build(grid, potential);
        result.Validate(grid);
        return result;
    }
}
