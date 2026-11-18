using Evolve.Game;

namespace Evolve.Menu.Checks;

// Uses the actual linked production sources; failures print diagnostics and return a nonzero exit code.
internal static class MenuInteractionChecks
{
    // Keep a small named suite instead of adding a test framework dependency.
    public static int Run()
    {
        (string Name, Action Check)[] checks =
        [
            ("Initial menu remains idle", InitialState),
            ("Matching pointer press and release activate each button once", MatchingPointerRelease),
            ("Different buttons, gaps and unpaired releases never activate", InvalidPointerRelease),
            ("Pointer drag feedback follows the original button", PointerDragFeedback),
            ("Down navigation wraps through all four buttons", DownNavigation),
            ("Up navigation wraps through all four buttons", UpNavigation),
            ("Keyboard selection takes precedence over stationary hover", KeyboardSelectionPriority),
            ("Enter and Space activate each selected button on matching release", ActivationKeys),
            ("Activation defaults to the hovered button or Start", DefaultActivation),
            ("Repeated and overlapping activation keys do not duplicate activation", RepeatedActivation),
            ("Cancellation invalidates mouse and keyboard presses", PendingCancellation),
            ("Focus reset clears input but preserves the last notice", FocusReset),
            ("Switching input source invalidates the old press", SwitchingInputSource),
            ("Unavailable features update readable notices without requesting exit", UnavailableMessages),
            ("Exit is requested only after valid activation", ExitRequest),
            ("Hit detection matches button bounds across framebuffer sizes", LayoutHitBounds)
        ];

        int failures = 0;
        foreach (var (name, check) in checks)
        {
            try
            {
                check();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{checks.Length - failures}/{checks.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    // No implicit selection or notice may change the approved idle rendering.
    private static void InitialState()
    {
        var menu = new MainMenuInteraction();
        Require(menu.HoveredButton is null && menu.SelectedButton is null && menu.FooterMessage is null, "Initial state is not empty.");
        foreach (var button in Enum.GetValues<MainMenuButton>())
            Require(!menu.IsHighlighted(button) && !menu.IsPressed(button), $"Unexpected initial feedback for {button}.");
    }

    // Exercise all actions rather than relying on a single representative index.
    private static void MatchingPointerRelease()
    {
        foreach (var button in Enum.GetValues<MainMenuButton>())
        {
            var menu = new MainMenuInteraction();
            menu.PressPointer(button);
            Require(menu.IsPressed(button) && menu.IsHighlighted(button), $"Missing press feedback for {button}.");
            Require(menu.ReleasePointer(button) == button, $"Matching release failed for {button}.");
            Require(!menu.IsPressed(button) && menu.ReleasePointer(button) is null, "A release was reused.");
        }
    }

    // Cover cross-button drags, release outside, and presses that began outside a button.
    private static void InvalidPointerRelease()
    {
        foreach (var origin in Enum.GetValues<MainMenuButton>())
        foreach (var target in Enum.GetValues<MainMenuButton>())
        {
            if (origin == target) continue;
            var menu = new MainMenuInteraction();
            menu.PressPointer(origin);
            Require(menu.ReleasePointer(target) is null, $"Drag from {origin} to {target} activated.");
            Require(menu.ReleasePointer(origin) is null, "Invalid release left a pending press.");
        }

        var outside = new MainMenuInteraction();
        outside.PressPointer(MainMenuButton.Start);
        Require(outside.ReleasePointer(null) is null, "Outside release activated.");
        outside.PressPointer(null);
        Require(outside.ReleasePointer(MainMenuButton.Exit) is null, "Press outside activated Exit.");
    }

    // Leaving a button removes pressed feedback; returning to the same button preserves ownership.
    private static void PointerDragFeedback()
    {
        var menu = new MainMenuInteraction();
        menu.PressPointer(MainMenuButton.Start);
        menu.MovePointer(MainMenuButton.Options);
        Require(!menu.IsPressed(MainMenuButton.Start) && !menu.IsPressed(MainMenuButton.Options), "Drag pressed the wrong button.");
        menu.MovePointer(MainMenuButton.Start);
        Require(menu.IsPressed(MainMenuButton.Start), "Original press ownership was lost.");
        Require(menu.ReleasePointer(MainMenuButton.Start) == MainMenuButton.Start, "Returning release did not activate its owner.");
    }

    // Check two full cycles, including the initial and wrap boundaries.
    private static void DownNavigation()
    {
        var menu = new MainMenuInteraction();
        MainMenuButton[] expected = [MainMenuButton.Start, MainMenuButton.Laboratory, MainMenuButton.Options, MainMenuButton.Exit];
        for (int step = 0; step < expected.Length * 2; step++)
        {
            menu.MoveSelection(1);
            Require(menu.SelectedButton == expected[step % expected.Length], $"Incorrect Down selection at step {step}.");
        }
    }

    // The first Up selects Exit rather than skipping the last button.
    private static void UpNavigation()
    {
        var menu = new MainMenuInteraction();
        MainMenuButton[] expected = [MainMenuButton.Exit, MainMenuButton.Options, MainMenuButton.Laboratory, MainMenuButton.Start];
        for (int step = 0; step < expected.Length * 2; step++)
        {
            menu.MoveSelection(-1);
            Require(menu.SelectedButton == expected[step % expected.Length], $"Incorrect Up selection at step {step}.");
        }
    }

    // Render-time hover refresh must not steal a keyboard user's selection.
    private static void KeyboardSelectionPriority()
    {
        var menu = new MainMenuInteraction();
        menu.MovePointer(MainMenuButton.Laboratory);
        menu.MoveSelection(1);
        menu.UpdateHover(MainMenuButton.Exit);
        Require(menu.IsHighlighted(MainMenuButton.Options) && !menu.IsHighlighted(MainMenuButton.Exit), "Hover stole keyboard selection.");
    }

    // Both physical key identities use production gating, including wrong-key and duplicate releases.
    private static void ActivationKeys()
    {
        foreach (var key in Enum.GetValues<MainMenuActivationKey>())
        foreach (var button in Enum.GetValues<MainMenuButton>())
        {
            var menu = new MainMenuInteraction();
            menu.MovePointer(button);
            menu.PressSelection(key);
            Require(menu.IsPressed(button), $"Missing {key} pressed feedback for {button}.");
            var other = key == MainMenuActivationKey.Enter ? MainMenuActivationKey.Space : MainMenuActivationKey.Enter;
            Require(menu.ReleaseSelection(other) is null && menu.IsPressed(button), "Wrong key completed or cleared a press.");
            Require(menu.ReleaseSelection(key) == button, $"Matching {key} release failed for {button}.");
            Require(menu.ReleaseSelection(key) is null && !menu.IsPressed(button), "Duplicate key release activated.");
        }
    }

    // Preserve UI-03's default selection rules for keyboard activation.
    private static void DefaultActivation()
    {
        var menu = new MainMenuInteraction();
        menu.PressSelection(MainMenuActivationKey.Enter);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Enter) == MainMenuButton.Start, "Default Enter did not select Start.");
        menu.ResetInput();
        menu.MovePointer(MainMenuButton.Laboratory);
        menu.PressSelection(MainMenuActivationKey.Space);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Space) == MainMenuButton.Laboratory, "Space ignored the hovered button.");
    }

    // OS repeat cannot arm a new activation after cancellation or replace a held key.
    private static void RepeatedActivation()
    {
        var menu = new MainMenuInteraction();
        menu.PressSelection(MainMenuActivationKey.Enter, isRepeat: true);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Enter) is null, "Repeat armed an activation.");
        menu.PressSelection(MainMenuActivationKey.Enter);
        menu.PressSelection(MainMenuActivationKey.Enter, isRepeat: true);
        menu.PressSelection(MainMenuActivationKey.Space);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Space) is null, "Overlapping key replaced the original.");
        Require(menu.ReleaseSelection(MainMenuActivationKey.Enter) == MainMenuButton.Start, "Original key was lost.");
        menu.PressSelection(MainMenuActivationKey.Space);
        menu.CancelPendingPress();
        menu.PressSelection(MainMenuActivationKey.Space, isRepeat: true);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Space) is null, "Repeat revived a cancelled press.");
    }

    // The same cancellation API serves pointer exit and client/framebuffer resize events.
    private static void PendingCancellation()
    {
        var menu = new MainMenuInteraction();
        menu.PressPointer(MainMenuButton.Exit);
        menu.CancelPendingPress();
        Require(menu.ReleasePointer(MainMenuButton.Exit) is null, "Cancelled pointer press activated.");
        foreach (var key in Enum.GetValues<MainMenuActivationKey>())
        {
            menu.PressSelection(key);
            menu.CancelPendingPress();
            Require(menu.ReleaseSelection(key) is null, $"Cancelled {key} press activated.");
        }
    }

    // Focus reset must restore idle state while keeping useful player feedback.
    private static void FocusReset()
    {
        var menu = new MainMenuInteraction();
        menu.Activate(MainMenuButton.Options);
        string? notice = menu.FooterMessage;
        menu.PressPointer(MainMenuButton.Exit);
        menu.ResetInput();
        Require(menu.ReleasePointer(MainMenuButton.Exit) is null, "Focus reset left a pointer press.");
        menu.PressSelection(MainMenuActivationKey.Space);
        menu.ResetInput();
        Require(menu.ReleaseSelection(MainMenuActivationKey.Space) is null, "Focus reset left a keyboard press.");
        Require(menu.SelectedButton is null && menu.HoveredButton is null && menu.FooterMessage == notice, "Focus reset lost its contract.");
    }

    // Input takeover must never let an old release activate a second action.
    private static void SwitchingInputSource()
    {
        var menu = new MainMenuInteraction();
        menu.PressSelection(MainMenuActivationKey.Enter);
        menu.MovePointer(MainMenuButton.Exit);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Enter) is null, "Pointer takeover kept a key press.");
        menu.PressPointer(MainMenuButton.Exit);
        menu.MoveSelection(1);
        Require(menu.ReleasePointer(MainMenuButton.Exit) is null, "Arrow takeover kept a pointer press.");
        menu.PressPointer(MainMenuButton.Exit);
        menu.PressSelection(MainMenuActivationKey.Space);
        Require(menu.ReleasePointer(MainMenuButton.Exit) is null, "Space takeover kept a pointer press.");
        menu.PressPointer(MainMenuButton.Start);
        Require(menu.ReleaseSelection(MainMenuActivationKey.Space) is null, "Click takeover kept a key press.");
    }

    // All unavailable actions produce distinct notices and replace the previous notice.
    private static void UnavailableMessages()
    {
        var menu = new MainMenuInteraction();
        (MainMenuButton Button, string Label)[] features =
            [(MainMenuButton.Start, "INICIAR"), (MainMenuButton.Laboratory, "LAB"), (MainMenuButton.Options, "OPCIONES")];
        var notices = new HashSet<string>();
        foreach (var (button, label) in features)
        {
            menu.PressPointer(button);
            Require(menu.ReleasePointer(button) is { } activated && !menu.Activate(activated), "Unavailable action requested exit.");
            string notice = menu.FooterMessage ?? throw new InvalidOperationException("Unavailable notice is missing.");
            Require(notice.Contains(label) && notice.Contains("NO ESTÁ DISPONIBLE"), $"Unreadable notice for {button}.");
            Require(notices.Add(notice), "Notice did not distinguish the selected feature.");
        }
    }

    // A successful Exit requests shutdown without creating a placeholder notice or screen.
    private static void ExitRequest()
    {
        foreach (var key in Enum.GetValues<MainMenuActivationKey>())
        {
            var menu = new MainMenuInteraction();
            menu.MovePointer(MainMenuButton.Exit);
            menu.PressSelection(key);
            Require(menu.ReleaseSelection(key) is { } activated && menu.Activate(activated), "Valid Exit did not request shutdown.");
            Require(menu.FooterMessage is null, "Exit created an unavailable notice.");
        }

        var pointer = new MainMenuInteraction();
        pointer.PressPointer(MainMenuButton.Exit);
        Require(pointer.ReleasePointer(MainMenuButton.Exit) is { } clicked && pointer.Activate(clicked), "Mouse Exit did not request shutdown.");
    }

    // Inspect real rendered row coordinates at normal, compact and high-DPI framebuffer sizes.
    private static void LayoutHitBounds()
    {
        (int Width, int Height)[] sizes = [(400, 300), (800, 600), (1280, 720), (1920, 1080), (2560, 1440)];
        foreach (var (width, height) in sizes)
        {
            var layout = MainMenuLayout.Create(width, height);
            foreach (var button in Enum.GetValues<MainMenuButton>())
            {
                float top = layout.ButtonY(button);
                float centerY = top + layout.ButtonHeight / 2;
                Require(layout.HitTest(layout.Center, centerY) == button, $"Button center missed at {width}x{height}.");
                Require(layout.HitTest(layout.ButtonLeft, top) == button, "Top-left edge missed.");
                Require(layout.HitTest(layout.ButtonLeft - 1, centerY) is null, "Left shadow hit a button.");
                Require(layout.HitTest(layout.ButtonLeft + layout.ButtonWidth, centerY) is null, "Right edge hit a button.");
                Require(layout.HitTest(layout.Center, top + layout.ButtonHeight) is null, "Bottom edge hit a button.");
                Require(layout.HitTest(layout.Center, top + layout.ButtonHeight + layout.ButtonGap / 2) is null, "Gap hit a button.");
            }
        }
    }

    // One assertion helper is sufficient for this dependency-free executable suite.
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
