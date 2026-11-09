using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GEvolve.Game;

/// <summary>Editing state for the existing focused field; its owner still validates and stores text.</summary>
public sealed class TextFieldEditor
{
    private object? _owner;
    private Func<string>? _read;
    private Action<string>? _write;
    private string _observed = "";
    private int _cursor, _anchor;
    private bool _suppressText;
    public bool HasFocus => _owner is not null;
    public int Cursor { get { Synchronize(); return _cursor; } }
    public int SelectionStart { get { Synchronize(); return Math.Min(_anchor, _cursor); } }
    public int SelectionLength { get { Synchronize(); return Math.Abs(_anchor - _cursor); } }

    public void Focus(object owner, Func<string> read, Action<string> write)
    {
        if (ReferenceEquals(owner, _owner)) { Synchronize(); return; }
        _owner = owner; _read = read; _write = write; _suppressText = false;
        _observed = read(); _cursor = _anchor = _observed.Length;
    }
    public void Blur()
    {
        _owner = null; _read = null; _write = null; _observed = "";
        _cursor = _anchor = 0; _suppressText = false;
    }
    public void UpdateModifiers(bool control, bool alt = false, bool command = false)
        => _suppressText = control || alt || command;

    public bool KeyDown(Keys key, bool control, bool shift, bool alt, bool command,
        Func<string?> readClipboard, Action<string> writeClipboard)
    {
        UpdateModifiers(control, alt, command);
        if (!HasFocus) return false;
        Synchronize();
        if (alt) return false;
        if (control || command)
        {
            switch (key)
            {
                case Keys.A: _anchor = 0; _cursor = _observed.Length; return true;
                case Keys.C:
                    if (SelectionLength > 0) writeClipboard(_observed.Substring(SelectionStart, SelectionLength));
                    return true;
                case Keys.X:
                    if (SelectionLength > 0)
                    { writeClipboard(_observed.Substring(SelectionStart, SelectionLength)); ReplaceSelection(""); }
                    return true;
                case Keys.V:
                    var pasted = readClipboard()?.Trim();
                    if (!string.IsNullOrEmpty(pasted)) ReplaceSelection(pasted);
                    return true;
            }
            // Other modified shortcuts remain available to the window/application.
            return false;
        }
        switch (key)
        {
            case Keys.Left:
                Move(!shift && SelectionLength > 0 ? SelectionStart : Math.Max(0, _cursor - 1), shift); return true;
            case Keys.Right:
                Move(!shift && SelectionLength > 0 ? SelectionStart + SelectionLength : Math.Min(_observed.Length, _cursor + 1), shift); return true;
            case Keys.Home: Move(0, shift); return true;
            case Keys.End: Move(_observed.Length, shift); return true;
            case Keys.Backspace:
                if (SelectionLength == 0 && _cursor > 0) _anchor = _cursor - 1;
                ReplaceSelection(""); return true;
            case Keys.Delete:
                if (SelectionLength == 0 && _cursor < _observed.Length) _anchor = _cursor + 1;
                ReplaceSelection(""); return true;
            default: return false; // Printable keys are inserted ONLY by TextInput.
        }
    }
    public bool TextInput(string text)
    {
        if (!HasFocus || _suppressText || string.IsNullOrEmpty(text) || text.Any(char.IsControl)) return false;
        Synchronize(); ReplaceSelection(text); return true;
    }
    private void Move(int position, bool extend)
    { _cursor = position; if (!extend) _anchor = position; }
    private void ReplaceSelection(string insertion)
    {
        int start = SelectionStart, length = SelectionLength;
        string candidate = _observed.Remove(start, length).Insert(start, insertion);
        if (candidate == _observed) return;
        _write!(candidate);
        string accepted = _read!();
        // A rejected edit preserves both the original value and selection.
        if (accepted == _observed) return;
        _observed = accepted;
        _cursor = _anchor = accepted == candidate ? start + insertion.Length : accepted.Length;
    }
    private void Synchronize()
    {
        if (_read is null) return;
        string current = _read();
        if (current == _observed) return;
        _observed = current; _cursor = _anchor = current.Length;
    }
}
