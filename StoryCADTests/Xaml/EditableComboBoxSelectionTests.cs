using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace StoryCADTests.Xaml;

/// <summary>
///     Regression guard for issue #1551. Setting Text on an editable ComboBox never moves its
///     selection. When the bound value goes empty, the control restores its selected item's text
///     through the two-way Text binding, and the previous element's value lands on the new one.
///     Every editable ComboBox with an x:Bind on Text must also carry a one-way SelectedItem
///     binding to the same property, so the selection follows the value and there is no stale
///     item to restore. Static XAML scan, no UI, same approach as <see cref="AutomationConventionTests"/>.
/// </summary>
[TestClass]
public class EditableComboBoxSelectionTests
{
    /// <summary>Same scope as the AutomationId convention: every XAML file that can hold an editable ComboBox.</summary>
    private static readonly string[] ScopeDirectories =
    {
        "StoryCAD/Views",
        "StoryCADLib/Controls",
        "StoryCADLib/Services/Dialogs",
        "StoryCADLib/Collaborator/Views",
    };

    /// <summary>
    ///     Parses "{x:Bind Path, Mode=X, ...}". Group 1 is the path, group 2 the mode when given.
    ///     Tolerates the stray spaces some existing bindings carry ("{x:Bind  Vm.X", "Mode =TwoWay").
    /// </summary>
    private static readonly Regex XBind = new(
        @"^\{x:Bind\s+([A-Za-z0-9_.]+)\s*(?:,\s*Mode\s*=\s*(\w+))?(?:\s*,[^}]*)?\}$",
        RegexOptions.Compiled);

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

    private static IEnumerable<string> ScopeFiles()
    {
        foreach (var relDir in ScopeDirectories)
        {
            var absDir = Path.Combine(RepoRoot, relDir.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(Directory.Exists(absDir), $"Scope directory not found: {absDir}");

            foreach (var file in Directory.EnumerateFiles(absDir, "*.xaml", SearchOption.AllDirectories))
            {
                yield return Path.GetRelativePath(RepoRoot, file).Replace('\\', '/');
            }
        }
    }

    private static string? GetAttributeValue(XElement element, string attributeLocalName) =>
        element.Attributes()
            .FirstOrDefault(a => a.Name.Namespace == XNamespace.None && a.Name.LocalName == attributeLocalName)
            ?.Value;

    private static int LineOf(XElement element) => ((IXmlLineInfo)element).LineNumber;

    [TestMethod]
    public void EditableComboBox_WithTextBinding_HasOneWaySelectedItemBindingToSameProperty()
    {
        var violations = new List<string>();
        var checkedCount = 0;

        foreach (var relPath in ScopeFiles())
        {
            var absPath = Path.Combine(RepoRoot, relPath.Replace('/', Path.DirectorySeparatorChar));
            var doc = XDocument.Load(absPath, LoadOptions.SetLineInfo);

            foreach (var element in doc.Descendants())
            {
                if (element.Name.LocalName != "ComboBox" || GetAttributeValue(element, "IsEditable") != "True")
                {
                    continue;
                }

                var text = GetAttributeValue(element, "Text");
                var textMatch = text == null ? null : XBind.Match(text);
                if (textMatch is not { Success: true })
                {
                    continue; // no x:Bind on Text: the #1551 write-back path does not exist
                }

                checkedCount++;
                var path = textMatch.Groups[1].Value;
                var where = $"{relPath}:{LineOf(element)} <ComboBox> Text=\"{text}\"";

                var selected = GetAttributeValue(element, "SelectedItem");
                var selectedMatch = selected == null ? null : XBind.Match(selected);
                if (selectedMatch is not { Success: true })
                {
                    violations.Add($"{where} has no SelectedItem=\"{{x:Bind {path}, Mode=OneWay}}\"");
                    continue;
                }

                if (selectedMatch.Groups[1].Value != path)
                {
                    violations.Add($"{where} SelectedItem binds {selectedMatch.Groups[1].Value}, not {path}");
                }

                if (selectedMatch.Groups[2].Value != "OneWay")
                {
                    violations.Add($"{where} SelectedItem must be Mode=OneWay (two-way would write null into the ViewModel when the selection clears)");
                }
            }
        }

        Assert.IsTrue(checkedCount > 0, "No editable ComboBox with an x:Bind on Text was found; the scan is not looking at the pages");
        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} editable ComboBox(es) whose selection will not follow the bound value (#1551):\n{string.Join("\n", violations)}");
    }
}
