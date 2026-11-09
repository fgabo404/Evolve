using GEvolve.Game;
using GEvolve.Engine.World;
using OpenTK.Windowing.GraphicsLibraryFramework;

/// <summary>Real Game bindings and OpenTK key values; no window, GL or native clipboard access.</summary>
public static class TextFieldEditingTests
{
    public static void Run()
    {
        var editor = new TextFieldEditor();
        var form = new GameSession(); form.OpenCreateWorld(); form.SetSeedText("2026");
        string clipboard = "42"; int reads = 0, writes = 0;
        bool Key(Keys key, bool ctrl = false, bool shift = false, bool alt = false) =>
            editor.KeyDown(key, ctrl, shift, alt, false, () => { reads++; return clipboard; }, value => { writes++; clipboard = value; });
        void Release() => editor.UpdateModifiers(false);
        void Paste(string value) { clipboard = value; Key(Keys.V, ctrl: true); Release(); }
        void SelectAll() { Key(Keys.A, ctrl: true); Release(); }
        void FocusForm() => editor.Focus(form, () => form.SeedText, form.SetSeedText);
        foreach (var key in new[] { Keys.A, Keys.C, Keys.X, Keys.V, Keys.Backspace, Keys.Delete, Keys.Left, Keys.Home })
            Check(!Key(key, ctrl: true), "Unfocused input does not consume global shortcuts");
        Check(!editor.TextInput("9") && reads == 0 && writes == 0, "No clipboard or text access without focused field");

        FocusForm(); SelectAll(); Paste("123456789");
        Check(form.SeedText == "123456789" && editor.Cursor == 9, "Positive seed paste replaces selection");
        SelectAll(); Paste("-2147483648");
        Check(form.SeedText == "-2147483648" && editor.Cursor == 11, "Negative int32 minimum paste");
        SelectAll(); Paste("2147483647");
        Check(form.SeedText == "2147483647", "Positive int32 maximum paste");
        SelectAll(); Paste("  -731\r\n");
        Check(form.SeedText == "-731", "Boundary clipboard whitespace normalized without filtering invalid internal text");
        SelectAll(); Paste("2026");
        Key(Keys.Home); Key(Keys.Right); Key(Keys.Right, shift: true); Key(Keys.Right, shift: true);
        Check(editor.SelectionStart == 1 && editor.SelectionLength == 2, "Shift arrows select interior substring");
        Key(Keys.C, ctrl: true); Release(); Check(clipboard == "02" && form.SeedText == "2026", "Copy selected substring only");
        Key(Keys.X, ctrl: true); Release(); Check(clipboard == "02" && form.SeedText == "26" && editor.Cursor == 1, "Cut selected substring");
        Key(Keys.V, ctrl: true); Release(); Check(form.SeedText == "2026", "Paste at cursor restores cut substring");
        Key(Keys.Home); Key(Keys.Right, shift: true); Key(Keys.Right, shift: true);
        Check(editor.TextInput("9") && form.SeedText == "926", "Typing replaces selection instead of appending");
        Key(Keys.Home); Key(Keys.Delete); Check(form.SeedText == "26" && editor.Cursor == 0, "Delete removes forward");
        Key(Keys.End); Key(Keys.Backspace); Check(form.SeedText == "2" && editor.Cursor == 1, "Backspace removes backward");
        Key(Keys.Home); Key(Keys.Backspace); Key(Keys.End); Key(Keys.Delete);
        Check(form.SeedText == "2", "Boundary deletion is harmless");
        Key(Keys.Home, shift: true); Check(editor.SelectionLength == 1, "Shift Home extends selection");
        Key(Keys.Right); Check(editor.SelectionLength == 0 && editor.Cursor == 1, "Right collapses selection to end");
        Key(Keys.Home); Key(Keys.End, shift: true); Key(Keys.Left);
        Check(editor.Cursor == 0 && editor.SelectionLength == 0, "Left collapses selection to start");
        Key(Keys.C, ctrl: true); Key(Keys.X, ctrl: true); Release();
        Check(form.SeedText == "2" && clipboard == "02", "Copy/cut with no selection leaves clipboard and text intact");

        SelectAll(); Key(Keys.Delete); Check(form.SeedText == "", "Delete removes selection");
        editor.TextInput("-731"); SelectAll(); Key(Keys.Backspace);
        Check(form.SeedText == "" && editor.Cursor == 0, "Backspace removes selection");
        editor.TextInput("-"); Check(form.SeedText == "-", "Transient sign is editable without a fabricated seed");

        SelectAll(); Paste("2026"); SelectAll();
        foreach (string invalid in new[] { "abc", "12x34", "1 2", "--1", "1-2", "１２", "123456789012", "\n", "" })
        {
            Paste(invalid);
            Check(form.SeedText == "2026" && editor.SelectionLength == 4, "Rejected paste preserves original value and selection: " + invalid);
        }
        foreach (string overflow in new[] { "2147483648", "-2147483649" })
        {
            SelectAll(); Paste(overflow);
            Check(form.SeedText == overflow, "Editable numeric overflow retains original field validation policy");
            try { form.BeginGeneration(); throw new Exception("Overflow must not generate"); }
            catch (FormatException) { Check(form.Screen == GameScreen.CreateWorld && form.Error is not null, "Overflow remains in form with validation error"); }
        }

        SelectAll(); Paste("4"); Key(Keys.End);
        Check(!Key(Keys.D2) && editor.TextInput("2") && form.SeedText == "42", "Printable keydown does not insert; TextInput inserts once");
        SelectAll(); clipboard = "-731"; Key(Keys.V, ctrl: true);
        Check(!editor.TextInput("v") && !editor.TextInput("-731") && form.SeedText == "-731", "Shortcut text events cannot duplicate pasted characters");
        Release(); Key(Keys.End); Check(editor.TextInput("0") && form.SeedText == "-7310", "Text resumes after modifier release");
        Check(!editor.TextInput("\b") && !editor.TextInput("\u0016"), "Control characters never insert");
        Key(Keys.V, ctrl: true, alt: true);
        Check(!editor.TextInput("v") && form.SeedText == "-7310", "Modified native text does not leak into input"); Release();

        using var lab = new PangaeaLaboratory();
        lab.SetSeedText("42"); lab.RequestRestart(); Wait(lab);
        lab.ExecutePlatesAndMotion(); Wait(lab);
        foreach (var stage in new[] { PangaeaGenerationStage.Surface, PangaeaGenerationStage.Base,
            PangaeaGenerationStage.MountainSystems, PangaeaGenerationStage.Final, PangaeaGenerationStage.Temperature,
            PangaeaGenerationStage.Precipitation, PangaeaGenerationStage.Biomes }) { lab.Execute(stage); Wait(lab); }
        var grid = lab.Map!.Snapshot.Grid!; lab.SelectAt(grid.Cells[0].Center);
        Check(lab.Map.Snapshot.Biomes is not null && lab.SelectedCell is not null, "Fixture has complete ecological session and inspection");
        editor.Focus(lab, () => lab.SeedText, lab.SetSeedText);
        Check(editor.Cursor == 2 && editor.SelectionLength == 0, "Changing focused owner resets selection/cursor");
        SelectAll(); Paste("42"); Check(lab.Map is not null && lab.SelectedCell is not null, "Same seed paste does not invalidate session");
        SelectAll(); Paste("invalid"); Check(lab.SeedText == "42" && lab.Map is not null, "Rejected lab paste cannot invalidate valid session");
        SelectAll(); Paste("-731");
        Check(lab.SeedText == "-731" && form.SeedText == "-7310" && lab.Map is null && lab.SelectedCell is null &&
            lab.Stages.Count == 0 && !lab.Busy, "Changed valid lab paste clears entire session, ecology, partial results and inspection; other field untouched");
        lab.RequestRestart(); Wait(lab);
        Check(lab.Map!.Snapshot.Configuration.Seed == -731 && lab.Map.Snapshot.Plates is null && lab.Map.Snapshot.Climate is null &&
            lab.CanExecute(PangaeaGenerationStage.Plates) && !lab.CanExecute(PangaeaGenerationStage.Surface), "Restart with pasted negative seed returns to initial sequential flow");
        SelectAll(); Paste("2147483648"); lab.RequestRestart();
        Check(lab.Error is not null && lab.Map is null && !lab.Busy, "Lab overflow cannot start a world");
        FocusForm(); Key(Keys.End); editor.TextInput("1");
        Check(form.SeedText == "-73101" && lab.SeedText == "2147483648", "Only current focused field receives typed text");
        form.SetSeedText("7"); Check(editor.Cursor == 1 && editor.SelectionLength == 0, "External field updates synchronize cursor safely");
        editor.Blur(); int previousReads = reads, previousWrites = writes;
        Check(!Key(Keys.V, ctrl: true) && !editor.TextInput("5") && reads == previousReads && writes == previousWrites && form.SeedText == "7",
            "Blur/window departure leaves global shortcuts and clipboard untouched");
        Console.WriteLine("INPUT_EDIT_PASS: positive/negative int32; selection replacement; clipboard copy/cut/paste; cursor/delete/home/end; invalid/overflow; focus isolation; no duplicate key/text insertion; full laboratory invalidation and restart. No window/GL/native clipboard.");
    }
    private static void Wait(PangaeaLaboratory lab)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (lab.Busy) { lab.Update(); if (timer.Elapsed.TotalSeconds > 90) throw new TimeoutException("Input laboratory fixture timeout"); Thread.Sleep(1); }
        Check(lab.Error is null, "Laboratory fixture succeeds");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
