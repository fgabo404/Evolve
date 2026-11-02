using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.State.Game;

/// <summary>
/// Configuración efectiva de la capa 01. Las celdas tienen identidad por orden
/// fila-columna y se reconstruyen exactamente; no se guardan copias de las celdas.
/// </summary>
public sealed record HexGridState(HexGridDefinition Definition);
