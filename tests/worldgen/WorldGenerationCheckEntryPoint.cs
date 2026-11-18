// Runs the CORE-01 and CORE-02 checks against the production engine and definitions assemblies.
// Returns a failing process exit code without opening a window or running geographic layers.

using Evolve.WorldGeneration.Checks;

int contractsResult = CoreContractChecks.Run();
int contextResult = GenerationContextChecks.Run();
return contractsResult == 0 && contextResult == 0 ? 0 : 1;
