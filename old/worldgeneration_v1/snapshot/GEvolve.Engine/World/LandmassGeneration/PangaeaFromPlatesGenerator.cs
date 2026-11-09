using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Stage 02 Pangaea terrain, adapted from the legacy generator.
/// The stage 01 plate IDs are inputs and are never regenerated or overwritten.
/// </summary>
public sealed class PangaeaFromPlatesGenerator
{
    public const string Version = "pangaea-from-plates-3";

    public (byte[] Surface, byte[] Elevation) Generate(
        HexTopology topology, LandmassConfiguration configuration, byte[] plates, int attempt = 0)
        => Generate(topology, configuration, plates, attempt, null);

    public (byte[] Surface, byte[] Elevation) Generate(
        HexTopology topology, LandmassConfiguration configuration, byte[] plates, int attempt,
        Action<PangaeaElevationSources>? captureSources)
    {
        var surface = GenerateSurface(topology, configuration, plates, attempt);
        return (surface.Surface, GenerateElevation(surface, captureSources));
    }

    internal PangaeaSurfaceWork GenerateSurface(HexTopology topology,
        LandmassConfiguration configuration, byte[] plates, int attempt, bool captureBaseFields = false)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(plates);
        if (configuration.Type != LandmassType.Pangaea)
            throw new ArgumentException("Pangaea world type is required.", nameof(configuration));
        var profile = configuration.Pangaea
            ?? throw new ArgumentException("Pangaea profile is required.", nameof(configuration));
        int width = topology.Columns, height = topology.Rows, count = topology.Count;
        int numberOfPlates = TectonicPlateGenerator.NumberOfPlates;
        if (plates.Length != count || plates.Any(p => p >= numberOfPlates))
            throw new ArgumentException("Stage 01 plate IDs do not match the grid.", nameof(plates));

        // Geometric centers of the ACTUAL plates, not new random Voronoi seeds.
        // Circular X mean respects the east/west world seam.
        var cosine = new double[numberOfPlates];
        var sine = new double[numberOfPlates];
        var sumY = new double[numberOfPlates];
        var populations = new int[numberOfPlates];
        for (int id = 0; id < count; id++)
        {
            int plate = plates[id], x = id % width, y = id / width;
            double angle = 2 * Math.PI * x / width;
            cosine[plate] += Math.Cos(angle);
            sine[plate] += Math.Sin(angle);
            sumY[plate] += y;
            populations[plate]++;
        }
        if (populations.Any(p => p == 0))
            throw new ArgumentException("All sixteen tectonic plates must exist.", nameof(plates));

        var centerX = new double[numberOfPlates];
        var centerY = new double[numberOfPlates];
        var speedX = new double[numberOfPlates];
        var speedY = new double[numberOfPlates];
        var random = new DeterministicRandom(configuration.Seed, unchecked((uint)(0x4C414E44 + attempt)));
        for (int p = 0; p < numberOfPlates; p++)
        {
            double angle = Math.Atan2(sine[p], cosine[p]);
            if (angle < 0) angle += 2 * Math.PI;
            centerX[p] = angle * width / (2 * Math.PI);
            centerY[p] = sumY[p] / populations[p];
            speedX[p] = random.NextDouble() * 2 - 1;
            speedY[p] = random.NextDouble() * 2 - 1;
        }
        double Wrapped(double dx) => dx - width * Math.Round(dx / width);
        // Original Pangaea process: choose connected tectonic regions, compute
        // a signed coast-distance score, grow one continental mass, close inland
        // water pockets, then normalize relief. No changes to 'plates'.
        var adjacent=new HashSet<int>[numberOfPlates];
        for(int i=0;i<numberOfPlates;i++)adjacent[i]=[];
        var compression=new double[count];
        var extension=new double[count];
        for(int id=0;id<count;id++)
        {
            int a=plates[id];
            foreach(int other in topology.Neighbors(id))
            {
                int b=plates[other];
                if(a==b)continue;
                adjacent[a].Add(b);
                double dx=Wrapped(centerX[b]-centerX[a]),dy=centerY[b]-centerY[a];
                double rate=((speedX[a]-speedX[b])*dx+
                    (speedY[a]-speedY[b])*dy)/Math.Sqrt(dx*dx+dy*dy+1e-6);
                compression[id]=Math.Max(compression[id],rate);
                extension[id]=Math.Max(extension[id],-rate);
            }
        }
        int root=random.NextInt(numberOfPlates);
        var chosen=new HashSet<int>{root};
        var front=new HashSet<int>(adjacent[root]);
        int targetPlates=Math.Max(5,numberOfPlates*2/5);
        double heading=random.NextDouble()*Math.PI*2;
        while(chosen.Count<targetPlates && front.Count>0)
        {
            int next=-1;double best=double.NegativeInfinity;
            foreach(int p in front.Order())
            {
                double dx=Wrapped(centerX[p]-centerX[root]);
                double direction=Math.Atan2(centerY[p]-centerY[root],dx);
                double priority=Math.Cos(direction-heading)+
                    2*(random.NextDouble()-.5);
                if(priority>best){best=priority;next=p;}
            }
            front.Remove(next);
            if(!chosen.Add(next))continue;
            foreach(int p in adjacent[next])
                if(!chosen.Contains(p))front.Add(p);
            heading+=(random.NextDouble()-.5)*1.3;
        }
        var continent=new bool[count];
        for(int i=0;i<count;i++)continent[i]=chosen.Contains(plates[i]);
        // Distancia firmada aproximada por propagación hexagonal desde costas.
        var distance=new int[count];
        Array.Fill(distance,int.MaxValue);
        var queue=new Queue<int>();
        for(int id=0;id<count;id++)
        {
            foreach(int nb in topology.Neighbors(id))
                if(continent[id]!=continent[nb])
                {distance[id]=0;queue.Enqueue(id);break;}
        }
        while(queue.Count>0)
        {
            int id=queue.Dequeue();
            foreach(int nb in topology.Neighbors(id))
                if(distance[nb]>distance[id]+1)
                {distance[nb]=distance[id]+1;queue.Enqueue(nb);}
        }
        var score=new double[count];
        var uplift=new double[count];
        for(int id=0;id<count;id++)
        {
            int x=id%width,y=id/width;
            double large=PeriodicNoise.Sample(x,y,width,height,4,configuration.Seed+attempt*2011+131);
            double medium=PeriodicNoise.Sample(x,y,width,height,10,configuration.Seed+attempt*2011+337);
            double fine=PeriodicNoise.Sample(x,y,width,height,21,configuration.Seed+attempt*2011+719);
            // La selección de placas guía la silueta; el ruido deforma las costas sin tocar los límites tectónicos.
            score[id]=(continent[id]?1:-1)*Math.Min(distance[id],20)+
                5.3*large+2.5*medium+.8*fine+
                1.2*(compression[id]-extension[id]);
            uplift[id]=Math.Max(0,compression[id]-extension[id]);
        }
        // Expandir una sola masa por vecinos hexagonales hasta el 40% objetivo.
        var surface=new byte[count];
        var frontier=new PriorityQueue<int,double>();
        var queued=new bool[count];
        int start=Array.FindIndex(plates,p=>p==root);
        if(start<0)start=count/2;
        frontier.Enqueue(start,-score[start]);queued[start]=true;
        int wanted=(int)Math.Round(profile.TargetLandFraction*count);
        int total=0;
        while(frontier.TryDequeue(out int id,out _) && total<wanted)
        {
            if(surface[id]!=0)continue;
            surface[id]=1;total++;
            foreach(int nb in topology.Neighbors(id))
                if(surface[nb]==0 && !queued[nb])
                {queued[nb]=true;frontier.Enqueue(nb,-score[nb]+nb*1e-10);}
        }
        // Agua cerrada: la conectada a norte/sur es el océano exterior.
        var ocean=new bool[count];
        queue.Clear();
        for(int x=0;x<width;x++)
        {
            foreach(int id in new[]{x,(height-1)*width+x})
                if(surface[id]==0 && !ocean[id]){ocean[id]=true;queue.Enqueue(id);}
        }
        while(queue.Count>0)
        {
            int id=queue.Dequeue();
            foreach(int nb in topology.Neighbors(id))
                if(surface[nb]==0 && !ocean[nb])
                {ocean[nb]=true;queue.Enqueue(nb);}
        }
        for(int id=0;id<count;id++)if(surface[id]==0 && !ocean[id])surface[id]=1;

        return new(topology, configuration, attempt, surface, uplift)
        {
            Fields = !captureBaseFields ? null : new PangaeaBaseFields
            {
                CenterX = centerX.ToArray(), CenterY = centerY.ToArray(), VelocityX = speedX.ToArray(), VelocityY = speedY.ToArray(),
                Compression = compression.ToArray(), Extension = extension.ToArray(), InitialPressure = uplift.ToArray()
            }
        };
    }

    internal byte[] GenerateElevation(PangaeaSurfaceWork work,
        Action<PangaeaElevationSources>? captureSources)
    {
        var topology = work.Topology;
        var configuration = work.Configuration;
        int attempt = work.Attempt, width = topology.Columns, height = topology.Rows, count = topology.Count;
        byte[] surface = work.Surface;
        double[] uplift = work.InitialUplift;

        // Suavizar la presión tectónica a través de los vecinos reales.
        for(int pass=0;pass<4;pass++)
        {
            var next=new double[count];
            for(int id=0;id<count;id++)
            {
                double sum=uplift[id]*2;int weight=2;
                foreach(int nb in topology.Neighbors(id)){sum+=uplift[nb];weight++;}
                next[id]=sum/weight;
            }
            uplift=next;
        }
        var potential=new double[count];
        var localDetail=new double[count];
        double[]? regionalNoise = work.Fields is null ? null : new double[count];
        var sorted=new List<double>();
        for(int id=0;id<count;id++)
        {
            int x=id%width,y=id/width;
            localDetail[id]=PeriodicNoise.Sample(x,y,width,height,15,configuration.Seed+attempt*2011+2053);
            potential[id]=2.5*uplift[id]+
                1.3*(regionalNoise is null
                    ? PeriodicNoise.Sample(x,y,width,height,5,configuration.Seed+attempt*2011+1009)
                    : (regionalNoise[id] = PeriodicNoise.Sample(x,y,width,height,5,configuration.Seed+attempt*2011+1009)))+
                .45*localDetail[id];
            if(surface[id]==1)sorted.Add(potential[id]);
        }
        sorted.Sort();
        // Escala continua 0..100: la tectónica conserva sus presiones y la
        // silueta continental; solo se transforma el potencial en elevación.
        // Los percentiles reducen la influencia de valores extremos y los
        // puntos de control reservan las elevaciones más altas para las cumbres.
        double[] quantile = [0, .34, .78, .955, 1];
        double[] level = [0, 15, 50, 80, 100];
        var anchors=new double[quantile.Length];
        for(int i=0;i<anchors.Length;i++)
            anchors[i]=sorted[(int)Math.Round(quantile[i]*(sorted.Count-1))];
        var elevation=new byte[count];
        for(int id=0;id<count;id++)
        {
            if(surface[id]==0)continue; // Océano: referencia de nivel 0.
            double value=potential[id];
            int section=0;
            while(section<anchors.Length-2 && value>anchors[section+1])
                section++;
            double span=anchors[section+1]-anchors[section];
            double fraction=span>1e-12
                ?Math.Clamp((value-anchors[section])/span,0,1):0;
            elevation[id]=(byte)Math.Clamp(
                (int)Math.Round(level[section]+fraction*
                    (level[section+1]-level[section])),1,100);
        }
        long calibrationStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        byte[] calibrated = ShapeUpperRelief(surface, elevation, localDetail, uplift);
        byte[] priorCalibration = captureSources is null ? [] : (byte[])calibrated.Clone();
        RecoverRegionalShoulders(elevation, calibrated);
        double calibrationMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(calibrationStarted).TotalMilliseconds;
        if (captureSources is not null)
            captureSources(PangaeaElevationSources.Measure(surface, calibrated,
                potential, uplift, quantile, anchors, level) with
            {
                Previous = PangaeaElevationSources.Measure(surface, elevation,
                    potential, uplift, quantile, anchors, level),
                PreviousBaseByCellId = elevation,
                PriorCalibration = PangaeaElevationSources.Measure(surface, priorCalibration,
                    potential, uplift, quantile, anchors, level),
                PriorCalibrationBaseByCellId = priorCalibration,
                CalibrationMilliseconds = calibrationMilliseconds,
                Fields = work.Fields is null ? null : work.Fields with
                {
                    SmoothedPressure = uplift.ToArray(), RegionalNoise = regionalNoise!,
                    LocalNoise = localDetail.ToArray(), Potential = potential.ToArray()
                }
            });
        return calibrated;
    }

    /// <summary>
    /// Recover only regional height that P03-A.3 removed. Fully retain recovery
    /// through 75, taper it to zero at 85, and never lower a structured summit.
    /// Thus P03-A.3's complete >=85 base mask and heights remain identical.
    /// </summary>
    internal static void RecoverRegionalShoulders(byte[] regional, byte[] structured)
    {
        for (int id = 0; id < regional.Length; id++)
        {
            if (regional[id] <= structured[id]) continue;
            double t = Math.Clamp((regional[id] - 75) / 10.0, 0, 1);
            double recovery = 1 - t * t * (3 - 2 * t);
            structured[id] = (byte)Math.Round(structured[id] +
                recovery * (regional[id] - structured[id]));
        }
    }

    /// <summary>
    /// Preserve low/middle relief; structure the upper regional envelope with the
    /// already sampled local detail and pressure, instead of broad noise alone.
    /// No new noise, RNG, land classification or tectonic field is introduced.
    /// </summary>
    internal static byte[] ShapeUpperRelief(byte[] surface, byte[] regional,
        double[] detail, double[] uplift)
    {
        const int shoulder = 60;
        double minimumDetail = double.PositiveInfinity, maximumDetail = double.NegativeInfinity;
        double maximumUplift = 0;
        for (int id = 0; id < surface.Length; id++)
        {
            if (surface[id] != 1) continue;
            minimumDetail = Math.Min(minimumDetail, detail[id]);
            maximumDetail = Math.Max(maximumDetail, detail[id]);
            maximumUplift = Math.Max(maximumUplift, uplift[id]);
        }
        double detailSpan = maximumDetail - minimumDetail;
        double Support(int id)
        {
            double local = detailSpan > 1e-12 ? (detail[id] - minimumDetail) / detailSpan : 0;
            double pressure = maximumUplift > 1e-12 ? uplift[id] / maximumUplift : 0;
            // A quarter of the regional envelope remains even without a local
            // crest. Squared detail localizes crests; pressure can also support them.
            return .25 + .75 * Math.Max(local * local, pressure);
        }
        double maximumProminence = 0;
        for (int id = 0; id < surface.Length; id++)
            if (surface[id] == 1 && regional[id] > shoulder)
                maximumProminence = Math.Max(maximumProminence,
                    (regional[id] - shoulder) * Support(id));
        var result = (byte[])regional.Clone();
        if (maximumProminence <= 1e-12) return result;
        for (int id = 0; id < surface.Length; id++)
        {
            if (surface[id] != 1 || regional[id] <= shoulder) continue;
            double prominence = (regional[id] - shoulder) * Support(id);
            // Scale only this structured upper band to its observed maximum.
            // There is still a height-100 summit; no quota of high land is imposed.
            result[id] = (byte)Math.Clamp((int)Math.Round(shoulder +
                (100 - shoulder) * prominence / maximumProminence), shoulder, 100);
        }
        return result;
    }
}
