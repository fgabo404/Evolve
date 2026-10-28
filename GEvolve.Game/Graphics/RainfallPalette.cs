using System.Numerics;
namespace GEvolve.Game.Graphics;
public static class RainfallPalette
{
    private static readonly (double Millimetres,Vector3 Color)[] Stops=[
        (0,Rgb(0xD4A35E)),(250,Rgb(0xDFC977)),(500,Rgb(0x91BC82)),
        (1000,Rgb(0x468F87)),(2000,Rgb(0x2863A8)),(4000,Rgb(0x193E81))];
    public static Vector3 ForMillimetres(double mm)
    {
        if(!double.IsFinite(mm))throw new ArgumentOutOfRangeException(nameof(mm));
        if(mm<=0)return Stops[0].Color;
        for(int i=1;i<Stops.Length;i++)
            if(mm<=Stops[i].Millimetres)
                return Vector3.Lerp(Stops[i-1].Color,Stops[i].Color,
                    (float)((mm-Stops[i-1].Millimetres)/(Stops[i].Millimetres-Stops[i-1].Millimetres)));
        return Stops[^1].Color;
    }
    private static Vector3 Rgb(int hex)=>new((hex>>16&255)/255f,(hex>>8&255)/255f,(hex&255)/255f);
}
