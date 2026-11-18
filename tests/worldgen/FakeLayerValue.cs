// Supplies a value-only test payload for immutable per-stage storage checks.
// Represents neutral sample data, not a grid cell, geographic output or layer executor.

namespace Evolve.WorldGeneration.Checks;

internal readonly record struct FakeLayerValue(int Identifier, double Sample);
