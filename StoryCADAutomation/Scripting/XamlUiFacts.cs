using System.Text.RegularExpressions;
using StoryCADXamlScan;

namespace StoryCADAutomation.Scripting;

/// <summary>
///     The static XAML facts the linter checks scripts against. Held as plain sets so the
///     linter is constructible from literals in tests; <see cref="LoadFromXamlScan" /> is the
///     production path.
/// </summary>
public sealed partial class XamlUiFacts
{
    /// <summary>
    ///     Shell.xaml's menu texts carry a right-aligned shortcut column separated from the
    ///     label by a run of spaces ("Save Story                      Ctrl+S"); a script path
    ///     segment names only the label.
    /// </summary>
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ShortcutColumnGap();

    /// <summary>
    ///     Joins a menu path's labels into one <see cref="MenuPaths" /> key. A label can contain
    ///     '/' ("Open/Create file"), so '/' cannot be the separator.
    /// </summary>
    private const char MenuPathSeparator = '\n';

    /// <summary>Every AutomationProperties.AutomationId value in the convention-scope XAML.</summary>
    public required IReadOnlySet<string> AutomationIds { get; init; }

    /// <summary>
    ///     Whole menu paths the text-path form of `menu` can walk on Windows, as
    ///     <see cref="MenuPathKey" /> keys of normalized labels (#1421 review M3).
    /// </summary>
    public required IReadOnlySet<string> MenuPaths { get; init; }

    /// <summary>TabViewItem header labels the tab text form may name.</summary>
    public required IReadOnlySet<string> TabLabels { get; init; }

    /// <summary>
    ///     Menu item id to the ids clicked to reach it (owning button first). The runner uses it
    ///     so `menu` opens the right menu directly; empty when facts are built from literals.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> MenuOpeners { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    ///     Every static label in the XAML (Text, Header, Label, Content, AutomationProperties.Name,
    ///     all platform variants), normalized. The manual label check matches bold text against it.
    /// </summary>
    public IReadOnlySet<string> AllLabels { get; init; } = new HashSet<string>();

    /// <summary>
    ///     Loads the facts through the shared scan library (one scan for convention tests and
    ///     lint; a second copy would drift — design, Script lint). The scan locates the repo by
    ///     walking up from the executing assembly's directory to StoryCAD.sln, so `check` only
    ///     works from a checkout of the StoryCAD repo; outside one this throws the scan's
    ///     InvalidOperationException. That is by design: lint is a repo-side fitness function
    ///     (CI runs it per PR), while `run` never needs these facts.
    /// </summary>
    public static XamlUiFacts LoadFromXamlScan()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var occurrence in AutomationXamlScan.AutomationIdOccurrences())
        {
            ids.Add(occurrence.Id);
        }

        var menuPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in AutomationXamlScan.MenuLabelPaths())
        {
            menuPaths.Add(MenuPathKey(path.Labels));
        }

        var tabLabels = new HashSet<string>(StringComparer.Ordinal);
        var allLabels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in AutomationXamlScan.LabelOccurrences())
        {
            allLabels.Add(Normalize(label.Value));
            if (label.ElementName == "TabViewItem")
            {
                tabLabels.Add(Normalize(label.Value));
            }
        }

        var menuOpeners = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var path in AutomationXamlScan.MenuItemPaths())
        {
            menuOpeners.TryAdd(path.Id, path.Openers);
        }

        return new XamlUiFacts
        {
            AutomationIds = ids, MenuPaths = menuPaths, TabLabels = tabLabels, MenuOpeners = menuOpeners,
            AllLabels = allLabels,
        };
    }

    /// <summary>The <see cref="MenuPaths" /> key for a path's labels, each normalized.</summary>
    public static string MenuPathKey(IEnumerable<string> labels)
        => string.Join(MenuPathSeparator, labels.Select(Normalize));

    /// <summary>The labels of a <see cref="MenuPaths" /> key.</summary>
    internal static string[] MenuPathLabels(string key) => key.Split(MenuPathSeparator);

    /// <summary>Trims and drops everything from the first 2+-space run (the shortcut column).</summary>
    internal static string Normalize(string label)
        => ShortcutColumnGap().Split(label.Trim())[0].Trim();
}
