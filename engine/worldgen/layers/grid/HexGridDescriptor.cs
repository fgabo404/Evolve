// Describes the approved grid configuration as one unmanaged value for WG-CORE publication.
// Reconstructs immutable HexGridData through its accepted preset mapping without retaining managed references.
using Evolve.Definitions.WorldGeneration;

namespace Evolve.Engine.WorldGeneration.Layers.Grid;

public readonly record struct HexGridDescriptor
{
    public MapSize Size { get; }

    // Construct the approved grid to validate the selection; only its size selector needs to be published.
    public HexGridDescriptor(MapSize size)
    {
        _ = new HexGridData(size);
        Size = size;
    }

    // Dimensions and calculated queries come from the same authoritative mapping used during generation.
    public HexGridData ToGridData() => new(Size);
}
