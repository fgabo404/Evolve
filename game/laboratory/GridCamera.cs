// Converts between grid presentation coordinates and framebuffer pixels using one uniform scale.
// Keeps zoom anchored at the cursor and preserves user navigation through viewport resizing.
namespace Evolve.Game;

internal sealed class GridCamera
{
    private LaboratoryBounds bounds;
    private bool fitted = true;
    public LaboratoryBounds Viewport { get; private set; }
    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double Scale { get; private set; } = 1;

    public void SetBounds(LaboratoryBounds value) { bounds = value; Fit(); }
    public void SetViewport(LaboratoryBounds value)
    {
        if (Viewport == value) return;
        Viewport = value;
        if (fitted) Fit();
    }

    public void Fit()
    {
        CenterX = bounds.X + bounds.Width / 2;
        CenterY = bounds.Y + bounds.Height / 2;
        if (bounds.Width > 0 && bounds.Height > 0 && Viewport.Width > 0 && Viewport.Height > 0)
            Scale = 0.95 * Math.Min(Viewport.Width / bounds.Width, Viewport.Height / bounds.Height);
        fitted = true;
    }

    public (double X, double Y) ToScreen(double x, double y) =>
        (Viewport.X + Viewport.Width / 2 + (x - CenterX) * Scale,
         Viewport.Y + Viewport.Height / 2 + (y - CenterY) * Scale);
    public (double X, double Y) ToWorld(double x, double y) =>
        ((x - Viewport.X - Viewport.Width / 2) / Scale + CenterX,
         (y - Viewport.Y - Viewport.Height / 2) / Scale + CenterY);

    public void Pan(double dx, double dy)
    {
        CenterX -= dx / Scale;
        CenterY -= dy / Scale;
        fitted = false;
    }

    public void Zoom(double wheel, double x, double y)
    {
        var before = ToWorld(x, y);
        Scale = Math.Clamp(Scale * Math.Pow(1.2, Math.Clamp(wheel, -10, 10)), 0.01, 500);
        var after = ToWorld(x, y);
        CenterX += before.X - after.X;
        CenterY += before.Y - after.Y;
        fitted = false;
    }
}
