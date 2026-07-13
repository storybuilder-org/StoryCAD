using FlaUI.Core.WindowsAPI;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Parses logical chords like "Primary+S" into virtual keys. "Primary" maps to Ctrl on
///     this backend and would map to Cmd on a future macOS one; that is why scripts carry
///     the logical name and literal "Ctrl+..." is only a lint warning, not an error
///     (devdocs/issue_1421_dsl_design.md "Pointer and keyboard").
/// </summary>
internal static class KeyChord
{
    /// <summary>
    ///     Splits a chord into modifiers and the final key. Throws ArgumentException on
    ///     malformed chords; the script linter is expected to catch those before a live run.
    /// </summary>
    public static (VirtualKeyShort[] Modifiers, VirtualKeyShort Key) Parse(string chord)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chord);
        var parts = chord.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException($"Chord '{chord}' has no keys.", nameof(chord));
        }

        var modifiers = new List<VirtualKeyShort>();
        for (var i = 0; i < parts.Length - 1; i++)
        {
            modifiers.Add(ParseModifier(parts[i], chord));
        }

        return (modifiers.ToArray(), ParseKey(parts[^1], chord));
    }

    private static VirtualKeyShort ParseModifier(string token, string chord) => token.ToUpperInvariant() switch
    {
        "PRIMARY" => VirtualKeyShort.CONTROL, // Ctrl on this backend, Cmd on a future macOS one
        "CTRL" or "CONTROL" => VirtualKeyShort.CONTROL,
        "SHIFT" => VirtualKeyShort.SHIFT,
        "ALT" => VirtualKeyShort.ALT,
        "WIN" or "WINDOWS" => VirtualKeyShort.LWIN,
        _ => throw new ArgumentException($"Unknown modifier '{token}' in chord '{chord}'."),
    };

    private static VirtualKeyShort ParseKey(string token, string chord)
    {
        var upper = token.ToUpperInvariant();

        if (upper.Length == 1)
        {
            var c = upper[0];
            if (c is >= 'A' and <= 'Z')
            {
                return (VirtualKeyShort)((int)VirtualKeyShort.KEY_A + (c - 'A'));
            }

            if (c is >= '0' and <= '9')
            {
                return (VirtualKeyShort)((int)VirtualKeyShort.KEY_0 + (c - '0'));
            }
        }

        if (upper.Length is 2 or 3 && upper[0] == 'F' && int.TryParse(upper[1..], out var fn) && fn is >= 1 and <= 24)
        {
            return (VirtualKeyShort)((int)VirtualKeyShort.F1 + (fn - 1));
        }

        return upper switch
        {
            "ENTER" or "RETURN" => VirtualKeyShort.RETURN,
            "ESC" or "ESCAPE" => VirtualKeyShort.ESCAPE,
            "TAB" => VirtualKeyShort.TAB,
            "SPACE" => VirtualKeyShort.SPACE,
            "BACKSPACE" or "BACK" => VirtualKeyShort.BACK,
            "DELETE" or "DEL" => VirtualKeyShort.DELETE,
            "INSERT" or "INS" => VirtualKeyShort.INSERT,
            "HOME" => VirtualKeyShort.HOME,
            "END" => VirtualKeyShort.END,
            "PAGEUP" or "PGUP" => VirtualKeyShort.PRIOR,
            "PAGEDOWN" or "PGDN" => VirtualKeyShort.NEXT,
            "LEFT" => VirtualKeyShort.LEFT,
            "RIGHT" => VirtualKeyShort.RIGHT,
            "UP" => VirtualKeyShort.UP,
            "DOWN" => VirtualKeyShort.DOWN,
            _ => throw new ArgumentException($"Unknown key '{token}' in chord '{chord}'."),
        };
    }
}
