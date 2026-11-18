// Runs the CORE-01 through CORE-03 checks against the production engine and definitions assemblies.
// Returns a failing process exit code without opening a window or running geographic layers.

using Evolve.WorldGeneration.Checks;

int contractsResult = CoreContractChecks.Run();
int contextResult = GenerationContextChecks.Run();
int orchestratorResult = WorldGenerationOrchestratorChecks.Run();
return contractsResult == 0 && contextResult == 0 && orchestratorResult == 0 ? 0 : 1;
