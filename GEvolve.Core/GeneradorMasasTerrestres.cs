namespace GEvolve.Core;

/// <summary>Cuatro estrategias fractales independientes para siluetas de tierra.</summary>
public static class GeneradorMasasTerrestres
{
    public static float[] Crear(WorldSettings c)
    {
        int w = c.Ancho, h = c.Alto;
        var salida = new float[w * h];
        var rng = new Random(c.Semilla);
        var regiones = new List<(float x,float y,float rx,float ry,float giro)>();
        int cantidad = c.Tipo switch
        {
            TipoMundo.Pangea => 7,
            TipoMundo.ContinentesGrandes => 5,
            TipoMundo.ContinentesPequenos => 11,
            _ => 35
        };
        for (int i = 0; i < cantidad; i++)
        {
            float x, y, rx, ry;
            if (c.Tipo == TipoMundo.Pangea)
            {
                // Centro dominante y brazos conectados que forman penínsulas.
                float ang = i * 2.39996f;
                float radio = i == 0 ? 0f : .07f + .075f * (float)rng.NextDouble();
                x = .5f + MathF.Cos(ang) * radio;
                y = .50f + MathF.Sin(ang) * radio;
                rx = i == 0 ? .29f : .15f + .07f * (float)rng.NextDouble();
                ry = i == 0 ? .31f : .16f + .07f * (float)rng.NextDouble();
            }
            else
            {
                // Rejilla escalonada con desplazamiento evita centros amontonados.
                int columnas = c.Tipo == TipoMundo.ContinentesGrandes ? 3 :
                    c.Tipo == TipoMundo.ContinentesPequenos ? 4 : 7;
                int filas = (cantidad + columnas - 1) / columnas;
                x = ((i % columnas) + .30f + .40f * (float)rng.NextDouble()) / columnas;
                y = .12f + .76f * ((i / columnas) + .25f + .50f * (float)rng.NextDouble()) / filas;
                float escala = c.Tipo switch
                {
                    TipoMundo.ContinentesGrandes => .18f,
                    TipoMundo.ContinentesPequenos => .10f,
                    _ => .047f
                };
                rx = escala * (.72f + .60f * (float)rng.NextDouble());
                ry = escala * (1.12f + .65f * (float)rng.NextDouble());
            }
            regiones.Add((x,y,rx,ry,(float)rng.NextDouble() * MathF.PI));
        }
        for (int fila = 0; fila < h; fila++)
        for (int col = 0; col < w; col++)
        {
            float x = (col + .5f) / w, y = (fila + .5f) / h;
            // Warp de escala continental más detalles de costa.
            float wx = x + .045f * Ruido(x,y,c.Semilla+11,4,3);
            float wy = y + .040f * Ruido(x,y,c.Semilla+23,3,4);
            float cobertura = -1f;
            foreach (var region in regiones)
            {
                float dx = wx - region.x;
                dx -= MathF.Round(dx);
                float dy = wy - region.y;
                float ca = MathF.Cos(region.giro), sa = MathF.Sin(region.giro);
                float u = (dx * ca - dy * sa) / region.rx;
                float v = (dx * sa + dy * ca) / region.ry;
                // Cada región se fusiona con sus vecinas mediante un máximo fractal.
                cobertura = MathF.Max(cobertura, 1f - MathF.Sqrt(u*u+v*v));
            }
            float contorno = .16f*Ruido(x,y,c.Semilla+41,7,6)
                           + .085f*Ruido(x,y,c.Semilla+59,17,13)
                           + .027f*Ruido(x,y,c.Semilla+71,39,29);
            if (c.Tipo == TipoMundo.Archipielagos) contorno *= .65f;
            // Menor preferencia de tierra en las franjas polares; no afecta envoltura E/O.
            float polar = .12f * Math.Clamp((.10f - MathF.Min(y,1f-y))/.10f,0f,1f);
            salida[fila*w+col] = cobertura + contorno - polar;
        }
        return salida;
    }

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
