namespace GEvolve.Definitions.WorldGeneration.Configuration;

/// <summary>Patrones de distribución de tierra para el generador.</summary>
public enum TipoMundo { ContinentesGrandes, ContinentesPequenos, Pangea, Archipielagos }

/// <summary>Edad geológica que modifica la intensidad del relieve.</summary>
public enum EdadGeologica { Joven, Normal, Antiguo }

/// <summary>Parámetros del mundo y escala de relieve.</summary>
public sealed record WorldSettings(int Ancho = 100, int Alto = 50, int Semilla = 2026,
    int TamanoChunk = 10, int NivelMar = 30,
    TipoMundo Tipo = TipoMundo.ContinentesGrandes,
    EdadGeologica Edad = EdadGeologica.Normal,
    int PorcentajeOceanos = 60, int IntensidadMontanosa = 40)
{
    /// <summary>Valida dimensiones y configuraciones del mapa.</summary>
    public void Validar()
    {
        if (Ancho < 1 || Alto < 1 || (long)Ancho * Alto > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(Ancho));
        if (TamanoChunk < 1 || TamanoChunk > 256)
            throw new ArgumentOutOfRangeException(nameof(TamanoChunk));
        if (!Enum.IsDefined(Tipo) || !Enum.IsDefined(Edad))
            throw new ArgumentOutOfRangeException(nameof(Tipo));
        if (PorcentajeOceanos < 5 || PorcentajeOceanos > 95)
            throw new ArgumentOutOfRangeException(nameof(PorcentajeOceanos));
        if (IntensidadMontanosa < 0 || IntensidadMontanosa > 100)
            throw new ArgumentOutOfRangeException(nameof(IntensidadMontanosa));
        if (NivelMar < 1 || NivelMar > 99)
            throw new ArgumentOutOfRangeException(nameof(NivelMar));
    }
}
