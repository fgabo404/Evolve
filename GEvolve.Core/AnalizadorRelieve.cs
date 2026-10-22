namespace GEvolve.Core;

/// <summary>Forma local del relieve según los vecinos hexagonales.</summary>
public enum FormaRelieve { Llanura, Ladera, Valle, Cresta, Cumbre }

/// <summary>Resumen de pendientes y morfología, sin depender del motor gráfico.</summary>
public readonly record struct AnalisisRelieve(
    byte PendienteMaxima, float PendienteMedia, FormaRelieve Forma, bool PasoDificil);

/// <summary>Calcula pendientes mediante los seis vecinos odd-r de cada celda.</summary>
public static class AnalizadorRelieve
{
    private static readonly (int dx, int dy)[] VecinosPares =
        [(1, 0), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1)];
    private static readonly (int dx, int dy)[] VecinosImpares =
        [(1, 0), (1, -1), (0, -1), (-1, 0), (0, 1), (1, 1)];

    /// <summary>Analiza cada celda con continuidad longitudinal y polos limitados.</summary>
    public static AnalisisRelieve[] Analizar(WorldMap mapa)
    {
        ArgumentNullException.ThrowIfNull(mapa);
        var resultado = new AnalisisRelieve[mapa.Celdas.Count];
        foreach (WorldCell celda in mapa.Celdas)
        {
            int mayor = 0, suma = 0, total = 0, superiores = 0, inferiores = 0;
            var vecinos = (celda.Fila & 1) == 0 ? VecinosPares : VecinosImpares;
            foreach (var (dx, dy) in vecinos)
            {
                if (!mapa.IntentarObtener(celda.Columna + dx, celda.Fila + dy, out WorldCell vecino))
                    continue;
                int diferencia = (int)celda.Altura - vecino.Altura;
                int absoluta = Math.Abs(diferencia);
                mayor = Math.Max(mayor, absoluta);
                suma += absoluta;
                total++;
                if (diferencia > 3) inferiores++;
                if (diferencia < -3) superiores++;
            }

            FormaRelieve forma;
            if (celda.Relieve == NivelRelieve.Impasable) forma = FormaRelieve.Cumbre;
            else if (inferiores >= 4 && celda.Altura >= mapa.Configuracion.NivelMar)
                forma = FormaRelieve.Cresta;
            else if (superiores >= 4 && celda.Superficie == TipoSuperficie.Tierra)
                forma = FormaRelieve.Valle;
            else if (mayor >= 12) forma = FormaRelieve.Ladera;
            else forma = FormaRelieve.Llanura;

            resultado[celda.Fila * mapa.Configuracion.Ancho + celda.Columna] =
                new AnalisisRelieve((byte)mayor, total == 0 ? 0f : suma / (float)total,
                    forma, celda.Relieve == NivelRelieve.Impasable || mayor >= 24);
        }
        return resultado;
    }
}
