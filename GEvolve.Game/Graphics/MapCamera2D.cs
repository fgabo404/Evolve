using System.Numerics;
using GEvolve.Engine.Grid;

namespace GEvolve.Game.Graphics;

public readonly record struct VisibleHexRange(int FirstColumn, int LastColumn, int FirstRow, int LastRow)
{
    public int Count => Math.Max(0, LastColumn - FirstColumn + 1) *
        Math.Max(0, LastRow - FirstRow + 1);
}

/// <summary>Cámara en unidades de Engine. X es periódica; Y conserva límites abiertos.</summary>
public sealed class MapCamera2D
{
    public const int TopBarHeight = 58;
    public float X { get; private set; }
    public float Y { get; private set; }
    public float Scale { get; private set; } = 1;
    public float FitScale { get; private set; } = 1;
    public int ViewWidth { get; private set; } = 1;
    public int ViewHeight { get; private set; } = 1;
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int Version { get; private set; }

    public void Fit(int columns, int rows, int width, int height)
    {
        Columns = columns;
        Rows = rows;
        ViewWidth = Math.Max(width, 1);
        ViewHeight = Math.Max(height - TopBarHeight, 1);
        float period = FlatHexGeometry.PeriodWidth(columns);
        float mapHeight = FlatHexGeometry.MapHeight(rows);
        FitScale = .94f * MathF.Min(ViewWidth / period, ViewHeight / mapHeight);
        Scale = MathF.Max(FitScale, 4f);
        X = (period - ViewWidth / Scale) / 2;
        Y = (mapHeight - ViewHeight / Scale) / 2;
        Normalize();
        Version++;
    }

    public Vector2 ScreenToWorld(Vector2 screen)
        => new(X + screen.X / Scale, Y + (screen.Y - TopBarHeight) / Scale);

    public Vector2 WorldToScreen(Vector2 world)
        => new((world.X - X) * Scale, TopBarHeight + (world.Y - Y) * Scale);

    public VisibleHexRange VisibleRange()
    {
        float right = X + ViewWidth / Scale;
        float bottom = Y + ViewHeight / Scale;
        return new VisibleHexRange(
            (int)MathF.Floor((X - 2f) / 1.5f),
            (int)MathF.Ceiling((right + 1f) / 1.5f),
            Math.Max(0, (int)MathF.Floor(Y / 1.7320508f) - 2),
            Math.Min(Rows - 1, (int)MathF.Ceiling(bottom / 1.7320508f) + 1));
    }

    public void Pan(float deltaX, float deltaY)
    {
        X -= deltaX / Scale;
        Y -= deltaY / Scale;
        Normalize();
        Version++;
    }

    public void Zoom(float wheelDelta, Vector2 screen)
    {
        if (wheelDelta == 0) return;
        Vector2 anchor = ScreenToWorld(screen);
        float factor = MathF.Pow(1.16f, wheelDelta);
        float next = Math.Clamp(Scale * factor, FitScale * .75f,
            MathF.Min(FitScale * 40f, 35f));
        if (MathF.Abs(next - Scale) < .00001f) return;
        Scale = next;
        X = anchor.X - screen.X / Scale;
        Y = anchor.Y - (screen.Y - TopBarHeight) / Scale;
        Normalize();
        Version++;
    }

    private void Normalize()
    {
        float period = FlatHexGeometry.PeriodWidth(Columns);
        X = ((X % period) + period) % period;
        float mapHeight = FlatHexGeometry.MapHeight(Rows);
        float visibleHeight = ViewHeight / Scale;
        Y = visibleHeight >= mapHeight
            ? (mapHeight - visibleHeight) / 2
            : Math.Clamp(Y, -1f, mapHeight - visibleHeight + 1f);
    }
}
