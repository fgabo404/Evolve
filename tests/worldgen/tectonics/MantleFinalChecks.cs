// Independently evaluates selected A0 noise and potential before reusing the validated complete R0 edge oracle.
// Checks strict candidate metadata, immutable outputs and exact historical compatibility without production activation.
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleFinalChecks
{
    internal static void Identity()
    {
        var identity = MantleHistoricalProfile.Identity; var parameters = MantleHistoricalProfile.Parameters;
        MantleHistoricalProfile.Validate(identity, parameters);
        foreach (var bad in new[] {identity with {Identifier="other"},identity with {Revision=2},identity with {Model="B"},
            identity with {Operator="R2"},identity with {Sampling="A1"},identity with {NumericPolicy="reordered"},identity with {SeedDomain=1}})
            Expect<ArgumentException>(()=>MantleHistoricalProfile.Validate(bad,parameters));
        foreach (var bad in new[] {parameters with {BroadFrequency=1},parameters with {RegionalFrequency=12},
            parameters with {TectonicWeight=MathF.BitIncrement(2)},parameters with {BroadWeight=4},parameters with {RegionalWeight=1},parameters with {ReferenceSpeed=.5f}})
            Expect<ArgumentException>(()=>MantleHistoricalProfile.Validate(identity,bad));
        foreach(long seed in TectonicInfluenceFinalChecks.Seeds) Require(Seeds(seed)==MantleNoiseFields.Seeds(seed),"Independent candidate seed vector.");
        var grid=new HexGridData(MapSize.Small);var (_,packed)=GenerateComplete(MapSize.Small);
        var sources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(packed,grid));
        var influence=TectonicInfluenceFields.Build(sources,TectonicInfluenceProfile.Resolve(grid));
        var field=MantleHistoricalProfile.Build(grid,Request(MapSize.Small),influence);
        byte[] copy=MantleCandidateVectors.Serialize(field);string before=MantleCandidateVectors.Hash(copy);Array.Clear(copy);
        Require(before==MantleCandidateVectors.Hash(MantleCandidateVectors.Serialize(field)),"Serialized copy mutated immutable result.");
        Require(packed[1]==1&&packed[2]==7&&!TectonicData.FromLayerData(packed,grid).HasInfluence,"Candidate activated normal publication.");
    }

    internal static void FullOracles()
    {
        foreach(var size in Enum.GetValues<MapSize>()) foreach(long seed in TectonicInfluenceFinalChecks.Seeds)
        {
            var(grid,packed)=GenerateComplete(size,seed);
            var sources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(packed,grid));
            var influence=TectonicInfluenceFields.Build(sources,TectonicInfluenceProfile.Resolve(grid));
            var request=Request(size,seed);var p=MantleHistoricalProfile.Parameters;
            var noise=MantleNoiseFields.Build(grid,request,p);var field=MantleHistoricalProfile.Build(grid,request,influence);
            Verify(grid,request,influence,noise,field);
            Console.WriteLine($"MANTLE FINAL ORACLE {size}/{seed}: complete independent A0 signals/potential and R0 flows passed.");
        }
    }

    // This oracle calls neither the sampler nor potential/flux builders to form expected noise/potential values.
    internal static MantleConvectionField Verify(HexGridData grid,GenerationRequest request,TectonicInfluenceFields influence,
        MantleNoiseFields noise,MantleConvectionField field)
    {
        var seeds=Seeds(request.Seed);
        for(int cell=0;cell<grid.CellCount;cell++)
        {
            float cx=(float)(1d+1.5d*(cell%grid.Columns));
            float cy=(float)(Math.Sqrt(3d)*(cell/grid.Columns+.5d*((cell%grid.Columns)&1)+.5d));
            double x=(double)cx/(1.5d*grid.Columns)*grid.Columns,y=(double)cy/(Math.Sqrt(3d)*(grid.Rows+.5d))*grid.Rows;
            double broad=Noise(x,y,grid.Columns,grid.Rows,2,seeds.Broad),regional=Noise(x,y,grid.Columns,grid.Rows,6,seeds.Regional);
            Require(BitConverter.DoubleToUInt64Bits(broad)==BitConverter.DoubleToUInt64Bits(noise.Broad(cell))&&
                BitConverter.DoubleToUInt64Bits(regional)==BitConverter.DoubleToUInt64Bits(noise.Regional(cell)),"Independent complete A0 sampler/coordinate oracle.");
            var source=influence[cell];double z=((double)source.Extension-source.Compression)/((1d+source.Extension)+source.Compression);
            float expected=(float)(((2d*z+2d*broad)+2d*regional)/6d);
            Require(BitConverter.SingleToUInt32Bits(field[cell].Potential)==BitConverter.SingleToUInt32Bits(expected==0?0:expected),"Independent full candidate potential.");
        }
        MantleIndependentOracle.Check(grid,field);field.Validate(grid);return field;
    }

    internal static (ulong Broad,ulong Regional) Seeds(long seed)
    {
        ulong state=Mix(unchecked((ulong)seed+2UL*0x9E3779B97F4A7C15UL))^0x31544E4D31304757;
        ulong Next(){state=unchecked(state+0x9E3779B97F4A7C15UL);return Mix(state);}
        return(Next(),Next());
    }
    private static ulong Mix(ulong x){unchecked{x=(x^(x>>30))*0xBF58476D1CE4E5B9UL;x=(x^(x>>27))*0x94D049BB133111EBUL;return x^(x>>31);}}
    private static double Noise(double column,double row,int columns,int rows,int frequency,ulong seed)
    {
        double sum=0,total=0,weight=1;
        for(int octave=0;octave<3;octave++)
        {
            int period=frequency<<octave;double x=column*period/columns,y=row*period/rows;
            int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double fx=x-ix,fy=y-iy;
            fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            ulong latticeSeed=unchecked(seed+(ulong)octave*0x9E3779B97F4A7C15UL);
            double Value(int a,int b){int wrap=(int)(((long)a%period+period)%period);ulong bits=unchecked(latticeSeed^(ulong)(long)wrap*0x9E3779B97F4A7C15UL^(ulong)(long)b*0xD1B54A32D192ED03UL);return 2d*(Mix(bits)>>11)*(1d/9007199254740992d)-1d;}
            double a=Value(ix,iy),b=Value(ix+1,iy),c=Value(ix,iy+1),d=Value(ix+1,iy+1);
            double top=a+(b-a)*fx,bottom=c+(d-c)*fx;sum+=weight*(top+(bottom-top)*fy);total+=weight;weight*=.5;
        }
        return sum/total;
    }
}
