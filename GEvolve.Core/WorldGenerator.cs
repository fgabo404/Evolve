namespace GEvolve.Core;

/// <summary>Clase visual inicial del terreno, independiente de OpenGL.</summary>
public enum TipoSuperficie { Oceano, Tierra }

/// <summary>Celda del mundo procedural con coordenadas, elevación y superficie.</summary>
public readonly record struct CeldaMundo(HexCoord Coordenada, float Elevacion, TipoSuperficie Superficie,
    NivelRelieve Relieve = NivelRelieve.Medio, byte? NivelNumerico = null);

/// <summary>
/// Crea mapas rectangulares de celdas hexagonales con una semilla reproducible.
/// Las máscaras continentales son una aproximación visual, no clima ni biomas.
/// </summary>
public static class WorldGenerator
{
    /// <summary>Genera ancho × alto celdas y las centra alrededor del origen axial.</summary>
    public static IReadOnlyList<CeldaMundo> Crear(int ancho = 100, int alto = 50, int semilla = 2026)
    {
        if (ancho < 1 || alto < 1 || (long)ancho * alto > 100_000)
            throw new ArgumentOutOfRangeException(nameof(ancho), "Dimensiones inválidas.");

        var celdas = new List<CeldaMundo>(ancho * alto);
        for (int fila = 0; fila < alto; fila++)
        for (int columna = 0; columna < ancho; columna++)
        {
            // Coordenadas offset odd-r -> axial, para dibujar rectángulos hexagonales.
            int q = columna - (fila - (fila & 1)) / 2 - ancho / 2 + alto / 4;
            int r = fila - alto / 2;
            float x = columna / (float)ancho;
            float y = fila / (float)alto;

            // Tres masas continentales con costas sinuosas y ruido suavizado.
            float mascara = 0f;
            mascara = MathF.Max(mascara, Isla(x,y,0.29f,0.37f,0.25f,0.35f));
            mascara = MathF.Max(mascara, Isla(x,y,0.71f,0.61f,0.24f,0.28f));
            mascara = MathF.Max(mascara, Isla(x,y,0.53f,0.16f,0.12f,0.15f));
            float ruido = 0.20f * Onda(x, y, semilla);
            float tierra = mascara + ruido - 0.31f;
            TipoSuperficie tipo = tierra > 0f ? TipoSuperficie.Tierra : TipoSuperficie.Oceano;
            // Agua plana; tierra progresivamente más alta, sin escala ecológica definitiva.
            float elevacion = tipo == TipoSuperficie.Oceano ? 0.08f :
                Math.Clamp(0.13f + tierra * 2.3f, 0.13f, 1.65f);
            celdas.Add(new CeldaMundo(new HexCoord(q,r), elevacion, tipo));
        }
        return celdas;
    }

    private static float Isla(float x,float y,float cx,float cy,float rx,float ry)
    {
        float dx = (x-cx)/rx, dy=(y-cy)/ry;
        return MathF.Max(0f,1f-MathF.Sqrt(dx*dx+dy*dy));
    }

    private static float Onda(float x,float y,int semilla)
    {
        float s = semilla * 0.037f;
        return (MathF.Sin(x*23f+s)*MathF.Cos(y*19f-s) +
                0.55f*MathF.Sin(x*48f-y*36f+s*1.7f) +
                0.35f*MathF.Cos(x*83f+y*51f-s*0.6f))/1.9f;
    }
}
