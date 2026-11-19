// Defines immutable offset and axial coordinates used exclusively by the WG-00 geometry.
// These value types contain no terrain state and do not depend on rendering.
namespace Evolve.Engine.WorldGeneration.Layers.Grid.Geometry;

// Column and row are the canonical grid address.
public readonly record struct HexOffset(int Column, int Row);

// Axial coordinates are a calculated representation of the same hexagonal lattice.
public readonly record struct HexAxial(int Q, int R)
{
    // Cubic coordinate S is derived, never stored.
    public long S => -(long)Q - R;

    public HexAxial Move(HexDirection direction)
    {
        return direction switch
        {
            HexDirection.North => new HexAxial(Q, checked(R - 1)),
            HexDirection.NorthEast => new HexAxial(checked(Q + 1), checked(R - 1)),
            HexDirection.SouthEast => new HexAxial(checked(Q + 1), R),
            HexDirection.South => new HexAxial(Q, checked(R + 1)),
            HexDirection.SouthWest => new HexAxial(checked(Q - 1), checked(R + 1)),
            HexDirection.NorthWest => new HexAxial(checked(Q - 1), R),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    // The L-infinity cube metric is equivalent to half the L1 cube metric.
    public int DistanceTo(HexAxial other)
    {
        long dq = (long)other.Q - Q;
        long dr = (long)other.R - R;
        return checked((int)Math.Max(Math.Abs(dq), Math.Max(Math.Abs(dr), Math.Abs(dq + dr))));
    }
}

// Direction values are fixed in clockwise order and have explicit opposites.
public enum HexDirection
{
    North = 0,
    NorthEast = 1,
    SouthEast = 2,
    South = 3,
    SouthWest = 4,
    NorthWest = 5
}

// Abstract cylindrical geographic references, not spherical metric coordinates.
public readonly record struct HexGeographicPosition(double Longitude, double Latitude);
