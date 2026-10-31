using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>Connected convergent margins available before crust or land are selected.</summary>
public static class OrogenicOpportunityAnalyzer
{
    public sealed record Result(double[,] PairScore, double[] PlateScore, int MarginCount);

    public static Result Analyze(HexTopology topology, TectonicMotionState motion)
    {
        var byPair = new Dictionary<(byte, byte), List<int>>();
        for (int index = 0; index < motion.Interactions.Length; index++)
        {
            var edge = motion.Interactions[index];
            if (edge.Convergence < 1 || edge.Convergence < edge.Shear * .65) continue;
            var pair = (Math.Min(edge.FirstPlateId, edge.SecondPlateId),
                Math.Max(edge.FirstPlateId, edge.SecondPlateId));
            if (!byPair.TryGetValue(pair, out var list)) byPair[pair] = list = [];
            list.Add(index);
        }
        var pairScore = new double[16, 16];
        int margins = 0;
        foreach (var (pair, indices) in byPair)
        {
            var byCell = new Dictionary<int, List<int>>();
            foreach (int index in indices)
            {
                var edge = motion.Interactions[index];
                Add(edge.FirstCellId, index);
                Add(edge.SecondCellId, index);
            }
            var remaining = indices.ToHashSet();
            foreach (int start in indices)
            {
                if (!remaining.Remove(start)) continue;
                int length = 0;
                double convergence = 0;
                var queue = new Queue<int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    var edge = motion.Interactions[index];
                    length++;
                    convergence += edge.Convergence;
                    Visit(edge.FirstCellId);
                    Visit(edge.SecondCellId);
                }
                if (length < 4) continue;
                margins++;
                double score = length * Math.Sqrt((convergence / length) / 8.0);
                pairScore[pair.Item1, pair.Item2] += score;

                void Visit(int cell)
                {
                    Enqueue(cell);
                    foreach (int neighbor in topology.Neighbors(cell)) Enqueue(neighbor);
                }
                void Enqueue(int cell)
                {
                    if (!byCell.TryGetValue(cell, out var candidates)) return;
                    foreach (int candidate in candidates)
                        if (remaining.Remove(candidate)) queue.Enqueue(candidate);
                }
            }
            void Add(int cell, int index)
            {
                if (!byCell.TryGetValue(cell, out var list)) byCell[cell] = list = [];
                list.Add(index);
            }
        }
        double maximum = 0;
        for (int first = 0; first < 16; first++)
        for (int second = first + 1; second < 16; second++)
            maximum = Math.Max(maximum, pairScore[first, second]);
        var plateScore = new double[16];
        if (maximum > 0)
            for (int first = 0; first < 16; first++)
            for (int second = first + 1; second < 16; second++)
            {
                double score = pairScore[first, second] / maximum;
                pairScore[first, second] = pairScore[second, first] = score;
                plateScore[first] = Math.Max(plateScore[first], score);
                plateScore[second] = Math.Max(plateScore[second], score);
            }
        return new(pairScore, plateScore, margins);
    }
}
