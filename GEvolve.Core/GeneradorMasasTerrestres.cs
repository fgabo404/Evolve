namespace GEvolve.Core;

/// <summary>Siluetas continentales a partir de esqueletos ramificados y fractales periódicos.</summary>
public static class GeneradorMasasTerrestres
{
    private readonly record struct Tramo(float X, float Y, float Radio, float Peso);

    public static float[] Crear(WorldSettings config)
    {
        if (config.Tipo == TipoMundo.Archipielagos)
            return GeneradorArchipielagos.Crear(config);

        int ancho = config.Ancho, alto = config.Alto;
        var azar = new Random(config.Semilla);
        var trazos = new List<Tramo>();
        int grupos = config.Tipo switch
        {
            TipoMundo.Pangea => 1,
            TipoMundo.ContinentesGrandes => 4,
            TipoMundo.ContinentesPequenos => 9,
            _ => 21
        };
        // Sin rejillas ni elipses: cada continente es una red de ejes ramificados.
        for (int g = 0; g < grupos; g++)
        {
            float cx = .08f + .84f * (float)azar.NextDouble();
            float cy = .17f + .66f * (float)azar.NextDouble();
            if (config.Tipo == TipoMundo.Pangea) { cx = .5f; cy = .5f; }
            float escala = config.Tipo switch
            {
                TipoMundo.Pangea => .24f,
                TipoMundo.ContinentesGrandes => .105f,
                TipoMundo.ContinentesPequenos => .060f,
                _ => .023f
            };
            escala *= .65f + .8f * (float)azar.NextDouble();
            float angulo = (float)(azar.NextDouble() * Math.PI * 2);
            int pasos = config.Tipo switch
            {
                TipoMundo.Pangea => 24,
                TipoMundo.ContinentesGrandes => 14,
                TipoMundo.ContinentesPequenos => 9,
                _ => 5
            };
            var espina = new List<(float x,float y)>();
            float x = cx, y = cy;
            for (int p = 0; p < pasos; p++)
            {
                float t = p / (float)Math.Max(1, pasos - 1);
                float radio = escala * (.55f + .60f * MathF.Sin(MathF.PI * (.12f + .78f * t)));
                radio *= .70f + .5f * (float)azar.NextDouble();
                trazos.Add(new Tramo(x, y, radio, 1f));
                espina.Add((x,y));
                angulo += ((float)azar.NextDouble() - .5f) * .95f;
                float paso = escala * (.36f + .28f * (float)azar.NextDouble());
                x = Modulo(x + MathF.Cos(angulo) * paso);
                y = Math.Clamp(y + MathF.Sin(angulo) * paso, .10f, .90f);
            }
            // Brazos/penínsulas parten de la espina, no son círculos independientes.
            int brazos = config.Tipo == TipoMundo.Archipielagos ? 1 : 3 + azar.Next(3);
            for (int b = 0; b < brazos; b++)
            {
                var anclaje = espina[azar.Next(espina.Count)];
                x = anclaje.x; y = anclaje.y;
                angulo = (float)(azar.NextDouble() * Math.PI * 2);
                int largo = 3 + azar.Next(Math.Max(4, pasos / 2));
                for (int p = 0; p < largo; p++)
                {
                    float caida = 1f - .65f * p / largo;
                    float radio = escala * caida * (.32f + .30f * (float)azar.NextDouble());
                    trazos.Add(new Tramo(x, y, radio, .94f));
                    angulo += ((float)azar.NextDouble() - .5f) * .8f;
                    x = Modulo(x + MathF.Cos(angulo) * escala * .45f);
                    y = Math.Clamp(y + MathF.Sin(angulo) * escala * .45f, .06f, .94f);
                }
            }
        }

        var salida = new float[ancho * alto];
        for (int fila = 0; fila < alto; fila++)
        for (int col = 0; col < ancho; col++)
        {
            float x = (col + .5f) / ancho, y = (fila + .5f) / alto;
            // Deformación continental con dos escalas: golfos, cabos y estrechos.
            float wx = Modulo(x + .050f * Ruido(x,y,config.Semilla+11,3,3)
                                + .018f * Ruido(x,y,config.Semilla+19,9,7));
            float wy = y + .055f * Ruido(x,y,config.Semilla+23,3,3)
                         + .018f * Ruido(x,y,config.Semilla+29,8,8);
            float masa = -1.5f;
            foreach (var tramo in trazos)
            {
                float dx = wx - tramo.X;
                dx -= MathF.Round(dx); // El mundo envuelve este y oeste.
                float dy = wy - tramo.Y;
                float d = MathF.Sqrt(dx*dx + dy*dy);
                masa = MathF.Max(masa, tramo.Peso - d / tramo.Radio);
            }
            float fractal = .31f * Ruido(x,y,config.Semilla+41,5,5)
                          + .16f * Ruido(x,y,config.Semilla+59,13,11)
                          + .065f * Ruido(x,y,config.Semilla+71,33,23);
            float margenPolar = .32f * Math.Clamp((.10f - MathF.Min(y,1f-y))/.10f,0f,1f);
            salida[fila*ancho+col] = masa + fractal - margenPolar;
        }
        return salida;
    }

    private static float Modulo(float valor) => valor - MathF.Floor(valor);

    private static float Ruido(float x,float y,int semilla,int fx,int fy)
    {
        float px=x*fx,py=y*fy;
        int ix=(int)MathF.Floor(px),iy=(int)MathF.Floor(py);
        float tx=px-ix,ty=py-iy;
        tx=tx*tx*(3f-2f*tx);ty=ty*ty*(3f-2f*ty);
        float a=Hash(ix,iy,fx,semilla),b=Hash(ix+1,iy,fx,semilla);
        float c=Hash(ix,iy+1,fx,semilla),d=Hash(ix+1,iy+1,fx,semilla);
        return ((a+(b-a)*tx)*(1f-ty)+(c+(d-c)*tx)*ty)*2f-1f;
    }
    private static float Hash(int x,int y,int n,int seed)
    {
        unchecked
        {
            uint v=(uint)seed*747796405u+(uint)((x%n+n)%n)*2891336453u+(uint)y*277803737u;
            v^=v>>16;v*=2246822519u;v^=v>>13;v*=3266489917u;v^=v>>16;
            return (v&0x00FFFFFFu)/16777215f;
        }
    }
}
