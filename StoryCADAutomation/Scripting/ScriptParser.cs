using System.Globalization;
using System.Text.RegularExpressions;
using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Scripting;

/// <summary>
///     Line parser for the automation DSL (devdocs/issue_1421_dsl_design.md "Script language"):
///     line-oriented UTF-8, one statement per line, double-quoted strings, # comments, blank
///     lines ignored, script "..." exactly once as the first statement. No variables, no
///     control flow, no includes; {scratch} passes through as literal text and is substituted
///     only where the design uses it (file-dialog paths, at execution time).
/// </summary>
public static partial class ScriptParser
{
    /// <summary>
    ///     Bare-id shape: the #1420 inventory ids are PascalCase identifiers; digits-only ids
    ///     also pass because native dialog controls carry numeric ids ("1001"). Verb keywords
    ///     are all lowercase with hyphens, so they can never collide with an inventory id.
    /// </summary>
    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex BareIdShape();

    /// <summary>Parses a script file. UTF-8 per the design; a missing file is the caller's error.</summary>
    public static ScriptParseResult ParseFile(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses script text to statements plus per-line errors (the parser recovers per line).</summary>
    public static ScriptParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var statements = new List<ScriptStatement>();
        var errors = new List<ScriptDiagnostic>();

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var raw = lines[i].TrimEnd('\r').Trim();
            try
            {
                var tokens = Tokenize(raw);
                if (tokens.Count == 0)
                {
                    continue; // blank or comment-only
                }

                statements.Add(BuildStatement(tokens, lineNumber, raw));
            }
            catch (SyntaxException ex)
            {
                errors.Add(new ScriptDiagnostic(DiagnosticSeverity.Error, lineNumber, ex.Message));
            }
        }

        // Structural rule: script "..." exactly once, as the first statement.
        string? scriptName = null;
        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i].Verb != ScriptVerb.Script)
            {
                continue;
            }

            if (i == 0)
            {
                scriptName = statements[i].Text;
            }
            else
            {
                errors.Add(new ScriptDiagnostic(
                    DiagnosticSeverity.Error, statements[i].Line,
                    "script \"...\" may appear only once, as the first statement."));
            }
        }

        if (statements.Count == 0)
        {
            errors.Add(new ScriptDiagnostic(
                DiagnosticSeverity.Error, 1, "the script has no statements; it must open with script \"name\"."));
        }
        else if (statements[0].Verb != ScriptVerb.Script)
        {
            errors.Add(new ScriptDiagnostic(
                DiagnosticSeverity.Error, statements[0].Line,
                "the first statement must be script \"name\" (design, Script language)."));
        }

        return new ScriptParseResult { ScriptName = scriptName, Statements = statements, Errors = errors };
    }

    // --- tokenizer -------------------------------------------------------------------------

    private enum TokenKind
    {
        /// <summary>Bare word: a verb, keyword, id, or number.</summary>
        Word,

        /// <summary>Double-quoted string, quotes stripped. No escape sequences in v1.</summary>
        String,

        /// <summary>name:"..." composite, the UIA Name fallback address form.</summary>
        NameString,
    }

    private readonly record struct Token(TokenKind Kind, string Value);

    private static List<Token> Tokenize(string line)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '#')
            {
                break; // comment to end of line; # inside a quoted string does not reach here
            }

            if (c == '"')
            {
                tokens.Add(new Token(TokenKind.String, ReadString(line, ref i)));
                continue;
            }

            var start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]) && line[i] != '"' && line[i] != '#')
            {
                i++;
            }

            var word = line[start..i];
            if (word == "name:")
            {
                if (i >= line.Length || line[i] != '"')
                {
                    throw new SyntaxException("name: must be immediately followed by a quoted string, e.g. name:\"Don't Save\".");
                }

                tokens.Add(new Token(TokenKind.NameString, ReadString(line, ref i)));
            }
            else if (word.StartsWith("name:", StringComparison.Ordinal))
            {
                throw new SyntaxException($"'{word}' is malformed; the name form is name:\"...\" with a quoted string.");
            }
            else
            {
                tokens.Add(new Token(TokenKind.Word, word));
            }
        }

        return tokens;
    }

    private static string ReadString(string line, ref int i)
    {
        i++; // opening quote
        var start = i;
        while (i < line.Length && line[i] != '"')
        {
            i++;
        }

        if (i >= line.Length)
        {
            throw new SyntaxException("unterminated string; strings are double-quoted with no escape sequences in v1.");
        }

        var value = line[start..i];
        i++; // closing quote
        return value;
    }

    // --- statement grammar -------------------------------------------------------------------

    /// <summary>Per-line parse failure; caught by the line loop and recorded as a diagnostic.</summary>
    private sealed class SyntaxException(string message) : Exception(message);

    private static ScriptStatement BuildStatement(List<Token> tokens, int line, string source)
    {
        if (tokens[0].Kind != TokenKind.Word)
        {
            throw new SyntaxException("a statement starts with a verb, not a string.");
        }

        var reader = new ArgReader(tokens);
        var verbWord = reader.Next().Value;
        ScriptStatement statement = verbWord switch
        {
            "script" => New(ScriptVerb.Script) with { Text = TakeString(reader, "run name") },
            "step" => New(ScriptVerb.Step) with { Text = TakeString(reader, "step name") },
            "launch" => New(ScriptVerb.Launch),
            "close" => New(ScriptVerb.Close),
            "expect-exit" => New(ScriptVerb.ExpectExit),
            "click" => New(ScriptVerb.Click) with { Target = TakeTarget(reader) },
            "double-click" => New(ScriptVerb.DoubleClick) with { Target = TakeTarget(reader) },
            "right-click" => New(ScriptVerb.RightClick) with { Target = TakeTarget(reader) },
            "drag" => BuildDrag(reader),
            "press" => New(ScriptVerb.Press) with { Text = TakeString(reader, "key chord") },
            "type" => New(ScriptVerb.Type) with { Text = TakeString(reader, "text to type", allowEmpty: true) },
            "focus" => New(ScriptVerb.Focus) with { Target = TakeTarget(reader) },
            "set" => New(ScriptVerb.Set) with { Target = TakeTarget(reader), Text = TakeString(reader, "value", allowEmpty: true) },
            "select" => New(ScriptVerb.Select) with { Target = TakeTarget(reader), Text = TakeString(reader, "item name") },
            "toggle" => BuildToggle(reader),
            "open-node" => BuildOpenNode(reader),
            "expand" => New(ScriptVerb.Expand) with { Target = TakeTarget(reader) },
            "collapse" => New(ScriptVerb.Collapse) with { Target = TakeTarget(reader) },
            "menu" => BuildMenu(reader),
            "context-menu" => BuildContextMenu(reader),
            "tab" => BuildTab(reader),
            "save-file-dialog" => New(ScriptVerb.SaveFileDialog) with { Text = TakeString(reader, "file path") },
            "open-file-dialog" => New(ScriptVerb.OpenFileDialog) with { Text = TakeString(reader, "file path") },
            "dialog" => BuildDialog(reader),
            "wait" => New(ScriptVerb.Wait) with { Target = TakeTarget(reader) },
            "wait-window" => New(ScriptVerb.WaitWindow) with { Text = TakeString(reader, "window title") },
            "pause" => BuildPause(reader),
            "expect" => BuildExpect(reader),
            "expect-no" => New(ScriptVerb.ExpectNo) with { Target = TakeTarget(reader) },
            "narrate" => New(ScriptVerb.Narrate) with { Text = TakeString(reader, "narration text") },
            "screenshot" => BuildScreenshot(reader),
            _ => throw new SyntaxException($"unknown verb '{verbWord}'; the verb set is fixed in devdocs/issue_1421_dsl_design.md."),
        };

        if (!reader.AtEnd)
        {
            throw new SyntaxException($"unexpected trailing argument '{reader.Peek!.Value.Value}'.");
        }

        return statement;

        ScriptStatement New(ScriptVerb verb) => new() { Verb = verb, Line = line, Source = source };

        ScriptStatement BuildDrag(ArgReader r)
        {
            var from = TakeTarget(r);
            TakeKeyword(r, "to");
            return New(ScriptVerb.Drag) with { Target = from, DragTarget = TakeTarget(r) };
        }

        ScriptStatement BuildToggle(ArgReader r)
        {
            var target = TakeTarget(r);
            var direction = TakeWord(r, "'on' or 'off'");
            return direction switch
            {
                "on" => New(ScriptVerb.Toggle) with { Target = target, On = true },
                "off" => New(ScriptVerb.Toggle) with { Target = target, On = false },
                _ => throw new SyntaxException($"toggle takes 'on' or 'off', not '{direction}'."),
            };
        }

        ScriptStatement BuildOpenNode(ArgReader r)
        {
            // open-node maps to the driver's Invoke-on-tree-row navigation and only makes
            // sense for tree rows, so the grammar requires the tree "path" form.
            TakeKeyword(r, "tree");
            return New(ScriptVerb.OpenNode) with { Target = TreeTarget(TakeString(r, "tree path")) };
        }

        ScriptStatement BuildMenu(ArgReader r)
        {
            var token = r.Peek ?? throw new SyntaxException("menu takes a leaf AutomationId or a quoted text path.");
            r.Next();
            return token.Kind switch
            {
                TokenKind.Word => New(ScriptVerb.Menu) with { Target = IdTarget(token.Value) },
                TokenKind.String => New(ScriptVerb.Menu) with { Text = ValidatedPath(token.Value, "menu text path") },
                _ => throw new SyntaxException("menu takes a leaf AutomationId or a quoted text path, not name:\"...\"."),
            };
        }

        ScriptStatement BuildContextMenu(ArgReader r)
        {
            var target = TakeTarget(r);
            var path = ValidatedPath(TakeString(r, "flyout path"), "flyout path");
            return New(ScriptVerb.ContextMenu) with { Target = target, Text = path };
        }

        ScriptStatement BuildTab(ArgReader r)
        {
            var token = r.Peek ?? throw new SyntaxException("tab takes an AutomationId or a quoted header text.");
            r.Next();
            return token.Kind switch
            {
                TokenKind.Word => New(ScriptVerb.Tab) with { Target = IdTarget(token.Value) },
                TokenKind.String when !string.IsNullOrWhiteSpace(token.Value)
                    => New(ScriptVerb.Tab) with { Text = token.Value },
                TokenKind.String => throw new SyntaxException("tab header text must not be empty."),
                _ => throw new SyntaxException("tab takes an AutomationId or a quoted header text, not name:\"...\"."),
            };
        }

        ScriptStatement BuildDialog(ArgReader r)
        {
            var title = TakeString(r, "dialog title");
            TakeKeyword(r, "click");
            return New(ScriptVerb.Dialog) with { Text = title, Target = TakeTarget(r) };
        }

        ScriptStatement BuildScreenshot(ArgReader r)
        {
            // screenshot "name.png" captures the main window; screenshot dialog "name.png" the open ContentDialog.
            if (r.Peek is { Kind: TokenKind.Word, Value: "dialog" })
            {
                r.Next();
                return New(ScriptVerb.ScreenshotDialog) with { Text = TakeString(r, "png file name") };
            }

            return New(ScriptVerb.Screenshot) with { Text = TakeString(r, "png file name") };
        }

        ScriptStatement BuildPause(ArgReader r)
        {
            var word = TakeWord(r, "duration in seconds");
            if (!double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                || !double.IsFinite(seconds) || seconds < 0)
            {
                throw new SyntaxException($"pause takes a non-negative number of seconds, not '{word}'.");
            }

            return New(ScriptVerb.Pause) with { Seconds = seconds };
        }

        ScriptStatement BuildExpect(ArgReader r)
        {
            var first = r.Peek ?? throw new SyntaxException(
                "expect takes a target plus a condition, or one of: tree contains \"path\", window \"title\".");

            // "expect tree ..." is ambiguous one token in: "contains" starts the tree-contains
            // form; a quoted string is a tree-path target followed by a condition.
            if (first is { Kind: TokenKind.Word, Value: "tree" })
            {
                r.Next();
                var second = r.Peek;
                if (second is { Kind: TokenKind.Word, Value: "contains" })
                {
                    r.Next();
                    var path = TakeString(r, "tree path");
                    ValidateTreePath(path);
                    return New(ScriptVerb.ExpectTreeContains) with { Text = path };
                }

                if (second is { Kind: TokenKind.String })
                {
                    r.Next();
                    return WithCondition(TreeTarget(second.Value.Value));
                }

                throw new SyntaxException("after 'expect tree', expected contains \"path\" or a quoted tree path.");
            }

            if (first is { Kind: TokenKind.Word, Value: "window" })
            {
                r.Next();
                return New(ScriptVerb.ExpectWindow) with { Text = TakeString(r, "window title") };
            }

            return WithCondition(TakeTarget(r));

            ScriptStatement WithCondition(ElementAddress target)
            {
                var condition = TakeWord(r, "condition (exists, text \"value\", enabled, disabled)");
                return condition switch
                {
                    "exists" => New(ScriptVerb.ExpectExists) with { Target = target },
                    "text" => New(ScriptVerb.ExpectText) with { Target = target, Text = TakeString(r, "expected text", allowEmpty: true) },
                    "enabled" => New(ScriptVerb.ExpectEnabled) with { Target = target },
                    "disabled" => New(ScriptVerb.ExpectDisabled) with { Target = target },
                    _ => throw new SyntaxException($"unknown expect condition '{condition}'; use exists, text \"value\", enabled, or disabled."),
                };
            }
        }
    }

    // --- argument helpers ---------------------------------------------------------------------

    private sealed class ArgReader(List<Token> tokens)
    {
        private int _index = 0;

        public bool AtEnd => _index >= tokens.Count;

        public Token? Peek => AtEnd ? null : tokens[_index];

        public Token Next() => tokens[_index++];
    }

    private static string TakeString(ArgReader reader, string what, bool allowEmpty = false)
    {
        var token = reader.Peek;
        if (token is not { Kind: TokenKind.String })
        {
            throw new SyntaxException($"expected a quoted {what}.");
        }

        reader.Next();
        if (!allowEmpty && string.IsNullOrWhiteSpace(token.Value.Value))
        {
            throw new SyntaxException($"the {what} must not be empty.");
        }

        return token.Value.Value;
    }

    private static string TakeWord(ArgReader reader, string what)
    {
        var token = reader.Peek;
        if (token is not { Kind: TokenKind.Word })
        {
            throw new SyntaxException($"expected {what}.");
        }

        reader.Next();
        return token.Value.Value;
    }

    private static void TakeKeyword(ArgReader reader, string keyword)
    {
        var token = reader.Peek;
        if (token is not { Kind: TokenKind.Word } || token.Value.Value != keyword)
        {
            throw new SyntaxException($"expected '{keyword}'.");
        }

        reader.Next();
    }

    /// <summary>The uniform target grammar: bare AutomationId | tree "path" | name:"...".</summary>
    private static ElementAddress TakeTarget(ArgReader reader)
    {
        var token = reader.Peek ?? throw new SyntaxException("expected a target: an AutomationId, tree \"path\", or name:\"...\".");
        switch (token.Kind)
        {
            case TokenKind.Word when token.Value == "tree":
                reader.Next();
                return TreeTarget(TakeString(reader, "tree path"));
            case TokenKind.Word:
                reader.Next();
                return IdTarget(token.Value);
            case TokenKind.NameString:
                reader.Next();
                if (string.IsNullOrWhiteSpace(token.Value))
                {
                    throw new SyntaxException("name:\"...\" must not be empty.");
                }

                return ElementAddress.FromName(token.Value);
            default:
                throw new SyntaxException(
                    $"a bare string \"{token.Value}\" is not a target; use an AutomationId, tree \"path\", or name:\"...\".");
        }
    }

    private static ElementAddress IdTarget(string word)
    {
        if (!BareIdShape().IsMatch(word))
        {
            throw new SyntaxException($"'{word}' is not a valid AutomationId (letters, digits, and underscores only).");
        }

        return ElementAddress.FromAutomationId(word);
    }

    private static ElementAddress TreeTarget(string path)
    {
        ValidateTreePath(path);
        return ElementAddress.FromTreePath(path);
    }

    private static void ValidateTreePath(string path)
    {
        try
        {
            _ = ElementAddress.FromTreePath(path); // TreePathParser validates structure eagerly
        }
        catch (ArgumentException ex)
        {
            throw new SyntaxException(ex.Message);
        }
    }

    /// <summary>Slash-separated menu/flyout paths must have no blank segments.</summary>
    private static string ValidatedPath(string path, string what)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new SyntaxException($"the {what} must not be empty.");
        }

        if (path.Split('/').Any(string.IsNullOrWhiteSpace))
        {
            throw new SyntaxException($"the {what} '{path}' contains an empty segment.");
        }

        return path;
    }
}
