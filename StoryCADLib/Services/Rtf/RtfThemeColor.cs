using System.Text;
using System.Text.RegularExpressions;

namespace StoryCADLib.Services.Rtf;

/// <summary>
///     Removes the theme text colors that RichEditBoxExtended.UpdateTheme puts into RTF.
///     A theme color saved in an outline makes text unreadable in the other theme (#370, #1675).
/// </summary>
public static class RtfThemeColor
{
    // White (dark theme), black (light theme) and light grey (an earlier version of the control).
    private static readonly (int Red, int Green, int Blue)[] ThemeColors =
    [
        (255, 255, 255),
        (0, 0, 0),
        (211, 211, 211)
    ];

    private static readonly Regex RedPattern = new(@"\\red(\d+)", RegexOptions.CultureInvariant);
    private static readonly Regex GreenPattern = new(@"\\green(\d+)", RegexOptions.CultureInvariant);
    private static readonly Regex BluePattern = new(@"\\blue(\d+)", RegexOptions.CultureInvariant);

    /// <summary>
    ///     Changes each \cfN that refers to a theme color to \cf0 (the automatic color).
    ///     All other characters stay the same. Text that is not RTF, or that this method
    ///     cannot read, is returned unchanged.
    /// </summary>
    public static string RemoveThemeColors(string rtf)
    {
        if (string.IsNullOrEmpty(rtf))
        {
            return rtf;
        }

        if (!rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.Ordinal))
        {
            return rtf;
        }

        try
        {
            var themeEntries = FindThemeEntries(rtf);
            if (themeEntries.Count == 0)
            {
                return rtf;
            }

            return ReplaceThemeReferences(rtf, themeEntries);
        }
        catch (Exception)
        {
            return rtf;
        }
    }

    private static HashSet<int> FindThemeEntries(string rtf)
    {
        var entries = new HashSet<int>();

        const string tableStart = @"{\colortbl";
        var start = rtf.IndexOf(tableStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return entries;
        }

        start += tableStart.Length;
        var end = rtf.IndexOf('}', start);
        if (end < 0)
        {
            return entries;
        }

        var parts = rtf.Substring(start, end - start).Split(';');
        // The text after the last semicolon is not an entry. Entry 0 is never a theme entry.
        for (var k = 1; k < parts.Length - 1; k++)
        {
            var red = RedPattern.Match(parts[k]);
            var green = GreenPattern.Match(parts[k]);
            var blue = BluePattern.Match(parts[k]);
            if (!red.Success || !green.Success || !blue.Success)
            {
                continue;
            }

            var color = (int.Parse(red.Groups[1].Value), int.Parse(green.Groups[1].Value),
                int.Parse(blue.Groups[1].Value));
            if (Array.IndexOf(ThemeColors, color) >= 0)
            {
                entries.Add(k);
            }
        }

        return entries;
    }

    private static string ReplaceThemeReferences(string rtf, HashSet<int> themeEntries)
    {
        var result = new StringBuilder(rtf.Length);
        var i = 0;
        while (i < rtf.Length)
        {
            if (rtf[i] != '\\' || i + 1 >= rtf.Length)
            {
                result.Append(rtf[i]);
                i++;
                continue;
            }

            var next = rtf[i + 1];
            if (next == '\'')
            {
                // A hex character: \'hh
                var length = Math.Min(4, rtf.Length - i);
                result.Append(rtf, i, length);
                i += length;
                continue;
            }

            if (!char.IsAsciiLetter(next))
            {
                // A control symbol, such as \\, \{, \}, \*, \~ or \-
                result.Append(rtf, i, 2);
                i += 2;
                continue;
            }

            // A control word and its optional parameter.
            var wordStart = i + 1;
            var wordEnd = wordStart;
            while (wordEnd < rtf.Length && char.IsAsciiLetter(rtf[wordEnd]))
            {
                wordEnd++;
            }

            var paramStart = wordEnd;
            var paramEnd = paramStart;
            if (paramEnd < rtf.Length && rtf[paramEnd] == '-')
            {
                paramEnd++;
            }

            while (paramEnd < rtf.Length && char.IsAsciiDigit(rtf[paramEnd]))
            {
                paramEnd++;
            }

            var word = rtf.Substring(wordStart, wordEnd - wordStart);
            var parameter = rtf.Substring(paramStart, paramEnd - paramStart);

            if (word == "bin")
            {
                // Binary data can hold any byte. Do not change this RTF.
                return rtf;
            }

            result.Append('\\').Append(word);
            if (word == "cf" && int.TryParse(parameter, out var entry) && themeEntries.Contains(entry))
            {
                result.Append('0');
            }
            else
            {
                result.Append(parameter);
            }

            i = paramEnd;
        }

        return result.ToString();
    }
}
