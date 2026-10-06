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
    ///     Elements whose labels menu text-path segments resolve against: top-level menu
    ///     buttons (AppBarButton carries Label) and flyout items (Text, plus the explicit
    ///     AutomationProperties.Name that runtime Name addressing actually matches).
    /// </summary>
    private static readonly string[] MenuElementNames = { "AppBarButton", "MenuFlyoutItem", "MenuFlyoutSubItem" };

    /// <summary>Every AutomationProperties.AutomationId value in the convention-scope XAML.</summary>
    public required IReadOnlySet<string> AutomationIds { get; init; }

    /// <summary>Labels a menu text-path segment may name, normalized (trimmed, shortcut column stripped).</summary>
    public required IReadOnlySet<string> MenuLabels { get; init; }

    /// <summary>TabViewItem header labels the tab text form may name.</summary>
    public required IReadOnlySet<string> TabLabels { get; init; }

    /// <summary>
    ///     Menu item id to the ids clicked to reach it (owning button first). The runner uses it
    ///     so `menu` opens the right menu directly; empty when facts are built from literals.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> MenuOpeners { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

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

        var menuLabels = new HashSet<string>(StringComparer.Ordinal);
        var tabLabels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in AutomationXamlScan.LabelOccurrences())
        {
            if (MenuElementNames.Contains(label.ElementName))
            {
                menuLabels.Add(Normalize(label.Value));
            }
            else if (label.ElementName == "TabViewItem")
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
            AutomationIds = ids, MenuLabels = menuLabels, TabLabels = tabLabels, MenuOpeners = menuOpeners,
        };
    }

    /// <summary>Trims and drops everything from the first 2+-space run (the shortcut column).</summary>
    internal static string Normalize(string label)
        => ShortcutColumnGap().Split(label.Trim())[0].Trim();
}
