namespace GEvolve.Core;

/// <summary>Genera topologías y alturas reproducibles, sin biomas ni dependencias gráficas.</summary>
public static class WorldSystem
{
    /// <summary>Construye un mundo con el porcentaje objetivo exacto de agua.</summary>
    public static WorldMap Generar(WorldSettings config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validar();
        int ancho = config.Ancho, alto = config.Alto, total = checked(ancho * alto);
        var campos = new float[total];
        TipoMundo tipo = config.Tipo == TipoMundo.Aleatorio
            ? (TipoMundo)(Math.Abs((long)config.Semilla) % 5) : config.Tipo;
        for (int fila = 0; fila < alto; fila++)
        for (int columna = 0; columna < ancho; columna++)
        {
            float x = (columna + .5f) / ancho;
            float y = (fila + .5f) / alto;
            float onda = Ondas(x, y, config.Semilla);
            float tierra = tipo switch
            {
                TipoMundo.Pangea => Masa(x, y, .5f, .49f, .38f, .43f),
                TipoMundo.Archipielagos => Archipielago(x, y, config.Semilla),
                TipoMundo.MarInterior => 1f - Masa(x, y, .51f, .51f, .25f, .27f),
                TipoMundo.Fractal => .45f + .32f * onda + .19f * Ondas(x, y, config.Semilla + 97),
                _ => MathF.Max(MathF.Max(
                    Masa(x, y, .28f, .43f, .25f, .31f),
                    Masa(x, y, .74f, .60f, .24f, .32f)),
                    Masa(x, y, .50f, .22f, .14f, .19f))
            };
            // Las orillas polares quedan por debajo del umbral oceánico.
            float polo = Math.Clamp(MathF.Min(y, 1f - y) / .075f, 0f, 1f);
            campos[fila * ancho + columna] = (tierra + .12f * onda) * polo;
        }

        // Ordenación estable para que el porcentaje solicitado sea exactamente
        // reproducible y no dependa de puntos de corte estimados.
        int cantidadAgua = (int)MathF.Round(total * config.PorcentajeOceanos / 100f);
        int[] indices = Enumerable.Range(0, total).ToArray();
        Array.Sort(indices, (a, b) =>
        {
            int comparacion = campos[a].CompareTo(campos[b]);
            return comparacion != 0 ? comparacion : a.CompareTo(b);
        });
        var esAgua = new bool[total];
        for (int i = 0; i < cantidadAgua; i++) esAgua[indices[i]] = true;
        float umbral = (campos[indices[cantidadAgua - 1]] +
                         campos[indices[cantidadAgua]]) * .5f;
        float rango = MathF.Max(.15f, campos.Max() - umbral);
        float edad = config.Edad switch
        {
            EdadGeologica.Joven => 1.25f,
            EdadGeologica.Antiguo => .75f,
            _ => 1f
        };

        var resultado = new WorldCell[total];
        for (int fila = 0; fila < alto; fila++)
        for (int columna = 0; columna < ancho; columna++)
        {
            int indice = fila * ancho + columna;
            float x = (columna + .5f) / ancho;
            float y = (fila + .5f) / alto;
            float interior = Math.Clamp((campos[indice] - umbral) / rango, 0f, 1f);
            float cresta = MathF.Pow(MathF.Abs(MathF.Sin(2f * MathF.PI *
                (4f * x + 1.6f * y + config.Semilla * .00017f))), 7f);
            float elevacion = 1f + 63f * MathF.Pow(interior, .72f)
                + 36f * (config.IntensidadMontanosa / 40f) * edad
                * MathF.Sqrt(interior) * cresta;
            byte nivel = esAgua[indice] ? (byte)0
                : (byte)Math.Clamp((int)MathF.Round(elevacion), 1, 100);
            TipoSuperficie superficie = esAgua[indice]
                ? TipoSuperficie.Oceano : TipoSuperficie.Tierra;
            int q = columna - (fila - (fila & 1)) / 2 - ancho / 2 + alto / 4;
            int r = fila - alto / 2;
            resultado[indice] = new WorldCell(columna, fila, new HexCoord(q, r),
                nivel, superficie) { Relieve = ReglasRelieve.Clasificar(nivel, config.NivelMar) };
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

    private static float Archipielago(float x, float y, int semilla)
    {
        float maximo = 0f;
        for (int i = 0; i < 18; i++)
        {
            // Centros deterministas sin Random ni estado compartido.
            float cx = Fraccion(MathF.Sin((i + 1) * 127.1f + semilla * .001f) * 43758.5453f);
            float cy = .12f + .76f * Fraccion(MathF.Sin((i + 1) * 311.7f + semilla * .003f) * 12741.635f);
            maximo = MathF.Max(maximo, Masa(x, y, cx, cy, .055f, .085f));
        }
        return maximo;
    }

    private static float Fraccion(float valor) => valor - MathF.Floor(valor);

    private static float Ondas(float x, float y, int semilla)
    {
        // Armónicos longitudinales enteros: superficie periódica E/O.
        float fase = semilla * .01371f;
        float a = MathF.Sin(2f * MathF.PI * (3f * x + fase))
                * MathF.Cos(2f * MathF.PI * (2f * y - fase));
        float b = MathF.Cos(2f * MathF.PI * (7f * x - fase))
                * MathF.Sin(2f * MathF.PI * (5f * y + fase));
        return .7f * a + .3f * b;
    }
}
