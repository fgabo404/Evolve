using GEvolve.Engine.World.LandmassGeneration.Shared;
using GEvolve.Engine.World.Tectonics;

namespace GEvolve.Engine.World.LandmassGeneration;

/// <summary>
/// Gentle, deterministic tectonic guidance for coast formation. Plate borders
/// influence land growth, but do not become mandatory coastlines or continent masks.
/// Derived once per world attempt, never mutates the input plate IDs.
/// </summary>
internal sealed class PlateSurfaceInfluence
{
    private readonly byte[]? _plates;
    private readonly double[] _regionalPreference = new double[TectonicPlateGenerator.NumberOfPlates];
    private readonly ushort[]? _boundaryDistance;

    public bool Enabled => _plates is not null;

    public PlateSurfaceInfluence(HexTopology topology, ReadOnlySpan<byte> plates, int seed)
    {
        if (plates.IsEmpty) return;
        if (plates.Length != topology.Count)
            throw new ArgumentException("Plate array must match the hex topology.", nameof(plates));
        for (int id = 0; id < plates.Length; id++)
            if (plates[id] >= TectonicPlateGenerator.NumberOfPlates)
                throw new ArgumentException("Invalid tectonic plate ID.", nameof(plates));

        _plates = plates.ToArray();
        var random = new DeterministicRandom(seed, 0x434F4153);
        for (int i = 0; i < _regionalPreference.Length; i++)
            _regionalPreference[i] = random.NextDouble() * 2 - 1;

        _boundaryDistance = new ushort[topology.Count];
        Array.Fill(_boundaryDistance, ushort.MaxValue);
        var queue = new Queue<int>();
        for (int id = 0; id < topology.Count; id++)
        {
            foreach (int neighbor in topology.Neighbors(id))
                if (_plates[id] != _plates[neighbor])
                {
                    _boundaryDistance[id] = 0;
                    queue.Enqueue(id);
                    break;
                }
        }

        while (queue.TryDequeue(out int cell))
        foreach (int neighbor in topology.Neighbors(cell))
            if (_boundaryDistance[neighbor] == ushort.MaxValue)
            {
                _boundaryDistance[neighbor] = checked((ushort)(_boundaryDistance[cell] + 1));
                queue.Enqueue(neighbor);
            }
    }

    public int DistanceToBoundary(int cell) =>
        _boundaryDistance is null ? int.MaxValue : _boundaryDistance[cell];

    public double ExpansionCost(int from, int to)
    {
        if (_plates is null) return 0;
        double crossing = _plates[from] != _plates[to] ? .25 : -.055;
        // Slightly more or less continental-friendly plate interiors.
        return crossing + .11 * _regionalPreference[_plates[to]];
    }

    public double RadiusFactor(int center, int cell)
    {
        if (_plates is null) return 1;
        // Keep the original minimum radius, so the small-continent type
        // does not become unexpectedly underpopulated at high resolutions.
        double regional = .09 * (_regionalPreference[_plates[cell]] + 1) / 2;
        double crossing = _plates[center] == _plates[cell] ? 0 : .04;
        return 1 + Math.Max(0, regional - crossing);
    }

    public double IslandFactor(int center)
    {
        if (_boundaryDistance is null) return 1;
        // Prefer some elongated tectonic island arcs; still allow hotspot islands.
        return 1 + .15 * Math.Exp(-_boundaryDistance[center] / 3.0);
    }
}
