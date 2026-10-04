using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Scripting;

/// <summary>
///     The `check` lint, rule for rule the design's "Script lint as a fitness function"
///     section (devdocs/issue_1421_dsl_design.md). Errors: bare ids missing from the XAML,
///     runtime-inert ids, unmatched menu/tab text-path segments, non-{scratch}-rooted
///     dialog paths, malformed chords. Warnings: name: outside dialog scopes, literal
///     platform chords. Stated limits, checkable only live: tree node names (runtime outline
///     data), native-dialog element names, and whether a statically valid id surfaces an
///     automation peer at runtime outside the deny-list.
/// </summary>
public sealed partial class ScriptLinter
{
    /// <summary>
    ///     Runtime-inert deny-list: these ids exist in Shell.xaml but never surface an
    ///     automation peer (ItemsRepeater creates none; rows live under two Tree controls
    ///     with id "ListControl" — #1420 runtime facts). A pure existence scan would pass
    ///     them, so they are rejected by name.
    /// </summary>
    private static readonly string[] RuntimeInertIds = { "NavigationTree", "TrashTree" };

    /// <summary>
    ///     ContentDialog template-part ids. They come from the WinUI ContentDialog template, not
    ///     StoryCAD XAML, so the scan never sees them; checked live on Brigid 2026-10-04. WinUI
    ///     shows one ContentDialog at a time, so each id is unambiguous (#1421 review M4).
    /// </summary>
    private static readonly string[] ContentDialogButtonIds = { "PrimaryButton", "SecondaryButton", "CloseButton" };

    /// <summary>
    ///     Modifier tokens that hardcode one platform. "Primary" is the logical form (Ctrl
    ///     here, Cmd on a future macOS backend); token names mirror
    ///     <see cref="KeyChord" />'s modifier table.
    /// </summary>
    private static readonly string[] PlatformModifierTokens = { "CTRL", "CONTROL", "WIN", "WINDOWS" };

    private readonly XamlUiFacts _facts;

    public ScriptLinter(XamlUiFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        _facts = facts;
    }

    /// <summary>Lints parsed statements; parse errors are the parser's output, not repeated here.</summary>
    public IReadOnlyList<ScriptDiagnostic> Lint(IReadOnlyList<ScriptStatement> statements)
    {
        ArgumentNullException.ThrowIfNull(statements);
        var findings = new List<ScriptDiagnostic>();
        foreach (var statement in statements)
        {
            LintAddress(statement, statement.Target, findings);
            LintAddress(statement, statement.DragTarget, findings);
            switch (statement.Verb)
            {
                case ScriptVerb.Menu when statement.Text is not null:
                    LintMenuPath(statement, findings);
                    break;
                case ScriptVerb.Tab when statement.Text is not null:
                    LintTabLabel(statement, findings);
                    break;
                case ScriptVerb.SaveFileDialog:
                case ScriptVerb.OpenFileDialog:
                    LintScratchPath(statement, findings);
                    break;
                case ScriptVerb.Press:
                    LintChord(statement, findings);
                    break;
                case ScriptVerb.Screenshot:
                    LintScreenshotName(statement, findings);
                    break;
            }
        }

        return findings;
    }

    private void LintAddress(ScriptStatement statement, ElementAddress? address, List<ScriptDiagnostic> findings)
    {
        if (address is null)
        {
            return;
        }

        switch (address.Kind)
        {
            case ElementAddressKind.AutomationId:
                if (RuntimeInertIds.Contains(address.Value))
                {
                    findings.Add(Error(statement,
                        $"'{address.Value}' never surfaces an automation peer at runtime (ItemsRepeater creates none); " +
                        "address tree rows with tree \"path\" instead."));
                }
                else if (_facts.AutomationIds.Contains(address.Value))
                {
                    // A StoryCAD XAML id: always valid.
                }
                else if (ContentDialogButtonIds.Contains(address.Value))
                {
                    // ContentDialog buttons are for click and dialog only. Untitled ContentDialogs
                    // (the file menu) cannot be scoped by the dialog verb, so click accepts them;
                    // WinUI shows one ContentDialog at a time, which keeps the id unambiguous.
                    if (statement.Verb is not (ScriptVerb.Click or ScriptVerb.Dialog))
                    {
                        findings.Add(Error(statement,
                            $"'{address.Value}' is a ContentDialog button; use it only with click or dialog."));
                    }
                }
                else if (statement.Verb == ScriptVerb.Dialog && NativeControlIdPattern().IsMatch(address.Value))
                {
                    // A native dialog control id (all digits, e.g. "1" for OK): outside the XAML,
                    // so its existence can only be checked live.
                }
                else
                {
                    findings.Add(Error(statement,
                        $"AutomationId '{address.Value}' does not exist in the convention-scope XAML " +
                        "(devdocs/automation_naming_convention.md is the namespace)."));
                }

                break;
            case ElementAddressKind.Name when statement.Verb != ScriptVerb.Dialog:
                findings.Add(Warning(statement,
                    $"name:\"{address.Value}\" outside a dialog scope; name addressing is for native dialogs " +
                    "without AutomationIds, prefer a bare AutomationId elsewhere (design, Element addressing)."));
                break;
        }
    }

    private void LintMenuPath(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        foreach (var segment in statement.Text!.Split('/'))
        {
            if (_facts.MenuLabels.Contains(XamlUiFacts.Normalize(segment)))
            {
                continue;
            }

            findings.Add(Error(statement,
                $"menu path segment '{segment.Trim()}' matches no Text/Header/Label value on a menu element in the XAML; " +
                "if the item's label itself contains '/', address it by its leaf AutomationId instead."));
        }
    }

    private void LintTabLabel(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        if (!_facts.TabLabels.Contains(XamlUiFacts.Normalize(statement.Text!)))
        {
            findings.Add(Error(statement,
                $"tab text '{statement.Text}' matches no TabViewItem Header value in the XAML."));
        }
    }

    /// <summary>
    ///     Dialog-verb paths must be {scratch}-rooted, as an error: the runner is an
    ///     auto-clicker with the operator's privileges, and this rule makes "scripts never
    ///     touch real user files" mechanical instead of a review convention (design, Dialogs).
    ///     ".." segments are rejected for the same reason; they would re-escape the root.
    /// </summary>
    private static void LintScratchPath(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        var path = statement.Text!;
        if (!path.StartsWith("{scratch}/", StringComparison.Ordinal)
            && !path.StartsWith("{scratch}\\", StringComparison.Ordinal))
        {
            findings.Add(Error(statement,
                $"dialog path '{path}' is not {{scratch}}-rooted; file-dialog paths must start with {{scratch}}/ " +
                "so scripts never touch real user files (design, Dialogs)."));
            return;
        }

        if (path.Split('/', '\\').Any(segment => segment == ".."))
        {
            findings.Add(Error(statement,
                $"dialog path '{path}' contains '..', which would escape the scratch root."));
        }

        var rest = path["{scratch}/".Length..];
        if (rest.Contains("{scratch}", StringComparison.Ordinal) || rest.Contains(':'))
        {
            findings.Add(Error(statement,
                $"dialog path '{path}' may not repeat {{scratch}} or contain ':' after the root; " +
                "either can point the path outside the scratch folder."));
        }
    }

    /// <summary>
    ///     A screenshot name is a plain .png file name, so a script can only write into the
    ///     run's output folder (design, Capture).
    /// </summary>
    private static void LintScreenshotName(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        var name = statement.Text!;
        if (!ScreenshotNamePattern().IsMatch(name) || name.Contains("..", StringComparison.Ordinal))
        {
            findings.Add(Error(statement,
                $"screenshot name '{name}' must be a plain .png file name (letters, digits, '-', '_', '.'), " +
                "with no folder, so the image lands in the run's output folder."));
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[0-9]+$")]
    private static partial System.Text.RegularExpressions.Regex NativeControlIdPattern();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Za-z0-9_.-]+\.png$")]
    private static partial System.Text.RegularExpressions.Regex ScreenshotNamePattern();

    private static void LintChord(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        var chord = statement.Text!;
        if (KeyChord.Validate(chord) is string parseError)
        {
            findings.Add(Error(statement, parseError));
            return;
        }

        var parts = chord.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (PlatformModifierTokens.Contains(parts[i].ToUpperInvariant()))
            {
                findings.Add(Warning(statement,
                    $"'{chord}' hardcodes the {parts[i]} modifier; use the logical form (Primary = Ctrl here, " +
                    "Cmd on a future macOS backend) so translated scripts stay platform-neutral."));
            }
        }
    }

    private static ScriptDiagnostic Error(ScriptStatement statement, string message)
        => new(DiagnosticSeverity.Error, statement.Line, message);

    private static ScriptDiagnostic Warning(ScriptStatement statement, string message)
        => new(DiagnosticSeverity.Warning, statement.Line, message);
}
