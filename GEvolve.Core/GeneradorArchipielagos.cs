namespace GEvolve.Core;

/// <summary>
/// Archipiélagos de inspiración geográfica: arcos insulares, islas mayores,
/// islotes satélite y canales. Solo produce campo tierra/océano.
/// </summary>
public static class GeneradorArchipielagos
{
    private readonly record struct Isla(float X, float Y, float Rx, float Ry, float Angulo, float Fuerza);

    public static float[] Crear(WorldSettings config)
    {
        int ancho = config.Ancho, alto = config.Alto;
        var azar = new Random(config.Semilla);
        var islas = new List<Isla>();
        // Arcos en lugares y orientaciones variables, no centros en cuadrícula.
        int arcos = 6 + azar.Next(3);
        for (int a = 0; a < arcos; a++)
        {
            float centroX = (float)azar.NextDouble();
            float centroY = .16f + .68f * (float)azar.NextDouble();
            float orientacion = (float)(azar.NextDouble() * Math.PI * 2);
            float longitud = .13f + .12f * (float)azar.NextDouble();
            float curvatura = ((float)azar.NextDouble() - .5f) * .95f;
            int miembros = 8 + azar.Next(9);
            for (int j = 0; j < miembros; j++)
            {
                float t = j / (float)(miembros - 1);
                float desplazamiento = (t - .5f) * longitud;
                float arco = curvatura * longitud * MathF.Sin(t * MathF.PI);
                float x = Modulo(centroX + MathF.Cos(orientacion) * desplazamiento
                    - MathF.Sin(orientacion) * arco
                    + ((float)azar.NextDouble() - .5f) * .014f);
                float y = Math.Clamp(centroY + MathF.Sin(orientacion) * desplazamiento
                    + MathF.Cos(orientacion) * arco
                    + ((float)azar.NextDouble() - .5f) * .014f, .08f, .92f);
                // Unas pocas islas principales, muchas menores y satélites.
                float principal = azar.NextDouble() < .19 ? 2.2f : 1f;
                float rx = (.009f + .013f * (float)azar.NextDouble()) * principal;
                float ry = rx * (.75f + 1.9f * (float)azar.NextDouble());
                float angulo = orientacion + ((float)azar.NextDouble() - .5f) * 1.2f;
                islas.Add(new Isla(x, y, rx, ry, angulo, 1f));
                int satelites = azar.Next(3);
                for (int k = 0; k < satelites; k++)
                {
                    float delta = (float)(azar.NextDouble() * Math.PI * 2);
                    float separacion = .012f + .026f * (float)azar.NextDouble();
                    islas.Add(new Isla(
                        Modulo(x + MathF.Cos(delta) * separacion),
                        Math.Clamp(y + MathF.Sin(delta) * separacion, .07f, .93f),
                        rx * .28f, ry * .29f, angulo, .82f));
                }
            }
        }

        var campo = new float[ancho * alto];
        for (int fila = 0; fila < alto; fila++)
        for (int col = 0; col < ancho; col++)
        {
            float x = (col + .5f) / ancho, y = (fila + .5f) / alto;
            float warpX = .008f * Ruido(x, y, config.Semilla + 117, 9, 8);
            float warpY = .009f * Ruido(x, y, config.Semilla + 129, 8, 9);
            float valor = -1.2f;
            foreach (var isla in islas)
            {
                float dx = x + warpX - isla.X;
                dx -= MathF.Round(dx);
                float dy = y + warpY - isla.Y;
                float cos = MathF.Cos(isla.Angulo), sin = MathF.Sin(isla.Angulo);
                float u = (dx * cos + dy * sin) / isla.Rx;
                float v = (-dx * sin + dy * cos) / isla.Ry;
                float d = MathF.Sqrt(u*u + v*v);
                valor = MathF.Max(valor, isla.Fuerza - d);
            }
            float costa = .16f * Ruido(x,y,config.Semilla+233,17,14)
                        + .06f * Ruido(x,y,config.Semilla+247,31,23);
            float polo = .3f * Math.Clamp((.10f - MathF.Min(y,1f-y))/.10f,0f,1f);
            campo[fila*ancho+col] = valor + costa - polo;
        }
        return campo;
    }

    private static float Modulo(float x) => x - MathF.Floor(x);
    private static float Ruido(float x,float y,int semilla,int fx,int fy)
    {
        float px=x*fx,py=y*fy;
        int ix=(int)MathF.Floor(px),iy=(int)MathF.Floor(py);
        float tx=px-ix,ty=py-iy;
        tx=tx*tx*(3f-2f*tx); ty=ty*ty*(3f-2f*ty);
        float a=Hash(ix,iy,fx,semilla), b=Hash(ix+1,iy,fx,semilla);
        float c=Hash(ix,iy+1,fx,semilla), d=Hash(ix+1,iy+1,fx,semilla);
        return ((a+(b-a)*tx)*(1f-ty)+(c+(d-c)*tx)*ty)*2f-1f;
    }
    private static float Hash(int x,int y,int n,int semilla)
    {
        unchecked
        {
            uint v=(uint)semilla*747796405u+(uint)((x%n+n)%n)*2891336453u+(uint)y*277803737u;
            v^=v>>16;v*=2246822519u;v^=v>>13;v*=3266489917u;v^=v>>16;
            return (v&0x00FFFFFFu)/16777215f;
        }
    }
}
