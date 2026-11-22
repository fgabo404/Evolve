// Tracks transient sidebar editing, dropdowns and scrolling independently of native OpenTK events.
// Owns no world configuration or generation progress; shared layout supplies all hit bounds.
using Evolve.Definitions.WorldGeneration;

namespace Evolve.Game;

internal sealed class LaboratoryUiState
{
    public bool EditingSeed { get; set; }
    public LaboratoryAction? Dropdown { get; private set; }
    public double Scroll { get; private set; }
    public int HighlightedOption { get; private set; }
    public int OptionCount => Dropdown switch
    {
        LaboratoryAction.Size => Enum.GetValues<MapSize>().Length,
        LaboratoryAction.WorldType => Enum.GetValues<WorldType>().Length,
        LaboratoryAction.View => 2,
        _ => 0
    };

    public void Toggle(LaboratoryAction action)
    {
        if (action is not (LaboratoryAction.Size or LaboratoryAction.WorldType or LaboratoryAction.View))
            throw new ArgumentOutOfRangeException(nameof(action));
        Dropdown = Dropdown == action ? null : action;
        HighlightedOption = 0;
        EditingSeed = false;
    }

    public void CloseTransient() { Dropdown = null; EditingSeed = false; HighlightedOption = 0; }
    public void Reset() { CloseTransient(); Scroll = 0; }
    public void MoveOption(int direction)
    {
        if (OptionCount > 0) HighlightedOption = (HighlightedOption + direction % OptionCount + OptionCount) % OptionCount;
    }
    public void HoverOption(int index)
    {
        if (index >= 0 && index < OptionCount) HighlightedOption = index;
    }
    public void ScrollBy(double pixels, LaboratoryLayout layout)
    {
        Scroll = Math.Clamp(layout.Scroll + pixels, 0, layout.MaximumScroll);
        CloseTransient();
    }

    public LaboratoryHit? HitTest(LaboratoryLayout layout, double x, double y) =>
        layout.HitTest(x, y, Dropdown, OptionCount);
}
