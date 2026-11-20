using System.Xml.Linq;
using static StoryCADXamlScan.AutomationXamlScan;

namespace StoryCADTests.Xaml;

/// <summary>
///     Checks the attributes that each RichEditBoxExtended element in XAML must set (#1675).
///     A unit test cannot make the control, so these tests read the XAML as XML.
///     They replace the three ignored tests of RichEditBoxExtendedTests.
/// </summary>
[TestClass]
public class RichEditBoxExtendedXamlTests
{
    [TestMethod]
    public void RichEditBoxExtended_InAllXaml_SetsTextWrappingWrap()
    {
        var elements = RichEditBoxElements().ToList();
        var violations = elements
            .Where(e => GetAttributeValue(e.Element, "TextWrapping") != "Wrap")
            .Select(e => $"{e.File}:{LineOf(e.Element)} does not set TextWrapping=\"Wrap\"")
            .ToList();

        Assert.IsTrue(elements.Count > 0, "No RichEditBoxExtended element found in the XAML.");
        Assert.IsTrue(violations.Count == 0, string.Join("\n", violations));
    }

    [TestMethod]
    public void RichEditBoxExtended_WhenEditable_SetsAcceptsReturn()
    {
        var elements = RichEditBoxElements()
            .Where(e => GetAttributeValue(e.Element, "IsReadOnly") != "True")
            .ToList();
        var violations = elements
            .Where(e => GetAttributeValue(e.Element, "AcceptsReturn") is not ("True" or "False"))
            .Select(e => $"{e.File}:{LineOf(e.Element)} does not set AcceptsReturn")
            .ToList();

        Assert.IsTrue(elements.Count > 0, "No editable RichEditBoxExtended element found in the XAML.");
        Assert.IsTrue(violations.Count == 0, string.Join("\n", violations));
    }

    private static IEnumerable<(string File, XElement Element)> RichEditBoxElements() =>
        ScopeFiles()
            .SelectMany(file => LoadXaml(file).Descendants()
                .Where(e => e.Name.LocalName == "RichEditBoxExtended")
                .Select(e => (file, e)));
}
