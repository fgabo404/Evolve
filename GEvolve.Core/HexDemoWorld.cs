namespace GEvolve.Core;

/// <summary>Una celda de demostración con coordenada axial y altura visual.</summary>
/// <param name="Coordenada">Identidad hexagonal estable.</param>
/// <param name="Elevacion">Altura de la tapa superior en unidades del mundo.</param>
public readonly record struct HexTile(HexCoord Coordenada, float Elevacion);

/// <summary>
/// Mapa mínimo de prueba: centro más seis vecinos.
/// No implementa todavía generación procedural, biomas ni recursos.
/// </summary>
public static class HexDemoWorld
{
    /// <summary>Devuelve siete celdas contiguas con alturas variadas y reproducibles.</summary>
    public static IReadOnlyList<HexTile> Crear()
    {
        HexCoord centro = new(0, 0);
        return new HexTile[]
        {
            new(centro, 0.95f),
            new(centro.Vecino(0), 0.40f),
            new(centro.Vecino(1), 0.62f),
            new(centro.Vecino(2), 0.24f),
            new(centro.Vecino(3), 0.77f),
            new(centro.Vecino(4), 0.48f),
            new(centro.Vecino(5), 0.30f)
        };
    }
}
