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
    Verificar(sistema.Celdas.Where(x => x.Superficie == TipoSuperficie.Oceano)
        .All(x => x.Altura == 0 && x.Relieve == NivelRelieve.Mar),
        "Todos los hexágonos del océano deben ser de nivel 0.");
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
    Verificar(sistema.Celdas.All(x => x.Relieve ==
        ReglasRelieve.Clasificar(x.Altura, config.NivelMar)),
        "Cada celda debe respetar la clasificación oficial del relieve.");
    Verificar(ReglasRelieve.Clasificar(0, 30) == NivelRelieve.Mar
        && ReglasRelieve.Clasificar(40, 30) == NivelRelieve.Medio
        && ReglasRelieve.Clasificar(65, 30) == NivelRelieve.Alto
        && ReglasRelieve.Clasificar(80, 30) == NivelRelieve.Impasable,
        "Umbrales de relieve de cuatro niveles.");
    Verificar(sistema.Analisis.Count == 5000, "Un análisis de pendiente por hexágono.");
    Verificar(sistema.Analisis.All(x => x.PendienteMaxima <= 100 &&
        float.IsFinite(x.PendienteMedia)), "Pendientes válidas.");
    Verificar(sistema.Analisis.SequenceEqual(WorldSystem.Generar(config).Analisis),
        "Análisis de relieve reproducible.");
    Verificar(sistema.Celdas.Select((celda, i) => (celda, i))
        .Where(x => x.celda.Altura >= 80)
        .All(x => sistema.Analisis[x.i].PasoDificil),
        "Cumbres de nivel 80 o superior siempre restringidas.");
    var mundoGrande = WorldSystem.Generar(new WorldSettings(200, 150, 2026, 10, 30));
    Verificar(mundoGrande.Celdas.Count == 30_000, "Mapa 200x150 debe contener 30000 hexágonos.");
    Verificar(mundoGrande.Chunks.Count == 300, "El mapa grande tendrá 300 regiones lógicas.");
    Verificar(mundoGrande.IntentarObtener(-1, 80, out var extremoA)
        && mundoGrande.IntentarObtener(199, 80, out var extremoB)
        && extremoA == extremoB, "La continuidad este-oeste se conserva en 200x150.");
    Verificar(mundoGrande.Celdas.Where(x => x.Superficie == TipoSuperficie.Oceano)
        .All(x => x.Altura == 0), "Todos los niveles del mar deben ser cero.");
    Verificar(mundoGrande.Celdas.Where(x => x.Superficie == TipoSuperficie.Tierra)
        .All(x => x.Altura >= 1 && x.Relieve != NivelRelieve.Mar),
        "La tierra baja puede comenzar en nivel 1.");
    Verificar(mundoGrande.Celdas.Any(x => x.Superficie == TipoSuperficie.Tierra && x.Altura <= 10),
        "Deben existir costas bajas entre niveles 1 y 10.");
    Verificar(mundoGrande.Celdas.Any(x => x.Altura >= 60 && x.Altura < 80),
        "Deben existir elevaciones altas 60-79.");
    Verificar(mundoGrande.Celdas.Any(x => x.Altura >= 80),
        "Deben existir cumbres de nivel 80-100.");
    Verificar(ReglasRelieve.Clasificar(5, 30) == NivelRelieve.Medio,
        "Una costa de nivel 5 no es océano.");
    // Perfil aprobado de generación y verificación del porcentaje exacto.
    var perfil = new WorldSettings(200, 150, 2026, 10, 30,
        TipoMundo.Continentes, EdadGeologica.Normal, 60, 40);
    var generado = WorldSystem.Generar(perfil);
    Verificar(generado.Celdas.Count(x => x.Superficie == TipoSuperficie.Oceano) == 18_000,
        "El océano debe cubrir 60% de las 30000 celdas.");
    Verificar(generado.Celdas.All(x =>
        x.Superficie == TipoSuperficie.Oceano ? x.Altura == 0 : x.Altura is >= 1 and <= 100),
        "Mar 0 y tierra 1-100.");
    Verificar(generado.Celdas.SequenceEqual(WorldSystem.Generar(perfil).Celdas),
        "Misma semilla y configuración deben producir mismo mapa.");
    Verificar(!generado.Celdas.SequenceEqual(WorldSystem.Generar(perfil with
        { Tipo = TipoMundo.Pangea }).Celdas), "Cambiar tipo debe modificar el mapa.");
    Verificar(!generado.Celdas.SequenceEqual(WorldSystem.Generar(perfil with
        { Semilla = 2027 }).Celdas), "Cambiar semilla debe modificar el mapa.");
    foreach (TipoMundo variedad in Enum.GetValues<TipoMundo>())
    {
        var variante = WorldSystem.Generar(perfil with { Tipo = variedad });
        Verificar(variante.Celdas.Count == 30_000 &&
            variante.Celdas.Count(x => x.Superficie == TipoSuperficie.Oceano) == 18_000,
            "Cada tipo debe preservar dimensiones y cuota oceánica.");
    }
    Verificar(LanzaArgumento(() => WorldSystem.Generar(perfil with
        { PorcentajeOceanos = 101 })), "Porcentaje inválido rechazado.");
    Console.WriteLine("OK — Tipos de mundo, semilla y cuota oceánica de 60%.");
    Console.WriteLine("OK — Mundo 200x150, 30000 hexágonos, 300 chunks y mar nivel cero.");
    Console.WriteLine("OK — Pendientes, valles, crestas y pasos difíciles calculados.");
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
