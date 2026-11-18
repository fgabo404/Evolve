// Runs the CORE-01 contract checks against the production engine and definitions assemblies.
// Returns a failing process exit code without opening a window or running geographic layers.

using Evolve.WorldGeneration.Checks;

return CoreContractChecks.Run();
