using System.Text.RegularExpressions;

namespace StoryCADAutomation.Driver;

/// <summary>
///     The three address forms from the design's "Element addressing" section
///     (devdocs/issue_1421_dsl_design.md).
/// </summary>
public enum ElementAddressKind
{
    /// <summary>Bare AutomationId from the #1420 inventory; the default form.</summary>
    AutomationId,

    /// <summary>
    ///     Node-name path from the outline root, e.g. "Hamlet/Problems/Hamlet vs. Claudius",
    ///     resolved under the runtime tree hosts (Tree controls with id "ListControl").
    ///     "[n]" suffix disambiguates same-name siblings, 1-based.
    /// </summary>
    TreePath,

    /// <summary>UIA Name fallback, needed for native dialogs that carry no AutomationId.</summary>
    Name,
}

/// <summary>
///     A target address the interpreter hands the driver. Immutable; no UIA types
///     (the macOS-seam rule: verbs and addresses name intent, never UIA mechanics).
/// </summary>
public sealed class ElementAddress
{
    private ElementAddress(ElementAddressKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Which address form this is.</summary>
    public ElementAddressKind Kind { get; }

    /// <summary>The id, name, or raw tree path.</summary>
    public string Value { get; }

    /// <summary>Address by AutomationId (the default script form).</summary>
    public static ElementAddress FromAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return new ElementAddress(ElementAddressKind.AutomationId, automationId);
    }

    /// <summary>Address a tree row by node-name path, e.g. "Hamlet/Characters/Hamlet[2]".</summary>
    public static ElementAddress FromTreePath(string path)
    {
        TreePathParser.Parse(path); // validate eagerly so a malformed path fails at address creation
        return new ElementAddress(ElementAddressKind.TreePath, path);
    }

    /// <summary>Address by UIA Name (native-dialog fallback; the linter discourages it elsewhere).</summary>
    public static ElementAddress FromName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ElementAddress(ElementAddressKind.Name, name);
    }

    /// <summary>Script-shaped rendering, used in failure messages.</summary>
    public override string ToString() => Kind switch
    {
        ElementAddressKind.TreePath => $"tree \"{Value}\"",
        ElementAddressKind.Name => $"name:\"{Value}\"",
        _ => Value,
    };
}

/// <summary>One tree-path segment: node name plus 1-based index among same-name siblings.</summary>
internal readonly record struct TreePathSegment(string Name, int Index)
{
    public override string ToString() => Index == 1 ? Name : $"{Name}[{Index}]";
}

/// <summary>
///     Parses "Hamlet/Characters/Hamlet[2]" into segments. Node names are runtime outline
///     data, so only structure is validated here; whether a path resolves is a live question.
/// </summary>
internal static partial class TreePathParser
{
    [GeneratedRegex(@"^(?<name>.+?)\[(?<index>\d+)\]$")]
    private static partial Regex IndexSuffix();

    public static IReadOnlyList<TreePathSegment> Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var segments = new List<TreePathSegment>();
        foreach (var raw in path.Split('/'))
        {
            var text = raw.Trim();
            if (text.Length == 0)
            {
                throw new ArgumentException($"Tree path '{path}' contains an empty segment.", nameof(path));
            }

            var match = IndexSuffix().Match(text);
            if (match.Success)
            {
                var index = int.Parse(match.Groups["index"].Value);
                if (index < 1)
                {
                    throw new ArgumentException($"Tree path index in '{text}' must be 1-based.", nameof(path));
                }

                segments.Add(new TreePathSegment(match.Groups["name"].Value, index));
            }
            else
            {
                segments.Add(new TreePathSegment(text, 1));
            }
        }

        return segments;
    }
}
