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
    // La cuadrícula debe ser reproducible y tener identidades únicas.
    var cuadricula = HexDemoWorld.CrearCuadricula(20, 20);
    Verificar(cuadricula.Count == 400, "La cuadrícula debe tener 400 celdas.");
    Verificar(cuadricula.Select(x => x.Coordenada).Distinct().Count() == 400,
        "Coordenadas axiales únicas.");
    Verificar(cuadricula.All(x => float.IsFinite(x.Elevacion) && x.Elevacion > 0f),
        "Alturas válidas.");
    Verificar(HexDemoWorld.CrearCuadricula(20, 20).SequenceEqual(cuadricula),
        "Alturas reproducibles.");
    // Verificar el mapa radial, generado desde un único centro axial.
    var radial = HexDemoWorld.CrearRadial(11);
    Verificar(radial.Count == 397, "Radio 11 debe producir 397 hexágonos.");
    Verificar(radial.Select(x => x.Coordenada).Distinct().Count() == 397,
        "Las coordenadas radiales deben ser únicas.");
    Verificar(radial.Count(x => x.Coordenada == origen) == 1,
        "Debe existir un único hexágono central.");
    Verificar(radial.All(x => origen.Distancia(x.Coordenada) <= 11),
        "El mapa radial debe conservar su contorno hexagonal.");
    Verificar(radial.Count(x => origen.Distancia(x.Coordenada) == 11) == 66,
        "El anillo exterior debe tener 6 * radio celdas.");
    float cima = radial.Single(x => x.Coordenada == origen).Elevacion;
    Verificar(radial.All(x => x.Elevacion <= cima), "La cima central debe ser máxima.");
    Verificar(radial.Where(x => origen.Distancia(x.Coordenada) == 11)
        .All(x => MathF.Abs(x.Elevacion - 0.15f) < 0.0001f),
        "El borde debe tener la elevación mínima.");
    Verificar(radial.GroupBy(x => origen.Distancia(x.Coordenada))
        .OrderBy(x => x.Key).Select(x => x.First().Elevacion)
        .Zip(radial.GroupBy(x => origen.Distancia(x.Coordenada))
            .OrderBy(x => x.Key).Select(x => x.First().Elevacion).Skip(1),
            (a, b) => a > b).All(x => x),
        "La altura debe descender entre anillos.");
    Verificar(radial.SequenceEqual(HexDemoWorld.CrearRadial(11)),
        "La generación de prueba debe ser determinista.");
    Verificar(LanzaArgumento(() => HexDemoWorld.CrearRadial(-1)),
        "El radio negativo debe rechazarse.");
    // Verificación mínima del prototipo procedural sin requerir GPU.
    var mundo = WorldGenerator.Crear(100, 50, semilla: 2026);
    Verificar(mundo.Count == 5000, "El mundo debe contener 5000 hexágonos.");
    Verificar(mundo.Select(x => x.Coordenada).Distinct().Count() == 5000,
        "Las coordenadas del mundo deben ser únicas.");
    Verificar(mundo.Any(x => x.Superficie == TipoSuperficie.Oceano) &&
             mundo.Any(x => x.Superficie == TipoSuperficie.Tierra),
        "Deben existir tanto tierra como océanos.");
    Verificar(mundo.All(x => float.IsFinite(x.Elevacion) && x.Elevacion > 0f),
        "Todas las elevaciones deben ser válidas.");
    Verificar(mundo.SequenceEqual(WorldGenerator.Crear(100, 50, semilla: 2026)),
        "La misma semilla debe reproducir el mundo.");
    // Nuevo sistema de generación, regiones y topología este-oeste.
    var config = new WorldSettings(100, 50, 2026, 10, 30);
    var sistema = WorldSystem.Generar(config);
    Verificar(sistema.Celdas.Count == 5000, "Sistema: 5000 celdas.");
    Verificar(sistema.Chunks.Count == 50, "Sistema: 50 chunks de 10x10.");
    Verificar(sistema.Chunks.Sum(x => x.Celdas.Count) == 5000, "Chunks completos.");
    Verificar(sistema.Celdas.All(x => x.Altura <= 100), "Altura 0-100.");
    Verificar(sistema.Celdas.Any(x => x.Superficie == TipoSuperficie.Tierra)
        && sistema.Celdas.Any(x => x.Superficie == TipoSuperficie.Oceano),
        "Debe existir mar y tierra.");
    Verificar(sistema.Celdas.SequenceEqual(WorldSystem.Generar(config).Celdas),
        "La generación debe ser reproducible.");
    Verificar(sistema.IntentarObtener(-1, 20, out var oeste)
        && sistema.IntentarObtener(99, 20, out var este) && oeste == este,
        "El extremo oeste debe conectar con el este.");
    Verificar(!sistema.IntentarObtener(4, -1, out _)
        && !sistema.IntentarObtener(4, 50, out _),
        "Norte y sur no envuelven.");
    Console.WriteLine("OK — Sistema de mundo: elevación 0-100, chunks y topología.");
    Console.WriteLine("OK — Mundo procedural 100x50 y 5000 celdas verificadas.");
    Console.WriteLine("OK — Grid 20x20 y mapa radial de 397 hexágonos verificados.");
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
