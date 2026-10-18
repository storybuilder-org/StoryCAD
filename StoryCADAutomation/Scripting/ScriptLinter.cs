using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Scripting;

/// <summary>
///     The `check` lint, rule for rule the design's "Script lint as a fitness function"
///     section (devdocs/issue_1421_dsl_design.md). Errors: bare ids missing from the XAML,
///     runtime-inert ids, menu text paths not in the XAML, unmatched tab text, non-{scratch}-rooted
///     dialog paths, malformed chords. Warnings: name: outside dialog scopes, literal
///     platform chords, screenshot names outside the manual image standard. Stated limits, checkable only live: tree node names (runtime outline
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
                case ScriptVerb.Menu when statement.Target is { Kind: ElementAddressKind.AutomationId } leaf:
                    LintMenuLeaf(statement, leaf.Value, findings);
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
                case ScriptVerb.ScreenshotDialog:
                    LintScreenshotName(statement, findings);
                    break;
                case ScriptVerb.ShowImage:
                    LintImageName(statement, findings);
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
                else if (CollaboratorBuiltIdPattern().IsMatch(address.Value))
                {
                    // A Collaborator control built in C# (Collaborator #197): no XAML holds it, and
                    // the workflow labels live in CollaboratorLib, so its existence is checked live.
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

    /// <summary>
    ///     The whole path must be one the runtime can walk on Windows: the owning button's label,
    ///     then sub-menu labels, then the leaf, each the UIA Name (#1421 review M3). Checking
    ///     segments one by one passed "File/Exit", whose Windows Name is "Quit".
    /// </summary>
    private void LintMenuPath(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        var segments = statement.Text!.Split('/');
        if (_facts.MenuPaths.Contains(XamlUiFacts.MenuPathKey(segments)))
        {
            return;
        }

        // Name the labels that can follow the longest valid start of the path, so the author
        // sees "Quit" next to "Exit".
        var labels = _facts.MenuPaths.Select(XamlUiFacts.MenuPathLabels).ToList();
        var known = segments.Select(XamlUiFacts.Normalize).ToArray();
        var depth = 0;
        while (depth < known.Length - 1
               && labels.Any(path => path.Length > depth + 1 && path.Take(depth + 1).SequenceEqual(known.Take(depth + 1))))
        {
            depth++;
        }

        var next = labels.Where(path => path.Length > depth && path.Take(depth).SequenceEqual(known.Take(depth)))
            .Select(path => path[depth]).Distinct().Order(StringComparer.Ordinal);
        var where = depth == 0 ? "at the top level" : $"after '{string.Join("/", known.Take(depth))}'";
        findings.Add(Error(statement,
            $"menu path '{statement.Text}' is not a menu path in the XAML (Windows UIA names); " +
            $"{where} the menus have: {string.Join(", ", next)}. " +
            "If a label itself contains '/', address the item by its leaf AutomationId instead."));
    }

    /// <summary>
    ///     The runner opens a menu item by clicking the toolbar button and sub-menus the XAML
    ///     puts it under; it never searches the menus (#1422). An id with no such owner (a
    ///     context-menu item, or a control that is not a menu item) cannot be reached by menu.
    /// </summary>
    private void LintMenuLeaf(ScriptStatement statement, string id, List<ScriptDiagnostic> findings)
    {
        if (!_facts.MenuOpeners.ContainsKey(id))
        {
            findings.Add(Error(statement,
                $"'{id}' is not an item on a toolbar menu in the XAML, so menu cannot open it; " +
                "use context-menu for a tree right-click menu, or click for other controls."));
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
        else if (!ManualImageNamePattern().IsMatch(name))
        {
            findings.Add(Warning(statement,
                $"screenshot name '{name}' does not follow <Area>-<Screen>[-<State>][-Mac].png, hyphenated " +
                "capitalized words (devdocs/automation_naming_convention.md, Manual image names)."));
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Z0-9][A-Za-z0-9]*(-[A-Z0-9][A-Za-z0-9]*)+\.png$")]
    private static partial System.Text.RegularExpressions.Regex ManualImageNamePattern();

    /// <summary>
    ///     A show image names a .png or .jpg file in the assets folder, with no folder part
    ///     (video design, section 6.2 rule 5). Whether the file exists is checked by the video
    ///     command, which knows the assets folder.
    /// </summary>
    private static void LintImageName(ScriptStatement statement, List<ScriptDiagnostic> findings)
    {
        var name = statement.Text!;
        if (!ImageNamePattern().IsMatch(name) || name.Contains("..", StringComparison.Ordinal))
        {
            findings.Add(Error(statement,
                $"show image '{name}' must be a plain .png or .jpg file name (letters, digits, '-', '_', '.', ' '), " +
                "with no folder; the file lives in the assets folder."));
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Za-z0-9_. -]+\.(png|jpg)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex ImageNamePattern();

    [System.Text.RegularExpressions.GeneratedRegex(@"^[0-9]+$")]
    private static partial System.Text.RegularExpressions.Regex NativeControlIdPattern();

    // The shapes StoryCADLib's CollaboratorAutomationIds builds. The runner cannot reference
    // StoryCADLib, so the shapes are repeated here.
    [System.Text.RegularExpressions.GeneratedRegex(@"^Workflow(Group|Settings)?[A-Z][A-Za-z0-9]*(NavItem|StarButton|Check|Combo|TextBox)$")]
    private static partial System.Text.RegularExpressions.Regex CollaboratorBuiltIdPattern();

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
