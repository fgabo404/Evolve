namespace GEvolve.Core;

/// <summary>Genera continentes, mar y elevaciones reproducibles independientes de OpenGL.</summary>
public static class WorldSystem
{
    /// <summary>Genera un mapa rectangular de celdas hexagonales según configuración.</summary>
    public static WorldMap Generar(WorldSettings config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validar();
        var resultado = new WorldCell[config.Ancho * config.Alto];
        for (int fila = 0; fila < config.Alto; fila++)
        for (int columna = 0; columna < config.Ancho; columna++)
        {
            float x = (columna + 0.5f) / config.Ancho;
            float y = (fila + 0.5f) / config.Alto;
            float continente = MathF.Max(
                Masa(x, y, 0.28f, 0.43f, 0.25f, 0.31f),
                Masa(x, y, 0.74f, 0.60f, 0.24f, 0.32f));
            continente = MathF.Max(continente, Masa(x, y, 0.50f, 0.22f, 0.14f, 0.19f));
            // Cordilleras interiores: crestas periódicas en longitud para
            // conservar el relieve continuo al cruzar la costura E/O.
            float cresta = MathF.Pow(MathF.Abs(MathF.Sin(2f * MathF.PI *
                (4f * x + 1.6f * y + config.Semilla * 0.00017f))), 12f);
            float montana = continente * 0.37f * cresta;
            float altura = 0.24f + 0.62f * continente
                + 0.16f * Ondas(x, y, config.Semilla) + montana;
            // La franja polar reduce la tierra cerca del norte y sur.
            altura *= Math.Clamp(MathF.Min(y, 1f - y) / 0.12f, 0f, 1f);
            byte nivel = (byte)Math.Clamp((int)MathF.Round(100f * altura), 0, 100);
            TipoSuperficie tipo = nivel < config.NivelMar
                ? TipoSuperficie.Oceano : TipoSuperficie.Tierra;
            int q = columna - (fila - (fila & 1)) / 2 - config.Ancho / 2 + config.Alto / 4;
            int r = fila - config.Alto / 2;
            resultado[fila * config.Ancho + columna] =
                new WorldCell(columna, fila, new HexCoord(q, r), nivel, tipo)
                {
                    Relieve = ReglasRelieve.Clasificar(nivel, config.NivelMar)
                };
        }
        return new WorldMap(config, resultado);
    }

    private static float Masa(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = MathF.Abs(x - cx);
        dx = MathF.Min(dx, 1f - dx) / rx;
        float dy = (y - cy) / ry;
        return MathF.Max(0f, 1f - MathF.Sqrt(dx * dx + dy * dy));
    }

    private static float Ondas(float x, float y, int semilla)
    {
        // Frecuencias enteras en longitud: continuidad de ondas en la costura E/O.
        float fase = semilla * 0.01371f;
        float a = MathF.Sin(2f * MathF.PI * (3f * x + fase))
                * MathF.Cos(2f * MathF.PI * (2f * y - fase));
        float b = MathF.Cos(2f * MathF.PI * (7f * x - fase))
                * MathF.Sin(2f * MathF.PI * (5f * y + fase));
        return 0.7f * a + 0.3f * b;
    }
}
