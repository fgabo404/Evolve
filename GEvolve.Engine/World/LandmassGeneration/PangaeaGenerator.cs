using GEvolve.Definitions.WorldGeneration.Configuration;
using GEvolve.Engine.World.LandmassGeneration.Shared;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Pangea 2.x: esqueleto continental ancho, costa accidentada multiescala y corrección hexagonal.</summary>
public sealed class PangaeaGenerator : ILandmassGenerator
{
    public LandmassType Type => LandmassType.Pangaea;
    private readonly record struct Node(double X, double Y, double Radius);

    // Proporciones relativas al mapa; la semilla desplaza núcleos, radios y costas.
    private static readonly Node[] Backbone =
    [
        new(.17,.61,.185), new(.26,.40,.23), new(.43,.27,.245),
        new(.59,.34,.20), new(.73,.53,.20), new(.67,.73,.175),
        new(.49,.71,.215), new(.33,.77,.175)
    ];
    private static readonly (Node Node, int Parent)[] Branches =
    [
        (new(.25,.36,.16),1),(new(.19,.17,.10),1),
        (new(.44,.19,.14),2),(new(.58,.14,.10),2),
        (new(.80,.36,.13),4),(new(.91,.26,.09),4),
        (new(.77,.73,.14),5),(new(.89,.87,.10),5),
        (new(.51,.89,.13),6),(new(.38,.50,.15),1),
        (new(.09,.66,.115),0)
    ];

    public byte[] Generate(HexTopology topology, LandmassConfiguration configuration, int attempt)
    {
        var profile = configuration.Pangaea
            ?? throw new ArgumentException("Pangaea parameters missing.");
        int columns = topology.Columns, rows = topology.Rows, count = topology.Count;
        var rng = new DeterministicRandom(configuration.Seed, (uint)(0xA9EA + attempt));
        double offset = (rng.NextDouble() - .5) * columns * .18;
        Node Perturb(Node n, double movement) => new(
            Mod(n.X * columns + offset + (rng.NextDouble() - .5) * movement * columns, columns),
            Math.Clamp(n.Y * rows + (rng.NextDouble() - .5) * movement * rows, 3, rows - 4),
            n.Radius * Math.Min(columns, rows) * (.90 + rng.NextDouble() * .20));
        var main = Backbone.Select(n => Perturb(n, .12)).ToArray();
        var arms = Branches.Select(b => Perturb(b.Node, .17)).ToArray();
        var influence = new double[count];
        Array.Fill(influence, -1000);
        double deltaX(double x, double center)
        {
            double delta = x - center;
            if (delta > columns / 2.0) delta -= columns;
            if (delta < -columns / 2.0) delta += columns;
            return delta;
        }
        void Stamp(Node node)
        {
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                double distance = Math.Sqrt(Math.Pow(deltaX(col,node.X),2) +
                    Math.Pow((row-node.Y)*1.12,2));
                int id = row*columns+col;
                influence[id] = Math.Max(influence[id], node.Radius-distance);
            }
        }
        void Join(Node a, Node b)
        {
            double dx = deltaX(b.X,a.X);
            int steps = Math.Max(6,(int)(Math.Sqrt(dx*dx+Math.Pow(b.Y-a.Y,2))*2));
            for(int i=0;i<=steps;i++)
            {
                double t = (double)i/steps;
                Stamp(new Node(Mod(a.X+t*dx,columns),a.Y+t*(b.Y-a.Y),
                    a.Radius+(b.Radius-a.Radius)*t));
            }
        }
        foreach(var node in main) Stamp(node);
        for(int i=1;i<main.Length;i++) Join(main[i-1],main[i]);
        for(int i=0;i<arms.Length;i++)
        {
            Stamp(arms[i]);
            Join(main[Branches[i].Parent],arms[i]);
        }
        // Entrantes marinos anchos, deformados por el mismo desplazamiento horizontal.
        foreach(var bay in new [] { (56d,36d,15d,15d,12d),(14d,22d,12d,16d,13d),(85d,46d,11d,12d,11d) })
        {
            double cx=Mod(bay.Item1/100*columns+offset+(rng.NextDouble()-.5)*columns*.10,columns);
            double cy=Math.Clamp(bay.Item2/70*rows+(rng.NextDouble()-.5)*rows*.10,2,rows-3);
            double sx=bay.Item3/100*columns, sy=bay.Item4/70*rows;
            for(int row=0;row<rows;row++)
            for(int col=0;col<columns;col++)
            {
                double dist=Math.Pow(deltaX(col,cx)/sx,2)+Math.Pow((row-cy)/sy,2);
                influence[row*columns+col]-=bay.Item5*Math.Min(columns,rows)/70.0*Math.Exp(-2*dist);
            }
        }
        var ranking = new double[count];
        for(int row=0;row<rows;row++)
        for(int col=0;col<columns;col++)
        {
            int id=row*columns+col;
            // Cuatro bandas: accidentes grandes, bahías, puntas y detalles costeros.
            // El mapa sigue siendo periódico en X. No alteramos su conectividad aquí.
            double broad = PeriodicNoise.Sample(col,row,columns,rows,
                Math.Max(2,profile.NoiseFrequency/2),configuration.Seed+311+attempt*1877);
            double medium = PeriodicNoise.Sample(col,row,columns,rows,
                profile.NoiseFrequency,configuration.Seed+1013+attempt*1877);
            double fine = PeriodicNoise.Sample(col,row,columns,rows,
                profile.NoiseFrequency*2,configuration.Seed+1999+attempt*1877);
            double detail = PeriodicNoise.Sample(col,row,columns,rows,
                profile.NoiseFrequency*4,configuration.Seed+2999+attempt*1877);
            ranking[id]=influence[id]+profile.CoastRoughness*
                (6.0*broad+8.0*medium+4.5*fine+2.0*detail);
        }
        var sorted=(double[])ranking.Clone();
        Array.Sort(sorted);
        int target=(int)(count*profile.TargetLandFraction);
        double threshold=sorted[Math.Clamp(count-target,0,count-1)];
        var land=new byte[count];
        for(int i=0;i<count;i++) if(ranking[i]>=threshold) land[i]=1;

        // Un mar está abierto solo si alcanza norte/sur; el borde este-oeste envuelve.
        var visited=new bool[count];
        var queue=new Queue<int>();
        for(int col=0;col<columns;col++)
        {
            int north=col, south=(rows-1)*columns+col;
            if(land[north]==0 && !visited[north]) { visited[north]=true; queue.Enqueue(north); }
            if(land[south]==0 && !visited[south]) { visited[south]=true; queue.Enqueue(south); }
        }
        while(queue.Count>0)
        {
            int id=queue.Dequeue();
            foreach(int other in topology.Neighbors(id))
                if(land[other]==0 && !visited[other])
                { visited[other]=true;queue.Enqueue(other); }
        }
        for(int i=0;i<count;i++) if(land[i]==0 && !visited[i]) land[i]=1;

        // Conservar exclusivamente la masa mayor, con conectividad odd-q real.
        Array.Fill(visited,false);
        int[] largest=[];
        for(int id=0;id<count;id++)
        {
            if(land[id]==0 || visited[id]) continue;
            var component=new List<int>();
            visited[id]=true;queue.Enqueue(id);
            while(queue.Count>0)
            {
                int current=queue.Dequeue();component.Add(current);
                foreach(int other in topology.Neighbors(current))
                    if(land[other]==1 && !visited[other])
                    { visited[other]=true;queue.Enqueue(other); }
            }
            if(component.Count>largest.Length) largest=component.ToArray();
        }
        Array.Fill(land,(byte)0);
        foreach(int id in largest)land[id]=1;
        return land;
    }
    private static double Mod(double value,int width) => (value%width+width)%width;
}
