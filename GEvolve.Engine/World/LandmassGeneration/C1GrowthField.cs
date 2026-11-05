using System.Runtime.InteropServices;
using System.Security.Cryptography;
using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;
using GEvolve.State.Game;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>A screened graph diffusion potential, not transported mass or topographic height.</summary>
public sealed class C1GrowthField
{
    private readonly HexTopology _topology;
    private readonly int[] _neighbors;
    private readonly double[] _sum;
    public double[] Conductances { get; }
    public double[] Resistance { get; }
    public double[] TensorXX { get; }
    public double[] TensorXY { get; }
    public double[] TensorYY { get; }
    public double[] Opportunity { get; }
    public double[] Stability { get; }
    public IReadOnlyDictionary<ulong, int> InteractionIndices => _interactions;
    private readonly Dictionary<ulong, int> _interactions = new();

    public C1GrowthField(HexTopology topology, byte[] plates, TectonicMotionState motion)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(plates);
        ArgumentNullException.ThrowIfNull(motion);
        _topology = topology;
        int n = topology.Count;
        if (plates.Length != n || plates.Any(p => p >= 16) || plates.Distinct().Count() != 16 ||
            motion.Version != TectonicMotionGenerator.Version || motion.Interactions is null ||
            motion.CompressionByCellId?.Length != n || motion.ExtensionByCellId?.Length != n ||
            motion.ShearByCellId?.Length != n || motion.DistanceToBoundaryByCellId?.Length != n ||
            !double.IsFinite(motion.InfluenceRadiusHexes) || motion.InfluenceRadiusHexes <= 0 ||
            motion.InfluenceRadiusHexes > n / 4.0)
            throw new ArgumentException("C1 requires all official plate and motion arrays.");
        for (int k = 0; k < motion.Interactions.Length; k++)
        {
            var edge = motion.Interactions[k];
            if ((uint)edge.FirstCellId >= (uint)n || (uint)edge.SecondCellId >= (uint)n ||
                edge.FirstCellId >= edge.SecondCellId || plates[edge.FirstCellId] != edge.FirstPlateId ||
                plates[edge.SecondCellId] != edge.SecondPlateId || edge.FirstPlateId == edge.SecondPlateId ||
                !topology.Neighbors(edge.FirstCellId).Contains(edge.SecondCellId) ||
                !double.IsFinite(edge.Convergence) || !double.IsFinite(edge.Divergence) || !double.IsFinite(edge.Shear) ||
                edge.Convergence < 0 || edge.Divergence < 0 || edge.Shear < 0 ||
                !_interactions.TryAdd(Key(edge.FirstCellId, edge.SecondCellId), k))
                throw new ArgumentException("Invalid or duplicate official contact for C1.");
            var expectedNormal = TectonicMotionGenerator.BoundaryNormal(topology, edge.FirstCellId, edge.SecondCellId);
            if (!double.IsFinite(edge.Normal.X) || !double.IsFinite(edge.Normal.Y) ||
                Math.Abs(edge.Normal.X - expectedNormal.X) > 1e-8 || Math.Abs(edge.Normal.Y - expectedNormal.Y) > 1e-8)
                throw new ArgumentException("C1 contact normal differs from physical wrapped geometry.");
        }
        _neighbors = new int[n * 6]; Array.Fill(_neighbors, -1);
        Conductances = new double[n * 6]; _sum = new double[n];
        Resistance = new double[n]; Stability = new double[n]; Opportunity = new double[n];
        TensorXX = new double[n]; TensorXY = new double[n]; TensorYY = new double[n];
        var weights = new double[n]; var visited = new int[n]; var distance = new int[n]; var queue = new int[n];
        int stamp = 0, radius = Math.Max(1, (int)Math.Ceiling(motion.InfluenceRadiusHexes));
        int prospectRadius = Math.Max(2, (int)Math.Ceiling(motion.InfluenceRadiusHexes * 4));
        foreach (var edge in motion.Interactions)
        {
            if (!Convergent(edge)) continue;
            var normal = TectonicMotionGenerator.BoundaryNormal(topology, edge.FirstCellId, edge.SecondCellId);
            double tx = -normal.Y, ty = normal.X, strength = Math.Tanh(edge.Convergence / 5);
            Spread(edge.FirstCellId); Spread(edge.SecondCellId);
            void Spread(int source)
            {
                int read = 0, write = 1; stamp++; queue[0] = source; visited[source] = stamp; distance[source] = 0;
                while (read < write)
                {
                    int cell = queue[read++]; double w = strength * (1 - distance[cell] / (radius + 1.0));
                    if (distance[cell] <= radius)
                    {
                        TensorXX[cell] += w * tx * tx; TensorXY[cell] += w * tx * ty; TensorYY[cell] += w * ty * ty;
                        weights[cell] += w;
                    }
                    Opportunity[cell] = Math.Max(Opportunity[cell], strength * (1 - distance[cell] / (prospectRadius + 1.0)));
                    if (distance[cell] == prospectRadius) continue;
                    foreach (int next in topology.Neighbors(cell))
                        if (plates[next] == plates[source] && visited[next] != stamp)
                        { visited[next] = stamp; distance[next] = distance[cell] + 1; queue[write++] = next; }
                }
            }
        }
        int verifiedEdges = 0;
        for (int id = 0; id < n; id++)
        {
            if (!float.IsFinite(motion.CompressionByCellId[id]) || !float.IsFinite(motion.ExtensionByCellId[id]) ||
                !float.IsFinite(motion.ShearByCellId[id])) throw new ArgumentException("Non-finite tectonic field for C1.");
            double c = Math.Tanh(motion.CompressionByCellId[id] / 5.0);
            double e = Math.Tanh(motion.ExtensionByCellId[id] / 5.0);
            double s = Math.Tanh(motion.ShearByCellId[id] / 5.0);
            if (!double.IsFinite(c + e + s) || c < 0 || e < 0 || s < 0)
                throw new ArgumentException("Invalid tectonic field for C1.");
            Resistance[id] = 1 + e + .25 * s - .5 * c;
            Stability[id] = .5 * Math.Clamp(motion.DistanceToBoundaryByCellId[id] / (2 * motion.InfluenceRadiusHexes), 0, 1)
                + .25 * (1 - c) + .25 * (1 - e);
            Opportunity[id] = .6 * Stability[id] + .4 * Opportunity[id];
            double denominator = weights[id] == 0 ? 1 : weights[id];
            TensorXX[id] = 1 + 2 * TensorXX[id] / denominator;
            TensorXY[id] = 2 * TensorXY[id] / denominator;
            TensorYY[id] = 1 + 2 * TensorYY[id] / denominator;
        }
        for (int id = 0; id < n; id++)
        {
            var neighbors = topology.Neighbors(id);
            for (int slot = 0; slot < neighbors.Length; slot++)
            {
                int next = neighbors[slot]; _neighbors[id * 6 + slot] = next;
                if (next < id) continue;
                if (plates[id] != plates[next])
                {
                    if (!_interactions.TryGetValue(Key(id, next), out int index))
                        throw new ArgumentException("C1 is missing a real inter-plate interaction.");
                    verifiedEdges++;
                    if (!Convergent(motion.Interactions[index])) continue;
                }
                var normal = TectonicMotionGenerator.BoundaryNormal(topology, id, next);
                double xx = (TensorXX[id] + TensorXX[next]) / 2, xy = (TensorXY[id] + TensorXY[next]) / 2,
                    yy = (TensorYY[id] + TensorYY[next]) / 2;
                double g = (xx * normal.X * normal.X + 2 * xy * normal.X * normal.Y + yy * normal.Y * normal.Y)
                    / Math.Sqrt(3 * Resistance[id] * Resistance[next]);
                if (!double.IsFinite(g) || g <= 0) throw new InvalidOperationException("Non-positive C1 conductance.");
                int reverse = topology.Neighbors(next).IndexOf(id);
                Conductances[id * 6 + slot] = Conductances[next * 6 + reverse] = g;
                _sum[id] += g; _sum[next] += g;
            }
        }
        if (verifiedEdges != _interactions.Count) throw new ArgumentException("Unexpected official contact count.");
    }

    // x^T A x = sum(x_i^2) + (budget/pi) * sum_unique_edges g_ij*(x_i-x_j)^2 > 0.
    public void Apply(double[] x, double[] output, int budget)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(output);
        if (budget <= 0 || x.Length != _topology.Count || output.Length != x.Length || ReferenceEquals(x, output))
            throw new ArgumentException("Separate C1 operator vectors and a positive budget are required.");
        double scale = budget / Math.PI;
        for (int id = 0; id < x.Length; id++)
        {
            double laplacian = 0;
            for (int k = 0; k < 6; k++)
            {
                int slot = id * 6 + k, next = _neighbors[slot];
                if (next >= 0) laplacian += Conductances[slot] * (x[id] - x[next]);
            }
            output[id] = x[id] + scale * laplacian;
        }
    }

    public (double[] Values, C1SolverReport Report) Solve(int originId, int anchor, int budget)
    {
        if ((uint)anchor >= (uint)_topology.Count || budget <= 0) throw new ArgumentOutOfRangeException(nameof(anchor));
        int n = _topology.Count; double scale = budget / Math.PI;
        var x = new double[n]; var residual = new double[n]; var z = new double[n]; var p = new double[n]; var ap = new double[n];
        residual[anchor] = 1;
        for (int id = 0; id < n; id++) p[id] = z[id] = residual[id] / (1 + scale * _sum[id]);
        double rz = Dot(residual, z); int iterations = 0; string reason = "iteration limit";
        while (iterations < C1Profile.SolverIterations)
        {
            Apply(p, ap, budget);
            double curvature = Dot(p, ap);
            if (!double.IsFinite(curvature) || curvature <= 0) { reason = "non-positive PCG curvature"; break; }
            double alpha = rz / curvature;
            for (int id = 0; id < n; id++) { x[id] += alpha * p[id]; residual[id] -= alpha * ap[id]; }
            iterations++;
            if (Math.Sqrt(Dot(residual, residual)) <= C1Profile.SolverTolerance) { reason = "converged"; break; }
            for (int id = 0; id < n; id++) z[id] = residual[id] / (1 + scale * _sum[id]);
            double nextRz = Dot(residual, z), beta = nextRz / rz;
            if (!double.IsFinite(beta)) { reason = "non-finite PCG update"; break; }
            for (int id = 0; id < n; id++) p[id] = z[id] + beta * p[id];
            rz = nextRz;
        }
        Apply(x, ap, budget); double error = 0;
        for (int id = 0; id < n; id++) { double r = (id == anchor ? 1 : 0) - ap[id]; error += r * r; }
        double actualResidual = Math.Sqrt(error), minimum = x.Min(), maximum = x.Max();
        bool converged = double.IsFinite(actualResidual) && actualResidual <= C1Profile.SolverTolerance &&
            double.IsFinite(minimum) && double.IsFinite(maximum) && minimum >= -1e-10 && maximum > 0;
        if (converged) reason = "converged";
        else if (reason == "converged") reason = "true residual or finite/nonnegative field check failed";
        return (x, new C1SolverReport(originId, converged, iterations, actualResidual, minimum, maximum, reason)
            { FieldHash = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(x.AsSpan()))) });
    }

    private static double Dot(double[] a, double[] b)
    { double result = 0; for (int id = 0; id < a.Length; id++) result += a[id] * b[id]; return result; }
    public static bool Convergent(PlateInteraction edge) => edge.Convergence >= .5 && edge.Convergence > edge.Divergence && edge.Convergence >= .4 * edge.Shear;
    public static ulong Key(int a, int b) => ((ulong)(uint)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
}
