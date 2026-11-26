// Executes the declared CAL02 one-factor matrix and records paired spatial, aliasing and Vref evidence.
// Preserves historical controls and separates measured operations from surveys, analysis and serialization.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Evolve.Definitions.WorldGeneration;
using Evolve.Engine.WorldGeneration.Layers.Grid;
using Evolve.Engine.WorldGeneration.Layers.Tectonics;
using static Evolve.WorldGeneration.Tectonics.Checks.TectonicCheckFixtures;
using static Evolve.WorldGeneration.Tectonics.Checks.MantleSpatialAlternatives;

namespace Evolve.WorldGeneration.Tectonics.Checks;

internal static class MantleSpatialRunner
{
    private readonly record struct Timing(double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Phase(string Size, long Seed, string Candidate, int Batch, bool Warmup, string Operation, double Milliseconds, long AllocatedBytes, long RetainedGcDeltaBytes);
    private sealed record Metric(string Size, long Seed, string Candidate, int Batch, string Operator, string Geometry, float Vref, string Hash,
        double PhiMin, double PhiMax, double PhiRms, double XMin, double XMax, double XRms, double YMin, double YMax, double YRms,
        double WMin, double WMax, double WRms, double PhiDeltaRms, double XDeltaRms, double YDeltaRms, double WDeltaRms,
        double ZRms, double ZP50, double ZP99, double SaturatedPercent, double PhiZCorrelation, double PhiDeformationCorrelation,
        double BoundaryZAbsMean, double InteriorPhiRms, double DoubleBalance, double DoubleBudget, double FloatBalance, double FloatBudget);
    private sealed record SamplingRow(string Size, string Geometry, int BaseFrequency, int Octave, int EffectiveFrequency, double SamplesX, double SamplesY, double MaximumEdgeAdvance, string Classification);
    private sealed record ShiftRow(string Size, long Seed, string Geometry, int BroadFrequency, int RegionalFrequency, double ShiftX, double ShiftY,
        double PhiDeltaRms, double XDeltaRms, double YDeltaRms, double WDeltaRms, double SeamProbeMaximumError, string Hash);

    internal static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any()) throw new ArgumentException("Use an empty evidence directory.");
        Directory.CreateDirectory(directory);
        var survey = MantleSpatialDesign.Resolve(); var matrix = MantleSpatialDesign.Matrix(survey);
        var options = new JsonSerializerOptions { WriteIndented=true, Converters={new JsonStringEnumConverter()} };
        // Resolve and persist every configuration before executing any alternative case.
        File.WriteAllText(Path.Combine(directory,"design.json"),JsonSerializer.Serialize(new
        {
            Seeds=MantleSpatialDesign.Seeds, Matrix=matrix,
            VrefBits=new[] {survey.Low,survey.Middle,survey.High}.Select(value=>BitConverter.SingleToUInt32Bits(value).ToString("X8")).ToArray(),
            SurveyPolicy="float median of twelve per-world positive C+E quantiles, each world equally weighted; quantile floor(q*(n-1))",
            Runtime=RuntimeInformation.FrameworkDescription, OS=RuntimeInformation.OSDescription,
            Architecture=RuntimeInformation.ProcessArchitecture.ToString(), Processor=Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), Environment.ProcessorCount,
            Stopwatch.Frequency, TieredCompilation=Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"), Warmups=1, Batches=3, IterationsPerPhase=1,
            SeedDomain="unchanged experimental WG01MNT1; no frozen Mantle profile", Notes="Single-threaded; no dedicated-machine claim; GC outside timers; signed retained deltas are not peak heap."
        },options));
        WriteCsv(Path.Combine(directory,"influence-survey.csv"),survey.Worlds);
        var controlled = new List<MantleSpatialChecks.ContinuousRow>(); MantleSpatialChecks.Continuous(controlled);
        WriteCsv(Path.Combine(directory,"controlled-resolution.csv"),controlled);
        var sampling=new List<SamplingRow>();
        foreach(var size in Enum.GetValues<MapSize>()) foreach(var geometry in Enum.GetValues<Geometry>()) foreach(int frequency in new[]{1,2,3,4,6,12,24})
        {
            var screen=Sampling(new HexGridData(size),frequency,geometry);
            for(int octave=0;octave<3;octave++)
            {
                double factor=(1<<octave)/4d, advance=screen.EdgeAdvance*factor;
                sampling.Add(new(size.ToString(),geometry.ToString(),frequency,octave,frequency<<octave,screen.SamplesX/factor,screen.SamplesY/factor,advance,
                    advance>=.5?"INSUFFICIENT":advance>.25?"WARNING":"BASE-OCTAVE-MARGIN"));
            }
        }
        WriteCsv(Path.Combine(directory,"sampling.csv"),sampling);
        var phases=new List<Phase>();var metrics=new List<Metric>();var shifts=new List<ShiftRow>();
        foreach(var size in Enum.GetValues<MapSize>()) foreach(long seed in MantleSpatialDesign.Seeds)
        {
            var(grid,packed)=GenerateComplete(size,seed); var before=packed.ToArray();
            var sources=TectonicInfluenceSources.Extract(grid,TectonicData.FromLayerData(packed,grid));
            var influence=TectonicInfluenceFields.Build(sources,TectonicInfluenceProfile.Resolve(grid));var distances=TectonicBoundaryDistances.Build(sources);
            string earlier=TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence,distances));
            var p=matrix[0].Parameters;
            var baseline=Result.Build(grid,Potential(grid,influence,Signals.Build(grid,Request(size,seed),p,Geometry.A0),p),Operator.R0);
            MantleSpatialDesign.VerifyHistorical(size.ToString(),seed,"CONTROL",MantleSpatialDesign.Hash(baseline));
            foreach(var row in matrix)
            {
                string? expectedHash=null;
                for(int batch=0;batch<=3;batch++)
                {
                    var signals=Measure(()=>Signals.Build(grid,Request(size,seed),row.Parameters,row.SpatialPolicy));
                    var potential=Measure(()=>Potential(grid,influence,signals.Value,row.Parameters));
                    var flow=Measure(()=>Result.Build(grid,potential.Value,row.Operation));
                    var validation=Measure(()=>{flow.Value.Validate(grid);return flow.Value;});
                    string hash=MantleSpatialDesign.Hash(flow.Value);
                    Require(expectedHash is null||expectedHash==hash,"CAL02 measured replay differs.");expectedHash=hash;
                    MantleSpatialDesign.VerifyHistorical(size.ToString(),seed,row.Name,hash);
                    Add("Signals",signals.Timing);Add("Potential",potential.Timing);Add("Edges",flow.Timing);Add("Validation",validation.Timing);
                    if(batch>0) metrics.Add(Analyze(size.ToString(),seed,row,batch,flow.Value,baseline,influence,distances,hash));
                    void Add(string operation,Timing t)=>phases.Add(new(size.ToString(),seed,row.Name,batch,batch==0,operation,t.Milliseconds,t.AllocatedBytes,t.RetainedGcDeltaBytes));
                }
            }
            foreach(var geometry in Enum.GetValues<Geometry>()) foreach(var frequency in new[]{(1,3),(2,6),(4,12)})
            {
                var parameters=p with{BroadFrequency=frequency.Item1,RegionalFrequency=frequency.Item2};
                var reference=Result.Build(grid,Potential(grid,influence,Signals.Build(grid,Request(size,seed),parameters,geometry),parameters),Operator.R0);
                foreach(var shift in new[]{(0d,0d),(.375,0d),(0d,Math.Sqrt(3)/4),(.375,Math.Sqrt(3)/4)})
                {
                    var field=Result.Build(grid,Potential(grid,influence,Signals.Build(grid,Request(size,seed),parameters,geometry,shift.Item1,shift.Item2),parameters),Operator.R0);field.Validate(grid);
                    var differences=Differences(field,reference);double seam=0;var seeds=MantleNoiseFields.Seeds(seed);
                    foreach(int cell in new[]{0,grid.Columns-1,grid.CellCount/2,grid.CellCount-1})
                    {
                        var center=Evolve.Engine.WorldGeneration.Layers.Grid.Geometry.FlatHexGeometry.Center(grid.GetOffset(cell));
                        double a=Sample(grid,center.X+shift.Item1,center.Y+shift.Item2,frequency.Item2,seeds.Regional,geometry);
                        double b=Sample(grid,center.X+shift.Item1+1.5*grid.Columns,center.Y+shift.Item2,frequency.Item2,seeds.Regional,geometry);
                        seam=Math.Max(seam,Math.Abs(a-b));
                    }
                    shifts.Add(new(size.ToString(),seed,geometry.ToString(),frequency.Item1,frequency.Item2,shift.Item1,shift.Item2,
                        differences[0],differences[1],differences[2],differences[3],seam,MantleSpatialDesign.Hash(field)));
                }
            }
            Require(before.SequenceEqual(packed)&&earlier==TectonicInfluenceVectors.Hash(TectonicInfluenceVectors.Serialize(influence,distances)),"Frozen fields changed.");
            Console.WriteLine($"CAL02 {size}/{seed}: 15 paired candidates, exact archived controls, 24 subcell probes; all balance/replay checks passed.");
        }
        WriteCsv(Path.Combine(directory,"timings.csv"),phases);WriteCsv(Path.Combine(directory,"metrics.csv"),metrics);WriteCsv(Path.Combine(directory,"shifts.csv"),shifts);
        WriteCsv(Path.Combine(directory,"summary.csv"),phases.Where(row=>!row.Warmup).GroupBy(row=>(row.Size,row.Candidate,row.Operation)).Select(group=>new
        {
            group.Key.Size,group.Key.Candidate,group.Key.Operation,Samples=group.Count(),MinimumMs=group.Min(row=>row.Milliseconds),
            MedianMs=Median(group.Select(row=>row.Milliseconds)),MaximumMs=group.Max(row=>row.Milliseconds),
            MeanAllocatedBytes=group.Average(row=>row.AllocatedBytes),MinimumRetainedBytes=group.Min(row=>row.RetainedGcDeltaBytes),MaximumRetainedBytes=group.Max(row=>row.RetainedGcDeltaBytes)
        }));
        Console.WriteLine($"CAL02 complete: {phases.Count} phase rows, {metrics.Count} paired metric rows, {shifts.Count} shift rows; no policy selected.");
    }

    private static Metric Analyze(string size,long seed,MantleSpatialDesign.Candidate row,int batch,Result field,Result control,
        TectonicInfluenceFields influence,TectonicBoundaryDistances distances,string hash)
    {
        double[] sums=new double[4],squares=new double[4],min=Enumerable.Repeat(double.PositiveInfinity,4).ToArray(),max=Enumerable.Repeat(double.NegativeInfinity,4).ToArray();
        double[] z=new double[field.Count];double zp=0,zz=0,pp=0,sp=0,sz=0,dp=0,dd=0,sd=0,interior=0,boundary=0;int interiorCount=0,boundaryCount=0,saturated=0;
        int reach=influence.Parameters.GetReach(new HexGridData(Enum.Parse<MapSize>(size)));
        Span<double> values=stackalloc double[4];
        for(int cell=0;cell<field.Count;cell++)
        {
            var value=field[cell];values[0]=value.Potential;values[1]=value.FlowX;values[2]=value.FlowY;values[3]=value.SignedVertical;
            for(int channel=0;channel<4;channel++){double sample=values[channel];sums[channel]+=sample;squares[channel]+=sample*sample;min[channel]=Math.Min(min[channel],sample);max[channel]=Math.Max(max[channel],sample);}
            var source=influence[cell];double deformation=(double)source.Extension-source.Compression;
            z[cell]=deformation/(((double)row.Parameters.ReferenceSpeed+source.Extension)+source.Compression);
            double phi=value.Potential;sz+=z[cell];sp+=phi;zz+=z[cell]*z[cell];pp+=phi*phi;zp+=z[cell]*phi;sd+=deformation;dd+=deformation*deformation;dp+=deformation*phi;
            if(Math.Abs(z[cell])>=.9)saturated++;
            if(distances[cell]==0){boundary+=Math.Abs(z[cell]);boundaryCount++;}
            if(distances[cell]>reach){interior+=phi*phi;interiorCount++;}
        }
        double Correlation(double xy,double sx,double xx){double d=Math.Sqrt(Math.Max(0,(field.Count*xx-sx*sx)*(field.Count*pp-sp*sp)));return d==0?0:(field.Count*xy-sx*sp)/d;}
        double[] deltas=Differences(field,control);Array.Sort(z);double zrms=Math.Sqrt(zz/field.Count);
        var b=field.Balance;
        return new(size,seed,row.Name,batch,row.Operation.ToString(),row.SpatialPolicy.ToString(),row.Parameters.ReferenceSpeed,hash,
            min[0],max[0],Math.Sqrt(squares[0]/field.Count),min[1],max[1],Math.Sqrt(squares[1]/field.Count),min[2],max[2],Math.Sqrt(squares[2]/field.Count),
            min[3],max[3],Math.Sqrt(squares[3]/field.Count),deltas[0],deltas[1],deltas[2],deltas[3],zrms,z[(z.Length-1)/2],z[(int)(.99*(z.Length-1))],
            saturated*100d/field.Count,Correlation(zp,sz,zz),Correlation(dp,sd,dd),boundaryCount==0?0:boundary/boundaryCount,interiorCount==0?0:Math.Sqrt(interior/interiorCount),
            b.DoubleSum,b.DoubleBudget,b.FloatSum,b.FloatBudget);
    }
    private static double[] Differences(Result a,Result b)
    {
        double[] squares=new double[4];
        for(int cell=0;cell<a.Count;cell++){var x=a[cell];var y=b[cell];squares[0]+=Math.Pow((double)x.Potential-y.Potential,2);squares[1]+=Math.Pow((double)x.FlowX-y.FlowX,2);squares[2]+=Math.Pow((double)x.FlowY-y.FlowY,2);squares[3]+=Math.Pow((double)x.SignedVertical-y.SignedVertical,2);}
        for(int channel=0;channel<4;channel++)squares[channel]=Math.Sqrt(squares[channel]/a.Count);return squares;
    }
    private static (T Value,Timing Timing) Measure<T>(Func<T> operation) where T:class
    {
        long retained=GC.GetTotalMemory(true),allocated=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();T value=operation();
        long end=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread()-allocated,after=GC.GetTotalMemory(true);GC.KeepAlive(value);
        return(value,new((end-start)*1000d/Stopwatch.Frequency,bytes,after-retained));
    }
    private static double Median(IEnumerable<double> values){double[] sorted=values.Order().ToArray();return sorted.Length%2==0?sorted[sorted.Length/2-1]/2+sorted[sorted.Length/2]/2:sorted[sorted.Length/2];}
    private static void WriteCsv<T>(string path,IEnumerable<T> rows)
    {
        var properties=typeof(T).GetProperties();using var writer=new StreamWriter(path);writer.WriteLine(string.Join(",",properties.Select(p=>p.Name)));
        foreach(var row in rows)writer.WriteLine(string.Join(",",properties.Select(p=>"\""+Convert.ToString(p.GetValue(row),CultureInfo.InvariantCulture)!.Replace("\"","\"\"")+"\"")));
    }
}
