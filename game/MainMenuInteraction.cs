namespace Evolve.Game;

// Tracks menu input and unavailable notices without depending on OpenGL or window APIs.
internal sealed class MainMenuInteraction
{
    public const int ButtonCount = 4;
    private MainMenuButton? pointerPress;
    private MainMenuButton? keyboardPress;
    private MainMenuActivationKey? activationKey;

    public MainMenuButton? HoveredButton { get; private set; }
    public MainMenuButton? SelectedButton { get; private set; }
    public string? FooterMessage { get; private set; }

    // Re-evaluate hover after resizing without stealing an existing keyboard selection.
    public void UpdateHover(MainMenuButton? button) => HoveredButton = button;

    // Actual pointer movement takes over highlighting and cancels a pending keyboard press.
    public void MovePointer(MainMenuButton? button)
    {
        HoveredButton = button;
        SelectedButton = null;
        keyboardPress = null;
        activationKey = null;
    }

    // Remember the button that owned the initial left press, including a press outside buttons.
    public void PressPointer(MainMenuButton? button)
    {
        MovePointer(button);
        pointerPress = button;
    }

    // A release activates only its original button; dragging to another button cancels activation.
    public MainMenuButton? ReleasePointer(MainMenuButton? button)
    {
        MainMenuButton? pressed = pointerPress;
        pointerPress = null;
        HoveredButton = button;
        return pressed is not null && pressed == button ? pressed : null;
    }

    // Wrap in both directions, beginning at the first or last button when none is selected.
    public void MoveSelection(int direction)
    {
        MainMenuButton? current = SelectedButton ?? HoveredButton;
        int index = current is { } button ? (int)button : direction > 0 ? -1 : 0;
        CancelPendingPress();
        SelectedButton = (MainMenuButton)((index + direction + ButtonCount) % ButtonCount);
    }

    // Ignore OS repeats and overlapping activation keys until the original key is released.
    public void PressSelection(MainMenuActivationKey key, bool isRepeat = false)
    {
        if (isRepeat || activationKey is not null) return;
        CancelPendingPress();
        activationKey = key;
        SelectedButton ??= HoveredButton ?? MainMenuButton.Start;
        keyboardPress = SelectedButton;
    }

    // Selection changes or cancellation invalidate the original keyboard press.
    public MainMenuButton? ReleaseSelection(MainMenuActivationKey key)
    {
        if (activationKey != key) return null;
        activationKey = null;
        MainMenuButton? pressed = keyboardPress;
        keyboardPress = null;
        return pressed is not null && pressed == SelectedButton ? pressed : null;
    }

    // Keyboard selection takes visual priority over a stationary pointer.
    public bool IsHighlighted(MainMenuButton button) => (SelectedButton ?? HoveredButton) == button;

    // Pointer dragging outside its original button removes the depressed appearance.
    public bool IsPressed(MainMenuButton button) =>
        pointerPress == button && HoveredButton == button ||
        keyboardPress == button && SelectedButton == button;

    // Preserve selection and the last notice while invalidating in-flight activation.
    public void CancelPendingPress()
    {
        pointerPress = null;
        keyboardPress = null;
        activationKey = null;
    }

    // Focus loss restores the approved idle appearance without discarding the last notice.
    public void ResetInput()
    {
        CancelPendingPress();
        HoveredButton = null;
        SelectedButton = null;
    }

    // Return an exit request to the window; unavailable features only update the footer.
    public bool Activate(MainMenuButton button)
    {
        if (button == MainMenuButton.Exit)
        {
            return true;
        }

        FooterMessage = button switch
        {
            MainMenuButton.Start => "INICIAR AÚN NO ESTÁ DISPONIBLE.",
            MainMenuButton.Laboratory => "LAB AÚN NO ESTÁ DISPONIBLE.",
            MainMenuButton.Options => "OPCIONES AÚN NO ESTÁ DISPONIBLE.",
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };
        return false;
    }
}
