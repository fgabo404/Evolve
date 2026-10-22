namespace GEvolve.Core;

/// <summary>Catálogo visual v0.2.1. No agrega recursos ni efectos de juego.</summary>
public enum Bioma
{
    OceanoAbierto, Arrecife, EstuarioLitoral, Manglar,
    BosqueTropical, BosqueSubtropical, BosqueTemplado, BosqueBoreal,
    DesiertoArenoso, DesiertoRocoso, Salar,
    Sabana, Pradera, Estepa, Humedal, PantanoEverglades,
    TundraArtica, TundraAntartica, Alpino, Cumbres
}

/// <summary>Asignación determinista según latitud, altitud, humedad y costas.</summary>
public static class GeneradorBiomas
{
    public static Bioma Clasificar(byte altura, bool agua, int fila, int columna,
        int ancho, int alto, int distanciaCosta, int semilla)
    {
        float latitud = (fila + .5f) / alto;
        float frio = MathF.Abs(latitud * 2f - 1f);
        float x = (columna + .5f) / ancho;
        // Ruido regional suave y periódico E/O para zonas climáticas coherentes.
        // Regiones climáticas amplias para evitar el efecto de mosaico aleatorio.
        float humedad = .53f
            + .39f * Campo(x, latitud, semilla + 320, 3, 3)
            + .08f * Campo(x, latitud, semilla + 472, 7, 5)
            + .12f * MathF.Cos(latitud * MathF.PI * 6f);
        humedad = Math.Clamp(humedad, 0f, 1f);
        float temperatura = 1f - frio * 1.12f - altura * .0048f;
        if (agua)
        {
            // Franja de poca profundidad alrededor de islas y continentes.
            if (frio > .89f) return latitud < .5f ? Bioma.TundraArtica : Bioma.TundraAntartica;
            if (distanciaCosta <= 2)
                return frio < .40f ? Bioma.Arrecife : Bioma.EstuarioLitoral;
            return Bioma.OceanoAbierto;
        }
        if (altura >= 80) return Bioma.Cumbres;
        if (altura >= 60) return Bioma.Alpino;
        if (frio > .82f) return latitud < .5f ? Bioma.TundraArtica : Bioma.TundraAntartica;
        if (temperatura < .30f) return Bioma.BosqueBoreal;
        if (distanciaCosta <= 2 && frio < .38f && humedad > .70f) return Bioma.Manglar;
        if (humedad > .83f && altura <= 18)
            return frio < .43f ? Bioma.PantanoEverglades : Bioma.Humedal;
        if (humedad > .75f && altura <= 14) return Bioma.Humedal;
        if (humedad < .22f) return altura <= 8 ? Bioma.Salar : Bioma.DesiertoArenoso;
        if (humedad < .32f) return altura > 22 ? Bioma.DesiertoRocoso : Bioma.DesiertoArenoso;
        if (frio < .36f)
        {
            if (humedad > .64f) return Bioma.BosqueTropical;
            if (humedad > .45f) return Bioma.BosqueSubtropical;
            return Bioma.Sabana;
        }
        if (frio < .61f)
        {
            if (humedad > .61f) return Bioma.BosqueTemplado;
            if (humedad > .38f) return Bioma.Pradera;
            return Bioma.Estepa;
        }
        return humedad > .44f ? Bioma.BosqueBoreal : Bioma.Estepa;
    }

    private static float Campo(float x, float y, int semilla, int fx, int fy)
    {
        float xx = x * fx, yy = y * fy;
        int ix = (int)MathF.Floor(xx), iy = (int)MathF.Floor(yy);
        float tx = xx - ix, ty = yy - iy;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        float a = Hash(ix, iy, semilla, fx), b = Hash(ix + 1, iy, semilla, fx);
        float c = Hash(ix, iy + 1, semilla, fx), d = Hash(ix + 1, iy + 1, semilla, fx);
        return (a + (b - a) * tx) * (1 - ty) + (c + (d - c) * tx) * ty;
    }

    private static float Hash(int x, int y, int semilla, int wrap)
    {
        unchecked
        {
            uint h = (uint)semilla * 747796405u
                + (uint)((x % wrap + wrap) % wrap) * 2891336453u + (uint)y * 277803737u;
            h ^= h >> 16; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215f * 2f - 1f;
        }
    }
}
