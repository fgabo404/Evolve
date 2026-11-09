using System.Numerics;

namespace GEvolve.Engine;

/// <summary>
/// Conversiones matemáticas entre coordenadas hexagonales axiales y puntos
/// del plano. Usa hexágonos pointy-top (vértice hacia arriba) de radio constante.
/// No depende de OpenTK ni del motor gráfico.
/// </summary>
public static class HexGeometry
{
    /// <summary>
    /// Radio estándar de una celda: 1 unidad del mundo desde centro a vértice.
    /// No representa píxeles; todos los hexágonos comparten este radio.
    /// </summary>
    public const float RadioEstandar = 1f;

    // Aproximación de √3, suficiente para las transformaciones en float.
    private const float Raiz3 = 1.7320508075688772f;

    /// <summary>
    /// Devuelve la posición XY del centro de una celda axial.
    /// El radio es la distancia del centro a cualquiera de sus vértices.
    /// </summary>
    /// <param name="hex">Coordenadas axiales (Q, R) de la celda.</param>
    /// <param name="radio">Radio del hexágono, estrictamente positivo.</param>
    /// <returns>Centro de la celda en unidades del mundo.</returns>
    public static Vector2 APosicion(HexCoord hex, float radio = 1f)
    {
        ValidarRadio(radio);

        float x = radio * Raiz3 * (hex.Q + hex.R / 2f);
        float y = radio * 1.5f * hex.R;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Obtiene la celda hexagonal más cercana a una posición XY.
    /// Primero transforma a coordenadas cúbicas fraccionarias y después
    /// corrige el redondeo para cumplir Q + R + S = 0.
    /// En bordes exactos, el resultado puede depender del redondeo.
    /// </summary>
    /// <param name="posicion">Punto en coordenadas del mundo.</param>
    /// <param name="radio">Radio del hexágono, estrictamente positivo.</param>
    /// <returns>Coordenadas axiales de la celda más cercana.</returns>
    public static HexCoord AHexagono(Vector2 posicion, float radio = 1f)
    {
        ValidarRadio(radio);
        if (!float.IsFinite(posicion.X) || !float.IsFinite(posicion.Y))
            throw new ArgumentOutOfRangeException(nameof(posicion), "La posición debe ser finita.");

        float q = (Raiz3 / 3f * posicion.X - posicion.Y / 3f) / radio;
        float r = (2f / 3f * posicion.Y) / radio;
        float s = -q - r;

        int rq = checked((int)MathF.Round(q));
        int rr = checked((int)MathF.Round(r));
        int rs = checked((int)MathF.Round(s));

        float dq = MathF.Abs(rq - q);
        float dr = MathF.Abs(rr - r);
        float ds = MathF.Abs(rs - s);

        if (dq > dr && dq > ds)
            rq = -rr - rs;
        else if (dr > ds)
            rr = -rq - rs;

        return new HexCoord(rq, rr);
    }

    /// <summary>Rechaza radios nulos, negativos, infinitos o NaN.</summary>
    private static void ValidarRadio(float radio)
    {
        if (!float.IsFinite(radio) || radio <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radio), "El radio debe ser finito y mayor que cero.");
    }
}
