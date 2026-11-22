// Extracts immutable per-cell channel maxima exclusively from stored M boundary projections.
// Keeps zero-amplitude boundary membership separate from sources for distance calculations.
using Evolve.Engine.WorldGeneration.Layers.Grid;

namespace Evolve.Engine.WorldGeneration.Layers.Tectonics;

internal readonly record struct TectonicInfluenceChannels(float Compression, float Extension, float Shear);
internal readonly record struct TectonicInfluenceSource(int CellId, TectonicInfluenceChannels Channels);

internal sealed class TectonicInfluenceSources
{
    private readonly TectonicInfluenceChannels[] channels;
    private readonly bool[] boundary;
    internal HexGridData Grid { get; }
    internal int ActiveCount { get; }
    internal TectonicInfluenceChannels Maximum { get; }
    internal TectonicInfluenceChannels this[int cell] => channels[cell];
    internal bool IsBoundary(int cell) => boundary[cell];

    private TectonicInfluenceSources(HexGridData grid, IEnumerable<TectonicInfluenceSource> samples, IEnumerable<int> boundaryCells)
    {
        Grid = grid;
        channels = new TectonicInfluenceChannels[grid.CellCount];
        boundary = new bool[grid.CellCount];
        foreach (int cell in boundaryCells) { grid.GetOffset(cell); boundary[cell] = true; }
        foreach (var sample in samples)
        {
            grid.GetOffset(sample.CellId);
            Validate(sample.Channels);
            var old = channels[sample.CellId];
            channels[sample.CellId] = Max(old, sample.Channels);
        }
        var maximum = new TectonicInfluenceChannels();
        foreach (var value in channels)
        {
            if (value.Compression > 0 || value.Extension > 0 || value.Shear > 0) ActiveCount++;
            maximum = Max(maximum, value);
        }
        Maximum = maximum;
    }

    internal static TectonicInfluenceSources Extract(HexGridData grid, TectonicData data)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(data);
        if (!data.HasBoundaries || data.CellCount != grid.CellCount || new HexGridData(data.Size).Columns != grid.Columns)
            throw new ArgumentException("Influence requires completed M boundaries matching Grid.");
        return FromBoundaries(grid, Enumerable.Range(0, data.BoundaryCount).Select(data.GetBoundary));
    }

    // This internal fixture/build entry also permits independent sparse-source experiments.
    internal static TectonicInfluenceSources Create(HexGridData grid, IEnumerable<TectonicInfluenceSource> samples, IEnumerable<int> boundaryCells)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(boundaryCells);
        return new(grid, samples, boundaryCells);
    }

    internal static TectonicInfluenceSources FromBoundaries(HexGridData grid, IEnumerable<TectonicBoundary> boundaries)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(boundaries);
        // Materialize only boundary records once; never allocate a world array per edge/source.
        var edges = boundaries.ToArray();
        foreach (var edge in edges)
        {
            if (edge.FirstCellId >= edge.SecondCellId || grid.GetDistance(edge.FirstCellId, edge.SecondCellId) != 1 ||
                !float.IsFinite(edge.SignedNormal) || !float.IsFinite(edge.SignedTangent))
                throw new ArgumentException("Expected finite canonical neighboring boundary records.");
        }
        return new(grid, Samples(), BoundaryCells());

        IEnumerable<int> BoundaryCells()
        {
            foreach (var edge in edges) { yield return edge.FirstCellId; yield return edge.SecondCellId; }
        }

        IEnumerable<TectonicInfluenceSource> Samples()
        {
            foreach (var edge in edges)
            {
                var value = new TectonicInfluenceChannels(edge.Convergence, edge.Divergence, edge.Shear);
                yield return new(edge.FirstCellId, value);
                yield return new(edge.SecondCellId, value);
            }
        }
    }

    internal static void Validate(TectonicInfluenceChannels value)
    {
        if (!float.IsFinite(value.Compression) || value.Compression < 0 ||
            !float.IsFinite(value.Extension) || value.Extension < 0 || !float.IsFinite(value.Shear) || value.Shear < 0)
            throw new ArgumentException("Influence amplitudes must be finite and nonnegative.");
    }

    private static TectonicInfluenceChannels Max(TectonicInfluenceChannels a, TectonicInfluenceChannels b) =>
        new(Math.Max(0, Math.Max(a.Compression, b.Compression)), Math.Max(0, Math.Max(a.Extension, b.Extension)), Math.Max(0, Math.Max(a.Shear, b.Shear)));
}
