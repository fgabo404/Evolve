// Grants only the named headless check assembly access to internal storage transitions.
// Lets tests verify actual context ownership and publication without exposing public mutators or a fake orchestrator.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Evolve.WorldGeneration.Checks")]
