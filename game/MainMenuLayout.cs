namespace Evolve.Game;

// Places a fixed logical menu on whole framebuffer pixels for sharp scaling.
internal readonly record struct MainMenuLayout(int Left, int Top, int Scale)
{
    public const int Width = 320;
    public const int Height = 244;
    public const int ButtonWidth = 240;
    public const int ButtonHeight = 28;
    public const int ButtonTop = 100;
    public const int ButtonSpacing = 36;

    // Leave margins and center again whenever the actual framebuffer size changes.
    public static MainMenuLayout Create(int width, int height)
    {
        int scale = Math.Max(1, Math.Min(width / 400, height / 300));
        return new((width - Width * scale) / 2, (height - Height * scale) / 2, scale);
    }
}
