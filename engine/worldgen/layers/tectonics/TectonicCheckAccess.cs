// Grants the named headless checks access to tectonic numeric vectors and private calibration metrics.
// Tests use this assembly grant only for Tectonics; no public test hooks or context mutators are introduced.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Evolve.WorldGeneration.Tectonics.Checks")]
