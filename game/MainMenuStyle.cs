using OpenTK.Mathematics;

namespace Evolve.Game;

// Keeps the original blue palette independent of layout and rendering code.
internal static class MainMenuStyle
{
    public static readonly Color4 Background = new(0.035f, 0.075f, 0.16f, 1f);
    public static readonly Color4 BackgroundDetail = new(0.045f, 0.095f, 0.19f, 1f);
    public static readonly Color4 Border = new(0.36f, 0.68f, 0.91f, 1f);
    public static readonly Color4 Highlight = new(0.22f, 0.46f, 0.70f, 1f);
    public static readonly Color4 Button = new(0.12f, 0.28f, 0.48f, 1f);
    public static readonly Color4 Shade = new(0.065f, 0.15f, 0.29f, 1f);
    public static readonly Color4 Shadow = new(0.02f, 0.04f, 0.09f, 1f);
    public static readonly Color4 Text = Color4.White;
}
