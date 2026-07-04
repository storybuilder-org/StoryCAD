using System.Xml;
using System.Xml.Linq;

namespace StoryCADTests.Xaml;

/// <summary>
///     Static XAML-scanning fitness function for issue #1420 (AutomationProperties annotation pass).
///     Parses the convention-scope XAML files as plain XML (no UI, no [UITestMethod]) and checks
///     them against devdocs/automation_naming_convention.md. See devdocs/issue_1420_implementation_plan.md
///     for the TDD cycle these tests drive.
/// </summary>
[TestClass]
public class AutomationConventionTests
{
    /// <summary>
    ///     Files whose interactive controls must be fully annotated. Grows by one batch per unit
    ///     (see the implementation plan's Units table) until Unit 9 replaces this with "all scope
    ///     files", at which point <see cref="Coverage_AnnotatedFiles_EveryInteractiveControlHasAutomationId"/>
    ///     becomes the permanent fitness function.
    /// </summary>
    private static readonly string[] AnnotatedFiles =
    {
        "StoryCAD/Views/Shell.xaml",
        "StoryCAD/Views/OverviewPage.xaml",
        "StoryCAD/Views/ProblemPage.xaml",
        "StoryCAD/Views/CharacterPage.xaml",
        "StoryCAD/Views/ScenePage.xaml",
        "StoryCAD/Views/SettingPage.xaml",
        "StoryCAD/Views/StoryWorldPage.xaml",
    };

    /// <summary>
    ///     Directories that make up the full convention scope (devdocs/automation_naming_convention.md
    ///     "Scope" section). Scanned recursively at test time so newly added XAML is caught automatically
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
    private static readonly HashSet<string> InteractiveElementNames = new()
    {
        "Button", "AppBarButton", "HyperlinkButton", "MenuFlyoutItem", "MenuFlyoutSubItem",
        "ComboBox", "TextBox", "CheckBox", "RadioButton", "RadioButtons", "ToggleSwitch", "NumberBox",
        "AutoSuggestBox", "TabView", "TabViewItem", "TreeView", "ListView", "GridView",
        "Flyout", "RichEditBoxExtended", "BrowseTextBox",
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
    private static readonly Dictionary<string, string> SuffixByElementName = new()
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
    };

    private const string AutomationIdAttribute = "AutomationProperties.AutomationId";

    private static string? _repoRoot;

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

            Assert.IsNotNull(dir, $"Could not locate StoryCAD.sln by walking up from {AppContext.BaseDirectory}");
            _repoRoot = dir!.FullName;
            return _repoRoot;
        }
    }

    /// <summary>Enumerates every XAML file under the convention scope directories, relative to the repo root.</summary>
    private static IEnumerable<string> ScopeFiles()
    {
        foreach (var relDir in ScopeDirectories)
        {
            var absDir = Path.Combine(RepoRoot, relDir.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(Directory.Exists(absDir), $"Convention scope directory not found: {absDir}");

            foreach (var file in Directory.EnumerateFiles(absDir, "*.xaml", SearchOption.AllDirectories))
            {
                yield return Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            }
        }
    }

    private static XDocument LoadXaml(string relativePath)
    {
        var absPath = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(absPath), $"XAML file not found: {absPath}");
        return XDocument.Load(absPath, LoadOptions.SetLineInfo);
    }

    private static bool IsInsideDataTemplate(XElement element) =>
        element.Ancestors().Any(a => a.Name.LocalName == "DataTemplate");

    /// <summary>
    ///     Returns the value of an *unconditional* attribute (no namespace prefix). A namespaced
    ///     variant such as win:AutomationProperties.AutomationId deliberately does not match:
    ///     the convention requires AutomationProperties attributes to be unconditional (ADR-001),
    ///     and <see cref="Unconditional_AllFiles_NoNamespacedAutomationProperties"/> fails any
    ///     namespaced AutomationProperties.* attribute outright.
    /// </summary>
    private static string? GetAttributeValue(XElement element, string attributeLocalName) =>
        element.Attributes()
            .FirstOrDefault(a => a.Name.Namespace == XNamespace.None && a.Name.LocalName == attributeLocalName)
            ?.Value;

    private static int LineOf(XElement element) => ((IXmlLineInfo)element).LineNumber;

    /// <summary>
    ///     Every interactive element outside a DataTemplate, in every file listed in
    ///     <see cref="AnnotatedFiles"/>, must carry a non-empty AutomationId.
    /// </summary>
    [TestMethod]
    public void Coverage_AnnotatedFiles_EveryInteractiveControlHasAutomationId()
    {
        var violations = new List<string>();

        foreach (var relPath in AnnotatedFiles)
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                if (!InteractiveElementNames.Contains(element.Name.LocalName))
                {
                    continue;
                }

                if (IsInsideDataTemplate(element))
                {
                    continue;
                }

                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (string.IsNullOrEmpty(id))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> is missing AutomationProperties.AutomationId");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} interactive control(s) missing AutomationId:\n{string.Join("\n", violations)}");
    }

    /// <summary>No element with a DataTemplate ancestor may carry an AutomationId, in any scope file.</summary>
    [TestMethod]
    public void TemplateSafety_AllFiles_NoAutomationIdInsideDataTemplate()
    {
        var violations = new List<string>();

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

                if (IsInsideDataTemplate(element))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> AutomationId=\"{id}\" is inside a DataTemplate");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationId(s) found inside a DataTemplate:\n{string.Join("\n", violations)}");
    }

    /// <summary>Every AutomationId, anywhere in scope, must end with the suffix mapped to its element type.</summary>
    [TestMethod]
    public void Suffix_AllFiles_AutomationIdEndsWithRoleSuffix()
    {
        var violations = new List<string>();

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

                var localName = element.Name.LocalName;
                if (!SuffixByElementName.TryGetValue(localName, out var suffix))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{localName}> AutomationId=\"{id}\" has no suffix mapping in the convention table (propose one)");
                    continue;
                }

                if (!id.EndsWith(suffix, StringComparison.Ordinal))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{localName}> AutomationId=\"{id}\" does not end with required suffix \"{suffix}\"");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationId(s) with wrong or missing suffix:\n{string.Join("\n", violations)}");
    }

    /// <summary>Every AutomationId value, anywhere in scope, must be globally unique.</summary>
    [TestMethod]
    public void Uniqueness_AllFiles_AutomationIdsGloballyUnique()
    {
        var occurrences = new Dictionary<string, List<string>>(StringComparer.Ordinal);

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

                if (!occurrences.TryGetValue(id, out var locations))
                {
                    locations = new List<string>();
                    occurrences[id] = locations;
                }

                locations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}>");
            }
        }

        var duplicates = occurrences.Where(kv => kv.Value.Count > 1).ToList();
        var violations = duplicates
            .Select(kv => $"\"{kv.Key}\" used {kv.Value.Count} times: {string.Join(", ", kv.Value)}")
            .ToList();

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} duplicate AutomationId value(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     Every AutomationId value, anywhere in scope, must be a non-empty literal ASCII
    ///     letters/digits string (no bindings). A present-but-empty attribute is a violation,
    ///     not a skip.
    /// </summary>
    [TestMethod]
    public void Literalness_AllFiles_AutomationIdIsLiteral()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (id == null)
                {
                    continue; // attribute absent; coverage test owns that case
                }

                if (id.Length == 0)
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> has an empty AutomationProperties.AutomationId");
                    continue;
                }

                if (id.Contains('{') || !id.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> AutomationId=\"{id}\" is not a literal ASCII letters/digits string");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} non-literal or empty AutomationId value(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     AutomationProperties attributes must be unconditional (ADR-001): no win:/skia:/other
    ///     namespace prefix on any AutomationProperties.* attribute in any scope file. A prefixed
    ///     attribute would give Windows and macOS different identifiers and slip past the
    ///     namespace-filtered lookup the other tests use.
    /// </summary>
    [TestMethod]
    public void Unconditional_AllFiles_NoNamespacedAutomationProperties()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (attribute.Name.LocalName.StartsWith("AutomationProperties.", StringComparison.Ordinal)
                        && attribute.Name.Namespace != XNamespace.None)
                    {
                        violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> {attribute.Name.LocalName} carries namespace \"{attribute.Name.NamespaceName}\" (must be unconditional)");
                    }
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} namespaced AutomationProperties attribute(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     No Style Setter may target an AutomationProperties.* property, in any scope file,
    ///     whatever the Value. The Windows Runtime does not evaluate bindings in Setter.Value
    ///     (Setter class docs; unoplatform/uno#4826), so a bound Name silently never gets set;
    ///     a literal Value would name every realized item identically. Both are always wrong:
    ///     bind Name inline on the template element instead.
    /// </summary>
    [TestMethod]
    public void SetterSafety_AllFiles_NoAutomationPropertiesInStyleSetters()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                if (element.Name.LocalName != "Setter")
                {
                    continue;
                }

                var property = element.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "Property")
                    ?.Value;
                if (property != null && property.StartsWith("AutomationProperties.", StringComparison.Ordinal))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <Setter Property=\"{property}\"> sets AutomationProperties via a Style Setter (bindings in Setter.Value are never evaluated; a literal names every item identically)");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationProperties Style Setter(s):\n{string.Join("\n", violations)}");
    }
}
