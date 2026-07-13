using System.Xml;
using System.Xml.Linq;

namespace StoryCADXamlScan;

/// <summary>
///     Static XAML scan of the AutomationProperties annotation convention
///     (devdocs/automation_naming_convention.md). Parses the convention-scope XAML files as
///     plain XML: no UI, no UIA, no test framework. Extracted from StoryCADTests'
///     AutomationConventionTests for issue #1421 so the convention tests and the
///     StoryCADAutomation script linter share one scan; a second copy would drift
///     (devdocs/issue_1421_dsl_design.md "Script lint as a fitness function").
/// </summary>
public static class AutomationXamlScan
{
    /// <summary>
    ///     Directories that make up the full convention scope (devdocs/automation_naming_convention.md
    ///     "Scope" section). Scanned recursively at scan time so newly added XAML is caught automatically
    ///     rather than by re-surveying; StoryCADLib/Services/Dialogs recursion also covers its Tools subfolder.
    /// </summary>
    private static readonly string[] ScopeDirectories =
    {
        "StoryCAD/Views",
        "StoryCADLib/Controls",
        "StoryCADLib/Services/Dialogs",
        "StoryCADLib/Collaborator/Views",
    };

    /// <summary>Local (namespace-ignored) element names that require an AutomationId when outside a DataTemplate.</summary>
    public static readonly IReadOnlySet<string> InteractiveElementNames = new HashSet<string>
    {
        "Button", "AppBarButton", "HyperlinkButton", "MenuFlyoutItem", "MenuFlyoutSubItem",
        "ComboBox", "TextBox", "CheckBox", "RadioButton", "RadioButtons", "ToggleSwitch", "NumberBox",
        "AutoSuggestBox", "TabView", "TabViewItem", "TreeView", "TreeViewItem", "ListView", "GridView",
        "Flyout", "RichEditBoxExtended", "BrowseTextBox", "Expander",
        "NavigationView", "NavigationViewItem", "InfoBar",
    };

    /// <summary>
    ///     AutomationId suffix required per element local name, per the convention's suffix table.
    ///     Four rows were added in Unit 1 (marked "(added Unit 1)" in the convention doc):
    ///     "ItemsRepeater" covers Shell's NavigationTree root-node container (an ItemsRepeater
    ///     standing in for a TreeView because the real nested TreeView is templated);
    ///     "BrowseTextBox", "RadioButton", and "ToggleSwitch" fill gaps in the convention's
    ///     original suffix table (the test spec's interactive-element list includes all three,
    ///     but the original table did not). "RadioButtons" (the WinUI group control) was added
    ///     in Unit 2 for ProblemPage's Elements source selector.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> SuffixByElementName = new Dictionary<string, string>
    {
        ["Button"] = "Button",
        ["AppBarButton"] = "Button",
        ["HyperlinkButton"] = "Button",
        ["MenuFlyoutItem"] = "MenuItem",
        ["MenuFlyoutSubItem"] = "MenuItem",
        ["ComboBox"] = "Combo",
        ["TextBox"] = "TextBox",
        ["RichEditBoxExtended"] = "RichEdit",
        ["CheckBox"] = "Check",
        ["RadioButton"] = "Radio",   // added Unit 1: gap in original convention suffix table, see class remarks
        ["RadioButtons"] = "Radios", // added Unit 2: RadioButtons group is an items host generating focusable children
        ["ToggleSwitch"] = "Toggle", // added Unit 1: gap in original convention suffix table, see class remarks
        ["NumberBox"] = "NumberBox",
        ["AutoSuggestBox"] = "SearchBox",
        ["TabViewItem"] = "Tab",
        ["TabView"] = "Tabs",
        ["TreeView"] = "Tree",
        ["ListView"] = "List",
        ["GridView"] = "GridView",
        ["Flyout"] = "Flyout",
        ["BrowseTextBox"] = "TextBox", // added Unit 1: gap in original convention suffix table, see class remarks
        ["ItemsRepeater"] = "Tree",    // added Unit 1, see class remarks
        ["Expander"] = "Expander",     // added Unit 4: founder-accepted Expander suffix ruling on PR #1451
        ["NavigationView"] = "Nav",         // added Unit 6: founder ruling on PR #1455 (FileOpenMenu nav strip)
        ["NavigationViewItem"] = "NavItem", // added Unit 6: founder ruling on PR #1455 (FileOpenMenu nav strip)
        ["InfoBar"] = "InfoBar",            // added Unit 7: founder ruling on PR #1456 (id only; the peer announces Severity+Title+Message natively, so no explicit Name)
        ["TreeViewItem"] = "TreeItem",      // added Unit 8: founder ruling on PR #1457; all current instances are templated (Shell x3, NarrativeTool x2), so they carry a bound Name and no AutomationId (TemplateSafety); the suffix row exists for any future non-templated TreeViewItem
    };

    public const string AutomationIdAttribute = "AutomationProperties.AutomationId";

    private static string? _repoRoot;

    /// <summary>Repo root, located by walking up from the executing assembly's base directory to StoryCAD.sln.</summary>
    private static string RepoRoot
    {
        get
        {
            if (_repoRoot != null)
            {
                return _repoRoot;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StoryCAD.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException($"Could not locate StoryCAD.sln by walking up from {AppContext.BaseDirectory}");
            }

            _repoRoot = dir.FullName;
            return _repoRoot;
        }
    }

    /// <summary>Enumerates every XAML file under the convention scope directories, relative to the repo root.</summary>
    public static IEnumerable<string> ScopeFiles()
    {
        foreach (var relDir in ScopeDirectories)
        {
            var absDir = Path.Combine(RepoRoot, relDir.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absDir))
            {
                throw new DirectoryNotFoundException($"Convention scope directory not found: {absDir}");
            }

            // "*" plus a suffix filter, not a "*.xaml" pattern: EnumerateFiles pattern matching
            // is case-sensitive on Unix hosts, and this scan must stay correct for the macOS seam.
            foreach (var file in Directory.EnumerateFiles(absDir, "*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            }
        }
    }

    /// <summary>Loads a scope XAML file (repo-root-relative path) as XML with line info.</summary>
    public static XDocument LoadXaml(string relativePath)
    {
        var absPath = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absPath))
        {
            throw new FileNotFoundException($"XAML file not found: {absPath}", absPath);
        }

        return XDocument.Load(absPath, LoadOptions.SetLineInfo);
    }

    public static bool IsInsideDataTemplate(XElement element) =>
        element.Ancestors().Any(a => a.Name.LocalName == "DataTemplate");

    /// <summary>
    ///     Returns the value of an *unconditional* attribute (no namespace prefix). A namespaced
    ///     variant such as win:AutomationProperties.AutomationId deliberately does not match:
    ///     the convention requires AutomationProperties attributes to be unconditional (ADR-001),
    ///     and StoryCADTests' Unconditional_AllFiles_NoNamespacedAutomationProperties test fails
    ///     any namespaced AutomationProperties.* attribute outright.
    /// </summary>
    public static string? GetAttributeValue(XElement element, string attributeLocalName) =>
        element.Attributes()
            .FirstOrDefault(a => a.Name.Namespace == XNamespace.None && a.Name.LocalName == attributeLocalName)
            ?.Value;

    public static int LineOf(XElement element) => ((IXmlLineInfo)element).LineNumber;

    /// <summary>
    ///     Every AutomationId occurrence (non-empty value) across all scope files. Consumed by the
    ///     Uniqueness convention test and by the script linter's bare-id validation
    ///     (devdocs/issue_1421_dsl_design.md "Script lint as a fitness function").
    /// </summary>
    public static IEnumerable<AutomationIdOccurrence> AutomationIdOccurrences()
    {
        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                yield return new AutomationIdOccurrence(relPath, LineOf(element), element.Name.LocalName, id);
            }
        }
    }

    /// <summary>
    ///     Attribute local names that carry a user-visible label. Label covers AppBarButton
    ///     (top-level menu buttons carry Label, not Text); AutomationProperties.Name is included
    ///     because it overrides the UIA Name that runtime text-path addressing matches against
    ///     (Shell's padded menu texts pair with clean explicit Names).
    /// </summary>
    private static readonly string[] LabelAttributeNames =
    {
        "Text", "Header", "Label", "AutomationProperties.Name",
    };

    /// <summary>
    ///     Every user-visible label attribute across all scope files, for the script linter's
    ///     menu/tab text-path checks (devdocs/issue_1421_dsl_design.md "Script lint as a fitness
    ///     function"). Unlike <see cref="GetAttributeValue" />, conditional variants (win:Text,
    ///     skia:Text) ARE included: they are the per-platform runtime labels, and a text-path
    ///     segment that matches either platform's label is valid. Values that are bindings or
    ///     markup extensions ({x:Bind ...}) are skipped as not statically checkable; so are
    ///     labels set via nested property elements rather than attributes.
    /// </summary>
    public static IEnumerable<LabelOccurrence> LabelOccurrences()
    {
        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (!LabelAttributeNames.Contains(attribute.Name.LocalName))
                    {
                        continue;
                    }

                    var value = attribute.Value;
                    if (string.IsNullOrWhiteSpace(value) || value.StartsWith('{'))
                    {
                        continue;
                    }

                    yield return new LabelOccurrence(
                        relPath, LineOf(element), element.Name.LocalName, attribute.Name.LocalName, value);
                }
            }
        }
    }
}
