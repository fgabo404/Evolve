using System.Numerics;
using GEvolve.Core;

// Pruebas ligeras sin paquetes externos. Fallan con código de salida distinto de cero.
// Alcance: matemáticas hexagonales, no requieren GPU ni abrir una ventana.
try
{
    var origen = new HexCoord(0, 0);
    Verificar(origen.S == 0, "La tercera coordenada debe ser -Q-R.");
    Verificar(new HexCoord(2, -3).S == 1, "Coordenadas cúbicas válidas.");

    var vecinos = Enumerable.Range(0, 6).Select(origen.Vecino).ToArray();
    Verificar(vecinos.Distinct().Count() == 6, "Debe haber seis vecinos únicos.");
    Verificar(vecinos.All(h => origen.Distancia(h) == 1), "Todos los vecinos están a distancia uno.");
    Verificar(origen.Distancia(new HexCoord(3, -2)) == 3, "Distancia axial correcta.");
    Verificar(origen.Distancia(new HexCoord(3, -2)) ==
             new HexCoord(3, -2).Distancia(origen), "Distancia simétrica.");

    // Inversión de centro para distintas coordenadas y tamaños de hexágono.
    foreach (float radio in new[] { 0.5f, 1f, 3f })
    foreach (int q in new[] { -20, -1, 0, 1, 19 })
    foreach (int r in new[] { -20, -1, 0, 1, 19 })
    {
        var actual = new HexCoord(q, r);
        var centro = HexGeometry.APosicion(actual, radio);
        var recuperado = HexGeometry.AHexagono(centro, radio);
        Verificar(actual == recuperado, $"Conversión reversible: {actual}, radio {radio}.");
    }

    Verificar(HexGeometry.AHexagono(Vector2.Zero) == origen, "Origen XY corresponde al origen hexagonal.");
    Verificar(LanzaArgumento(() => HexGeometry.APosicion(origen, 0f)), "Radio cero rechazado.");
    Verificar(LanzaArgumento(() => HexGeometry.AHexagono(Vector2.Zero, -1f)), "Radio negativo rechazado.");
    Verificar(LanzaArgumento(() => HexGeometry.AHexagono(new Vector2(float.NaN, 0f))), "Posición no finita rechazada.");
    Console.WriteLine("OK — Pruebas esenciales de geometría hexagonal superadas.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR — {ex.Message}");
    return 1;
}

/// <summary>Comprueba una invariante de geometría; detiene el proceso al fallar.</summary>
static void Verificar(bool condicion, string mensaje)
{
    if (!condicion) throw new InvalidOperationException(mensaje);
}

/// <summary>Verifica que una entrada inválida produce ArgumentOutOfRangeException.</summary>
static bool LanzaArgumento(Action accion)
{
    try { accion(); return false; }
    catch (ArgumentOutOfRangeException) { return true; }
}
