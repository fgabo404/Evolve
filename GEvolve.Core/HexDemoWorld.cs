namespace GEvolve.Core;

/// <summary>Una celda de demostración con coordenada axial y altura visual.</summary>
/// <param name="Coordenada">Identidad hexagonal estable.</param>
/// <param name="Elevacion">Altura de la tapa superior en unidades del mundo.</param>
public readonly record struct HexTile(HexCoord Coordenada, float Elevacion);

/// <summary>
/// Terrenos de demostración: anillo de siete celdas y cuadrícula de tamaño configurable.
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
    /// <summary>
    /// Construye una cuadrícula de ancho × alto hexágonos pointy-top.
    /// Utiliza coordenadas axiales centradas alrededor del origen para
    /// encuadrar cómodamente el mapa desde una cámara orbital.
    /// La altura es una onda matemática de prueba: NO es todavía
    /// generación procedural geográfica ni la escala oficial 0–100.
    /// </summary>
    /// <param name="ancho">Número de celdas en el eje axial Q.</param>
    /// <param name="alto">Número de celdas en el eje axial R.</param>
    public static IReadOnlyList<HexTile> CrearCuadricula(int ancho = 20, int alto = 20)
    {
        if (ancho <= 0 || alto <= 0 || (long)ancho * alto > 100_000)
            throw new ArgumentOutOfRangeException(nameof(ancho), "El tamaño del mapa debe ser positivo y razonable.");

        var resultado = new List<HexTile>(ancho * alto);
        int qInicio = -(ancho / 2);
        int rInicio = -(alto / 2);

        for (int r = 0; r < alto; r++)
        for (int q = 0; q < ancho; q++)
        {
            int axialQ = qInicio + q;
            int axialR = rInicio + r;

            // Relieve suave únicamente para comprobar alturas y contornos 3D.
            // Un generador real recibirá semilla y condiciones del mundo.
            float ondulacion = 0.30f * MathF.Sin(q * 0.39f)
                             + 0.22f * MathF.Cos(r * 0.44f)
                             + 0.12f * MathF.Sin((q + r) * 0.31f);
            float elevacion = Math.Clamp(0.70f + ondulacion, 0.15f, 1.40f);
            resultado.Add(new HexTile(new HexCoord(axialQ, axialR), elevacion));
        }
        return resultado;
    }

}
