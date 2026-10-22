namespace GEvolve.Core;

/// <summary>Perfiles geográficos del prototipo; el jugador elige tipo, tamaño y semilla.</summary>
public static class WorldPresets
{
    /// <summary>Devuelve edad, agua y montañas predefinidas por tipo de mundo.</summary>
    public static WorldSettings Aplicar(TipoMundo tipo, int ancho, int alto, int semilla)
    {
        // Aleatorio mantiene su selección determinista según la semilla.
        var (edad, oceanos, montanas) = tipo switch
        {
            TipoMundo.Pangea => (EdadGeologica.Normal, 45, 50),
            TipoMundo.Archipielagos => (EdadGeologica.Normal, 75, 35),
            TipoMundo.MarInterior => (EdadGeologica.Antiguo, 40, 30),
            TipoMundo.Fractal => (EdadGeologica.Joven, 60, 55),
            TipoMundo.Aleatorio => (EdadGeologica.Normal, 60, 40),
            _ => (EdadGeologica.Normal, 60, 40)
        };
        return new WorldSettings(ancho, alto, semilla, 10, 30, tipo, edad, oceanos, montanas);
    }
}
