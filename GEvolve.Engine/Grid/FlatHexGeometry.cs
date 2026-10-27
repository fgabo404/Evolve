using System.Numerics;

namespace GEvolve.Engine.Grid;

public readonly record struct GridAddress(int Column, int Row);

/// <summary>Geometría flat-top de columnas impares desplazadas hacia abajo.</summary>
public static class FlatHexGeometry
{
    private const float Root3 = 1.7320508075688772f;

    public static HexCoord ToAxial(GridAddress address)
        => new(address.Column, address.Row - FloorHalf(address.Column));

    public static GridAddress ToOffset(HexCoord axial)
        => new(axial.Q, axial.R + FloorHalf(axial.Q));

    public static Vector2 Center(GridAddress address, float radius = 1f)
    {
        ValidateRadius(radius);
        return new Vector2(radius * (1f + 1.5f * address.Column),
            Root3 * radius * (address.Row + .5f * (address.Column & 1) + .5f));
    }

    /// <summary>Coordenada axial que contiene un punto del plano periódico.</summary>
    public static HexCoord AxialAt(Vector2 point, float radius = 1f)
    {
        ValidateRadius(radius);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            throw new ArgumentOutOfRangeException(nameof(point));

        float q = (point.X / radius - 1f) / 1.5f;
        float r = (point.Y / (Root3 * radius) - .5f) - q / 2f;
        float s = -q - r;
        int rq = checked((int)MathF.Floor(q + .5f));
        int rr = checked((int)MathF.Floor(r + .5f));
        int rs = checked((int)MathF.Floor(s + .5f));
        float dq = MathF.Abs(rq - q), dr = MathF.Abs(rr - r), ds = MathF.Abs(rs - s);
        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;
        return new HexCoord(rq, rr);
    }

    /// <summary>Identifica una celda existente incluso en copias visuales del cilindro.</summary>
    public static GridAddress? CellAt(Vector2 point, int columns, int rows, float radius = 1f)
    {
        if (columns <= 0 || (columns & 1) != 0 || rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        GridAddress raw = ToOffset(AxialAt(point, radius));
        if (raw.Row < 0 || raw.Row >= rows) return null;
        return new GridAddress(Mod(raw.Column, columns), raw.Row);
    }

    /// <summary>Traslada la coordenada axial al periodo canónico, conservando fila.</summary>
    public static HexCoord Wrap(HexCoord axial, int columns)
    {
        if (columns <= 0 || (columns & 1) != 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        int q = Mod(axial.Q, columns);
        int periods = (axial.Q - q) / columns;
        return new HexCoord(q, axial.R + periods * (columns / 2));
    }

    public static int WrappedDistance(HexCoord from, HexCoord to, int columns)
    {
        if (columns <= 0 || (columns & 1) != 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        int half = columns / 2;
        return Math.Min(from.Distancia(to), Math.Min(
            from.Distancia(new HexCoord(to.Q + columns, to.R - half)),
            from.Distancia(new HexCoord(to.Q - columns, to.R + half))));
    }

    public static float PeriodWidth(int columns, float radius = 1f)
    {
        ValidateRadius(radius);
        return 1.5f * columns * radius;
    }

    public static float MapHeight(int rows, float radius = 1f)
    {
        ValidateRadius(radius);
        return Root3 * (rows + .5f) * radius;
    }

    private static int FloorHalf(int value) => (value - (value & 1)) / 2;
    private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;
    private static void ValidateRadius(float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));
    }
}
