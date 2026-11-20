// Defines framebuffer-space Laboratory controls and a letterboxed 2:1 viewing frame.
// Drawing and hit detection share these bounds; the grid itself is never stretched to fill the frame.
namespace Evolve.Game;

internal enum LaboratoryAction { Size, WorldType, Seed, Fit, Create, RunGrid, Back }
internal readonly record struct LaboratoryBounds(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y) => Width > 0 && Height > 0 && x >= X && y >= Y && x < X + Width && y < Y + Height;
}

internal sealed class LaboratoryLayout
{
    private readonly int width;
    public LaboratoryBounds Viewport { get; }

    public LaboratoryLayout(int width, int height)
    {
        this.width = Math.Max(1, width);
        double availableWidth = Math.Max(1, width - 24);
        double availableHeight = Math.Max(1, height - 238);
        double frameWidth = Math.Min(availableWidth, 2 * availableHeight);
        Viewport = new((width - frameWidth) / 2, 180 + (availableHeight - frameWidth / 2) / 2, frameWidth, frameWidth / 2);
    }

    public LaboratoryBounds Control(LaboratoryAction action) => action switch
    {
        LaboratoryAction.Size => new(12, 40, (width - 36) / 2d, 28),
        LaboratoryAction.WorldType => new(24 + (width - 36) / 2d, 40, (width - 36) / 2d, 28),
        LaboratoryAction.Seed => new(12, 80, (width - 36) * 0.65, 28),
        LaboratoryAction.Fit => new(24 + (width - 36) * 0.65, 80, (width - 36) * 0.35, 28),
        _ => new(12 + ((int)action - (int)LaboratoryAction.Create) * (width - 12) / 3d, 120, (width - 48) / 3d, 28)
    };

    public LaboratoryAction? HitTest(double x, double y)
    {
        foreach (var action in Enum.GetValues<LaboratoryAction>())
            if (Control(action).Contains(x, y)) return action;
        return null;
    }

    public static (double X, double Y) ToFramebuffer(double x, double y,
        int clientWidth, int clientHeight, int framebufferWidth, int framebufferHeight) =>
        clientWidth > 0 && clientHeight > 0
            ? (x * framebufferWidth / clientWidth, y * framebufferHeight / clientHeight) : (0, 0);
}
