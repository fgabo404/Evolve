// Defines the approved blue-gray main-menu palette and interaction colors.
// Supplies presentation styling separately from layout and input behavior.

using OpenTK.Mathematics;

namespace Evolve.Game;

internal static class MainMenuStyle
{
    public static readonly Color4 Background = new(0.055f, 0.10f, 0.16f, 1f);
    public static readonly Color4 BackgroundBand = new(0.075f, 0.16f, 0.21f, 1f);
    public static readonly Color4 BackgroundDivider = new(0.24f, 0.42f, 0.48f, 1f);
    public static readonly Color4 Border = new(0.37f, 0.51f, 0.59f, 1f);
    public static readonly Color4 Button = new(0.16f, 0.24f, 0.31f, 1f);
    public static readonly Color4 HighlightedButton = new(0.20f, 0.29f, 0.37f, 1f);
    public static readonly Color4 HighlightedBorder = new(0.44f, 0.59f, 0.67f, 1f);
    public static readonly Color4 PressedButton = new(0.12f, 0.19f, 0.25f, 1f);
    public static readonly Color4 Shadow = new(0.02f, 0.05f, 0.09f, 0.7f);
    public static readonly Color4 Text = Color4.White;
    public static readonly Color4 VersionText = new(0.65f, 0.76f, 0.83f, 1f);
}
