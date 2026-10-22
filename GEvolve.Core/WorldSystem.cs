namespace GEvolve.Core;

/// <summary>Genera topologías, alturas y biomas reproducibles, sin dependencias gráficas.</summary>
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
            // Satélites continentales: islotes y archipiélagos periféricos.
            // Se suman ANTES de fijar la cuota de océano.
            float satelites = MaximoMasas(wx, wy, config.Semilla + 1307,
                config.Tipo == TipoMundo.Archipielagos ? 48 : 24, .035f, .053f);
            masa = MathF.Max(masa, .80f * satelites);
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
        // Distancia hexagonal a la costa mediante BFS multifuente.
        // Se usa topología offset por filas, con envolvimiento horizontal.
        var distancia = new int[total];
        Array.Fill(distancia, int.MaxValue);
        var cola = new Queue<int>();
        for (int i = 0; i < total; i++)
            if (esAgua[i]) { distancia[i] = 0; cola.Enqueue(i); }
        while (cola.Count > 0)
        {
            int i = cola.Dequeue(), y = i / ancho, x = i % ancho;
            int[] dx = (y & 1) == 0 ? new[] { -1, 1, -1, 0, -1, 0 } : new[] { -1, 1, 0, 1, 0, 1 };
            int[] dy = { 0, 0, -1, -1, 1, 1 };
            for (int n = 0; n < 6; n++)
            {
                int ny = y + dy[n];
                if (ny < 0 || ny >= alto) continue;
                int nx = (x + dx[n] + ancho) % ancho;
                int vecino = ny * ancho + nx;
                if (distancia[vecino] <= distancia[i] + 1) continue;
                distancia[vecino] = distancia[i] + 1;
                cola.Enqueue(vecino);
            }
        }
        // Distancia desde cualquier celda marina hasta la tierra más próxima.
        // Se utiliza para colorear plataformas costeras de forma coherente.
        var distanciaTierra = new int[total];
        Array.Fill(distanciaTierra, int.MaxValue);
        var colaMarina = new Queue<int>();
        for (int i = 0; i < total; i++)
            if (!esAgua[i]) { distanciaTierra[i] = 0; colaMarina.Enqueue(i); }
        while (colaMarina.Count > 0)
        {
            int i = colaMarina.Dequeue(), y = i / ancho, x = i % ancho;
            int[] dx = (y & 1) == 0 ? new[] { -1, 1, -1, 0, -1, 0 } : new[] { -1, 1, 0, 1, 0, 1 };
            int[] dy = { 0, 0, -1, -1, 1, 1 };
            for (int n = 0; n < 6; n++)
            {
                int ny = y + dy[n];
                if (ny < 0 || ny >= alto) continue;
                int nx = (x + dx[n] + ancho) % ancho, j = ny * ancho + nx;
                if (distanciaTierra[j] <= distanciaTierra[i] + 1) continue;
                distanciaTierra[j] = distanciaTierra[i] + 1;
                colaMarina.Enqueue(j);
            }
        }
        float edad = config.Edad switch
        {
            EdadGeologica.Joven => 1.15f,
            EdadGeologica.Antiguo => .80f,
            _ => 1f
        };
        float montanosidad = config.IntensidadMontanosa / 40f;
        float[] cordilleras = GeneradorCordilleras.Crear(ancho, alto, config.Semilla,
            esAgua, distancia, config.Tipo);
        var resultado = new WorldCell[total];
        for (int fila = 0; fila < alto; fila++)
        for (int columna = 0; columna < ancho; columna++)
        {
            int indice = fila * ancho + columna;
            float x = (columna + .5f) / ancho;
            float y = (fila + .5f) / alto;
            int costa = distancia[indice];
            // Llanuras amplias y graduales; el litoral siempre permanece bajo.
            float suavidad = Ruido(x, y, config.Semilla + 181, 4, 4);
            float llanura = 3f + 1.45f * MathF.Min(costa, 14) + 5f * suavidad;
            // Mesetas regionales suavizadas: altitud interior sin picos aislados.
            float meseta = Math.Clamp((Ruido(x, y, config.Semilla + 191, 3, 3) + .25f) * 1.9f, 0f, 1f);
            float factorInterior = Math.Clamp((costa - 3f) / 10f, 0f, 1f);
            llanura += 27f * meseta * factorInterior;
            // Cordilleras conectadas por semilla: laderas y cumbres distribuidas
            // en trayectorias locales, sin bandas que atraviesen todo el planeta.
            float pendienteCostera = Math.Clamp((costa - 1f) / 4f, 0f, 1f);
            float elevacion = Math.Clamp(llanura
                + 91f * montanosidad * edad * cordilleras[indice] * pendienteCostera,
                1f, 100f);
            byte nivel = esAgua[indice] ? (byte)0
                : (byte)Math.Clamp((int)MathF.Round(elevacion), 1, 100);
            TipoSuperficie superficie = esAgua[indice]
                ? TipoSuperficie.Oceano : TipoSuperficie.Tierra;
            int q = columna - (fila - (fila & 1)) / 2 - ancho / 2 + alto / 4;
            int r = fila - alto / 2;
            resultado[indice] = new WorldCell(columna, fila, new HexCoord(q, r),
                nivel, superficie) { Relieve = ReglasRelieve.Clasificar(nivel, config.NivelMar),
                Bioma = GeneradorBiomas.Clasificar(nivel, esAgua[indice], fila, columna,
                    ancho, alto, esAgua[indice] ? distanciaTierra[indice] : costa, config.Semilla) };
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
