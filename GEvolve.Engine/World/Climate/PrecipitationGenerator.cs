using GEvolve.Engine.World.BiomeGeneration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.Climate;

/// <summary>Preliminary deterministic yearly precipitation, transported by latitude-dependent zonal winds.</summary>
public sealed class PrecipitationGenerator
{
    public const string Version = "precipitation-1";

    public LandmassState Apply(HexTopology topology, LandmassState terrain)
    {
        if (terrain.ElevationByCellId is not { } heights || terrain.Climate is not { } climate)
            return terrain;
        int count=topology.Count, width=topology.Grid.Definition.Columns, rows=topology.Grid.Definition.Rows;
        if (heights.Length!=count || climate.BaseTemperatureTenthsByCellId.Length!=count)
            throw new ArgumentException("Invalid climate dimensions.",nameof(terrain));
        var rain=new ushort[count];
        var moisture=new byte[count];
        for(int row=0;row<rows;row++)
        {
            double latitude=Math.Abs(BiomeClassifier.Latitude(row,rows));
            int direction=latitude>=30 && latitude<60?1:-1;
            int start=(int)(((long)(uint)terrain.Configuration.Seed*1664525+row*1013904223L)%width);
            double vapor=.65;
            for(int step=0;step<width*2;step++)
            {
                int column=((start+direction*step)%width+width)%width;
                int id=row*width+column;
                int previous=row*width+((column-direction+width)%width);
                bool land=terrain.IsLand(id);
                double temperature=climate.BaseCelsius(id);
                double circulation=latitude<15?1.5:latitude<25?1:latitude<37?.38:latitude<60?1:.65;
                if(!land)vapor=Math.Min(1.9,vapor+.2*Math.Max(.25,(temperature+10)/40));
                double ascent=land && terrain.IsLand(previous)?
                    Math.Max(0,(int)heights[id]-(int)heights[previous]):0;
                double condensation=Math.Min(.85,(land?.07:.10)*circulation*
                    (1+Math.Min(3,ascent*.14)));
                double deposited=vapor*condensation;
                vapor=Math.Max(.04,vapor-deposited);
                if(land)vapor=Math.Min(1.9,vapor+.012);
                if(step>=width)
                    rain[id]=(ushort)Math.Clamp((int)Math.Round(deposited*6500),0,ushort.MaxValue);
            }
        }
        for(int id=0;id<count;id++)
        {
            double demand=380+Math.Max(0,climate.BaseCelsius(id))*28;
            moisture[id]=(byte)Math.Clamp((int)Math.Round(100.0*rain[id]/
                (rain[id]+demand)),0,100);
        }
        return terrain with { Climate=climate with {
            BasePrecipitationMmByCellId=rain,
            BaseMoistureByCellId=moisture,
            PrecipitationModelVersion=Version } };
    }
}
