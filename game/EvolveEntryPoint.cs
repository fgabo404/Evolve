// Starts the graphical application and owns the native window lifetime.
// Runs EvolveWindow and disposes it when the application loop ends.

using Evolve.Game;

using var window = new EvolveWindow();
window.Run();
