using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Engine;

/// <summary>Cuatro niveles funcionales de relieve; no son biomas.</summary>
public enum NivelRelieve { Mar, Medio, Alto, Impasable }

/// <summary>Reglas centrales de clasificación del relieve.</summary>
public static class ReglasRelieve
{
    /// <summary>El mar es nivel 0; tierra baja 1-59, alta 60-79 y cumbres 80-100.</summary>
    public static NivelRelieve Clasificar(byte altura, int nivelMar)
    {
        // NivelMar se conserva por compatibilidad de configuración: la nueva
        // topología usa siempre cero para agua y 1-100 para terreno emergido.
        if (altura == 0) return NivelRelieve.Mar;
        if (altura >= 80) return NivelRelieve.Impasable;
        return altura >= 60 ? NivelRelieve.Alto : NivelRelieve.Medio;
    }
}

/// <summary>Celda persistente con altura oficial de 0 a 100.</summary>
public readonly record struct WorldCell(int Columna, int Fila, HexCoord Coordenada,
    byte Altura, TipoSuperficie Superficie)
{
    /// <summary>Clasificación de relieve independiente del bioma.</summary>
    public NivelRelieve Relieve { get; init; }
    public Bioma Bioma { get; init; }
}

/// <summary>Región del mundo, independiente de OpenGL.</summary>
public sealed class WorldChunk
{
    /// <summary>Índice horizontal de la región.</summary>
    public int X { get; }
    /// <summary>Índice vertical de la región.</summary>
    public int Y { get; }
    /// <summary>Celdas pertenecientes a esta región.</summary>
    public IReadOnlyList<WorldCell> Celdas { get; }
    internal WorldChunk(int x, int y, IReadOnlyList<WorldCell> celdas)
        => (X, Y, Celdas) = (x, y, celdas);
}

/// <summary>Almacén inmutable con acceso a celdas y regiones.</summary>
public sealed class WorldMap
{
    private readonly WorldCell[] _celdas;
    /// <summary>Configuración usada al generar.</summary>
    public WorldSettings Configuracion { get; }
    /// <summary>Regiones agrupadas por tamaño fijo.</summary>
    public IReadOnlyList<WorldChunk> Chunks { get; }
    /// <summary>Resultado de pendientes y morfología indexado por fila y columna.</summary>
    public IReadOnlyList<AnalisisRelieve> Analisis { get; }
    /// <summary>Vista de las celdas del mundo.</summary>
    public IReadOnlyList<WorldCell> Celdas => _celdas;

    internal WorldMap(WorldSettings config, WorldCell[] celdas)
    {
        Configuracion = config;
        _celdas = celdas;
        var chunks = new List<WorldChunk>();
        for (int y = 0; y < config.Alto; y += config.TamanoChunk)
        for (int x = 0; x < config.Ancho; x += config.TamanoChunk)
        {
            var grupo = new List<WorldCell>();
            for (int fila = y; fila < Math.Min(y + config.TamanoChunk, config.Alto); fila++)
            for (int col = x; col < Math.Min(x + config.TamanoChunk, config.Ancho); col++)
                grupo.Add(_celdas[fila * config.Ancho + col]);
            chunks.Add(new WorldChunk(x / config.TamanoChunk, y / config.TamanoChunk, grupo));
        }
        Chunks = chunks;
        Analisis = AnalizadorRelieve.Analizar(this);
    }

    /// <summary>Consulta coordenadas offset; envuelve este-oeste y limita norte-sur.</summary>
    public bool IntentarObtener(int columna, int fila, out WorldCell celda)
    {
        if (fila < 0 || fila >= Configuracion.Alto)
        {
            celda = default;
            return false;
        }
        int x = ((columna % Configuracion.Ancho) + Configuracion.Ancho) % Configuracion.Ancho;
        celda = _celdas[fila * Configuracion.Ancho + x];
        return true;
    }
}
