#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Manual;
using StoryCADAutomation.Scripting;

namespace StoryCADTests.Automation;

/// <summary>
///     The `labels` command (#1422 D4): bold text in a manual page that matches no XAML label
///     is listed with its line.
/// </summary>
[TestClass]
public class LabelCheckTests
{
    private static readonly LabelCheck Check = new(new XamlUiFacts
    {
        AutomationIds = new HashSet<string>(),
        MenuPaths = new HashSet<string>(),
        TabLabels = new HashSet<string> { "Role" },
        AllLabels = new HashSet<string> { "Cancel", "Save Story", "View", "Toggle Navigation Pane", "Quit" },
    });

    [TestMethod]
    public void BoldPhrases_WithTwoOnOneLine_ReturnsBothWithLine()
    {
        var phrases = LabelCheck.BoldPhrases("Intro\nClick **Save Story**, then **Cancel**.").ToList();

        Assert.AreEqual(2, phrases.Count);
        Assert.AreEqual(new BoldPhrase(2, "Save Story"), phrases[0]);
        Assert.AreEqual(new BoldPhrase(2, "Cancel"), phrases[1]);
    }

    [TestMethod]
    public void BoldPhrases_InsideFencedCode_AreSkipped()
    {
        Assert.AreEqual(0, LabelCheck.BoldPhrases("```\n**Cancel**\n```\n").Count());
    }

    [TestMethod]
    [DataRow("Click **Cancel**.")]
    [DataRow("Click **cancel**.")]
    [DataRow("Click **Save Story...**.")]
    [DataRow("Open the **Role** tab.")]
    [DataRow("Use **View > Toggle Navigation Pane**.")]
    [DataRow("**Note:** labels are case-insensitive.")]
    public void Misses_WithKnownLabel_ReturnsNone(string markdown)
    {
        Assert.AreEqual(0, Check.Misses(markdown).Count);
    }

    [TestMethod]
    public void Misses_WithRenamedLabel_ReturnsItWithLine()
    {
        var misses = Check.Misses("# File menu\n\nSelect **Exit** to close StoryCAD.");

        Assert.AreEqual(1, misses.Count);
        Assert.AreEqual(new BoldPhrase(3, "Exit"), misses[0]);
    }

    [TestMethod]
    public void Misses_WithMenuPathWhosePartIsUnknown_ReturnsIt()
    {
        Assert.AreEqual(1, Check.Misses("**View > Hide Pane**").Count);
    }
}
#endif
