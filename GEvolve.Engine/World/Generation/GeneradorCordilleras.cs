using GEvolve.Definitions.WorldGeneration.Configuration;

namespace GEvolve.Engine;

/// <summary>Genera cadenas montañosas locales, continuas y reproducibles sobre tierra.</summary>
public static class GeneradorCordilleras
{
    public static float[] Crear(int ancho, int alto, int semilla, bool[] esAgua,
        int[] distanciaCosta, TipoMundo tipo)
    {
        int total = checked(ancho * alto);
        var campo = new float[total];
        var candidatos = Enumerable.Range(0, total)
            .Where(i => !esAgua[i] && distanciaCosta[i] >= 3).ToArray();
        if (candidatos.Length == 0) return campo;

        var azar = new Random(semilla);
        int cantidad = tipo switch
        {
            TipoMundo.Pangea => 5,
            TipoMundo.Archipielagos => 13,
            TipoMundo.ContinentesPequenos => 10,
            _ => 8
        };
        int alcance = tipo == TipoMundo.Archipielagos ? 12 : 26;
        for (int cadena = 0; cadena < cantidad; cadena++)
        {
            int inicio = candidatos[azar.Next(candidatos.Length)];
            float x = inicio % ancho, y = inicio / ancho;
            float angulo = (float)(azar.NextDouble() * 2 * Math.PI);
            float magnitud = .85f + (float)azar.NextDouble() * .15f;
            int largo = 9 + azar.Next(alcance);
            int cortes = 0;
            for (int paso = 0; paso < largo; paso++)
            {
                int ix = ((int)MathF.Round(x) % ancho + ancho) % ancho;
                int iy = (int)MathF.Round(y);
                if (iy < 2 || iy >= alto - 2) break;
                int centro = iy * ancho + ix;
                if (esAgua[centro] || distanciaCosta[centro] < 2)
                {
                    if (++cortes >= 3) break;
                }
                else
                {
                    cortes = 0;
                    // Perfil transversal: cresta alta y laderas progresivas.
                    float avance = MathF.Sin(MathF.PI * (paso + 1f) / (largo + 1f));
                    float fuerza = magnitud * (.66f + .34f * avance);
                    int radio = 8;
                    for (int dy = -radio; dy <= radio; dy++)
                    {
                        int fila = iy + dy;
                        if (fila < 0 || fila >= alto) continue;
                        for (int dx = -radio; dx <= radio; dx++)
                        {
                            int col = (ix + dx + ancho) % ancho;
                            int idx = fila * ancho + col;
                            if (esAgua[idx]) continue;
                            float distancia2 = dx * dx + dy * dy;
                            float cresta = fuerza * MathF.Exp(-distancia2 / 13f);
                            campo[idx] = MathF.Max(campo[idx], cresta);
                        }
                    }
                }
                // Giro progresivo, evita líneas rectas o bandas globales.
                angulo += ((float)azar.NextDouble() - .5f) * .42f;
                x = (x + MathF.Cos(angulo) * 1.35f + ancho) % ancho;
                y += MathF.Sin(angulo) * 1.35f;
            }
        }
        return campo;
    }
}
