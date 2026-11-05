using GEvolve.Engine.World.LandmassGeneration.Shared;

/// <summary>Diagnostics in unit-radius hex geometry. No acceptance quotas.</summary>
internal sealed record CoastMetrics(int[] Areas, int[] Labels, int Coast, int OpenEdges,
    int[] Orientations, double NormalizedCoast, double MeanCompactness, double MaxCompactness,
    int ClippedMasses, int ExactStraightMax, double DigitalStraightMax)
{
    private readonly record struct Vertex(int X, int Y);
    private readonly record struct Edge(Vertex A, Vertex B, int Dx, int Dy, int Orientation);

    public static CoastMetrics Measure(HexTopology topology, byte[] surface)
    {
        if (surface.Length != topology.Count || surface.Any(value => value > 1))
            throw new ArgumentException("Metrics require a complete binary surface.");
        var labels = new int[topology.Count]; Array.Fill(labels, -1);
        var areas = new List<int>();
        var pending = new Queue<int>();
        for (int id = 0; id < topology.Count; id++)
        {
            if (surface[id] == 0 || labels[id] >= 0) continue;
            int label = areas.Count, area = 0; labels[id] = label; pending.Enqueue(id);
            while (pending.TryDequeue(out int cell))
            {
                area++;
                foreach (int neighbor in topology.Neighbors(cell))
                    if (surface[neighbor] == 1 && labels[neighbor] < 0)
                    { labels[neighbor] = label; pending.Enqueue(neighbor); }
            }
            areas.Add(area);
        }
        // Exact integer vertices: x/(radius/2), y/(sqrt(3)*radius/2).
        // The X period identifies seam vertices without floating-point rounding.
        (int X, int Y)[] corners = [(2,0),(1,1),(-1,1),(-2,0),(-1,-1),(1,-1)];
        int period = 3 * topology.Columns;
        var edges = new List<Edge>(); var byVertex = new Dictionary<Vertex,List<int>>();
        var perimeters = new int[areas.Count]; var clipped = new bool[areas.Count];
        var orientations = new int[3]; int open = 0;
        for (int cell = 0; cell < topology.Count; cell++)
        {
            if (surface[cell] == 0) continue;
            int column = cell % topology.Columns, row = cell / topology.Columns, parity = column & 1;
            (int Column, int Row)[] neighbors = [(column+1,row+parity),(column,row+1),
                (column-1,row+parity),(column-1,row-1+parity),(column,row-1),(column+1,row-1+parity)];
            for (int side = 0; side < 6; side++)
            {
                var adjacent = neighbors[side];
                if (adjacent.Row < 0 || adjacent.Row >= topology.Rows)
                { open++; perimeters[labels[cell]]++; clipped[labels[cell]] = true; continue; }
                int id = topology.Id(adjacent.Column,adjacent.Row);
                if (!topology.Neighbors(cell).Contains(id))
                    throw new InvalidOperationException("Coast edge geometry differs from the official topology.");
                if (surface[id] == 1) continue;
                perimeters[labels[cell]]++; orientations[side % 3]++;
                var from = corners[side]; var to = corners[(side+1)%6];
                Vertex a = At(from), b = At(to);
                int index = edges.Count;
                edges.Add(new(a,b,to.X-from.X,to.Y-from.Y,side%3));
                Add(a); Add(b);
                Vertex At((int X,int Y) point) => new(((2+3*column+point.X)%period+period)%period,
                    2*row+parity+1+point.Y);
                void Add(Vertex vertex)
                { if (!byVertex.TryGetValue(vertex,out var list)) byVertex[vertex]=list=[]; list.Add(index); }
            }
        }
        // Trace actual coastline chains, with seam coordinates unwrapped locally.
        // Exact collinearity on a hex tiling is usually one edge. Also report
        // a macroscopic straight-run diagnostic at one radius of tolerance.
        var visited = new bool[edges.Count]; int exactMax = 0; double digitalMax = 0;
        foreach (var endpoint in byVertex.Where(pair=>pair.Value.Count==1).OrderBy(pair=>pair.Key.X).ThenBy(pair=>pair.Key.Y))
            if (!visited[endpoint.Value[0]]) Trace(endpoint.Key,endpoint.Value[0]);
        for (int index = 0; index < edges.Count; index++)
            if (!visited[index]) Trace(edges[index].A,index);
        var compactness = areas.Select((area,index)=>perimeters[index]/Math.Sqrt(area*3*Math.Sqrt(3)/2)).ToArray();
        double totalArea = areas.Sum() * 3*Math.Sqrt(3)/2;
        double mean = areas.Count==0 ? 0 : compactness.Select((value,index)=>value*areas[index]).Sum()/areas.Sum();
        return new(areas.OrderDescending().ToArray(),labels,edges.Count,open,orientations,
            totalArea==0?0:edges.Count/Math.Sqrt(totalArea), mean,compactness.DefaultIfEmpty(0).Max(),
            clipped.Count(value=>value),exactMax,digitalMax);

        void Trace(Vertex start,int index)
        {
            Vertex current=start; double x=start.X/2.0,y=start.Y*Math.Sqrt(3)/2;
            var points=new List<(double X,double Y)> { (x,y) };
            int lastOrientation=-1,run=0;
            while (!visited[index])
            {
                var edge=edges[index]; visited[index]=true;
                bool forward=current==edge.A;
                x+=(forward?edge.Dx:-edge.Dx)/2.0;
                y+=(forward?edge.Dy:-edge.Dy)*Math.Sqrt(3)/2;
                points.Add((x,y));
                run=edge.Orientation==lastOrientation?run+1:1;
                exactMax=Math.Max(exactMax,run); lastOrientation=edge.Orientation;
                current=forward?edge.B:edge.A;
                var next=byVertex[current].Where(e=>!visited[e]).ToArray();
                if (next.Length==0) break;
                if (next.Length!=1) throw new InvalidOperationException("Ambiguous coastline vertex.");
                index=next[0];
            }
            var segments=new Stack<(int First,int Last)>(); segments.Push((0,points.Count-1));
            while(segments.TryPop(out var segment))
            {
                int furthest=-1; double maximum=0;
                for(int i=segment.First+1;i<segment.Last;i++)
                {
                    double d=DistanceSquared(points[i],points[segment.First],points[segment.Last]);
                    if(d>maximum) { maximum=d; furthest=i; }
                }
                if(maximum>1 && furthest>=0)
                { segments.Push((segment.First,furthest)); segments.Push((furthest,segment.Last)); }
                else digitalMax=Math.Max(digitalMax,segment.Last-segment.First);
            }
        }
    }

    private static double DistanceSquared((double X,double Y) point,(double X,double Y) a,(double X,double Y) b)
    {
        double dx=b.X-a.X,dy=b.Y-a.Y,length=dx*dx+dy*dy;
        double t=length==0?0:Math.Clamp(((point.X-a.X)*dx+(point.Y-a.Y)*dy)/length,0,1);
        double x=point.X-a.X-t*dx,y=point.Y-a.Y-t*dy;
        return x*x+y*y;
    }
}
