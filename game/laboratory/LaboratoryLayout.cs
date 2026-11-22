// Defines the approved responsive Laboratory sidebar, popup, stage-card and map bounds.
// Scrolling keeps every sidebar control reachable at short heights; drawing and hit detection use the same clipped coordinates.
using Evolve.Engine.WorldGeneration;

namespace Evolve.Game;

internal enum LaboratoryAction { Size, WorldType, Seed, Randomize, View, Fit, Create, Configure, Back, Stage, Option }
internal readonly record struct LaboratoryHit(LaboratoryAction Action, int Index = -1);
internal readonly record struct LaboratoryBounds(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y) => Width > 0 && Height > 0 && x >= X && y >= Y && x < X + Width && y < Y + Height;
}

internal sealed class LaboratoryLayout
{
    public static readonly LaboratoryAction[] Controls =
        [LaboratoryAction.Size, LaboratoryAction.WorldType, LaboratoryAction.Seed, LaboratoryAction.Randomize,
         LaboratoryAction.View, LaboratoryAction.Fit, LaboratoryAction.Create, LaboratoryAction.Configure, LaboratoryAction.Back];
    public double Scale { get; }
    public double SidebarWidth { get; }
    public double Scroll { get; }
    public double MaximumScroll { get; }
    public LaboratoryBounds SidebarClip { get; }
    public LaboratoryBounds Viewport { get; }
    public double InformationHeight { get; }

    public LaboratoryLayout(int width, int height, double scroll = 0, double scale = 1, bool tectonicInformation = false)
    {
        Scale = Math.Clamp(scale, 1, 3);
        SidebarWidth = Math.Min(width - 80 * Scale, Math.Clamp(width / Scale * 0.23, 200, 280) * Scale);
        SidebarWidth = Math.Max(1, SidebarWidth);
        SidebarClip = new(8 * Scale, 8 * Scale, SidebarWidth - 16 * Scale, Math.Max(1, height - 56 * Scale));
        InformationHeight = tectonicInformation ? 166 : 106;
        MaximumScroll = Math.Max(0, (620 + InformationHeight) * Scale - SidebarClip.Height);
        Scroll = Math.Clamp(scroll, 0, MaximumScroll);
        Viewport = new(SidebarWidth + 12 * Scale, 12 * Scale,
            Math.Max(1, width - SidebarWidth - 24 * Scale), Math.Max(1, height - 24 * Scale));
    }

    // Logical sidebar coordinates are translated uniformly for DPI and scroll; footer navigation stays fixed.
    public LaboratoryBounds At(double x, double y, double width, double height) =>
        new(SidebarClip.X + x * Scale, SidebarClip.Y + y * Scale - Scroll, width * Scale, height * Scale);
    public LaboratoryBounds Card(double y, double height) => At(0, y, SidebarClip.Width / Scale, height);

    public LaboratoryBounds Control(LaboratoryAction action) => action switch
    {
        LaboratoryAction.WorldType => Card(54, 26),
        LaboratoryAction.Size => Card(104, 26),
        LaboratoryAction.Seed => At(0, 154, SidebarClip.Width / Scale - 34, 26),
        LaboratoryAction.Randomize => At(SidebarClip.Width / Scale - 28, 154, 28, 26),
        LaboratoryAction.View => Card(204, 26),
        LaboratoryAction.Create => Card(244, 28),
        LaboratoryAction.Configure => At(0, 280, SidebarClip.Width / Scale * 0.67 - 3, 24),
        LaboratoryAction.Fit => At(SidebarClip.Width / Scale * 0.67 + 3, 280, SidebarClip.Width / Scale * 0.33 - 3, 24),
        LaboratoryAction.Back => new(SidebarClip.X, SidebarClip.Y + SidebarClip.Height + 8 * Scale, SidebarClip.Width, 28 * Scale),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public LaboratoryBounds Stage(GenerationStage stage) => Card(380 + (int)stage * 27, 24);

    public LaboratoryBounds DropdownOption(LaboratoryAction action, int index, int count)
    {
        var control = Control(action);
        double popupHeight = count * 26 * Scale;
        double y = control.Y + control.Height + 3 * Scale;
        if (y + popupHeight > SidebarClip.Y + SidebarClip.Height)
            y = control.Y - popupHeight - 3 * Scale;
        y = Math.Clamp(y, SidebarClip.Y, Math.Max(SidebarClip.Y, SidebarClip.Y + SidebarClip.Height - popupHeight));
        return new(control.X, y + index * 26 * Scale, control.Width, 26 * Scale);
    }

    public LaboratoryHit? HitTest(double x, double y, LaboratoryAction? dropdown = null, int optionCount = 0)
    {
        if (Control(LaboratoryAction.Back).Contains(x, y)) return new(LaboratoryAction.Back);
        if (!SidebarClip.Contains(x, y)) return null;
        if (dropdown is { } popup)
        {
            for (int index = 0; index < optionCount; index++)
                if (DropdownOption(popup, index, optionCount).Contains(x, y)) return new(LaboratoryAction.Option, index);
            return null;
        }
        foreach (var action in Controls)
            if (action != LaboratoryAction.Back && Control(action).Contains(x, y)) return new(action);
        foreach (var stage in Enum.GetValues<GenerationStage>())
            if (Stage(stage).Contains(x, y)) return new(LaboratoryAction.Stage, (int)stage);
        return null;
    }

    public static (double X, double Y) ToFramebuffer(double x, double y,
        int clientWidth, int clientHeight, int framebufferWidth, int framebufferHeight) =>
        clientWidth > 0 && clientHeight > 0
            ? (x * framebufferWidth / clientWidth, y * framebufferHeight / clientHeight) : (0, 0);
}
