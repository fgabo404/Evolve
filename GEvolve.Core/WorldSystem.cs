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
        // La deformación de dominio da costas con golfos, cabos e istmos.
        // Todos los armónicos se repiten al atravesar X=0/1.
        for (int fila = 0; fila < alto; fila++)
        for (int columna = 0; columna < ancho; columna++)
        {
            float x = (columna + .5f) / ancho;
            float y = (fila + .5f) / alto;
            float wx = x + .045f * Ruido(x, y, config.Semilla + 11, 3, 3)
                         + .020f * Ruido(x, y, config.Semilla + 31, 8, 7);
            float wy = y + .050f * Ruido(x, y, config.Semilla + 23, 3, 3);
            float masa = config.Tipo switch
            {
                TipoMundo.Pangea => Masa(wx, wy, .50f, .50f, .38f, .40f)
                    + .24f * Masa(wx, wy, .32f, .42f, .19f, .25f),
                TipoMundo.Archipielagos => Archipielago(wx, wy, config.Semilla),
                TipoMundo.ContinentesPequenos => MaximoMasas(wx, wy, config.Semilla, 9,
                    .105f, .145f),
                _ => MaximoMasas(wx, wy, config.Semilla, 4, .205f, .30f)
            };
            float detalle = .14f * Ruido(x, y, config.Semilla + 53, 5, 5)
                + .070f * Ruido(x, y, config.Semilla + 73, 12, 12)
                + .025f * Ruido(x, y, config.Semilla + 97, 25, 20);
            float polar = Math.Clamp(MathF.Min(y, 1f - y) / .065f, 0f, 1f);
            campos[fila * ancho + columna] = (masa + detalle) * polar;
        }

        // Ordenación estable para que el porcentaje solicitado sea exactamente
        // reproducible y no dependa de puntos de corte estimados.
        int cantidadAgua = Math.Clamp((int)MathF.Round(total * config.PorcentajeOceanos / 100f),
            0, total);
        int[] indices = Enumerable.Range(0, total).ToArray();
        Array.Sort(indices, (a, b) =>
        {
            int comparacion = campos[a].CompareTo(campos[b]);
            return comparacion != 0 ? comparacion : a.CompareTo(b);
        });
        var esAgua = new bool[total];
        for (int i = 0; i < cantidadAgua; i++) esAgua[indices[i]] = true;
        float umbral = cantidadAgua == 0 ? campos.Min() - .001f
            : cantidadAgua == total ? campos.Max() + .001f
            : (campos[indices[cantidadAgua - 1]] + campos[indices[cantidadAgua]]) * .5f;
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

    /// <summary>Masas continentales con centros y proporciones reproducibles.</summary>
    private static float MaximoMasas(float x, float y, int semilla, int cantidad, float rx, float ry)
    {
        float maximo = 0f;
        for (int i = 0; i < cantidad; i++)
        {
            float cx = Fraccion(MathF.Sin((i + 1) * 127.1f + semilla * .001f) * 43758.5453f);
            float cy = .14f + .72f * Fraccion(MathF.Sin((i + 1) * 311.7f + semilla * .003f) * 12741.635f);
            float ancho = rx * (.7f + .6f * Fraccion(MathF.Sin(i * 83.7f + semilla) * 17621f));
            float alto = ry * (.7f + .6f * Fraccion(MathF.Sin(i * 32.7f + semilla) * 8171f));
            maximo = MathF.Max(maximo, Masa(x, y, cx, cy, ancho, alto));
        }
        return maximo;
    }

    private static float Archipielago(float x, float y, int semilla)
        => MaximoMasas(x, y, semilla, 35, .050f, .073f);

    /// <summary>Ruido suavizado multioctava, periódico en longitud.</summary>
    private static float Ruido(float x, float y, int semilla, int fx, int fy)
    {
        float px = x * fx, py = y * fy;
        int ix = (int)MathF.Floor(px), iy = (int)MathF.Floor(py);
        float tx = px - ix, ty = py - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Hash((ix % fx + fx) % fx, iy, semilla);
        float b = Hash(((ix + 1) % fx + fx) % fx, iy, semilla);
        float d = Hash((ix % fx + fx) % fx, iy + 1, semilla);
        float e = Hash(((ix + 1) % fx + fx) % fx, iy + 1, semilla);
        return ((a + (b - a) * tx) * (1f - ty) + (d + (e - d) * tx) * ty) * 2f - 1f;
    }

    private static float Hash(int x, int y, int semilla)
    {
        unchecked
        {
            uint h = (uint)semilla * 747796405u + (uint)x * 2891336453u + (uint)y * 277803737u;
            h ^= h >> 16; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215f;
        }
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
