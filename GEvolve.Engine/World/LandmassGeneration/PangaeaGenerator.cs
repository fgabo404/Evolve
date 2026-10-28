using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Modelo experimental determinista: placas Voronoi y velocidades, sin alturas.</summary>
public sealed class PangaeaGenerator : ILandmassGenerator
{
    public LandmassType Type => LandmassType.Pangaea;
    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
        => GenerateTectonic(topology, configuration, attempt).Surface;

    public (byte[] Surface, byte[] Plates) GenerateTectonic(
        HexTopology topology, LandmassConfiguration configuration, int attempt)
    {
        var profile = configuration.Pangaea
            ?? throw new ArgumentException("Missing Pangaea parameters.");
        int width=topology.Columns, height=topology.Rows, count=topology.Count;
        var random = new DeterministicRandom(configuration.Seed, (uint)(0x7100 + attempt));
        int numberOfPlates = Math.Clamp(count / 310, 18, 70);
        var centerX = new double[numberOfPlates];
        var centerY = new double[numberOfPlates];
        var speedX = new double[numberOfPlates];
        var speedY = new double[numberOfPlates];
        for(int p=0;p<numberOfPlates;p++)
        {
            centerX[p]=random.NextDouble()*width;
            centerY[p]=3+random.NextDouble()*(height-6);
            speedX[p]=random.NextDouble()*2-1;
            speedY[p]=random.NextDouble()*2-1;
        }
        double Wrapped(double dx) => dx-width*Math.Round(dx/width);
        var plates=new byte[count];
        for(int id=0;id<count;id++)
        {
            int x=id%width,y=id/width,best=0;
            double minimum=double.MaxValue;
            for(int p=0;p<numberOfPlates;p++)
            {
                double dx=Wrapped(x-centerX[p]),dy=y-centerY[p];
                double squaredDistance=dx*dx+dy*dy;
                if(squaredDistance<minimum){minimum=squaredDistance;best=p;}
            }
            plates[id]=(byte)best;
        }
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
        for(int id=0;id<count;id++)
        {
            int x=id%width,y=id/width;
            double large=PeriodicNoise.Sample(x,y,width,height,4,configuration.Seed+attempt*2011+131);
            double medium=PeriodicNoise.Sample(x,y,width,height,10,configuration.Seed+attempt*2011+337);
            double fine=PeriodicNoise.Sample(x,y,width,height,21,configuration.Seed+attempt*2011+719);
            // La posición de placa define la silueta; el ruido deforma sus bordes.
            score[id]=(continent[id]?1:-1)*Math.Min(distance[id],20)+
                5.3*large+2.5*medium+.8*fine+
                1.2*(compression[id]-extension[id]);
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

        return(surface,plates);
    }
}
