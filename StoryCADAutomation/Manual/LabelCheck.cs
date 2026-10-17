using System.Text.RegularExpressions;
using StoryCADAutomation.Scripting;

namespace StoryCADAutomation.Manual;

/// <summary>One bold phrase in a manual page, with its 1-based line.</summary>
public sealed record BoldPhrase(int Line, string Text);

/// <summary>
///     The `labels` command's check (#1422 D4): the manual names UI labels in bold
///     (**Cancel**, **Next**), so bold text that matches no XAML label may be a label the code
///     renamed. Bold is also used for emphasis, so the result is advisory.
/// </summary>
public sealed partial class LabelCheck
{
    [GeneratedRegex(@"\*\*(?<text>[^*]+?)\*\*")]
    private static partial Regex BoldText();

    private readonly HashSet<string> _labels;

    public LabelCheck(XamlUiFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        // Case-insensitive: the manual capitalizes labels in running text differently from the UI.
        _labels = new HashSet<string>(facts.AllLabels.Concat(facts.TabLabels).Select(Comparable),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Bold phrases in a markdown page with their 1-based line, outside fenced code.</summary>
    public static IEnumerable<BoldPhrase> BoldPhrases(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var inFence = false;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            foreach (Match match in BoldText().Matches(lines[i]))
            {
                yield return new BoldPhrase(i + 1, match.Groups["text"].Value.Trim());
            }
        }
    }

    /// <summary>
    ///     Bold phrases that match no label. A phrase ending in ':' is a run-in heading
    ///     (**Note:**) and is skipped. A menu path (**View > Toggle Navigation Pane**) matches
    ///     when each part is a label.
    /// </summary>
    public IReadOnlyList<BoldPhrase> Misses(string markdown)
        => BoldPhrases(markdown)
            .Where(bold => !bold.Text.EndsWith(':') && !IsLabel(bold.Text))
            .ToList();

    internal bool IsLabel(string phrase)
        => phrase.Split('>').All(part => _labels.Contains(Comparable(part)));

    /// <summary>XamlUiFacts.Normalize, then a trailing ellipsis dropped, on both the label and the phrase.</summary>
    private static string Comparable(string label)
        => XamlUiFacts.Normalize(label).TrimEnd('.', '…').Trim();
}
