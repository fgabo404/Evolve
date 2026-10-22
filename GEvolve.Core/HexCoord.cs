
namespace GEvolve.Core;

public readonly record struct HexCoord(int Q, int R)
{
    // Tercera coordenada del sistema cúbico.
    public int S => -Q - R;

    // Las seis direcciones del hexágono.
    private static readonly HexCoord[] Direcciones =
    [
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    ];

    public HexCoord Sumar(HexCoord otra)
        => new(Q + otra.Q, R + otra.R);

    public HexCoord Vecino(int direccion)
    {
        if (direccion < 0 || direccion >= 6)
            throw new ArgumentOutOfRangeException(nameof(direccion));

        return Sumar(Direcciones[direccion]);
    }

    public int Distancia(HexCoord otra)
    {
        int dq = Math.Abs(Q - otra.Q);
        int dr = Math.Abs(R - otra.R);
        int ds = Math.Abs(S - otra.S);

        return (dq + dr + ds) / 2;
    }
}
