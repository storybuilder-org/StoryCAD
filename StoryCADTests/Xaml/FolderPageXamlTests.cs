using static StoryCADXamlScan.AutomationXamlScan;

namespace StoryCADTests.Xaml;

/// <summary>
///     Checks the screen-reader name of the tabbed Notes box on FolderPage (#1452).
///     A unit test cannot make the page, so this test reads the XAML as XML.
/// </summary>
[TestClass]
public class FolderPageXamlTests
{
    [TestMethod]
    public void NotesTabRichEdit_InFolderPage_HasNameNotes()
    {
        var elements = LoadXaml("StoryCAD/Views/FolderPage.xaml")
            .Descendants()
            .Where(e => GetAttributeValue(e, AutomationIdAttribute) == "FolderNotesTabRichEdit")
            .ToList();

        Assert.AreEqual(1, elements.Count, "Expected one element with AutomationId FolderNotesTabRichEdit.");
        Assert.AreEqual("Notes", GetAttributeValue(elements[0], "AutomationProperties.Name"));
    }
}
